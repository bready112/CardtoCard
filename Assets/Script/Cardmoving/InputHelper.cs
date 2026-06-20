using UnityEngine;
using UnityEngine.InputSystem;

public static class InputHelper
{
    /// <summary>
    /// 🎯 [★ 쪼개기 핵심] 특정 콜라이더 구역 위에서 마우스 우클릭이 단독으로 딱 눌렸는지 판정해 주는 만능 함수
    /// </summary>
    public static bool IsRightClicked(this Collider2D collider, Camera targetCamera = null)
    {
        // 1. 콜라이더가 없거나 마우스가 연결 안 되어 있으면 무조건 탈락
        if (collider == null || Mouse.current == null) return false;

        // 2. 이번 프레임에 우클릭이 정확히 눌렸는가?
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            // 카메라가 지정되지 않았다면 기본 메인 카메라를 씁니다.
            Camera cam = targetCamera ?? Camera.main;
            if (cam == null) return false;

            // 3. 마우스 화면 좌표를 게임 속 2D 월드 좌표로 변환
            Vector2 mouseWindowPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));

            // 4. 마우스 좌표가 해당 콜라이더 영역 안에 겹쳐(Overlap) 있는지 최종 판정!
            return collider.OverlapPoint(mouseWorldPos);
        }

        return false;
    }
}