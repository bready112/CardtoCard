using UnityEngine;

public class CardLayerController : MonoBehaviour, IClickIt
{
    [Header("🎨 레이어 설정")]
    public string originalSortingLayer = "Default";
    public int currentOrderInLayer = 0;

    private SpriteRenderer _spriteRenderer;
    private Canvas _canvas;

    // 마우스로 들고 있는지 여부 (들려있을 때는 100 유지용)
    private bool _isPickedUp = false;

    // 현재 나랑 물리적으로 부딪혀 있는 오브젝트의 총 개수
    private int _collisionCount = 0;

    private void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _canvas = GetComponent<Canvas>() ?? GetComponentInChildren<Canvas>();

        if (_spriteRenderer != null)
        {
            originalSortingLayer = _spriteRenderer.sortingLayerName;
            currentOrderInLayer = _spriteRenderer.sortingOrder;
        }
        else if (_canvas != null)
        {
            originalSortingLayer = _canvas.sortingLayerName;
            currentOrderInLayer = _canvas.sortingOrder;
        }
    }

    /// <summary>
    /// 👆 클릭할 때 "Click" 레이어, Order 100으로 변경
    /// </summary>
    public void OnClickStart()
    {
        _isPickedUp = true;
        ApplyLayerSettings("Click", 100);
    }

    /// <summary>
    /// 🖐️ 클릭을 놓으면 현재 물리 충돌 상태에 맞게 레이어 정돈
    /// </summary>
    public void OnClickRelease()
    {
        _isPickedUp = false;

        // 손을 뗐는데 아무것도 안 부딪히고 공중에 떠 있다면 ➔ 즉시 0 리셋!
        if (_collisionCount == 0)
        {
            currentOrderInLayer = 0;
            ApplyLayerSettings(originalSortingLayer, 0);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        _collisionCount++;
        UpdateCardLayer(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (_isPickedUp) return;
        UpdateCardLayer(collision.gameObject);
    }

    /// <summary>
    /// 💨 떨어지면 0 처리 구역
    /// </summary>
    private void OnCollisionExit2D(Collision2D collision)
    {
        _collisionCount--;

        // 내 몸에 닿아있는 물체가 이제 하나도 없다면 (바닥이든 카드든 다 떨어졌다면)
        if (_collisionCount <= 0)
        {
            _collisionCount = 0;

            if (!_isPickedUp)
            {
                currentOrderInLayer = 0; // ➔ 무조건 레이어 0 고정!
                ApplyLayerSettings(originalSortingLayer, 0);
                Debug.Log($"🍃 [태그 스택 이탈] {gameObject.name} 카드가 허공으로 떨어져 레이어가 0이 되었습니다.");
            }
        }
    }

    /// <summary>
    /// 🪄 [질문자님 기획 반영]: Card 및 PlaySpace 태그 기반 초고속 연산 센터
    /// </summary>
    private void UpdateCardLayer(GameObject hitObj)
    {
        if (_isPickedUp || hitObj == null) return;

        // 규칙 1) 바닥에 직접 부딪혔다면 ➔ 레이어 1 고정!
        if (hitObj.CompareTag("PlaySpace"))
        {
            currentOrderInLayer = 1;
            ApplyLayerSettings(originalSortingLayer, 1);
            return;
        }

        // 규칙 2) 💥 [태그 저격 구역]: 부딪힌 녀석의 태그가 질문자님이 명시한 "Card"라면!
        if (hitObj.CompareTag("Card"))
        {
            // 태그가 확인된 안전한 상황에서만 컴포넌트를 콕 짚어 가져옵니다 (성능 대폭 최적화!)
            CardLayerController underCard = hitObj.GetComponent<CardLayerController>() ??
                                            hitObj.GetComponentInParent<CardLayerController>() ??
                                            hitObj.GetComponentInChildren<CardLayerController>();

            if (underCard != null && underCard != this)
            {
                int underCardOrder = underCard.GetCurrentOrder();

                // 밑에 깔린 카드가 마우스에 들려있는 상태(100)라면 그 카드의 원래 바닥 순서를 기준삼음
                if (underCardOrder == 100)
                {
                    underCardOrder = underCard.currentOrderInLayer;
                }

                // 규칙 3) 밑에 카드가 3이면 나는 4 (언제나 밑에 카드보다 +1 오더 보정)
                int newOrder = underCardOrder + 1;

                currentOrderInLayer = newOrder;
                ApplyLayerSettings(originalSortingLayer, newOrder);
            }
        }
    }

    public int GetCurrentOrder()
    {
        if (_spriteRenderer != null) return _spriteRenderer.sortingOrder;
        if (_canvas != null) return _canvas.sortingOrder;
        return currentOrderInLayer;
    }

    private void ApplyLayerSettings(string layerName, int order)
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sortingLayerName = layerName;
            _spriteRenderer.sortingOrder = order;
        }

        if (_canvas != null)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingLayerName = layerName;
            _canvas.sortingOrder = order;
        }
    }
}