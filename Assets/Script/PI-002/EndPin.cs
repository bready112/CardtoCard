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

    private void Awake()
    {
        _mainCamera = Camera.main;
        gameObject.name = "Tracking_EndPin";
        _tempLine = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
    }

    public void InitPureTracker(CardSnapToSlot owner)
    {
        _ownerCommandCenter = owner;
        _isTracking = true;
        _isFirstFrameSpanned = true;
        _currentTimer = 0f;

        _maxMouseDistance = WireConnectionValidator.MaxWireDistance;

        GameObject startPin = GameObject.Find("Tracking_StartPin");
        if (startPin != null) _startPinPosition = startPin.transform.position;
        else _startPinPosition = transform.position;

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

        _currentTimer += Time.deltaTime;
        if (_currentTimer >= connectionTimeout)
        {
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
        // 🧲 [치트키 구역]: 마우스 클릭 시 유니티 OnMouse를 기만하고 
        // 그물망 탐지(OverlapCircleAll)로 주변 슬롯을 강제로 낚아챕니다.
        // ====================================================================
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseWindowPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));
            mouseWorldPos.z = 0f;

            // 카드 몸통 콜라이더를 가볍게 무시하고, 내 주변 반지름 0.5f 안의 슬롯 싹 다 수거!
            Collider2D[] overlappedColliders = Physics2D.OverlapCircleAll(mouseWorldPos, 0.5f);
            WireSlot targetSlot = null;

            foreach (var col in overlappedColliders)
            {
                if (col != null && col.CompareTag("WirePin"))
                {
                    WireSlot slotScript = col.GetComponent<WireSlot>();
                    if (slotScript != null && !slotScript.isOccupied)
                    {
                        targetSlot = slotScript;
                        break; // 유효한 슬롯을 찾았으므로 루프 탈출
                    }
                }
            }

            // 주변에 진짜 슬롯 구멍이 걸려들었다면 다이렉트 완공 처리!
            if (targetSlot != null)
            {
                Debug.Log($"🎯 [EndPin 치트키] 카드 방해를 뚫고 {targetSlot.gameObject.name} 슬롯 클릭 성공!");
                ProcessClickConnectionWithTarget(targetSlot);
            }
            else
            {
                // 주변 반경에 슬롯이 아예 없는데 클릭한 거라면 허공 클릭이므로 가상선 취소
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