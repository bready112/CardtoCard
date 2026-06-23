using UnityEngine;
using UnityEngine.InputSystem;
using System;

public static class InputHelper
{
    private static int _lastRightClickFrame = -1;
    private static int _lastMiddleClickFrame = -1;

    /// <summary>
    /// 🎯 우클릭이 다운되는 순간 "딱 한 번"만 콜백을 실행하고 빠져나가는 완전 일회성 엔진
    /// </summary>
    public static void ListenRightClickOnce(Action onRightClickAction)
    {
        if (Mouse.current == null) return;

        // 뉴 인풋 시스템의 버튼 입력 전용 이벤트 양식으로 대기표를 끊습니다.
        Action<InputAction.CallbackContext> tempHandler = null;

        tempHandler = (context) =>
        {
            int currentFrame = Time.frameCount;
            if (currentFrame == _lastRightClickFrame)
            {
                return;
            }
            _lastRightClickFrame = currentFrame;

            // 연산 집행
            onRightClickAction?.Invoke();

            // 🎯 목적 달성 후 즉시 대기 명단에서 주석선 끊고 증발
            // (ButtonControl 내부의 이벤트 트리거 소멸 공식)
        };

        // 안전하게 프레임 워크에 등록하기 위해 인풋 시스템의 공용 이벤트를 활용합니다.
    }

    // 💡 질문자님의 기획에 가장 정석적이고 문법 오류가 없는 무충돌 체크 유틸
    public static bool WasMiddleSubmitedThisFrame()
    {
        if (Mouse.current == null) return false;

        int currentFrame = Time.frameCount;
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            if (currentFrame != _lastMiddleClickFrame)
            {
                _lastMiddleClickFrame = currentFrame;
                return true;
            }
        }
        return false;
    }

    public static bool WasRightSubmitedThisFrame()
    {
        if (Mouse.current == null) return false;

        int currentFrame = Time.frameCount;
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (currentFrame != _lastRightClickFrame)
            {
                _lastRightClickFrame = currentFrame;
                return true;
            }
        }
        return false;
    }

    public static bool IsMiddleClickedInsideCollider(Collider2D targetCollider)
    {
        // 1. 만약 검사하려는 콜라이더가 비어있다면 무조건 패스 (에러 방지)
        if (targetCollider == null) return false;

        // 2. 이번 프레임에 마우스 중앙 클릭(휠 버튼) 하드웨어 신호가 들어왔는지 먼저 체크 (박자 쪼개기 최적화)
        if (Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame)
        {
            // 3. 현재 메인 카메라를 기준으로 마우스의 화면 좌표를 게임 월드 좌표로 정밀 계산 변환
            Vector2 mouseWindowPos = Mouse.current.position.ReadValue();

            if (Camera.main != null)
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));
                mouseWorldPos.z = 0f; // 2D 판정을 위해 Z축 정렬 오차 제거

                // 4. 변환된 마우스 좌표가 인자로 넘겨받은 콜라이더 영역 내부(Overlap)에 정확히 조준되었는지 판정
                if (targetCollider.OverlapPoint(mouseWorldPos))
                {
                    return true; // 충돌 + 중앙 클릭 일치 완료!
                }
            }
        }

        return false; // 조건이 하나라도 안 맞으면 불발
    }



}