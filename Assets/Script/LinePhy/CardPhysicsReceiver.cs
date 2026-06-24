using UnityEngine;

public class CardPhysicsReceiver : MonoBehaviour
{
    private Rigidbody2D _rb;
    private SpringJoint2D _spring;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _spring = GetComponent<SpringJoint2D>();

        // 태어날 때는 고무줄을 꺼둡니다.
        if (_spring != null)
        {
            _spring.enabled = false;
        }
    }

    /// <summary>
    /// 📡 [수신]: 상대방 카드의 Rigidbody를 받아 내 스프링 고무줄을 연결하고 활성화합니다.
    /// </summary>
    public void ActivatePhysicsChain(Rigidbody2D targetRB)
    {
        if (_spring != null && targetRB != null)
        {
            _spring.connectedBody = targetRB;

            // ====================================================================
            // 🧲 [물리 쇠사슬 앵커 제로화] 
            // 연결 지점을 각 카드의 정중앙(0, 0)으로 강제 일치시킵니다.
            // 이 설정을 해야 두 카드가 멀어졌을 때 고무줄이 늘어나며 쫀득하게 잡아당깁니다!
            // ====================================================================
            _spring.anchor = Vector2.zero;
            _spring.connectedAnchor = Vector2.zero;
            // ====================================================================

            _spring.enabled = true;
            Debug.Log($"⛓️ [물리 수신기] {gameObject.name} 카드가 {targetRB.name} 카드와 고무줄로 묶였습니다!");
        }
    }

    /// <summary>
    /// 📡 [수신]: 선이 끊어지면 고무줄을 즉시 해제하고 비활성화합니다.
    /// </summary>
    public void DeactivatePhysicsChain()
    {
        if (_spring != null)
        {
            _spring.connectedBody = null;
            _spring.enabled = false;
            Debug.Log($"✂️ [물리 수신기] {gameObject.name} 카드의 고무줄 결속이 완벽히 풀렸습니다.");
        }
    }

    /// <summary>
    /// 사령탑들이 내 몸뚱이 주소를 긁어갈 수 있도록 오픈하는 창구
    /// </summary>
    public Rigidbody2D GetRigidbody() => _rb;
}