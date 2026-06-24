using UnityEngine;

public static class WireConnectionValidator
{
    // [규칙 2번] 선의 최대 허용 거리 설정
    private static float _maxWireDistance = 300.0f;

    // 🎯 EndPin이 이 값을 실시간으로 똑같이 읽어갈 수 있도록 통로를 열어줍니다.
    public static float MaxWireDistance => _maxWireDistance;

    public static bool IsConnectionValid(WireSlot startSlot, WireSlot targetSlot)
    {
        if (startSlot == null || targetSlot == null) return false;

        float currentDistance = Vector3.Distance(startSlot.transform.position, targetSlot.transform.position);
        if (currentDistance > _maxWireDistance)
        {
            Debug.LogWarning($"🚫 [규칙 위반] 선의 최대 거리 초과! (허용: {_maxWireDistance}m / 현재: {currentDistance:F1}m)");
            return false;
        }

        float startY = startSlot.transform.position.y;
        float targetY = targetSlot.transform.position.y;

        if (startSlot.slotType == WireSlot.SlotType.Input)
        {
            if (targetY < startY)
            {
                Debug.LogWarning($"🚫 [규칙 위반] Input 시작: 출발지 Y축({startY})보다 아래에 있는 슬롯({targetY})은 연결 불가!");
                return false;
            }
        }

        if (startSlot.slotType == WireSlot.SlotType.Output)
        {
            if (targetY > startY)
            {
                Debug.LogWarning($"🚫 [규칙 위반] Output 시작: 출발지 Y축({startY})보다 위에 있는 슬롯({targetY})은 연결 불가!");
                return false;
            }
        }

        if (startSlot.slotType == targetSlot.slotType)
        {
            Debug.LogWarning($"🚫 [규칙 위반] 동일한 타입끼리는 연결할 수 없습니다! ({startSlot.slotType} ↔ {targetSlot.slotType})");
            return false;
        }

        Debug.Log("🎯 [규칙 최종 통과] 모든 제약 조건을 만족하여 배선 계약을 승인합니다.");
        return true;
    }
}