using UnityEngine;
using UnityEngine.InputSystem;

public class CardDragHandler : CardDragHandlerBase
{
    private Transform _grandParentTransform; // 최상위 Canvas (TheCard)
    private Transform _parentTransform;      // 중간 Panel
    private Vector3 _canvasOffset;           // Canvas 전용 정밀 오프셋

    [Header("📐 캔버스 확대 배율")]
    public float canvasScaleMultiplier = 1.15f;

    // 내 자식들이 가진 콜라이더들을 쥐고 있을 캐시 배열
    private Collider2D[] _childColliders;

    protected override void Awake()
    {
        base.Awake();

        _parentTransform = transform.parent;
        if (_parentTransform != null)
        {
            _grandParentTransform = _parentTransform.parent;
        }

        // 💥 [최적화]: 내 몸뚱이와 자식 슬롯들이 가진 모든 Collider 2D를 미리 싹 긁어모아 기억해 둡니다.
        _childColliders = GetComponentsInChildren<Collider2D>(true);
    }

    protected override void OnMouseDown() { }

    private void Update()
    {
        if (Mouse.current == null) return;

        // 1. 마우스를 누르는 순간 (그물망으로 카드 낚아채기)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TriggerOverlapCheckAndStartDrag();
        }

        // 2. 마우스를 누르고 드래그 중일 때
        if (IsDraggingNow && Mouse.current.leftButton.isPressed)
        {
            ExecuteDragTracking();
        }

        // 3. 마우스를 떼는 순간 (MouseUp -> 드래그 종료)
        if (IsDraggingNow && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ResetToOriginalState();
        }
    }

    private void TriggerOverlapCheckAndStartDrag()
    {
        Vector2 mouseWindowPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));
        mouseWorldPos.z = 0f;

        Collider2D[] overlappedColliders = Physics2D.OverlapCircleAll(mouseWorldPos, 0.5f);
        bool hitMe = false;

        foreach (Collider2D col in overlappedColliders)
        {
            if (col != null && col.gameObject == this.gameObject)
            {
                hitMe = true;
                break;
            }
        }

        if (hitMe)
        {
            HandleCardMouseDown();
        }
    }

    public override void HandleCardMouseDown()
    {
        if (_grandParentTransform != null && _grandParentTransform.CompareTag("TheCard"))
        {
            IsDraggingNow = true;

            if (Mouse.current != null)
            {
                Vector2 mouseWindowPos = Mouse.current.position.ReadValue();
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));
                mouseWorldPos.z = 0f;

                _canvasOffset = _grandParentTransform.position - mouseWorldPos;
            }

            _grandParentTransform.localScale = Vector3.one * canvasScaleMultiplier;

            // ====================================================================
            // 🎯 [직통 제어]: 다른 중개자 없이, 내 인스펙터 밑의 충돌체들을 한 번에 다 꺼버립니다!
            // ====================================================================
            SetAllCollidersActive(false);
            // ====================================================================

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
#if UNITY_2023_1_OR_NEWER
                rb.linearVelocity = Vector2.zero;
#else
                rb.velocity = Vector2.zero;
#endif
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }
        }
    }

    private void ExecuteDragTracking()
    {
        if (_grandParentTransform == null) return;

        Vector2 mouseWindowPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));
        mouseWorldPos.z = 0f;

        _grandParentTransform.position = mouseWorldPos + _canvasOffset;
    }

    protected override void OnMouseDrag() { }

    public override void ResetToOriginalState()
    {
        IsDraggingNow = false;

        if (_grandParentTransform != null)
        {
            _grandParentTransform.localScale = Vector3.one;
        }

        if (_grandParentTransform != null)
        {
            originalPosition = transform.position;
        }

        // ====================================================================
        // 🎯 [직통 제어]: 마우스를 놓는 즉시(MouseUp), 꺼뒀던 인스펙터 충돌체들을 다시 켭니다!
        // ====================================================================
        SetAllCollidersActive(true);
        // ====================================================================

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
#if UNITY_2023_1_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.angularVelocity = 0f;
            rb.simulated = true;
        }
    }

    /// <summary>
    /// 내 계층구조 내부의 충돌체 컴포넌트들을 직접 순회하며 스위치를 온/오프하는 청정 함수
    /// </summary>
    private void SetAllCollidersActive(bool state)
    {
        if (_childColliders == null) return;

        foreach (Collider2D col in _childColliders)
        {
            if (col != null)
            {
                col.enabled = state;
            }
        }
        Debug.Log($"🔌 [CardDragHandler] 내부 인스펙터 충돌체 {_childColliders.Length}개의 활성화 상태를 {state}(으)로 직접 변경했습니다.");
    }
}