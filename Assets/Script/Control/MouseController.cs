using UnityEngine;

public class MouseController : MonoBehaviour
{
    // 🎯 어디서나 마우스 모드를 바꿀 수 있도록 싱글톤 세팅
    public static MouseController Instance { get; private set; }

    public enum MouseMode
    {
        Normal,      // 0: 평상시 모드 (무엇이든 클릭 가능)
        SlotOnly     // 1: 슬롯 제한 모드 (카드가 잠기는 모드)
    }

    [Header("⚙️ 현재 마우스 상태")]
    public MouseMode currentMode = MouseMode.Normal;

    // 📢 수신기(`CardColliderReceiver`)들에게 모드 변경을 알려줄 방송국(이벤트)
    public delegate void MouseModeChangedHandler(MouseMode newMode);
    public event MouseModeChangedHandler OnMouseModeChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 주석: 씬이 한 개여도 디버깅 및 에디터 안전장치로 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 🎯 외부에 모드 변경을 요청할 때 쓰는 함수
    /// </summary>
    public void SetMouseMode(MouseMode newMode)
    {
        currentMode = newMode;
        Debug.Log($"🖱️ [마우스 컨트롤러] 모드가 {newMode}로 변경되었습니다.");

        // 🔥 [라디오 방송 송출] 이 모드를 수신 대기 중인 모든 카드 수신기들에게 주파수를 쏩니다.
        OnMouseModeChanged?.Invoke(newMode);
    }
}