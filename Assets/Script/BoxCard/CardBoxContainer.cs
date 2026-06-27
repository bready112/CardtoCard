using UnityEngine;
using System.Collections.Generic;

public class CardBoxContainer : MonoBehaviour
{
    [Header("📦 상자 내부 저장소")]
    public string targetCardId = "";      // 현재 상자에 지정된 카드 ID
    public int currentCount = 0;          // 현재 저장된 카드 개수
    public const int MaxCount = 100;      // 최대 100개 저장 제한

    [Header("📐 상자 흡수 반경 크기")]
    public float absorbRadius = 1.0f;     // 상자가 카드를 빨아들일 원형 범위

    [Header("⏱️ 더블클릭 타이밍 세팅")]
    private float _lastClickTime = 0f;
    private const float DoubleClickTimeLimit = 0.3f;

    // 한 프레임에 중복으로 흡수되는 것을 막는 안전장치
    private HashSet<int> _processedCardInstanceIds = new HashSet<int>();

    private void Update()
    {
        // 💥 [핵심 변경]: 카드의 물리(Simulated)가 꺼져 있어도 상자 중심의 원형 그물망으로 무조건 찾아냅니다!
        Collider2D[] overlappedColliders = Physics2D.OverlapCircleAll(transform.position, absorbRadius);

        // 현재 내 그물망에 들어와 있는 카드들의 ID를 임시 저장할 리스트 (영역을 벗어난 카드 청소용)
        List<int> cardsInZone = new List<int>();

        foreach (Collider2D col in overlappedColliders)
        {
            if (col == null) continue;

            CardController incomingCard = col.GetComponent<CardController>();
            if (incomingCard == null || incomingCard.cardData == null) continue;

            int cardInstanceId = incomingCard.gameObject.GetInstanceID();
            cardsInZone.Add(cardInstanceId);

            // 🎯 [규칙 복원]: 신형 인풋 기준으로 마우스 왼쪽 버튼을 '누르고 있는 상태(드래그 중)'라면 흡수를 보류합니다.
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
            {
                continue;
            }

            // 마우스를 완전히 딱 뗐을(드롭) 때만 비로소 흡수 함수를 단 한 번 가동합니다.
            if (!_processedCardInstanceIds.Contains(cardInstanceId))
            {
                _processedCardInstanceIds.Add(cardInstanceId);
                TryAbsorbCard(incomingCard);
            }
        }

        // 🎯 [영역 이탈 청소]: 이번 프레임에 그물망에 걸리지 않은 카드가 있다면 중복 방지 리스트에서 지워줍니다.
        // 기존 OnTriggerExit2D 역할을 완벽하게 대체합니다.
        List<int> toRemove = new List<int>();
        foreach (int id in _processedCardInstanceIds)
        {
            if (!cardsInZone.Contains(id))
            {
                toRemove.Add(id);
            }
        }
        foreach (int id in toRemove)
        {
            _processedCardInstanceIds.Remove(id);
        }
    }

    /// <summary>
    /// 📥 카드 흡수 시스템
    /// </summary>
    private void TryAbsorbCard(CardController card)
    {
        if (card.cardData.yesBox != "1" || currentCount >= MaxCount)
        {
            _processedCardInstanceIds.Remove(card.gameObject.GetInstanceID());
            return;
        }

        // 단일 종류 제한 규칙
        if (string.IsNullOrEmpty(targetCardId))
        {
            targetCardId = card.cardData.id;
        }
        else if (targetCardId != card.cardData.id)
        {
            _processedCardInstanceIds.Remove(card.gameObject.GetInstanceID());
            return;
        }

        currentCount++;
        Debug.Log($"📥 상자 카드 흡수 성공! 현재 수량: ({currentCount} / {MaxCount})");

        Destroy(card.gameObject);
    }

    /// <summary>
    /// 상자를 더블클릭할 때 분출하는 언박싱 시스템
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
        if (currentCount <= 0 || string.IsNullOrEmpty(targetCardId))
        {
            Debug.LogWarning("📭 상자가 텅 비어있습니다.");
            return;
        }

        int spawnAmount = Mathf.Min(5, currentCount);
        Debug.Log($"📤 상자 언박싱! 내부에서 [{targetCardId}] 카드를 {spawnAmount}개 내보냅니다.");

        for (int i = 0; i < spawnAmount; i++)
        {
            currentCount--;

            Vector3 spawnOffset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f);
            Vector3 spawnPos = transform.position + spawnOffset;

            CardSpawnManager.Instance.SpawnCard(targetCardId, spawnPos);
        }

        if (currentCount <= 0)
        {
            targetCardId = "";
            Debug.Log("🔄 상자가 비었습니다. 새로운 종류의 카드를 받을 준비가 되었습니다.");
        }
    }

    // 에디터 뷰에서 흡수 범위를 시각적으로 확인하기 위한 기즈모
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, absorbRadius);
    }
}