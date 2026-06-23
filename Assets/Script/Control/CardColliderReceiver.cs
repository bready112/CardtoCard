using UnityEngine;

public class CardColliderReceiver : MonoBehaviour
{
    private Collider2D _myCollider;

    [Header("📡 실시간 상태 모니터링")]
    public bool isColliderEnabled;

    private void Awake()
    {
        _myCollider = GetComponent<Collider2D>();
        if (_myCollider != null) isColliderEnabled = _myCollider.enabled;
    }

    /// <summary>
    /// 🎯 외부에서 이 함수를 직통으로 호출해서 콜라이더를 끄고 켭니다!
    /// </summary>
    public void SetColliderActive(bool active)
    {
        if (_myCollider == null) _myCollider = GetComponent<Collider2D>();

        if (_myCollider != null)
        {
            _myCollider.enabled = active;
            isColliderEnabled = active; // 인스펙터 시각화용
            Debug.Log($"📡 [직통 수신기] {gameObject.name}의 콜라이더가 직접 제어되었습니다. (Enabled = {active})");
        }
    }
}