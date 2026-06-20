using UnityEngine;
using TMPro; // TextMeshPro를 제어하기 위해 반드시 필요한 주소록이야!
using System; // 컴퓨터의 현재 시간을 알려주는 DateTime을 쓰기 위해 필요해!

public class InGameClock : MonoBehaviour
{
    // 유니티 인스펙터창에서 글 상자를 연결해줄 슬롯이야.
    [SerializeField] private TextMeshProUGUI clockText;

    private void Update()
    {
        // 매 프레임마다 작동하면 컴퓨터가 힘들어하니까, 안전장치를 걸어두거나 
        // 그냥 단순하게 현재 시간을 예쁜 텍스트 형식으로 계속 업데이트해줄게!
        if (clockText != null)
        {
            // DateTime.Now는 내 컴퓨터의 실시간 현재 시간을 뜻해.
            // "HH:mm:ss"는 [시(24시간 포맷):분:초] 형태로 글자를 구워내라는 뜻이야!
            string currentTime = DateTime.Now.ToString("HH:mm:ss");

            // 최종적으로 UI 글상자에 ⏰ 아이콘과 함께 띄워줘.
            clockText.text = $"{currentTime}";
        }
    }
}