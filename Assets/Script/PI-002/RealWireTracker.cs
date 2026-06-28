using UnityEngine;

public class RealWireTracker : MonoBehaviour
{
    private Transform _startSlot;
    private Transform _endSlot;
    private LineRenderer _lineRenderer;
    private Collider2D _collider;

    private string _connectedCardName;
    private WireChainLinker _attachedLinker;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _collider = GetComponent<Collider2D>();

        if (_collider == null)
        {
            _collider = gameObject.AddComponent<EdgeCollider2D>();
        }
        _collider.isTrigger = true;
    }

    /// <summary>
    /// 🔌 EndPin이 연결 성공 후 진짜 슬롯 정보들과 함께, 기존에 쓰던 20개 선 정보(linker)를 통째로 주입해 주는 곳입니다.
    /// </summary>
    public void SetupTrackingSlots(Transform startSlot, Transform endSlot, string cardName, WireChainLinker linker)
    {
        _startSlot = startSlot;
        _endSlot = endSlot;
        _connectedCardName = cardName;
        _attachedLinker = linker;

        if (_attachedLinker != null)
        {
            _attachedLinker.transform.SetParent(this.transform);
        }

        string startPath = _startSlot != null ? GetHierarchyPath(_startSlot) : "❌ NULL (비어있음)";
        string endPath = _endSlot != null ? GetHierarchyPath(_endSlot) : "❌ NULL (비어있음)";

        Debug.LogWarning($"📡 [RealWireTracker] 가상선 재사용 및 진짜 슬롯 결속 최종 완료!\n" +
                         $"➡️ 출발지 정보(Output): {startPath}\n" +
                         $"➡️ 목적지 정보(Input): {endPath}");
    }
    // ====================================================================
    // 🎯 [LineRenderer 순서 꼬임 및 콜라이더 완벽 해결 버전]
    // ====================================================================
    private void Update()
    {
        // 양쪽 슬롯이 정상적으로 존재할 때만 연산합니다.
        if (_startSlot == null || _endSlot == null) return;

        // ❌ [삭제] _lineRenderer.positionCount = 2 로 덮어쓰던 코드를 완벽히 도려냈습니다!
        // 이제 20개 마디선(WireChainLinker)이 순서대로 찰랑거리며 제 형태를 유지합니다.

        // 2. EdgeCollider2D 물리선 포지션 실시간 동기화[cite: 18]
        EdgeCollider2D edgeCollider = _collider as EdgeCollider2D;
        if (edgeCollider != null)
        {
            // 내 본체 오브젝트의 월드 포지션을 출발 슬롯 위치와 일치시킵니다.
            transform.position = _startSlot.position;

            // 로컬 상의 시작점은 원점(0,0)입니다.
            Vector2 localStart = Vector2.zero;

            // 로컬 끝점은 목적지 슬롯 월드 좌표에서 내 본체 월드 좌표를 빼서 오차 없이 일치시킵니다.
            Vector2 localEnd = (Vector2)_endSlot.position - (Vector2)transform.position;

            Vector2[] points = new Vector2[2] { localStart, localEnd };
            edgeCollider.points = points;
        }
    }

    private string GetHierarchyPath(Transform trans)
    {
        string p2 = (trans.parent != null && trans.parent.parent != null) ? trans.parent.parent.name : "Root";
        string p1 = (trans.parent != null) ? trans.parent.name : "Sub";
        string me = trans.name;
        return $"{p2} -> {p1} -> {me}";
    }

    /// <summary>
    /// 🔌 WirePinClicker.cs가 호출하는 철거 메서드
    /// </summary>
    public void ExecuteWireDemolish(Transform clickedSlot)
    {
        Debug.Log($"🔌 [RealWireTracker] 철거 신호 접수. 반대편 위치를 계산합니다.");

        Vector3 spawnPos = transform.position;

        if (clickedSlot != null)
        {
            if (clickedSlot == _startSlot && _endSlot != null)
            {
                spawnPos = _endSlot.position;
            }
            else if (clickedSlot == _endSlot && _startSlot != null)
            {
                spawnPos = _startSlot.position;
            }
        }
        else
        {
            if (_endSlot != null) spawnPos = _endSlot.position;
            else if (_startSlot != null) spawnPos = _startSlot.position;
        }

        spawnPos.z = 0f;

        // 양쪽 슬롯 데이터 리셋 및 카드 부활 처리
        if (_startSlot != null)
        {
            WireSlot startSlotScript = _startSlot.GetComponent<WireSlot>();
            if (startSlotScript != null) startSlotScript.ResetSlotData();
        }

        if (_endSlot != null)
        {
            WireSlot endSlotScript = _endSlot.GetComponent<WireSlot>();
            if (endSlotScript != null) endSlotScript.ResetSlotData();
        }

        // 철거 시 반대편에 깨끗하게 카드 부활
        if (CardSpawnManager.Instance != null)
        {
            CardSpawnManager.Instance.SpawnCard("PI-002", spawnPos);
        }

        // 내 본체와 내부에 재사용 중이던 20개 전선 마디들까지 깔끔하게 통째로 파괴
        Destroy(gameObject);
    }
}