using UnityEngine;

public class CardDragHandler : CardDragHandlerBase
{
    private Transform _grandParentTransform; // 최상위 Canvas (TheCard)
    private Transform _parentTransform;      // 중간 Panel
    private Vector3 _canvasOffset;           // Canvas 전용 정밀 오프셋

    [Header("📐 캔버스 확대 배율")]
    public float canvasScaleMultiplier = 1.15f;

    protected override void Awake()
    {
        base.Awake();

        _parentTransform = transform.parent;
        if (_parentTransform != null)
        {
            _grandParentTransform = _parentTransform.parent;
        }
    }

    public override void HandleCardMouseDown()
    {
        if (_grandParentTransform != null && _grandParentTransform.CompareTag("TheCard"))
        {
            IsDraggingNow = true;

            // 🎯 [1번 문제 해결: 정밀 오프셋 계산]
            // 마우스를 클릭한 순간, 최상위 Canvas의 현재 위치와 마우스 월드 위치의 차이(거리)를 정확히 기억합니다.
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                Vector2 mouseWindowPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));
                mouseWorldPos.z = 0f;

                // Canvas 중심점과 마우스 사이의 오프셋 저장 (모서리 튕김 원천 차단)
                _canvasOffset = _grandParentTransform.position - mouseWorldPos;
            }

            // Canvas 전체 배율 확대
            _grandParentTransform.localScale = Vector3.one * canvasScaleMultiplier;

            // 물리 연산 잠금
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

    protected override void OnMouseDrag()
    {
        if (!IsDraggingNow || _grandParentTransform == null) return;

        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 mouseWindowPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));
            mouseWorldPos.z = 0f;

            // 🎯 [마우스 위치 추적] 처음 잡았던 오프셋을 더해 캔버스가 마우스에 자석처럼 찰떡으로 붙게 만듭니다.
            _grandParentTransform.position = mouseWorldPos + _canvasOffset;
        }
    }

    public override void ResetToOriginalState()
    {
        IsDraggingNow = false;

        // 크기 원래대로 복원
        if (_grandParentTransform != null)
        {
            _grandParentTransform.localScale = Vector3.one;
        }

        // 🎯 [2번 문제 해결: 자식 분리 차단]
        // 자식인 내 포지션을 건드리지 않고, 이동이 완료된 최상위 Canvas의 위치만 기틀로 인정합니다.
        // 부모(Base)의 UpdateOriginalPosition을 호출해봤자 나 자신의 기준점만 꼬이므로, 호출하지 않고 부모 위치만 묵인합니다.
        if (_grandParentTransform != null)
        {
            // 부모의 원래 자리(originalPosition) 데이터를 현재 캔버스 위치로 갱신하여 찢어짐을 방지합니다.
            originalPosition = transform.position;
        }

        // 물리 연산 복구
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = true;
        }
    }
}