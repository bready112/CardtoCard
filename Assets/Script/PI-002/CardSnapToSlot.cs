using UnityEngine;

// 💥 이제 이 코드는 최상위 "부모1 (TheCard Canvas)" 오브젝트에 직접 붙입니다!
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
        _dragHandler = GetComponent<CardDragHandler>() ?? GetComponentInParent<CardDragHandler>();

        // 🎯 [부모방 이사 반영]: 내 방 자체가 바로 최상위 캔버스 오브젝트가 됩니다!
        _myCanvasObject = this.gameObject;
    }

    private void OnMouseUp()
    {
        // Z축 기본 평면(0f) 기준으로 마우스 월드 좌표 계산
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(UnityEngine.InputSystem.Mouse.current.position.ReadValue().x, UnityEngine.InputSystem.Mouse.current.position.ReadValue().y, 10f));
        mouseWorldPos.z = 0f;

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
            CardController cardController = GetComponent<CardController>() ?? GetComponentInChildren<CardController>();

            if (cardController != null && cardController.cardData != null)
            {
                _actualCardID = cardController.cardData.id;
            }
            else
            {
                _actualCardID = _myCanvasObject.name.Split('_')[0];
            }

            _myOriginSlot = targetSlot;
            _myOriginSlot.LinkMasterCardCanvas(_myCanvasObject);
            _myOriginSlot.isOccupied = true;

            GameObject temBucket = GameObject.Find("Tem") ?? new GameObject("Tem");

            // 💥 [핵심 보완]: 런타임에 인스턴스화된 내 고유 StartPin의 주소를 명확히 보관합니다.
            GameObject sPinObj = null;
            if (startPinPrefab != null)
            {
                Vector3 startPinPos = _myOriginSlot.transform.position;
                startPinPos.z = 0f;
                sPinObj = Instantiate(startPinPrefab, startPinPos, Quaternion.identity);
                sPinObj.name = "Tracking_StartPin";
                sPinObj.transform.SetParent(temBucket.transform, true);
            }

            if (endPinPrefab != null)
            {
                Vector3 endPinPos = _myOriginSlot.transform.position;
                endPinPos.z = 0f;
                GameObject ePinObj = Instantiate(endPinPrefab, endPinPos, Quaternion.identity);
                ePinObj.transform.SetParent(temBucket.transform, true);

                EndPin endPinScript = ePinObj.GetComponent<EndPin>();
                if (endPinScript != null)
                {
                    // 🎯 [정석 주소 바인딩]: 내 사령탑 주소(this)와 함께, 내 쌍둥이 핀(sPinObj) 주소를 다이렉트로 주입합니다!
                    endPinScript.InitPureTracker(this, sPinObj);
                }
            }

            CardColliderReceiver receiver = GetComponentInChildren<CardColliderReceiver>();
            if (receiver != null)
            {
                receiver.SetColliderActive(false);
            }

            // 🎯 [구조 정리]: 복잡한 네비게이터 문장을 전부 지우고 본인방을 즉시 숨깁니다!
            _myCanvasObject.SetActive(false);
        }
    }

    public bool ReportTargetSlotDiscovered(WireSlot finalTargetSlot)
    {
        if (_myOriginSlot == null || finalTargetSlot == null) return false;

        if (!WireConnectionValidator.IsConnectionValid(_myOriginSlot, finalTargetSlot))
        {
            return false;
        }

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

        if (!isSlotStillValid)
        {
            Debug.LogWarning($"⚠️ [공사 보정] {finalTargetSlot.name}이 물리 몸통에 가려졌으나 그물망 연산으로 관통 수거했습니다.");
        }

        GameObject lineGroupObj = GameObject.Find("All_Line") ?? new GameObject("All_Line");

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

        CardPhysicsReceiver myReceiver = _myOriginSlot.transform.root.GetComponent<CardPhysicsReceiver>();
        CardPhysicsReceiver targetReceiver = finalTargetSlot.transform.root.GetComponent<CardPhysicsReceiver>();

        if (myReceiver != null && targetReceiver != null)
        {
            myReceiver.ActivatePhysicsChain(targetReceiver.GetRigidbody());
            targetReceiver.ActivatePhysicsChain(myReceiver.GetRigidbody());
        }

        Destroy(_myCanvasObject);
        return true;
    }

    private string GetDetailedPath(Transform trans)
    {
        string p2 = (trans.parent != null && trans.parent.parent != null) ? trans.parent.parent.name : "Root";
        string p1 = (trans.parent != null) ? trans.parent.name : "Sub";
        string me = trans.name;

        return $"{p2}.{p1}.{me}";
    }

    /// <summary>
    /// 🛠️ [정교한 취소 복구]: 내 기차 세트의 고유 StartPin을 넘겨받아 그것만 콕 집어 제거합니다.
    /// </summary>
    public void ForceCancelRestore(Vector3 currentMousePinPos, GameObject specificStartPin)
    {
        if (_myOriginSlot != null)
        {
            _myOriginSlot.RestoreMasterCard();

            if (_myCanvasObject != null)
            {
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

        // ❌ GameObject.Find를 삭제하고, 인자로 들어온 고유 핀만 즉시 폭파!
        if (specificStartPin != null)
        {
            Destroy(specificStartPin);
        }
    }
}