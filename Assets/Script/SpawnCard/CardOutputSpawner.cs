using UnityEngine;
using System.Collections;

public class CardOutputSpawner : MonoBehaviour
{
    private CardController _cardController;
    private bool _isOutputting = false;

    private void Awake()
    {
        _cardController = GetComponent<CardController>();
    }

    // WireZoneManager에서 빨랫줄 연결 대성공 신호를 보내면 이 메서드를 실행합니다!
    public void StartProduction(Transform targetDestination)
    {
        if (!_isOutputting)
        {
            _isOutputting = true;
            // 기획하신 작업 배율과 시간을 계산해서 루프(코루틴)를 돌립니다.
            StartCoroutine(ProductionLoop(targetDestination));
        }
    }

    private IEnumerator ProductionLoop(Transform destination)
    {
        CardData data = _cardController.cardData;

        while (_isOutputting && data.gathering > 0)
        {
            // 1. 기획서에 적힌 시간만큼 대기 (예: 3초)
            yield return new WaitForSeconds(data.possessionTime / data.workScale);

            // 2. outputPercent를 계산해서 스폰할 재료 카드를 결정
            Debug.Log($"📦 [{data.cardName}]에서 {data.outputPercent} 규칙에 따라 재료를 생산했습니다!");

            // 3. 자원 카드 프리팹을 내 위치에 뿅 생성
            // GameObject materialCard = Instantiate(자원프리팹, transform.position, ...);

            // 4. [이전 단계 연결] 빨랫줄을 타고 목적지(destination)까지 실시간 이동 연출 시작!
            // materialCard.GetComponent<MaterialFlyer>().FlyTo(destination);
        }
    }
}