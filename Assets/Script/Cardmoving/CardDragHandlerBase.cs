using UnityEngine;
using UnityEngine.InputSystem;

public class CardDragHandlerBase : MonoBehaviour
{
    protected Camera mainCamera;
    protected Vector3 originalPosition;
    protected Vector3 originalScale;
    private Vector3 _offset;

    [Header("✨ 카드 드래그 연출 설정")]
    [Tooltip("마우스로 카드를 클릭해 들고 있을 때 커질 비율")]
    public float dragScaleMultiplier = 1.15f;

    // 현재 이 카드가 드래그 중인지 여부 (자식 클래스들도 알 수 있게 protected)
    public bool IsDraggingNow { get; protected set; } = false;

    protected virtual void Awake()
    {
        mainCamera = Camera.main;
        originalPosition = transform.position;
        originalScale = transform.localScale;
    }

    protected virtual void Start() { }

    protected virtual void OnMouseDown()
    {
        // 🎯 [형제 구조 전용 안전장치]
        // 오직 'Card' 태그를 가진 나 자신을 눌렀을 때만 드래그 연산을 실행합니다.
        // 슬롯들이 형제로 분리되었기 때문에 슬롯 클릭 신호와 완벽히 격리됩니다.
        if (gameObject.CompareTag("Card"))
        {
            HandleCardMouseDown();
        }
    }

    // 🃏 [Card 클릭 핵심 로직 - 자식들이 오버라이드 가능]
    public virtual void HandleCardMouseDown()
    {
        IsDraggingNow = true;

        if (Mouse.current != null)
        {
            Vector2 mouseWindowPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));

            _offset = transform.position - mouseWorldPos;
            _offset.z = 0f;
        }

        // 들고 있을 때 쫀득하게 1.15배 커지기
        transform.localScale = originalScale * dragScaleMultiplier;
    }

    protected virtual void OnMouseDrag()
    {
        if (!IsDraggingNow) return;

        if (Mouse.current != null)
        {
            Vector2 mouseWindowPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));
            mouseWorldPos.z = 0f;

            // 마우스 위치에 offset을 더해 튕김 없이 부드럽게 추적
            transform.position = mouseWorldPos + _offset;
        }
    }

    protected virtual void OnMouseUp()
    {
        if (!IsDraggingNow) return;
        ResetToOriginalState();
    }

    // 🎯 [순정 상태 복귀 로직]
    public virtual void ResetToOriginalState()
    {
        IsDraggingNow = false;

        // 크기와 위치를 정직하게 처음 세팅으로 원상복구
        transform.localScale = originalScale;
        transform.position = originalPosition;
    }

    /// <summary>
    /// 외부 스폰 매니저나 카드 컨트롤러가 카드의 기본 배치 좌표를 갱신해 줄 때 사용하는 안전 창구
    /// </summary>
    public void UpdateOriginalPosition(Vector3 newPos)
    {
        originalPosition = newPos;
        originalPosition.z = 0f;
        transform.position = originalPosition;
    }
}