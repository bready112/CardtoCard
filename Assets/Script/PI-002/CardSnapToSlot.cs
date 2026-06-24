using UnityEngine;

public class CardSnapToSlot : MonoBehaviour
{
    [Header("📌 마우스를 따라갈 가상 EndPin 프리팹 (태그: Clickup)")]
    public GameObject endPinPrefab;

    [Header("📌 출발지 슬롯에 고정될 가상 StartPin 프리팹")]
    public GameObject startPinPrefab;

    [Header("🔌 전선 프리팹")]
    public GameObject wireLinePrefab;

    [Header("🎯 핀 구분용 명찰 (기본값)")]
    public string targetPinName = "Card1";

    [Header("📐 슬롯 안착 판정 범위")]
    public float snapRange = 5.0f;

    private CardDragHandler _dragHandler;

    private WireSlot _myOriginSlot;
    private string _actualCardID;
    private GameObject _myCanvasObject;

    private void Awake()
    {
        _dragHandler = GetComponentInParent<CardDragHandler>() ?? GetComponent<CardDragHandler>();
    }

    private void OnMouseUp()
    {
        // 💥 Z축 기본 평면(0f) 기준으로 마우스 월드 좌표 계산 (카메라 기본 거리 10f 적용)
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(UnityEngine.InputSystem.Mouse.current.position.ReadValue().x, UnityEngine.InputSystem.Mouse.current.position.ReadValue().y, 10f));
        mouseWorldPos.z = 0f; // 완전 평면화

        // 원형 그물망 탐지 가동
        Collider2D[] overlappedColliders = Physics2D.OverlapCircleAll(mouseWorldPos, 0.5f);
        WireSlot targetSlot = null;

