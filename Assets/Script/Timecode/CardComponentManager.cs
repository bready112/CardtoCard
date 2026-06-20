using System.Collections.Generic;
using UnityEngine;

public class CardComponentManager : MonoBehaviour
{
    [Header("🔒 드래그 중 일시정지할 스크립트들")]
    public List<MonoBehaviour> targetScripts = new List<MonoBehaviour>();

    [Header("⏳ 딜레이업 및 크기 연출 설정")]
    [Tooltip("마우스를 놓은 후, 스크립트가 켜지고 크기가 줄어들 때까지의 대기 시간")]
    public float activationDelay = 0.3f;
    [Tooltip("클릭해서 들고 있을 때 커질 비율 (1.15 = 15% 커짐)")]
    public float dragScaleMultiplier = 1.15f;

    private Vector3 _originalScale;
    private Coroutine _activeCoroutine;

    private void Awake()
    {
        // 게임 시작 시 카드의 순수한 원래 크기를 딱 기억해둡니다.
        _originalScale = transform.localScale;
    }

    /// <summary>
    /// 🎯 [사령탑 제어 함수] 드래그 시작/종료에 따라 크기와 스크립트를 일괄 통제
    /// </summary>
    public void ToggleCardScripts(bool isActive)
    {
        if (isActive)
        {
            // 마우스를 놓았을 때: 즉시 줄어들지 않고, 질문자님의 기획대로 딜레이 후 복귀!
            StartDelayActivation();
        }
        else
        {
            // 마우스를 눌렀을 때: 그 즉시 스크립트를 끄고 크기를 들어 올립니다!
            StopExistingDelay();
            SetScriptsEnabledState(false);

            // ⚡ [매니저 권한] 내 카드만 정확하게 크기를 키웁니다.
            transform.localScale = _originalScale * dragScaleMultiplier;
        }
    }

    private void StartDelayActivation()
    {
        StopExistingDelay();
        _activeCoroutine = StartCoroutine(DelayActivationRoutine());
    }

    private System.Collections.IEnumerator DelayActivationRoutine()
    {
        // 설정한 딜레이 시간(0.3초) 동안은 커진 상태와 스크립트 OFF 상태를 유지합니다.
        yield return new WaitForSeconds(activationDelay);

        // ⏳ 딜레이업 완료! 이제 안전하게 원래 크기로 줄이고 스크립트를 깨웁니다.
        transform.localScale = _originalScale;
        SetScriptsEnabledState(true);

        _activeCoroutine = null;
    }

    private void StopExistingDelay()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }
    }

    private void SetScriptsEnabledState(bool isEnabled)
    {
        foreach (var script in targetScripts)
        {
            if (script != null)
            {
                script.enabled = isEnabled;
            }
        }
    }

    /// <summary>
    /// 강제 복귀나 외부 예외 상황 시 크기를 원상태로 돌려놓는 안전장치
    /// </summary>
    public void ResetScaleToOriginal()
    {
        StopExistingDelay();
        transform.localScale = _originalScale;
        SetScriptsEnabledState(true);
    }
}