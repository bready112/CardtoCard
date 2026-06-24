using UnityEngine;

public class WireSlot : MonoBehaviour
{
    public enum SlotType { Input, Output }

    [Header("⚙️ 슬롯 설정")]
    public SlotType slotType;
    public bool isOccupied = false;

    [Header("🎨 핀 구분 명찰과 1:1 매칭될 핀 비주얼 오브젝트들")]
    public GameObject pinVisual_Pin1;
    public GameObject pinVisual_Pin2;
    public GameObject pinVisual_Pin3;

    [Header("🔗 연결 정보 및 실물 선 오브젝트")]
    [SerializeField] private WireSlot _connectedSlot;
    [SerializeField] private GameObject _spawnedWireInstance;

    private Collider2D _myCollider;
    private GameObject _masterCardCanvas;
    private GameObject _currentActivePinVisual;

    [Header("현재 이 슬롯에 연결되어 뻗어나간 실제 전선 오브젝트")]
    [SerializeField] private GameObject _connectedWireInstance;

    public string connectedSlotDetailedPath;
    private WireSlot _connectedTargetSlotScript;

    [Header("--- [독립 주소 시스템] ---")]
    [SerializeField] private Transform _myParentCard;
    [SerializeField] private string _myParentCardName;

    private void Awake()
    {
        _myCollider = GetComponent<Collider2D>();

        if (pinVisual_Pin1 != null) pinVisual_Pin1.SetActive(false);
        if (pinVisual_Pin2 != null) pinVisual_Pin2.SetActive(false);
        if (pinVisual_Pin3 != null) pinVisual_Pin3.SetActive(false);

        if (transform.parent != null && transform.parent.parent != null)
        {
            _myParentCard = transform.parent.parent;
            _myParentCardName = _myParentCard.name;
        }
    }

    public string GetMyParentCardName() { return _myParentCardName; }

    private void OnMouseUp()
    {
        // 이미 선이 꼽혀있지 않은 깨끗한 슬롯일 때만 반응합니다.
        if (!isOccupied)
        {
            // 현재 화면에 돌아다니고 있는 가상핀(EndPin)을 수색합니다.
            EndPin trackingEndPin = FindFirstObjectByType<EndPin>();

            if (trackingEndPin != null)
            {
                // 가상핀이 존재한다면, 이 슬롯을 목적지로 삼아 최종 배선 완공을 명령합니다!
                trackingEndPin.ProcessClickConnectionWithTarget(this);
                Debug.Log($"🔌 [WireSlot] {gameObject.name} 슬롯 위에서 마우스 업! 완공 프로세스를 호출합니다.");
            }
        }
    }

    public void ConnectWire(Vector3 targetPos, GameObject prefab, string detailedPath, WireSlot targetSlotScript, GameObject wireInstance)
    {
        this.connectedSlotDetailedPath = detailedPath;
        this._connectedTargetSlotScript = targetSlotScript;
        this._connectedWireInstance = wireInstance;
        this.isOccupied = true;

        string wireName = wireInstance != null ? wireInstance.name : "";

        if (wireName.Contains("Card1") || detailedPath.Contains("Card1")) _currentActivePinVisual = pinVisual_Pin1;
        else if (wireName.Contains("Card2") || detailedPath.Contains("Card2")) _currentActivePinVisual = pinVisual_Pin2;
        else if (wireName.Contains("Card3") || detailedPath.Contains("Card3")) _currentActivePinVisual = pinVisual_Pin3;
        else _currentActivePinVisual = pinVisual_Pin1;

        if (_currentActivePinVisual != null)
        {
            Vector3 pinLocalPos = _currentActivePinVisual.transform.localPosition;
            pinLocalPos.z = -1f;
            _currentActivePinVisual.transform.localPosition = pinLocalPos;

            _currentActivePinVisual.SetActive(true);

            WirePinClicker pinClicker = _currentActivePinVisual.GetComponent<WirePinClicker>();
            RealWireTracker trackerScript = wireInstance.GetComponent<RealWireTracker>();
            if (pinClicker != null && trackerScript != null)
            {
                pinClicker.SetupDirectWireReference(trackerScript);
            }
        }
    }

    public void ResetSlotData()
    {
        isOccupied = false;
        _connectedSlot = null;
        _spawnedWireInstance = null;
        _connectedWireInstance = null;
        _connectedTargetSlotScript = null;
        connectedSlotDetailedPath = "";

        if (_currentActivePinVisual != null)
        {
            _currentActivePinVisual.SetActive(false);
            _currentActivePinVisual = null;
        }

        // ====================================================================
        // 📡 [수신기 해제 신호 송신 - 보정 완료] 안전한 부모 카드 수색 구조
        // ====================================================================
        CardPhysicsReceiver myReceiver = (_myParentCard != null)
            ? _myParentCard.GetComponent<CardPhysicsReceiver>()
            : GetComponentInParent<CardPhysicsReceiver>();

        if (myReceiver != null)
        {
            myReceiver.DeactivatePhysicsChain();
        }
        // ====================================================================

        enabled = true;
    }

    public void LinkMasterCardCanvas(GameObject canvasObj) { _masterCardCanvas = canvasObj; }
    public void RestoreMasterCard() { if (_masterCardCanvas != null) _masterCardCanvas.SetActive(true); ResetSlotData(); }
    public GameObject GetMasterCardObject() { return _masterCardCanvas; }
}