using UnityEngine;

public static class CardHierarchyNavigator
{
    /// <summary>
    /// 🎯 [최상위 캔버스 네비게이션]
    /// 자식 오브젝트 위치 기준 고향 Canvas(TheCard) 주소 역추적
    /// </summary>
    public static Transform GetCanvasRoot(Transform currentTransform)
    {
        if (currentTransform == null) return null;

        Transform theCard = currentTransform.parent?.parent;
        if (theCard != null && theCard.CompareTag("TheCard"))
        {
            return theCard;
        }

        GameObject canvasObj = GameObject.FindWithTag("TheCard");
        if (canvasObj != null) return canvasObj.transform;

        return null;
    }

    /// <summary>
    /// 🎯 [기획 반영: 슬롯 안착 전용 우선순위 탐지]
    /// 마우스 위치 밑에 겹쳐 있는 모든 콜라이더를 관통하여 수집한 뒤,
    /// 1순위: WirePin(슬롯), 2순위: Card(카드) 순서로 완벽하게 필터링하여 반환합니다!
    /// </summary>
    public static Transform FindSlotInCanvas(Transform canvasRoot, Transform selfTransform, Vector3 checkPosition, float range = 5.0f)
    {
        if (canvasRoot == null) return null;

        // 1. 마우스가 놓인 월드 좌표(checkPosition)를 송곳처럼 뚫고 지나가는 관통 레이저를 쏩니다.
        RaycastHit2D[] hits = Physics2D.RaycastAll(checkPosition, Vector2.zero);

        Transform fallbackCard = null;

        // 2. 🔥 [우선순위 1등 수색] 겹친 오브젝트들 중에 'WirePin' 태그가 하나라도 있는지 전수조사합니다.
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.CompareTag("WirePin"))
            {
                // 슬롯을 찾았다면 뒤에 카드가 몇 장이 겹쳐있든 무시하고 즉시 이 슬롯 주소를 리턴합니다! (최고 우선순위)
                return hit.collider.transform;
            }

            // 혹시 레이저에 카드가 걸렸다면, 슬롯이 없을 때를 대비해 임시로 주소를 백업해 둡니다.
            if (hit.collider != null && hit.collider.CompareTag("Card"))
            {
                if (hit.collider.gameObject != selfTransform.gameObject) // 자기 자신 제외
                {
                    fallbackCard = hit.collider.transform;
                }
            }
        }

        // 3. 🔥 [우선순위 2등 구제] 주변에 WirePin(슬롯)이 아예 없었고, 카드만 겹쳐 있었다면 백업해 둔 카드를 리턴합니다.
        if (fallbackCard != null)
        {
            return fallbackCard;
        }

        return null;
    }

    /// <summary>
    /// 🎯 [모든 카드 전용 네비게이션]
    /// 캔버스 내부에서 'Card' 태그를 가진 다른 자식 카드의 Transform을 반환합니다.
    /// </summary>
    public static Transform FindCardInCanvas(Transform canvasRoot, Transform selfTransform, Vector3 checkPosition, float range = 5.0f)
    {
        if (canvasRoot == null) return null;

        RaycastHit2D[] hits = Physics2D.RaycastAll(checkPosition, Vector2.zero);

        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject == selfTransform.gameObject) continue;

            if (hit.collider != null && hit.collider.CompareTag("Card"))
            {
                return hit.collider.transform;
            }
        }
        return null;
    }
}