using UnityEngine;
using UnityEngine.InputSystem;

public class EndPin : MonoBehaviour
{
    private CardSnapToSlot _ownerCommandCenter;
    private LineRenderer _tempLine;
    private Camera _mainCamera;

    private bool _isTracking = false;
    private bool _isFirstFrameSpanned = true;

    [Header("⏳ 연결 도중 제약 설정")]
    private float _maxMouseDistance;

    public float connectionTimeout = 10.0f; // 연결 중 타이머 (10초)

    private float _currentTimer = 0f;
    private Vector3 _startPinPosition;

    private void Awake()
    {
        _mainCamera = Camera.main;
        gameObject.name = "Tracking_EndPin";
        _tempLine = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
    }

    /// <summary>
    /// 가상핀 순수 시동기
    /// </summary>
    public void InitPureTracker(CardSnapToSlot owner)
    {
        _ownerCommandCenter = owner;
        _isTracking = true;
        _isFirstFrameSpanned = true;
        _currentTimer = 0f;

        // [완벽 동기화] WireConnectionValidator과 값을 완벽하게 일치시킵니다.
        _maxMouseDistance = WireConnectionValidator.MaxWireDistance;
        Debug.Log($"🔌 [EndPin 시동] 가둠 한계 거리가 검사기 수치({_maxMouseDistance})와 동기화되었습니다.");

        GameObject startPin = GameObject.Find("Tracking_StartPin");
        if (startPin != null)
        {
            _startPinPosition = startPin.transform.position;
        }
        else
        {
            _startPinPosition = transform.position;
        }

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

        // 연결 중 10초 타이머 작동
        _currentTimer += Time.deltaTime;
        if (_currentTimer >= connectionTimeout)
        {
            Debug.LogWarning($"⏳ [시간 초과] 연결 제한 시간({connectionTimeout}초)이 지나 가상선이 자동 철거(취소)됩니다.");
            CancelAndReport();
            return;
        }

        if (Mouse.current != null)
        {
            // 1. 현재 실제 마우스 커서의 윈도우 좌표를 가져와 월드 좌표로 변환합니다.
            Vector2 mouseWindowPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));
            mouseWorldPos.z = -1f;

            // 2. 출발지에서 실제 마우스 커서까지의 거리와 방향을 계산합니다.
            Vector3 fromStartToMouse = mouseWorldPos - _startPinPosition;
            float currentDistance = fromStartToMouse.magnitude;

            // 🎯 [실제 화살표 커서 물리 가두기 엔진]
            // 실제 마우스 커서가 허용된 최대 거리를 넘어가려고 하면?
            if (currentDistance > _maxMouseDistance)
            {
                // 월드 상의 한계점 좌표를 계산합니다.
                Vector3 clampedWorldPos = _startPinPosition + (fromStartToMouse.normalized * _maxMouseDistance);

                // 💥 [핵심]: 제한된 월드 좌표를 다시 유니티 '화면 윈도우 스크린 좌표'로 역변환합니다.
                Vector3 clampedScreenPos = _mainCamera.WorldToScreenPoint(clampedWorldPos);

                // OS의 실제 마우스 포인터 화살표를 한계선 좌표로 강제 텔레포트 시켜서 못 나가게 막아버립니다!
                Mouse.current.WarpCursorPosition(new Vector2(clampedScreenPos.x, clampedScreenPos.y));

                // 최종 위치 동기화
                mouseWorldPos = clampedWorldPos;
                mouseWorldPos.z = -1f;
            }

            // 가상핀의 위치를 실제 제어된 마우스 좌표와 1:1 일치시킵니다.
            transform.position = mouseWorldPos;

            if (_tempLine != null && _tempLine.enabled)
            {
                _tempLine.SetPosition(0, _startPinPosition);
                _tempLine.SetPosition(1, mouseWorldPos);
            }
        }

        if (_isFirstFrameSpanned)
        {
            _isFirstFrameSpanned = false;
            return;
        }

        // 허공 클릭 시 취소 처리
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

    public void ProcessClickConnectionWithTarget(WireSlot finalTargetSlot)
    {
        if (finalTargetSlot == null || _ownerCommandCenter == null) return;

        bool isSuccess = _ownerCommandCenter.ReportTargetSlotDiscovered(finalTargetSlot);

        if (isSuccess)
        {
            _isTracking = false;
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("⚠️ 규칙에 맞지 않는 슬롯이므로 연결을 보류하고 가상선을 유지합니다.");
        }
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