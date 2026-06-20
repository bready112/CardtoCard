using UnityEngine;

public class CardController : MonoBehaviour
{
    // [★ 완벽 해결] '= new CardData()'를 지우고 선언만 남겨둡니다.
    // 이제 CardSpawnManager가 구워진 데이터 에셋(.asset)의 링크를 이 칸에 직접 꽂아줄 것입니다.
    public CardData cardData;

    private void Start()
    {
        // 데이터가 정상적으로 이식되었는지 안전하게 검증하는 디버그 로그 (선택사항)
        if (cardData != null)
        {
            Debug.Log($"🃏 [{gameObject.name}] '{cardData.cardName}' 카드 컨트롤러가 정상 가동 중입니다.");
        }
    }
}