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
                    Vector3 startPinPos = _myOriginSlot.transform.position;
                    startPinPos.z = 0f;
                    GameObject sPinObj = Instantiate(startPinPrefab, startPinPos, Quaternion.identity);
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

    /// <summary>
    /// 🎯 [철거 버그 및 파라미터 오염 완전 해결 버전]
    /// </summary>
    public bool ReportTargetSlotDiscovered(WireSlot finalTargetSlot, WireChainLinker linker)
    {
        // 1. 🛡️ [규칙 검증] 배선 승인 전 Validator 규칙 통과 여부 확인
        if (!WireConnectionValidator.IsConnectionValid(_myOriginSlot, finalTargetSlot))
        {
            Debug.LogWarning("🚫 [배선 승인 거부] 유효성 검사를 통과하지 못했습니다. 무반응 처리합니다.");
            return false;
        }

        // 2. 방어 코드: 타겟 슬롯이 무효하면 즉시 리턴
        if (finalTargetSlot == null || finalTargetSlot.isOccupied) return false;
        if (linker == null) return false;

        // 🎯 카드 캔버스의 고유 주소 이름을 완벽히 매핑합니다.
        string uniqueAddress = _myCanvasObject.name;

        // ⚡ 3. 선 정보 스와프 및 All_Line 이관 진행
        // (이 안에서 RealWireTracker의 SetupTrackingSlots가 이미 온전하게 최초 1회 실행됩니다!)
        Transform originSlotTransform = (_myOriginSlot != null) ? _myOriginSlot.transform : this.transform;
        linker.SwapToRealSlots(originSlotTransform, finalTargetSlot.transform, uniqueAddress);

        // 🎯 4. 계승 완료된 실제 전선 인스턴스와 RealWireTracker를 명확히 낚아챕니다.
        GameObject realWireInstance = GameObject.Find(uniqueAddress);
        if (realWireInstance != null)
        {
            // ❌ [치명적 버그 원인] wireTracker.SetupTrackingSlots 중복 호출 구역 제거!
            // 이미 위 SwapToRealSlots 내부에서 정석으로 데이터를 넘겼으므로 여기서 또 부르면 매개변수가 뒤틀립니다.

            // 🎯 5. 핀 클릭 주소 세팅 및 정식 가동 (ConnectWire 규격 연동)
            string startPath = GetDetailedPath(originSlotTransform);
            string endPath = GetDetailedPath(finalTargetSlot.transform);

            finalTargetSlot.ConnectWire(originSlotTransform.position, null, startPath, _myOriginSlot, realWireInstance);
            if (_myOriginSlot != null)
            {
                _myOriginSlot.ConnectWire(finalTargetSlot.transform.position, null, endPath, finalTargetSlot, realWireInstance);
            }
        }

        // 가상 찌꺼기 및 사용한 카드 데이터 정리
        GameObject startPinObj = GameObject.Find("Tracking_StartPin");
        if (startPinObj != null) Destroy(startPinObj);

        if (_myCanvasObject != null) Destroy(_myCanvasObject);
        else Destroy(gameObject);

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