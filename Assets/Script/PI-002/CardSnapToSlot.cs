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

    // 🎯 [핵심 주권 변수] 내 카드가 내려앉은 출발지 슬롯 정보를 기억합니다.
    private WireSlot _myOriginSlot;
    private string _actualCardID;
    private GameObject _myCanvasObject;

    private void Awake()
    {
        _dragHandler = GetComponentInParent<CardDragHandler>() ?? GetComponent<CardDragHandler>();
    }

    private void OnMouseUp()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(UnityEngine.InputSystem.Mouse.current.position.ReadValue().x, UnityEngine.InputSystem.Mouse.current.position.ReadValue().y, 10f));
        mouseWorldPos.z = 0f;

        RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);
        WireSlot targetSlot = null;

        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.CompareTag("WirePin"))
            {
                WireSlot slotScript = hit.collider.GetComponent<WireSlot>();
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

                // 1. 출발지 슬롯 정보를 내 몸통에 확실히 붙잡아둡니다.
                _myOriginSlot = targetSlot;

                // 2. 출발지 슬롯에 내 카드 캔버스 연동 및 점유 표시
                _myOriginSlot.LinkMasterCardCanvas(_myCanvasObject);
                _myOriginSlot.isOccupied = true;

                // 3. 고향 자리에 가상 고정 핀 생성
                GameObject temBucket = GameObject.Find("Tem") ?? new GameObject("Tem");
                if (startPinPrefab != null)
                {
                    GameObject sPinObj = Instantiate(startPinPrefab, _myOriginSlot.transform.position, Quaternion.identity);
                    sPinObj.name = "Tracking_StartPin";
                    sPinObj.transform.SetParent(temBucket.transform, true);
                }

                // 4. 가상 마우스 핀(EndPin)을 소환합니다. (★주소 기억시키지 말고 순수하게 생성만!)
                if (endPinPrefab != null)
                {
                    GameObject ePinObj = Instantiate(endPinPrefab, _myOriginSlot.transform.position, Quaternion.identity);
                    ePinObj.transform.SetParent(temBucket.transform, true);

                    EndPin endPinScript = ePinObj.GetComponent<EndPin>();
                    if (endPinScript != null)
                    {
                        // 🎯 핵심: 가상핀에게 주소를 주입하지 않고, 나 자신(CardSnapToSlot)을 리포터로 등록하여 연결만 해줍니다.
                        endPinScript.InitPureTracker(this);
                    }
                }

                // 5. 마우스 모드 제어
                if (MouseController.Instance != null)
                {
                    MouseController.Instance.SetMouseMode(MouseController.MouseMode.SlotOnly);
                }

                CardColliderReceiver receiver = GetComponentInChildren<CardColliderReceiver>();
                if (receiver != null)
                {
                    receiver.SetColliderActive(false);
                }

                if (_dragHandler != null) _dragHandler.enabled = false;

                // 6. 카드는 화면에서 숨깁니다.
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

    /// <summary>
    /// 🎯 [사상 대통합 핵심 함수] 
    /// 가상핀이 목적지 주소를 따와서 거꾸로 전해오면, 사령탑인 여기서 2개의 주소를 묶어 최종 선을 생성하고 전달합니다.
    /// </summary>
    public bool ReportTargetSlotDiscovered(WireSlot finalTargetSlot)
    {
        if (_myOriginSlot == null || finalTargetSlot == null) return false;



        // ====================================================================
        // 🎯 [새로운 스크립트 협동] 배선 계약서 최종 사인 직전, 독립 검사기 가동!
        // ====================================================================
        if (!WireConnectionValidator.IsConnectionValid(_myOriginSlot, finalTargetSlot))
        {
            // 💥 [취소 안 한다!] 규칙에 맞지 않으면 강제 취소(ForceCancelRestore)를 호출하지 않고,
            // 그냥 여기서 함수를 끝내버립니다. (가상핀과 가이드선이 파괴되지 않고 화면에 유지됨)
            return false;
        }





        // 1. 최종 실물 전선 오브젝트 인스턴스화 (CardSnapToSlot이 주도하여 생성)
        GameObject lineGroupObj = GameObject.Find("All_Line") ?? new GameObject("All_Line");
        GameObject realWireInstance = Instantiate(wireLinePrefab, Vector3.zero, Quaternion.identity);
        realWireInstance.transform.SetParent(lineGroupObj.transform, false);

        // 2. 선의 이름을 내 부모 카드(Canvas)의 명찰 주소로 낙인
        realWireInstance.name = _myCanvasObject.name;

        // 3. 전선 추적기(RealWireTracker) 세팅 가동
        RealWireTracker wireTracker = realWireInstance.GetComponent<RealWireTracker>();
        if (wireTracker != null)
        {
            wireTracker.SetupTrackingSlots(_myOriginSlot.transform, finalTargetSlot.transform, _myCanvasObject.name);
        }

        // 4. 부모.부모.자식 정밀 세부 주소 추출
        string originDetailPath = GetDetailedPath(_myOriginSlot.transform);
        string targetDetailPath = GetDetailedPath(finalTargetSlot.transform);

        // 5. 취합 완료된 2개의 주소와 실물 선 정보를 양쪽 슬롯에 강력하게 주입!
        _myOriginSlot.ConnectWire(finalTargetSlot.transform.position, wireLinePrefab, targetDetailPath, finalTargetSlot, realWireInstance);
        finalTargetSlot.ConnectWire(_myOriginSlot.transform.position, wireLinePrefab, originDetailPath, _myOriginSlot, realWireInstance);

        // 6. 연결이 완료되었으므로 주권 카드 원본 오브젝트 최종 파괴
        Destroy(_myCanvasObject);

        if (MouseController.Instance != null)
        {
            MouseController.Instance.SetMouseMode(MouseController.MouseMode.Normal);
        }

        GameObject startPin = GameObject.Find("Tracking_StartPin");
        if (startPin != null) Destroy(startPin);

        return true; // 합격 보고
    }

    private string GetDetailedPath(Transform trans)
    {
        string p2 = (trans.parent != null && trans.parent.parent != null) ? trans.parent.parent.name : "Root";
        string p1 = (trans.parent != null) ? trans.parent.name : "Sub";
        string me = trans.name;

        return $"{p2}.{p1}.{me}";
    }

    // 가상선 취소 시 카드 복구용 유틸리티
    /// <summary>
    /// 🎯 [규칙 1] 연결 도중 철거(취소): 클릭해서 취소한 바로 그 마우스 위치(현재 핀 위치)에 카드를 복구합니다.
    /// </summary>
    public void ForceCancelRestore(Vector3 currentMousePinPos)
    {
        if (_myOriginSlot != null)
        {
            // 출발지 슬롯 데이터는 깨끗하게 복구(수신 리셋)
            _myOriginSlot.RestoreMasterCard();

            if (_myCanvasObject != null)
            {
                // 카드는 엉뚱한 곳이 아니라 유저가 '클릭해서 취소한 마우스 자리'에 나타납니다.
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
        if (MouseController.Instance != null) MouseController.Instance.SetMouseMode(MouseController.MouseMode.Normal);
        GameObject startPin = GameObject.Find("Tracking_StartPin");
        if (startPin != null) Destroy(startPin);
    }
}