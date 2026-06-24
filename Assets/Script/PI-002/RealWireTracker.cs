using UnityEngine;
using UnityEngine.InputSystem;

public class RealWireTracker : MonoBehaviour
{
    private Transform _startSlot;
    private Transform _endSlot;
    private LineRenderer _lineRenderer;
    private Collider2D _collider; // 선분을 감싸는 충돌체 (Trigger)
  

    private string _connectedCardName;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _collider = GetComponent<Collider2D>();

        // 🎯 안전장치: 만약 프리팹에 콜라이더가 없다면 자동으로 생성해 줍니다.
        if (_collider == null)
        {
            _collider = gameObject.AddComponent<EdgeCollider2D>();
        }
        _collider.isTrigger = true;
    }

    public void SetupTrackingSlots(Transform startSlot, Transform endSlot, string cardName)
    {
        _startSlot = startSlot;
        _endSlot = endSlot;
        _connectedCardName = cardName;

        // ====================================================================
        // 🎯 [디버그 수색 엔진 주입] 들어온 슬롯의 세부 경로를 콘솔에 찍어봅니다.
        // ====================================================================
        string startPath = _startSlot != null ? GetHierarchyPath(_startSlot) : "❌ NULL (비어있음)";
        string endPath = _endSlot != null ? GetHierarchyPath(_endSlot) : "❌ NULL (비어있음)";

        Debug.LogWarning($"📡 [RealWireTracker 디버그] 전선({gameObject.name})에 슬롯 주소가 주입되었습니다!\n" +
                         $"➡️ 출발지 슬롯(_startSlot): {startPath}\n" +
                         $"➡️ 목적지 슬롯(_endSlot): {endPath}");
        // ====================================================================

        UpdateWirePositions();
        UpdateColliderTopology();
    }

    /// <summary>
    /// 🔍 슬롯의 '부모.부모.나' 계층 주소를 안전하게 뽑아내 주는 디버그용 유틸 함수
    /// </summary>
    private string GetHierarchyPath(Transform trans)
    {
        string p2 = (trans.parent != null && trans.parent.parent != null) ? trans.parent.parent.name : "Root";
        string p1 = (trans.parent != null) ? trans.parent.name : "Sub";
        string me = trans.name;

        return $"{p2} -> {p1} -> {me}";
    }

    private void LateUpdate()
    {
        // 매 프레임마다 슬롯들의 최신 위치를 실시간으로 선에 반영합니다!
        UpdateWirePositions();

        // 선이 움직였으므로 마우스 조준용 콜라이더 뼈대도 실시간으로 리모델링합니다.
        UpdateColliderTopology();
    }




    private void UpdateWirePositions()
    {
        if (_startSlot == null || _endSlot == null || _lineRenderer == null) return;

        Vector3 p0 = _startSlot.position;
        Vector3 p1 = _endSlot.position;
        p0.z = -1f;
        p1.z = -1f;

        _lineRenderer.SetPosition(0, p0);
        _lineRenderer.SetPosition(1, p1);
    }

    // 🎯 선의 시작점과 끝점에 맞게 콜라이더 영역을 실시간으로 동기화합니다.
    private void UpdateColliderTopology()
    {
        if (_startSlot == null || _endSlot == null) return;

        if (_collider is EdgeCollider2D edge)
        {
            Vector2[] points = new Vector2[2];
            points[0] = transform.InverseTransformPoint(_startSlot.position);
            points[1] = transform.InverseTransformPoint(_endSlot.position);
            edge.points = points;
            edge.edgeRadius = 0.1f; // 마우스로 조준하기 쉽게 두께를 살짝 줍니다.
        }
    }


    /// <summary>
    /// 🔌 2번 규칙: 핀에게 클릭된 슬롯 정보를 받아, 반대편 슬롯 위치에 카드를 부활시킵니다.
    /// </summary>
    public void ExecuteWireDemolish(Transform clickedSlot)
    {
        Debug.Log($"🔌 [2단계: 선 사령탑] 자해 신호 접수. 클릭된 슬롯의 반대편 위치를 계산합니다.");

        // ====================================================================
        // 🎯 [반대편 슬롯 판정 엔진] 클릭당한 곳의 반대편 위치를 정확히 타겟팅합니다.
        // ====================================================================
        Vector3 spawnPos = transform.position;

        if (clickedSlot != null)
        {
            // 클릭된 곳이 출발지 슬롯이라면? ➡️ 카드는 반대편인 목적지(_endSlot)에 부활!
            if (clickedSlot == _startSlot && _endSlot != null)
            {
                spawnPos = _endSlot.position;
                Debug.Log($"📡 [반대편 판정] 출발지 슬롯이 클릭되었으므로 카드는 반대편 [목적지 슬롯] 자리에 생성됩니다.");
            }
            // 클릭된 곳이 목적지 슬롯이라면? ➡️ 카드는 반대편인 출발지(_startSlot)에 부활!
            else if (clickedSlot == _endSlot && _startSlot != null)
            {
                spawnPos = _startSlot.position;
                Debug.Log($"📡 [반대편 판정] 목적지 슬롯이 클릭되었으므로 카드는 반대편 [출발지 슬롯] 자리에 생성됩니다.");
            }
        }
        else
        {
            // 예외 방지용 안전장치 기본값
            if (_endSlot != null) spawnPos = _endSlot.position;
            else if (_startSlot != null) spawnPos = _startSlot.position;
        }

        spawnPos.z = 0f;

        // ====================================================================
        // ① 양쪽 슬롯에게 자해 신호 전파 (순서 유지)
        // ====================================================================
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

        // ====================================================================
        // ② 위에서 계산된 '반대편 위치'에 깨끗한 PI-002 카드 생성
        // ====================================================================
        if (CardSpawnManager.Instance != null)
        {
            CardSpawnManager.Instance.SpawnCard("PI-002", spawnPos);
        }

        // ③ 선 소멸
        Destroy(gameObject);
    }
}