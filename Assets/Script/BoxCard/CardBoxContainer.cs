using UnityEngine;
using System.Collections.Generic;

public class CardBoxContainer : MonoBehaviour
{
    [Header("📦 상자 내부 저장소")]
    public string targetCardId = "";      // 현재 상자에 지정된 카드 ID
    public int currentCount = 0;          // 현재 저장된 카드 개수
    public const int MaxCount = 100;      // 최대 100개 저장 제한

    [Header("⏱️ 더블클릭 타이밍 세팅")]
    private float _lastClickTime = 0f;
    private const float DoubleClickTimeLimit = 0.3f;

    // ⚡ [버그 1, 3번 방어막] 한 프레임에 중복으로 흡수되거나 처리되는 것을 막는 안전장치
    private HashSet<int> _processedCardInstanceIds = new HashSet<int>();

    /// <summary>
    /// 문제 2번) 마우스를 누르고 카드를 상자 위에 '올려만 두었을 때'는 절대 먹지 않도록 완전 차단!
    /// </summary>
    private void OnTriggerStay2D(Collider2D other)
    {
        CardController incomingCard = other.GetComponent<CardController>();
        if (incomingCard == null || incomingCard.cardData == null) return;

        // 🎯 [문제 2번 해결] 신형 인풋 기준으로 마우스 왼쪽 버튼을 '누르고 있는 상태(드래그 중)'라면
        // 절대 코드가 아래로 내려가지 못하게 튕겨냅니다. (올려놔도 안 먹고 밀려나게 준비하는 단계)
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
        {
            return;
        }

        // 🎯 [문제 1번 해결] 이미 이번 드롭으로 처리 중인 실물 카드 오브젝트라면 중복 연산 금지!
        int cardInstanceId = incomingCard.gameObject.GetInstanceID();
        if (_processedCardInstanceIds.Contains(cardInstanceId)) return;

        // 마우스를 완전히 딱 뗐을(드롭) 때만 비로소 흡수 함수를 단 한 번 가동합니다.
        _processedCardInstanceIds.Add(cardInstanceId);
        TryAbsorbCard(incomingCard);
    }

    // 영역에서 카드가 아예 나가면 중복 처리 리스트에서 청소해 줍니다.
    private void OnTriggerExit2D(Collider2D other)
    {
        CardController incomingCard = other.GetComponent<CardController>();
        if (incomingCard != null)
        {
            _processedCardInstanceIds.Remove(other.gameObject.GetInstanceID());
        }
    }

    /// <summary>
    /// 📥 카드 흡수 시스템
    /// </summary>
    private void TryAbsorbCard(CardController card)
    {
        if (card.cardData.yesBox != "1" || currentCount >= MaxCount)
        {
            // 조건이 안 맞으면 중복 리스트에서 빼서 다시 기회를 줍니다.
            _processedCardInstanceIds.Remove(card.gameObject.GetInstanceID());
            return;
        }

        // 규칙 2) 단일 종류 제한
        if (string.IsNullOrEmpty(targetCardId))
        {
            targetCardId = card.cardData.id;
        }
        else if (targetCardId != card.cardData.id)
        {
            _processedCardInstanceIds.Remove(card.gameObject.GetInstanceID());
            return;
        }

        // 🎯 [문제 1번 해결] 정확하게 딱 '1씩' 정직하게 증가하도록 마감!
        currentCount++;
        Debug.Log($"📥 상자 카드 흡수 성공! 현재 수량: ({currentCount} / {MaxCount})");

        // 흡수된 실물 카드는 파괴
        Destroy(card.gameObject);
    }

    /// <summary>
    /// 문제 3번) 상자를 더블클릭할 때 내부 수량이 무한 복사/증가하던 버그 원천 차단!
    /// </summary>
    private void OnMouseDown()
    {
        float timeSinceLastClick = Time.time - _lastClickTime;

        if (timeSinceLastClick <= DoubleClickTimeLimit)
        {
            OnBoxDoubleClicked();
        }

        _lastClickTime = Time.time;
    }

    private void OnBoxDoubleClicked()
    {
        // 내보낼 카드 종류가 없거나 수량이 없으면 차단
        if (currentCount <= 0 || string.IsNullOrEmpty(targetCardId))
        {
            Debug.LogWarning("📭 상자가 텅 비어있습니다.");
            return;
        }

        // 5개씩 꺼내기 (남은 개수가 5개 미만이면 남은 만큼만)
        int spawnAmount = Mathf.Min(5, currentCount);
        Debug.Log($"📤 상자 언박싱! 내부에서 [{targetCardId}] 카드를 {spawnAmount}개 내보냅니다.");

        for (int i = 0; i < spawnAmount; i++)
        {
            // 🎯 [문제 3번 해결] 밖으로 실물 카드를 복사해서 뿜어내는 만큼, 
            // 상자 안에 남아있는 카드의 수량(currentCount)은 정확하게 '감소' 시켜야 복사 버그가 안 납니다!
            currentCount--;

            Vector3 spawnOffset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f);
            Vector3 spawnPos = transform.position + spawnOffset;

            // 실물 카드 월드에 스폰!
            CardSpawnManager.Instance.SpawnCard(targetCardId, spawnPos);
        }

        // 상자가 완전히 비게 되면 다른 아이템을 받을 수 있도록 문을 열어줍니다.
        if (currentCount <= 0)
        {
            targetCardId = "";
            Debug.Log("🔄 상자가 비었습니다. 새로운 종류의 카드를 받을 준비가 되었습니다.");
        }
    }
}