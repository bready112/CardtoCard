using UnityEngine;
using TMPro; // 🎯 TextMeshPro 입력창을 쓰기 위해 필수 장착!

public class DebugCardSpawner : MonoBehaviour
{
    [Header("🔗 유니티 에디터에서 Input Field를 연결해 주세요")]
    public TMP_InputField cardIdInputField;

    [Header("📍 카드가 생성될 디폴트 좌표 (필요시 수정)")]
    public Vector3 defaultSpawnPosition = Vector3.zero;

    /// <summary>
    /// 🎯 버튼을 누르거나 엔터를 쳤을 때 실행할 마법의 함수!
    /// </summary>
    public void SpawnTypedCard()
    {
        // 1. 입력창이나 공장이 없으면 안전하게 리턴
        if (cardIdInputField == null || CardSpawnManager.Instance == null)
        {
            Debug.LogError("❌ UI 입력창(InputField)이 연결되지 않았거나 CardSpawnManager가 없습니다!");
            return;
        }

        // 2. 입력창에 적힌 텍스트를 공백 없이 깔끔하게 긁어옵니다. (예: "PI-002")
        string typedCardId = cardIdInputField.text.Trim();

        // 3. 텍스트가 비어있으면 실행 안 함
        if (string.IsNullOrEmpty(typedCardId))
        {
            Debug.LogWarning("⚠️ 카드 ID를 입력하지 않았습니다!");
            return;
        }

        // 4. 🎯 [완성된 공장 가동] 질문자님이 직접 타이핑한 ID 그대로 원하는 위치에 한 줄로 스폰!
        CardSpawnManager.Instance.SpawnCard(typedCardId, defaultSpawnPosition);

        Debug.Log($"📟 [디버그 스폰] 입력된 코드 '{typedCardId}' 기반으로 카드를 소환했습니다.");

        // 5. (선택 사항) 다음 입력을 편하게 하도록 입력창을 깨끗하게 비워줍니다.
        cardIdInputField.text = "";
    }
}