using UnityEngine;

public static class TimeHelper
{
    /// <summary>
    /// 🎯 [★ 쪼개기 핵심] 매 프레임 시간이 줄어들거나(카운트다운) 차오르는 연산을 대신해주는 함수
    /// </summary>
    /// <param name="currentValue">현재 시간 변수 (ref를 쓰면 원본 데이터가 실시간으로 바뀝니다)</param>
    /// <param name="targetValue">목표 수치 (0초 혹은 최대 시간)</param>
    /// <param name="isCountingDown">true면 카운트다운, false면 카운트업(게이지 충전)</param>
    /// <return>목표치에 도달했다면 true를 반환</return>
    public static bool ProgressTime(ref float currentValue, float targetValue, bool isCountingDown)
    {
        if (isCountingDown)
        {
            // 시간이 0을 향해 줄어듦
            currentValue -= Time.deltaTime;
            if (currentValue <= targetValue)
            {
                currentValue = targetValue;
                return true; // 카운트다운 완료!
            }
        }
        else
        {
            // 게이지가 최대치를 향해 차오름
            currentValue += Time.deltaTime;
            if (currentValue >= targetValue)
            {
                currentValue = targetValue;
                return true; // 게이지 충전 완료!
            }
        }
        return false; // 아직 진행 중
    }
}