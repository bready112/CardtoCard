using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CardDragHandler : CardDragHandlerBase
{
    private Vector3 _canvasOffset;

    [Header("📐 캔버스 확대 배율")]
    public float canvasScaleMultiplier = 1.15f;

    private Collider2D[] _childColliders;

    protected override void Awake()
    {
        base.Awake();
        _childColliders = GetComponentsInChildren<Collider2D>(true);
    }

    protected override void OnMouseDown() { }

    private void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TriggerRaycastCheckAndStartDrag();
        }

        if (IsDraggingNow && Mouse.current.leftButton.isPressed)
        {
            ExecuteDragTracking();
        }

        if (IsDraggingNow && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ResetToOriginalState();
        }
    }

    private void TriggerRaycastCheckAndStartDrag()
    {
        Vector2 mouseWindowPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));

        RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null)
            {
                if (hit.collider.gameObject.CompareTag("PlaySpace")) continue;

                // 🛑 [1단계 차단]: 클릭한 부품이 전선 슬롯("WirePin")이라면 드래그 연산 즉시 차단
                if (hit.collider.gameObject.CompareTag("WirePin"))
                {
                    Debug.Log($"🔌 [드래그 차단] 슬롯 [{hit.collider.gameObject.name}] 영역은 카드를 들 수 없습니다.");
                    continue;
                }

                // 🎯 [2단계 필터 - 질문자님 추가 요청 반영]: 
                // 내 패밀리 계층(IsChildOf)이 맞더라도, 레이저가 뚫고 지나간 그 구체적인 오브젝트의 태그가
                // "Card" 또는 "TheCard"가 아니라면 드래그 권한을 주지 않고 통과(Skip)시킵니다!
                string hitTag = hit.collider.gameObject.tag;
                if (hitTag != "Card" && hitTag != "TheCard")
                {
                    Debug.Log($"🙅‍♂️ [드래그 거부] 클릭된 오브젝트 [{hit.collider.gameObject.name}]의 태그가 '{hitTag}'이므로 드래그 대상이 아닙니다.");
                    continue;
                }

                // 위의 1, 2단계 보안 필터를 모두 통과한 정당한 카드 몸통일 때만 드래그 시작!
                if (hit.collider.transform.IsChildOf(this.transform) || hit.collider.gameObject == this.gameObject)
                {
                    Debug.Log($"🃏 [드래그 승인] 적격 카드 몸체 [{hit.collider.gameObject.name}]를 잡았습니다.");
                    HandleCardMouseDown();
                    break;
                }
            }
        }
    }

    public override void HandleCardMouseDown()
    {
        if (this.CompareTag("TheCard"))
        {
            IsDraggingNow = true;

            if (Mouse.current != null)
            {
                Vector2 mouseWindowPos = Mouse.current.position.ReadValue();
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));
                mouseWorldPos.z = 0f;

                _canvasOffset = this.transform.position - mouseWorldPos;
            }

            this.transform.localScale = Vector3.one * canvasScaleMultiplier;

            SetAllCollidersActive(false);


            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 🛑 [수정]: simulated를 false로 끄지 마세요! 물리를 켜두어야 뒷카드를 당깁니다.
                rb.simulated = true;

                // 🎯 [핵심]: 드래그 중에는 Kinematic(외부 물리 힘 무시, 오직 코드 이동)으로 전환합니다.
                rb.bodyType = RigidbodyType2D.Kinematic;

#if UNITY_2023_1_OR_NEWER
                rb.linearVelocity = Vector2.zero;
#else
                rb.velocity = Vector2.zero;
#endif
                rb.angularVelocity = 0f;
            }
        }

        IClickIt clickInterface = GetComponentInChildren<IClickIt>();
        if (clickInterface != null)
        {
            clickInterface.OnClickStart();
        }
    }

    private void ExecuteDragTracking()
    {
        Vector2 mouseWindowPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseWindowPos.x, mouseWindowPos.y, 10f));
        mouseWorldPos.z = 0f;

        this.transform.position = mouseWorldPos + _canvasOffset;
    }

    protected override void OnMouseDrag() { }

    public override void ResetToOriginalState()
    {
        IsDraggingNow = false;

        this.transform.localScale = Vector3.one;
        originalPosition = this.transform.position;

        StartCoroutine(ReleaseCardWithPhysicsDelay());
    }

    private IEnumerator ReleaseCardWithPhysicsDelay()
    {
        SetAllCollidersActive(true);

        Rigidbody2D rb = GetComponent<Rigidbody2D>() ?? GetComponentInChildren<Rigidbody2D>();
        if (rb != null)
        {
            // 🎯 [핵심]: 마우스를 놓았으니 다시 일반적인 물리 법칙(Dynamic)을 따르도록 복구합니다!
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;

#if UNITY_2023_1_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.angularVelocity = 0f;
        }

        yield return new WaitForSeconds(0.04f);

        IClickIt clickInterface = GetComponentInChildren<IClickIt>();
        if (clickInterface != null)
        {
            clickInterface.OnClickRelease();
        }

        CardStackHandler stackHandler = GetComponentInChildren<CardStackHandler>();
        if (stackHandler != null)
        {
            stackHandler.CheckAndTriggerStackOnDrop();
        }
    }

    private void SetAllCollidersActive(bool state)
    {
        if (_childColliders == null) return;
        foreach (Collider2D col in _childColliders)
        {
            if (col != null) col.enabled = state;
        }
    }
}