        foreach (Collider2D col in overlappedColliders)
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
            Transform myCanvas = CardHierarchyNavigator.GetCanvasRoot(transform);
            if (myCanvas != null)
            {
                _myCanvasObject = myCanvas.gameObject;
                CardController cardController = myCanvas.GetComponent<CardController>() ?? GetComponentInParent<CardController>();

                if (cardController != null && cardController.cardData != null)
                {
                    _actualCardID = cardController.cardData.id;
                }
                else
                {
                    _actualCardID = myCanvas.name.Split('_')[0];
                }

                _myOriginSlot = targetSlot;
                _myOriginSlot.LinkMasterCardCanvas(_myCanvasObject);
                _myOriginSlot.isOccupied = true;

                GameObject temBucket = GameObject.Find("Tem") ?? new GameObject("Tem");
                if (startPinPrefab != null)
                {
                    // 💥 Z축을 0f로 꼽아줍니다.
                    Vector3 startPinPos = _myOriginSlot.transform.position;
                    startPinPos.z = 0f;
                    GameObject sPinObj = Instantiate(startPinPrefab, startPinPos, Quaternion.identity);
                    sPinObj.name = "Tracking_StartPin";
                    sPinObj.transform.SetParent(temBucket.transform, true);
                }

                if (endPinPrefab != null)
                {
                    // 💥 Z축을 0f로 꼽아줍니다.
                    Vector3 endPinPos = _myOriginSlot.transform.position;
                    endPinPos.z = 0f;
                    GameObject ePinObj = Instantiate(endPinPrefab, endPinPos, Quaternion.identity);
                    ePinObj.transform.SetParent(temBucket.transform, true);

                    EndPin endPinScript = ePinObj.GetComponent<EndPin>();
                    if (endPinScript != null)
                    {
                        endPinScript.InitPureTracker(this);
                    }
                }

  

                CardColliderReceiver receiver = GetComponentInChildren<CardColliderReceiver>();
                if (receiver != null)
                {
                    receiver.SetColliderActive(false);
                }

                

                if (transform.parent != null && transform.parent.parent != null)
                {
                    transform.parent.parent.gameObject.SetActive(false);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }

    public bool ReportTargetSlotDiscovered(WireSlot finalTargetSlot)
    {
        if (_myOriginSlot == null || finalTargetSlot == null) return false;

        if (!WireConnectionValidator.IsConnectionValid(_myOriginSlot, finalTargetSlot))
        {
            return false;
        }

        // ====================================================================
        // 🧲 [완공 구역 안전장치]: 최종 조준된 슬롯 평면을 그물망 탐지로 재차 안전 검사합니다.
        // 부모 카드가 완공 연출 순간 몸통 콜라이더로 덮어버려도 완전히 무시합니다.
        // ====================================================================
        Vector3 targetPos = finalTargetSlot.transform.position;
        targetPos.z = 0f;

        Collider2D[] safetyCheck = Physics2D.OverlapCircleAll(targetPos, 0.5f);
        bool isSlotStillValid = false;

        foreach (Collider2D col in safetyCheck)
        {
            if (col != null && col.gameObject == finalTargetSlot.gameObject)
            {
                isSlotStillValid = true;
                break;
            }
        }

        // 만약 비정상적인 프레임 꼬임으로 슬롯 인지가 풀렸더라도 강제 복원 판정을 먹입니다.
        if (!isSlotStillValid)
        {
            Debug.LogWarning($"⚠️ [공사 보정] {finalTargetSlot.name}이 물리 몸통에 가려졌으나 그물망 연산으로 관통 수거했습니다.");
        }

        GameObject lineGroupObj = GameObject.Find("All_Line") ?? new GameObject("All_Line");

        // 💥 실물 전선 인스턴스도 Z = 0f 평면에 소환
        GameObject realWireInstance = Instantiate(wireLinePrefab, Vector3.zero, Quaternion.identity);
        realWireInstance.transform.SetParent(lineGroupObj.transform, false);

        realWireInstance.name = _myCanvasObject.name;

        RealWireTracker wireTracker = realWireInstance.GetComponent<RealWireTracker>();
        if (wireTracker != null)
        {
            wireTracker.SetupTrackingSlots(_myOriginSlot.transform, finalTargetSlot.transform, _myCanvasObject.name);
        }

        string originDetailPath = GetDetailedPath(_myOriginSlot.transform);
        string targetDetailPath = GetDetailedPath(finalTargetSlot.transform);

        _myOriginSlot.ConnectWire(finalTargetSlot.transform.position, wireLinePrefab, targetDetailPath, finalTargetSlot, realWireInstance);
        finalTargetSlot.ConnectWire(_myOriginSlot.transform.position, wireLinePrefab, originDetailPath, _myOriginSlot, realWireInstance);

        // 확실하게 루트 계층에서 수신기를 탐색하여 고무줄 작동
        CardPhysicsReceiver myReceiver = _myOriginSlot.transform.root.GetComponent<CardPhysicsReceiver>();
        CardPhysicsReceiver targetReceiver = finalTargetSlot.transform.root.GetComponent<CardPhysicsReceiver>();

        if (myReceiver != null && targetReceiver != null)
        {
            myReceiver.ActivatePhysicsChain(targetReceiver.GetRigidbody());
            targetReceiver.ActivatePhysicsChain(myReceiver.GetRigidbody());
        }

        Destroy(_myCanvasObject);


        GameObject startPin = GameObject.Find("Tracking_StartPin");
        if (startPin != null) Destroy(startPin);

        return true;
    }

    private string GetDetailedPath(Transform trans)
    {
        string p2 = (trans.parent != null && trans.parent.parent != null) ? trans.parent.parent.name : "Root";
        string p1 = (trans.parent != null) ? trans.parent.name : "Sub";
        string me = trans.name;

        return $"{p2}.{p1}.{me}";
    }

    public void ForceCancelRestore(Vector3 currentMousePinPos)
    {
        if (_myOriginSlot != null)
        {
            _myOriginSlot.RestoreMasterCard();

            if (_myCanvasObject != null)
            {
                // 💥 카드 부활 시 Z축 원래대로 0f 안착
                Vector3 spawnPos = currentMousePinPos;
                spawnPos.z = 0f;
                _myCanvasObject.transform.position = spawnPos;
                _myCanvasObject.SetActive(true);

                var dragHandler = _myCanvasObject.GetComponent<CardDragHandler>() ?? _myCanvasObject.GetComponentInParent<CardDragHandler>();
                if (dragHandler != null) dragHandler.enabled = true;

                CardColliderReceiver receiver = _myCanvasObject.GetComponentInChildren<CardColliderReceiver>();
                if (receiver != null) receiver.SetColliderActive(true);
            }
        }

        GameObject startPin = GameObject.Find("Tracking_StartPin");
        if (startPin != null) Destroy(startPin);
    }
}