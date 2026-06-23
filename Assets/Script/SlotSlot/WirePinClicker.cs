using UnityEngine;

public class WirePinClicker : MonoBehaviour
{
    private Collider2D _myCollider;

    // 🎯 [다이렉트 연결 생명선] 슬롯을 거치지 않고 선을 직접 조준합니다.
    private RealWireTracker _myConnectedWireTracker;

    private void Awake()
    {
        _myCollider = GetComponent<Collider2D>() ?? gameObject.AddComponent<PolygonCollider2D>();
        _myCollider.isTrigger = true;
    }

    /// <summary>
    /// 🔌 선이 연결되는 최종 시점에 슬롯이 이 핀을 켜면서 선의 주소를 다이렉트로 박아줍니다.
    /// </summary>
    public void SetupDirectWireReference(RealWireTracker tracker)
    {
        _myConnectedWireTracker = tracker;
    }

    private void Update()
    {
        // 핀이 활성화되어 있을 때만 매 프레임 독립적으로 휠 클릭을 감시합니다.
        CheckMiddleClickOnPin();
    }

    private void CheckMiddleClickOnPin()
    {
        // 내 핀 이미지 영역 안에서 휠 클릭이 일어났는가?
        if (InputHelper.IsMiddleClickedInsideCollider(_myCollider))
        {
            if (_myConnectedWireTracker != null)
            {
                // 내 위에 박혀있는 진짜 주인 슬롯의 Transform을 가져옵니다.
                Transform mySlotTransform = GetComponentInParent<WireSlot>().transform;

                Debug.LogWarning($"🎨 [1단계: 핀 ➡️ 선] 핀 본체({gameObject.name})가 내 슬롯 위치를 담아 선에게 자해 명령을 송신합니다.");

                // 💥 [수정] 선에게 "내가 이 슬롯에서 신호 보낸다" 하고 주소를 넘겨줍니다.
                _myConnectedWireTracker.ExecuteWireDemolish(mySlotTransform);
            }
        }
    }
}