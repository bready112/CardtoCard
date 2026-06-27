using UnityEngine;
using System.Collections.Generic;

public class CardStackHandler : MonoBehaviour
{
    [Header("📐 스프레드 오프셋 세팅")]
    public float yOffsetDelta = -0.3f;
    public int baseSortingOrder = 10;

    [Header("🧲 자석 속도")]
    public float magnetSpeed = 12.0f;

    [Header("📋 내 밑으로 수평하게 매달린 최상위 부모1 카드들")]
    // 💥 이제 이 리스트는 자식 알맹이가 아니라, 최상위 '부모1(TheCard)' 게임오브젝트들을 관리합니다!
    public List<GameObject> childStack = new List<GameObject>();

    private const int MaxStackCount = 50;
    private GameObject _myGrandParent; // 나의 최상위 부모1 (TheCard) 캐시

    private void Awake()
    {
        // 내 카드 구조에서 최상위 부모1(TheCard)을 미리 찾아서 기억해 둡니다.
        Transform p2 = transform.parent;
        if (p2 != null && p2.parent != null)
        {
            _myGrandParent = p2.parent.gameObject; // 부모1 포획 성공!
        }
        else
        {
            _myGrandParent = this.gameObject; // 예외 처리용
        }
    }

    private void Update()
    {
        if (childStack.Count == 0) return;

        // 💥 [화면 중앙 뇌절 수정]: 내 최상위 부모1의 위치를 기준점으로 삼아 자식들을 줄 세웁니다!
        Vector3 baseWorldPos = _myGrandParent.transform.position;

        for (int i = 0; i < childStack.Count; i++)
        {
            GameObject childCard = childStack[i];
            if (childCard == null) continue;

            // 해당 카드 위에 정확하게 안착하도록 월드 좌표 기준으로 오프셋 계산!
            Vector3 targetWorldPos = baseWorldPos + new Vector3(0f, (i + 1) * yOffsetDelta, 0f);

            // 월드 좌표 공간에서 부드럽게 자석 보간 (Lerp)
            childCard.transform.position = Vector3.Lerp(
                childCard.transform.position,
                targetWorldPos,
                Time.deltaTime * magnetSpeed
            );
        }
    }

    /// <summary>
    /// 🎯 [드롭 연동 마법]: CardDragHandler 등에서 마우스를 놓을 때 이 함수를 직통 호출해 주면 됩니다!
    /// </summary>
    public void CheckAndTriggerStackOnDrop()
    {
        // 내 최상위 부모1의 현재 월드 위치를 기준으로 주변에 다른 카드가 있는지 그물망 수색합니다.
        Vector3 myPos = _myGrandParent.transform.position;
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(myPos, 0.6f);

        foreach (Collider2D col in hitColliders)
        {
            if (col == null) continue;

            // 나 자신이나 내 부모 계층의 콜라이더는 제외합니다.
            if (col.transform.IsChildOf(_myGrandParent.transform) || col.gameObject == _myGrandParent) continue;

            // 주변에 있는 카드가 CardStackHandler를 들고 있는지 수색합니다.
            CardStackHandler targetStack = col.GetComponent<CardStackHandler>() ?? col.GetComponentInChildren<CardStackHandler>();
            if (targetStack != null)
            {
                // 💥 정답 규칙: 내 알맹이가 아니라, 나의 최상위 '부모1(TheCard)'을 상대방 카드에게 넘겨서 쌓아달라고 요청합니다!
                if (targetStack.TryStackOnMe(_myGrandParent))
                {
                    break; // 한 장의 카드 뭉치에 안착했으므로 레이더 가동 종료
                }
            }
        }
    }

    /// <summary>
    /// 📥 들어온 '부모1' 카드를 50장 제한 규칙 안에서 수평으로 흡수하는 진짜 정석 함수
    /// </summary>
    public bool TryStackOnMe(GameObject incomingGrandParent)
    {
        if (incomingGrandParent == null || incomingGrandParent == _myGrandParent) return false;

        CardStackHandler incomingStack = incomingGrandParent.GetComponentInChildren<CardStackHandler>();
        int incomingCount = (incomingStack != null) ? incomingStack.childStack.Count + 1 : 1;

        if (this.childStack.Count + incomingCount >= MaxStackCount)
        {
            Debug.LogWarning("🚫 [Stack] 50장 제한 초과");
            return false;
        }

        // Yes Box 프리패스 검사
        CardController myController = GetComponent<CardController>() ?? GetComponentInChildren<CardController>();
        CardController incomingController = incomingGrandParent.GetComponentInChildren<CardController>();
        if (myController?.cardData?.yesBox != "1" || incomingController?.cardData?.yesBox != "1") return false;

        // 💥 [부모-부모-자식 방어선]: 들어온 뭉치가 이미 달고 있던 부하 '부모1' 카드들이 있다면
        // 중간 계층 구조를 완전히 격파하고, 나의 최상위 '부모1' 밑으로 평평하게 계층을 편입시킵니다!
        if (incomingStack != null && incomingStack.childStack.Count > 0)
        {
            foreach (GameObject subChildGP in incomingStack.childStack)
            {
                if (subChildGP != null)
                {
                    subChildGP.transform.SetParent(_myGrandParent.transform, true); // 내 부모1 밑으로 수평 교체!
                    childStack.Add(subChildGP);
                }
            }
            incomingStack.childStack.Clear();
        }

        // 들어온 본체 '부모1' 카드도 내 최상위 부모1 밑으로 정직하게 편입
        incomingGrandParent.transform.SetParent(_myGrandParent.transform, true);
        childStack.Add(incomingGrandParent);

        // 레이어 순서 싹 밀어버리기
        ApplyStackSortingOrders();

        Debug.Log($"📥 [수평 스택 완공] {incomingGrandParent.name}이 최상위 부모1 밑으로 안착했습니다. 총 {childStack.Count}장 겹침.");
        return true;
    }

    private void ApplyStackSortingOrders()
    {
        SetOrder(_myGrandParent, baseSortingOrder);
        for (int i = 0; i < childStack.Count; i++)
        {
            if (childStack[i] != null)
            {
                SetOrder(childStack[i], baseSortingOrder + (i + 1));
            }
        }
    }

    private void SetOrder(GameObject obj, int order)
    {
        SpriteRenderer spriteRen = obj.GetComponentInChildren<SpriteRenderer>();
        if (spriteRen != null) spriteRen.sortingOrder = order;

        Canvas canvas = obj.GetComponent<Canvas>() ?? obj.GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
        }
    }
}