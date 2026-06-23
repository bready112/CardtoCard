using UnityEngine;
using UnityEngine.InputSystem;

public class EndPin : MonoBehaviour
{
    private CardSnapToSlot _ownerCommandCenter; // 나를 보낸 사령탑 스크립트
    private LineRenderer _tempLine;
    private Camera _mainCamera;

    private bool _isTracking = false;
    private bool _isFirstFrameSpanned = true;

    private void Awake()
    {
        _mainCamera = Camera.main;
        gameObject.name = "Tracking_EndPin";
        _tempLine = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
    }

    /// <summary>
    /// 🎯 가상핀 순수 시동기: 주소 데이터 일절 기억하지 않고 사령탑 주소만 연결해 줍니다.
    /// </summary>
    public void InitPureTracker(CardSnapToSlot owner)
    {
        _ownerCommandCenter = owner;
        _isTracking = true;
        _isFirstFrameSpanned = true;

        if (_tempLine != null)
        {
            _tempLine.enabled = true;
            _tempLine.positionCount = 2;
            _tempLine.startWidth = 0.05f;
            _tempLine.endWidth = 0.05f;
            _tempLine.material = new Material(Shader.Find("Sprites/Default"));
            _tempLine.startColor = Color.white;
            _tempLine.endColor = Color.white;
        }
    }

    private void Update()
    {
        if (!_isTracking) return;

        if (Mouse.current != null)
        {
            Vector2 mouseWindowPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));
            mouseWorldPos.z = -1f;

            transform.position = mouseWorldPos;

            if (_tempLine != null && _tempLine.enabled)
            {
                // 실시간으로 출발지 자리는 고정시킨 채 마우스 포지션만 추적하여 갱신합니다.
                if (GameObject.Find("Tracking_StartPin") != null)
                {
                    _tempLine.SetPosition(0, GameObject.Find("Tracking_StartPin").transform.position);
                }
                _tempLine.SetPosition(1, mouseWorldPos);
            }
        }

        if (_isFirstFrameSpanned)
        {
            _isFirstFrameSpanned = false;
            return;
        }

        // 허공 클릭 시 복구 명령 토스
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseWindowPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));

            RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);
            bool hitSlot = false;

            foreach (var hit in hits)
            {
                if (hit.collider != null && (hit.collider.GetComponent<WireSlot>() != null || hit.collider.GetComponentInParent<WireSlot>() != null))
                {
                    hitSlot = true;
                    break;
                }
            }

            if (!hitSlot)
            {
                CancelAndReport();
            }
        }
    }

    /// <summary>
    /// 🎯 목적지 슬롯 조준 좌클릭에 성공하면 호출되는 가상핀의 최종 임무 함수입니다.
    /// </summary>
    public void ProcessClickConnectionWithTarget(WireSlot finalTargetSlot)
    {
        if (finalTargetSlot == null || _ownerCommandCenter == null) return;

        _isTracking = false;

        // 💥 [핵심 역발상]: 내가 직접 선을 만들지 않고, 알아낸 목적지 주소(finalTargetSlot)만 사령탑에 던집니다!
        _ownerCommandCenter.ReportTargetSlotDiscovered(finalTargetSlot);

        // 보고를 마쳤으므로 배달원인 가상핀은 미련 없이 소멸합니다.
        Destroy(gameObject);
    }

    private void CancelAndReport()
    {
        _isTracking = false;
        if (_ownerCommandCenter != null)
        {
            _ownerCommandCenter.ForceCancelRestore(transform.position);
        }
        Destroy(gameObject);
    }
}