using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환을 위해 필수
using TMPro; // 글자(TextMeshPro) 변경을 위해 필수

public class MainMenuController : MonoBehaviour
{
    [Header("Settings UI Panel")]
    [SerializeField] private GameObject settingsPanel; // 설정 창 오브젝트

    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "GameplayScene"; // 게임 화면 씬 이름

    [Header("UI Text References")]
    // 실시간으로 번역이 필요한 텍스트 컴포넌트들을 연결합니다.
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text startButtonText;
    [SerializeField] private TMP_Text settingsButtonText;
    [SerializeField] private TMP_Text exitButtonText;
    [SerializeField] private TMP_Text languageButtonText;

    [SerializeField] private TMP_Text settingsTitleText;
    [SerializeField] private TMP_Text settingsCloseButtonText;

    // 현재 언어 상태 (true = 한국어, false = 영어)
    private bool isKorean = true;

    void Start()
    {
        // 시작 시 설정창은 비활성화
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // 최초 진입 시 설정된 언어에 맞춰 UI 텍스트 초기화
        UpdateLanguageUI();
    }

    // 1. 게임 시작 버튼 기능
    public void ClickStartGame()
    {
        Debug.Log("게임을 시작합니다. 로딩 씬: " + gameplaySceneName);
        SceneManager.LoadScene(gameplaySceneName);
    }

    // 2. 설정 패널 켜기/끄기
    public void OpenSettingsPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // 3. 언어 토글 기능 (클릭 시 작동)
    public void ToggleLanguage()
    {
        isKorean = !isKorean; // 상태 반전
        UpdateLanguageUI();   // 화면 글자 싹 바꾸기
    }

    // 4. 언어 상태에 맞춰 UI 글자를 바꿔주는 핵심 번역 로직
    private void UpdateLanguageUI()
    {
        if (isKorean)
        {
            if (titleText != null) titleText.text = "카드 & 디펜스 왕국";
            if (startButtonText != null) startButtonText.text = "게임 시작";
            if (settingsButtonText != null) settingsButtonText.text = "설정";
            if (exitButtonText != null) exitButtonText.text = "게임 종료";
            if (languageButtonText != null) languageButtonText.text = "KO / EN (한국어)";

            if (settingsTitleText != null) settingsTitleText.text = "게임 설정";
            if (settingsCloseButtonText != null) settingsCloseButtonText.text = "닫기";
        }
        else
        {
            if (titleText != null) titleText.text = "CARD & DEFENSE KINGDOM";
            if (startButtonText != null) startButtonText.text = "START GAME";
            if (settingsButtonText != null) settingsButtonText.text = "SETTINGS";
            if (exitButtonText != null) exitButtonText.text = "EXIT GAME";
            if (languageButtonText != null) languageButtonText.text = "KO / EN (English)";

            if (settingsTitleText != null) settingsTitleText.text = "SETTINGS";
            if (settingsCloseButtonText != null) settingsCloseButtonText.text = "CLOSE";
        }
    }

    // 5. 게임 종료
    public void ClickExitGame()
    {
        Debug.Log("게임을 종료합니다.");
        Application.Quit();
    }
}