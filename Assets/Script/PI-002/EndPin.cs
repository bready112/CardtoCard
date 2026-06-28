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

        // Validator에 적어두신 수치를 그대로 가져옵니다[cite: 19].
        _maxMouseDistance = WireConnectionValidator.MaxWireDistance;

        GameObject startPin = GameObject.Find("Tracking_StartPin");
        if (startPin != null)
        {
            _startPinPosition = startPin.transform.position;
            _startPinPosition.z = 0f;
        }
        else
        {
            _startPinPosition = owner.transform.position;
            _startPinPosition.z = 0f;
        }

        WireChainLinker linker = GetComponent<WireChainLinker>() ?? gameObject.AddComponent<WireChainLinker>();
        GameObject segPrefab = Resources.Load<GameObject>("RopeSegment");

        if (startPin != null && segPrefab != null)
        {
            linker.LinkVirtualFirst(startPin.transform, this.transform, segPrefab);
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

        if (_startPinPosition == Vector3.zero)
        {
            GameObject startPin = GameObject.Find("Tracking_StartPin");
            if (startPin != null) _startPinPosition = startPin.transform.position;
        }

        if (Mouse.current != null)
        {
            Vector2 mouseWindowPos = Mouse.current.position.ReadValue();

            float targetZ = Mathf.Abs(_mainCamera.transform.position.z);
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, targetZ));
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

            // ====================================================================
            // 🎯 [버그 해결 핵심] EndPin이 일직선으로 그리기를 강탈하던 연산 완전 도려냄!
            // ====================================================================
            // 원래 있던 _tempLine.SetPosition(0/1) 구역을 완벽하게 삭제했습니다.
            // 이제 연결 도중(드래그 중)에도 오직 WireChainLinker가 사슬 순서대로 선을 그립니다.
            // ====================================================================

            // 마우스 클릭 감지 및 완공/철거 분기
            if (Mouse.current.leftButton.wasPressedThisFrame && !_isFirstFrameSpanned)
            {
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
                            break;
                        }
                    }
                }

                if (targetSlot != null)
                {
                    ProcessClickConnectionWithTarget(targetSlot);
                }
                else
                {
                    CancelAndReport();
                }
            }
        }

        if (_isFirstFrameSpanned)
        {
            _isFirstFrameSpanned = false;
        }
    }

    public void ProcessClickConnectionWithTarget(WireSlot finalTargetSlot)
    {
        if (finalTargetSlot == null || _ownerCommandCenter == null) return;

        WireChainLinker linker = GetComponent<WireChainLinker>();
        bool isSuccess = _ownerCommandCenter.ReportTargetSlotDiscovered(finalTargetSlot, linker);

        if (isSuccess)
        {
            _isTracking = false;
            Destroy(gameObject);
        }
    }

    private void CancelAndReport()
    {
        _isTracking = false;

        GameObject wireStorage = GameObject.Find("Wire_Temp_Storage");
        if (wireStorage != null)
        {
            Debug.Log("🔌 [철거 완료] 가상 전선 바구니(Wire_Temp_Storage) 오브젝트 파괴 완료.");
            Destroy(wireStorage);
        }

        if (_ownerCommandCenter != null)
        {
            _ownerCommandCenter.ForceCancelRestore(transform.position);
        }

        Destroy(gameObject);
    }
}