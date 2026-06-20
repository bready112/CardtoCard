using UnityEngine;
using UnityEngine.InputSystem; // New Input System을 사용하기 위한 필수 선언

public class CameraController : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 50f;
    public float minZoom = 210f;
    public float maxZoom = 800f;

    private Camera cam;
    private Vector3 dragOrigin;

    // 캔버스의 전체 가로/세로 크기 (기본값 설정)
    [Header("Canvas Total Size")]


    public float canvasWidth = 3000f;
    public float canvasHeight = 1800f;



    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        // 마우스가 연결되어 있지 않다면 에러 방지
        if (Mouse.current == null) return;

        PanCamera();
        ZoomCamera();
    }

    void LateUpdate()
    {
        // 1. 카메라 크기(줌) 제한 (100 ~ 165)
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);

        // 2. 현재 카메라의 가로/세로 절반 크기 계산
        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;

        // 3. 카메라가 움직일 수 있는 최소/최대 X, Y 좌표 계산
        // (중심이 0,0일 때 캔버스 경계를 넘지 않도록 제한)
        float minX = -(canvasWidth / 2f) + camHalfWidth;
        float maxX = (canvasWidth / 2f) - camHalfWidth;
        float minY = -(canvasHeight / 2f) + camHalfHeight;
        float maxY = (canvasHeight / 2f) - camHalfHeight;

        // 4. 만약 카메라 크기가 너무 커서 제한 범위가 뒤집히는 것 방지
        if (minX > maxX) { minX = maxX = 0f; }
        if (minY > maxY) { minY = maxY = 0f; }

        // 5. 현재 카메라 위치 제한 적용 (Z축은 유지)
        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        float clampedY = Mathf.Clamp(transform.position.y, minY, maxY);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }





    // 마우스 우클릭으로 카메라를 X, Y축으로 이동
    private void PanCamera()
    {
        // 우클릭을 처음 누른 순간 (기존: Input.GetMouseButtonDown)
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            // Mouse.current.position.ReadValue()로 현재 마우스 화면 좌표를 가져옴
            dragOrigin = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        }

        // 우클릭을 누르고 있는 동안 (기존: Input.GetMouseButton)
        if (Mouse.current.rightButton.isPressed)
        {
            Vector3 currentMousePos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector3 difference = dragOrigin - currentMousePos;

            cam.transform.position += difference;
        }
    }

    // 마우스 휠로 줌인/아웃
    private void ZoomCamera()
    {
        // 휠 스크롤 Y축 값 가져오기
        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll != 0.0f)
        {
            // 주의: New Input System의 휠 값은 120, -120 같이 매우 큰 단위로 들어옵니다.
            // 따라서 기존처럼 부드럽게 줌을 하려면 값을 많이 낮춰주어야 합니다. (0.01 곱하기)
            cam.orthographicSize -= scroll * zoomSpeed;

            // 줌 크기 제한
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }
}