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

    private void OnMouseDown()
    {
        if (!isOccupied)
        {
            EndPin trackingEndPin = FindFirstObjectByType<EndPin>();
            if (trackingEndPin != null) trackingEndPin.ProcessClickConnectionWithTarget(this);
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

            // ====================================================================
            // 🎯 [연결 고리 주입] 켜지는 자식 핀에게 선 사령탑(Tracker) 주소를 다이렉트로 박아줍니다!
            // ====================================================================
            WirePinClicker pinClicker = _currentActivePinVisual.GetComponent<WirePinClicker>();
            RealWireTracker trackerScript = wireInstance.GetComponent<RealWireTracker>();
            if (pinClicker != null && trackerScript != null)
            {
                pinClicker.SetupDirectWireReference(trackerScript);
            }
        }
    }

    // ❌ Update() 및 PassPinClickToWire() 등 신호를 쏘거나 토스하는 구역은 완전히 삭제되었습니다!
    // 슬롯은 절대로 신호를 먼저 보내지 않습니다.

    /// <summary>
    /// 📱 수신 전용 구역: 오직 선 사령탑의 명령을 '받기만' 하여 데이터를 리셋하고 핀을 숨깁니다.
    /// </summary>
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

        enabled = true;
        Debug.Log($"🎨 [슬롯 수신 세척완료] {gameObject.name} 슬롯이 선의 자해 명령을 받아 완벽하게 청소되었습니다.");
    }

    public void LinkMasterCardCanvas(GameObject canvasObj) { _masterCardCanvas = canvasObj; }
    public void RestoreMasterCard() { if (_masterCardCanvas != null) _masterCardCanvas.SetActive(true); ResetSlotData(); }
    public GameObject GetMasterCardObject() { return _masterCardCanvas; }
}