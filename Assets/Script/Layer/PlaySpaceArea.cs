using UnityEngine;

public class PlaySpaceArea : MonoBehaviour
{
    // PlaySpace에 카드가 놓였을 때 감지 (Trigger 설정 필요)
    private void OnTriggerStay2D(Collider2D other)
    {
        CardDragHandlerBase card = other.GetComponent<CardDragHandlerBase>();
        if (card == null) return;

        // ⚡ 사용자가 카드를 아직 드래그 중(클릭 중)일 때는 100을 유지해야 하므로 손을 뗐을 때만 작동!
        if (!card.IsDraggingNow)
        {
            SpriteRenderer cardRenderer = other.GetComponentInChildren<SpriteRenderer>();
            if (cardRenderer != null)
            {
                // 🎯 [기획 반영] PlaySpace 바로 위에 정착했으므로 레이어를 0으로 바꿉니다.
                cardRenderer.sortingOrder = 0;
                Debug.Log($"🌱 [PlaySpace] {other.gameObject.name} 카드가 바닥에 직접 닿아 Order가 0으로 고정되었습니다.");
            }
        }
    }
}