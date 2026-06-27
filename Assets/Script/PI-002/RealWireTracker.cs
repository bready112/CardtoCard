using UnityEngine;
using System.Collections.Generic;

public class RealWireTracker : MonoBehaviour
{
    private Transform _startSlot;
    private Transform _endSlot;
    private LineRenderer _lineRenderer;
    private Collider2D _collider;

    [Header("🧲 유니티 2D 물리 로프 설정")]
    public GameObject ropeSegmentPrefab;
    public int segmentCount = 12;

    private List<Transform> _ropeSegments = new List<Transform>();

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _collider = GetComponent<Collider2D>();

        if (_collider == null) _collider = gameObject.AddComponent<EdgeCollider2D>();
        _collider.isTrigger = true;

        if (_lineRenderer != null) _lineRenderer.textureMode = LineTextureMode.RepeatPerSegment;
    }

    public void SetupTrackingSlots(Transform startSlot, Transform endSlot, string cardName)
    {
        _startSlot = startSlot;
        _endSlot = endSlot;
        GeneratePhysicsRopeChain();
    }

    private void GeneratePhysicsRopeChain()
    {
        if (_startSlot == null || _endSlot == null) return;

        // 🎯 [인스펙터 누락 방어] 만약 슬롯이 비어있으면 Resources 폴더에서 강제 수혈
        if (ropeSegmentPrefab == null)
        {
            ropeSegmentPrefab = Resources.Load<GameObject>("RopeSegment");
            if (ropeSegmentPrefab == null)
            {
                Debug.LogError("🚨 Resources 폴더 안에 'RopeSegment' 프리팹 파일이 없습니다!");
                return;
            }
        }

        _ropeSegments.Clear();

        Vector3 startPos = _startSlot.position;
        Vector3 endPos = _endSlot.position;

        // 1. 출발지 슬롯 2D 고정 (Kinematic)
        Rigidbody2D lastRigidbody = _startSlot.GetComponent<Rigidbody2D>();
        if (lastRigidbody == null)
        {
            lastRigidbody = _startSlot.gameObject.AddComponent<Rigidbody2D>();
        }
        lastRigidbody.bodyType = RigidbodyType2D.Kinematic;

        // 2. 토막 생성 및 2D 힌지 결속
        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);
            Vector3 spawnPos = Vector3.Lerp(startPos, endPos, t);

            GameObject segmentObj = Instantiate(ropeSegmentPrefab, spawnPos, Quaternion.identity, this.transform);
            segmentObj.name = $"Wire2D_Segment_{i}";
            segmentObj.layer = LayerMask.NameToLayer("Wire");

            // 전부 2D 부품으로 철저하게 강제 세팅
            Rigidbody2D currentRb = segmentObj.GetComponent<Rigidbody2D>();
            if (currentRb == null) currentRb = segmentObj.AddComponent<Rigidbody2D>();

            // 뱀처럼 묵직하게 질질 끌리도록 드래그(항력) 브레이크 떡칠
            currentRb.linearDamping = 8f;
            currentRb.angularDamping = 5f;

            HingeJoint2D hinge = segmentObj.GetComponent<HingeJoint2D>();
            if (hinge == null) hinge = segmentObj.AddComponent<HingeJoint2D>();

            if (hinge != null && lastRigidbody != null)
            {
                hinge.connectedBody = lastRigidbody;
                hinge.anchor = Vector2.zero;
                hinge.connectedAnchor = Vector2.zero;
            }

            _ropeSegments.Add(segmentObj.transform);
            lastRigidbody = currentRb;
        }

        // 3. 목적지 슬롯 2D 연결 (에러가 나던 94번째 줄 원천 개조 구간)
        Rigidbody2D endRb = _endSlot.GetComponent<Rigidbody2D>();
        if (endRb == null)
        {
            endRb = _endSlot.gameObject.AddComponent<Rigidbody2D>();
        }
        endRb.bodyType = RigidbodyType2D.Kinematic;

        HingeJoint2D endHinge = _endSlot.GetComponent<HingeJoint2D>();
        if (endHinge == null) endHinge = _endSlot.gameObject.AddComponent<HingeJoint2D>();

        if (endHinge != null && _ropeSegments.Count > 0)
        {
            // 2D 리지드바디끼리 완벽하게 차원 통일 매칭 완료!
            endHinge.connectedBody = _ropeSegments[_ropeSegments.Count - 1].GetComponent<Rigidbody2D>();
            endHinge.anchor = Vector2.zero;
            endHinge.connectedAnchor = Vector2.zero;
        }
    }

    private void FixedUpdate()
    {
        if (_lineRenderer == null || _ropeSegments.Count == 0) return;

        _lineRenderer.positionCount = _ropeSegments.Count;
        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            if (_ropeSegments[i] != null)
            {
                Vector3 pos = _ropeSegments[i].position;
                pos.z = -1f; // 2D 렌더링 순서 보장
                _lineRenderer.SetPosition(i, pos);
            }
        }
    }

    public void ExecuteWireDemolish(Transform clickedSlot)
    {
        Vector3 spawnPos = transform.position;
        if (clickedSlot != null)
        {
            if (clickedSlot == _startSlot && _endSlot != null) spawnPos = _endSlot.position;
            else if (clickedSlot == _endSlot && _startSlot != null) spawnPos = _startSlot.position;
        }
        spawnPos.z = 0f;

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

        if (CardSpawnManager.Instance != null)
        {
            CardSpawnManager.Instance.SpawnCard("PI-002", spawnPos);
        }

        Destroy(gameObject);
    }
}