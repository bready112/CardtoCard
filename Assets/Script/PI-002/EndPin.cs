using UnityEngine;
using UnityEngine.InputSystem;

public class EndPin : MonoBehaviour
{
    private CardSnapToSlot _ownerCommandCenter;
    private LineRenderer _tempLine;
    private Camera _mainCamera;

    private bool _isTracking = false;
    private bool _isFirstFrameSpanned = true;

    private float _maxMouseDistance;
    public float connectionTimeout = 10.0f;
    private float _currentTimer = 0f;
    private Vector3 _startPinPosition;

    // 나와 생사고락을 함께할 고유 핀 주소 보관함
    private GameObject _myLinkedStartPin;

    private void Awake()
    {
        _mainCamera = Camera.main;
        gameObject.name = "Tracking_EndPin";
        _tempLine = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
    }

    public void InitPureTracker(CardSnapToSlot owner, GameObject linkedStartPin)
    {
        _ownerCommandCenter = owner;
        _myLinkedStartPin = linkedStartPin;

        _isTracking = true;
        _isFirstFrameSpanned = true;
        _currentTimer = 0f;

        _maxMouseDistance = WireConnectionValidator.MaxWireDistance;

        if (_myLinkedStartPin != null)
        {
            _startPinPosition = _myLinkedStartPin.transform.position;
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

        Debug.Log($"🔌 [EndPin 시동] 가상선 추적이 시작되었습니다. (매칭된 출발지 핀: {(_myLinkedStartPin != null ? _myLinkedStartPin.name : "없음")})");
    }

    private void Update()
    {
        if (!_isTracking) return;

        _currentTimer += Time.deltaTime;
        if (_currentTimer >= connectionTimeout)
        {
            Debug.LogWarning($"⏰ [EndPin 타임아웃] {connectionTimeout}초 동안 연결되지 않아 자동 철거됩니다.");
            CancelAndReport();
            return;
        }

        if (Mouse.current != null)
        {
            Vector2 mouseWindowPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));
            mouseWorldPos.z = 0f;

            Vector3 fromStartToMouse = mouseWorldPos - _startPinPosition;
            float currentDistance = fromStartToMouse.magnitude;

            if (currentDistance > _maxMouseDistance)
            {
                Vector3 clampedWorldPos = _startPinPosition + (fromStartToMouse.normalized * _maxMouseDistance);
                clampedWorldPos.z = 0f;

                Vector3 clampedScreenPos = _mainCamera.WorldToScreenPoint(clampedWorldPos);
                Mouse.current.WarpCursorPosition(new Vector2(clampedScreenPos.x, clampedScreenPos.y));
                mouseWorldPos = clampedWorldPos;
            }

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

        // ====================================================================
        // 🧲 [디버그 내비게이션 주입 구역]
        // ====================================================================
        if (Mouse.current != null &&
           (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.leftButton.wasReleasedThisFrame))
        {
            string clickActionType = Mouse.current.leftButton.wasPressedThisFrame ? "클릭 릴리즈(Press)" : "드롭 다운(Release)";

            Vector2 mouseWindowPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));
            mouseWorldPos.z = 0f;

            Collider2D[] overlappedColliders = Physics2D.OverlapCircleAll(mouseWorldPos, 1.5f);
            WireSlot targetSlot = null;

            Debug.Log($"🔍 [마우스 {clickActionType} 감지] 주변 반경 1.5f 탐색 시동 - 총 {overlappedColliders.Length}개의 콜라이더 스캔됨.");

            foreach (var col in overlappedColliders)
            {
                if (col != null)
                {
                    // 탐지된 모든 물체와 태그를 유니티 콘솔에 실시간 중계합니다.
                    Debug.Log($"  ➔ 스캔 오브젝트: [{col.gameObject.name}] | Tag: [{col.gameObject.tag}]");

                    if (col.CompareTag("WirePin"))
                    {
                        WireSlot slotScript = col.GetComponent<WireSlot>();
                        if (slotScript != null)
                        {
                            if (!slotScript.isOccupied)
                            {
                                targetSlot = slotScript;
                                Debug.Log($"   🎯 [타겟 확정] 선이 비어있는 적격 슬롯 발견: {targetSlot.gameObject.name}");
                                break;
                            }
                            else
                            {
                                Debug.Log($"   ❌ [선택 거부] 슬롯 {slotScript.gameObject.name}은 이미 전선이 꼽혀있습니다(isOccupied == true).");
                            }
                        }
                    }
                }
            }

            if (targetSlot != null)
            {
                Debug.Log($"🎯 [WirePin 오버레이 성공] 최종 타겟 슬롯 [{targetSlot.gameObject.name}]으로 인계 절차를 시작합니다.");
                ProcessClickConnectionWithTarget(targetSlot);
            }
            else
            {
                // 💥 이 자리에 아무런 슬롯도 발견되지 않아 철거 판정이 났을 때 로그 작동
                Debug.LogWarning($"💥 [철거 트리거 가동] 마우스 주위에 사용할 수 있는 'WirePin' 태그 슬롯이 단 하나도 없어 철거 프로세스를 작동합니다.");
                CancelAndReport();
            }
        }
    }

    public void ProcessClickConnectionWithTarget(WireSlot finalTargetSlot)
    {
        if (finalTargetSlot == null || _ownerCommandCenter == null) return;

        Debug.Log($"🔌 [완공 조건 검증 검사 부서] 사령탑 사령관에게 슬롯 [{finalTargetSlot.gameObject.name}] 최종 조립 승인을 요청합니다.");
        bool isSuccess = _ownerCommandCenter.ReportTargetSlotDiscovered(finalTargetSlot);

        if (isSuccess)
        {
            Debug.Log($"✨ [완공 승인 완료] 전선 조립 성공! {gameObject.name} 가상 장치를 씬에서 해제합니다.");
            _isTracking = false;

            if (_myLinkedStartPin != null)
            {
                Debug.Log($"🗑️ [임시 핀 해제] 가상 축이었던 [{_myLinkedStartPin.name}]을 파괴 수거합니다.");
                Destroy(_myLinkedStartPin);
            }

            Destroy(gameObject);
        }
        else
        {
            // 규칙 위반(같은 카드 쇼트 등)으로 사령탑에서 튕겨냈을 때 로그 작동
            Debug.LogError($"⚠️ [완공 승인 거절] 사령탑 검증기에서 연결 불가 처분을 내렸습니다. (예: 자기 자신 결선 거부 등). 가상선을 철거하지 않고 대기합니다.");
        }
    }

    private void CancelAndReport()
    {
        _isTracking = false;

        if (_ownerCommandCenter != null)
        {
            Debug.LogWarning($"🛠️ [취소 레포트 송신] 사령탑의 ForceCancelRestore를 호출합니다. 복구 위치: {transform.position}");
            _ownerCommandCenter.ForceCancelRestore(transform.position, _myLinkedStartPin);
        }

        Debug.Log($"❌ [EndPin 소멸] 가상 추적 장치 {gameObject.name}이 최종 파괴되었습니다.");
        Destroy(gameObject);
    }
}