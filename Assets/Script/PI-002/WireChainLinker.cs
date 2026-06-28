using UnityEngine;
using System.Collections.Generic;

public class WireChainLinker : MonoBehaviour
{
    [Header("🔗 전선 세그먼트 보관소 (Tem)")]
    public List<Transform> TemSegments = new List<Transform>();
    private const int segmentCount = 20;

    private LineRenderer _lineRenderer;
    private Rigidbody2D _currentStartBody;
    private Rigidbody2D _currentEndBody;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
    }

    /// <summary>
    /// 1단계: 드래그 시작 시, 오리지널 'Tem' 오브젝트 내부에 'Wire_Temp_Storage' 자식을 만들어 20개 마디를 넣습니다.
    /// </summary>
    public void LinkVirtualFirst(Transform startPin, Transform endPin, GameObject segmentPrefab)
    {
        _currentStartBody = startPin.GetComponent<Rigidbody2D>();
        _currentEndBody = endPin.GetComponent<Rigidbody2D>();

        // 🎯 1. 이미 맵에 생성되어 있는 오리지널 "Tem" 버킷을 찾습니다[cite: 2, 3].
        GameObject temBucket = GameObject.Find("Tem");
        if (temBucket == null)
        {
            temBucket = new GameObject("Tem");
        }

        // 🎯 2. [질문자님 오더 핵심 반영] Tem의 자식으로 들어갈 "Wire_Temp_Storage"를 생성합니다.
        GameObject wireStorage = new GameObject("Wire_Temp_Storage");
        wireStorage.transform.SetParent(temBucket.transform, false); // Tem 안에 Wire_Temp_Storage 넣기 완료!

        Vector3 startPos = startPin.position;
        Vector3 endPos = endPin.position;
        Rigidbody2D lastRigidbody = _currentStartBody;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);
            Vector3 spawnPos = Vector3.Lerp(startPos, endPos, t);

            // 🎯 3. [전선을 자식에 넣어줘] 20개 마디선은 'wireStorage(Wire_Temp_Storage)'의 자식으로 들어갑니다.
            GameObject segmentObj = Instantiate(segmentPrefab, spawnPos, Quaternion.identity, wireStorage.transform);
            segmentObj.name = $"Wire_Segment_{i}"; 

            Rigidbody2D currentRb = segmentObj.GetComponent<Rigidbody2D>(); 
            if (currentRb == null) 
            {
                currentRb = segmentObj.AddComponent<Rigidbody2D>(); 
            }

            currentRb.bodyType = RigidbodyType2D.Dynamic; 
            currentRb.gravityScale = 0.5f; 
            currentRb.linearDamping = 3f; 
            currentRb.angularDamping = 3f; 

            // [시작선 고정] 무적의 안전장치 0번 마디 ➔ StartPin
            if (i == 0) 
            {
                FixedJoint2D fixedJoint = segmentObj.GetComponent<FixedJoint2D>(); 
                if (fixedJoint == null) 
                {
                    fixedJoint = segmentObj.AddComponent<FixedJoint2D>(); 
                }

                fixedJoint.autoConfigureConnectedAnchor = false; 
                fixedJoint.connectedBody = _currentStartBody; 
                fixedJoint.anchor = Vector2.zero; 
                fixedJoint.connectedAnchor = Vector2.zero; 
            }
            // [중간선 고정] 힌지 사슬 엮기
            else 
            {
                HingeJoint2D hinge = segmentObj.GetComponent<HingeJoint2D>(); 
                if (hinge == null) 
                {
                    hinge = segmentObj.AddComponent<HingeJoint2D>(); 
                }

                hinge.autoConfigureConnectedAnchor = false; 
                hinge.connectedBody = lastRigidbody; 

                Vector2 localAnchor = segmentObj.transform.InverseTransformPoint(spawnPos); 
                Vector2 connectedLocalAnchor = lastRigidbody.transform.InverseTransformPoint(spawnPos); 
                hinge.anchor = localAnchor; 
                hinge.connectedAnchor = connectedLocalAnchor; 
            }

            TemSegments.Add(segmentObj.transform); 
            lastRigidbody = currentRb; 
        }

        FixedJoint2D endFixed = endPin.GetComponent<FixedJoint2D>(); 
        if (endFixed == null) 
        {
            endFixed = endPin.gameObject.AddComponent<FixedJoint2D>(); 
        }
        endFixed.autoConfigureConnectedAnchor = false; 

        if (TemSegments.Count > 0 && TemSegments[TemSegments.Count - 1] != null) 
        {
            endFixed.connectedBody = TemSegments[TemSegments.Count - 1].GetComponent<Rigidbody2D>(); 
            endFixed.anchor = Vector2.zero; 
            endFixed.connectedAnchor = Vector2.zero; 
        }
    }

    /// <summary>
    /// 2단계: 🎯 [질문자님 오더 100% 정방향 반영] 
    /// Wire_Segment_0 ➔ Output 슬롯 정보 주입 / Wire_Segment_19 ➔ Input 슬롯 정보 주입
    /// </summary>
    public void SwapToRealSlots(Transform realOutputSlot, Transform realInputSlot, string uniqueAddress)
    {
        // 1. 최상위 버킷인 "All_Line" 폴더를 씬에서 검색하거나 생성합니다.
        GameObject allLineBucket = GameObject.Find("All_Line");
        if (allLineBucket == null)
        {
            allLineBucket = new GameObject("All_Line");
        }

        // 2. Tem 폴더 밑에 있던 전선 바구니를 All_Line 폴더 밑으로 이관하고 진짜 주소로 이름 변경
        GameObject wireStorage = GameObject.Find("Wire_Temp_Storage");
        if (wireStorage != null)
        {
            wireStorage.transform.SetParent(allLineBucket.transform, false);
            wireStorage.name = uniqueAddress;

            // 진짜 전선 오브젝트 본체에 완공 사령탑(RealWireTracker) 컴포넌트 장착[cite: 1]
            RealWireTracker realTracker = wireStorage.GetComponent<RealWireTracker>() ?? wireStorage.AddComponent<RealWireTracker>();
            if (realTracker != null)
            {
                realTracker.SetupTrackingSlots(realOutputSlot, realInputSlot, uniqueAddress, this); 
            }

            // LineRenderer 매니저 본체도 계승된 오브젝트 자식으로 정렬
            transform.SetParent(wireStorage.transform, false);
        }

        // 🧲 진짜 물리 슬롯들의 리지드바디 확보 및 Kinematic 설정
        Rigidbody2D outputRb = realOutputSlot.GetComponent<Rigidbody2D>() ?? realOutputSlot.gameObject.AddComponent<Rigidbody2D>(); 
        Rigidbody2D inputRb = realInputSlot.GetComponent<Rigidbody2D>() ?? realInputSlot.gameObject.AddComponent<Rigidbody2D>(); 
        outputRb.bodyType = RigidbodyType2D.Kinematic; 
        inputRb.bodyType = RigidbodyType2D.Kinematic; 

        // ====================================================================
        // 🎯 [오더 반영 1] Wire_Segment_0에는 output슬롯 정보를 넣어줘
        // ➔ 0번 마디 본체(Wire_Segment_0)의 조인트 타겟을 진짜 'Output 슬롯'으로 설정!
        // ====================================================================
        if (TemSegments.Count > 0 && TemSegments[0] != null) 
        {
            FixedJoint2D startFixed = TemSegments[0].GetComponent<FixedJoint2D>(); 
            if (startFixed == null)
            {
                startFixed = TemSegments[0].gameObject.AddComponent<FixedJoint2D>();
            }

            // 자동 안커 해제 및 타겟 리지드바디를 진짜 'Output 슬롯'으로 완벽 주입!
            startFixed.autoConfigureConnectedAnchor = false; 
            startFixed.connectedBody = outputRb; 
            startFixed.anchor = Vector2.zero; 
            startFixed.connectedAnchor = Vector2.zero; 
        }

        // ====================================================================
        // 🎯 [오더 반영 2] Wire_Segment_19정보을 input 슬롯에 넣어주고
        // ➔ 19번 마디 본체(Wire_Segment_19)에 조인트를 달아 진짜 'Input 슬롯' 정보를 주입!
        // ====================================================================
        if (TemSegments.Count > 19 && TemSegments[19] != null)
        {
            // 19번 마디가 스스로 고정될 수 있도록 FixedJoint2D를 확보하거나 생성합니다.
            FixedJoint2D endFixed = TemSegments[19].GetComponent<FixedJoint2D>();
            if (endFixed == null)
            {
                endFixed = TemSegments[19].gameObject.AddComponent<FixedJoint2D>();
            }

            // 자동 안커 해제 및 타겟 리지드바디를 진짜 'Input 슬롯'으로 완벽 주입!
            endFixed.autoConfigureConnectedAnchor = false;
            endFixed.connectedBody = inputRb;
            endFixed.anchor = Vector2.zero;
            endFixed.connectedAnchor = Vector2.zero;
        }

        // 완공 후 전선 마디들의 중력 복구 (찰랑거리게)
        foreach (var seg in TemSegments) 
        {
            if (seg != null) 
            {
                Rigidbody2D rb = seg.GetComponent<Rigidbody2D>(); 
                if (rb != null) rb.gravityScale = 1.0f; 
            }
        }
    }

    private void FixedUpdate()
    {
        if (_lineRenderer == null || TemSegments.Count == 0) return;

        // 20개 마디선을 순서대로 그리기 위해 카운트를 일치시킵니다.
        _lineRenderer.positionCount = TemSegments.Count;
        
        for (int i = 0; i < TemSegments.Count; i++)
        {
            if (TemSegments[i] != null)
            {
                Vector3 pos = TemSegments[i].position;
                pos.z = -1f; // Z축 평면 고정[cite: 14]
                _lineRenderer.SetPosition(i, pos);
            }
        }
    }

    public List<Transform> tempSegments => TemSegments; 
}