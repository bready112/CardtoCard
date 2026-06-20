using UnityEngine;
using System.Collections.Generic;

public class CardSpawnManager : MonoBehaviour
{
    public static CardSpawnManager Instance;

    private Dictionary<string, CardData> _cardDataLibrary = new Dictionary<string, CardData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadBurnedAssetsToMemory();
    }

    private void LoadBurnedAssetsToMemory()
    {
        CardData[] loadedCards = Resources.LoadAll<CardData>("Cards");
        foreach (CardData card in loadedCards)
        {
            if (card != null && !_cardDataLibrary.ContainsKey(card.id))
            {
                _cardDataLibrary.Add(card.id, card);
            }
        }
        Debug.Log($"🎉 [데이터 로드] {_cardDataLibrary.Count}개의 에셋 파일을 라이브러리에 보존했습니다.");
    }

    /// <summary>
    /// ID를 치면 에셋(.asset)에 등록된 프리팹을 가져와 스폰하고 고유 시간 명찰을 부여합니다.
    /// </summary>
    public void SpawnCard(string cardId, Vector3 position)
    {
        if (!_cardDataLibrary.ContainsKey(cardId))
        {
            Debug.LogError($"❌ 구워진 데이터 에셋 중 '{cardId}'는 존재하지 않습니다!");
            return;
        }

        CardData dataFile = _cardDataLibrary[cardId];
        GameObject targetPrefab = dataFile.category;

        if (targetPrefab == null)
        {
            Debug.LogError($"❌ 에러: '{cardId}' 에셋 내부 마스터칸에 프리팹이 등록되어 있지 않습니다!");
            return;
        }

        // 1. 프리팹 실물 스폰
        GameObject newCard = Instantiate(targetPrefab, position, Quaternion.identity);

        // ====================================================================
        // 🎯 [★ CardTem 고유 시간 명찰 시스템]
        // ====================================================================
        System.DateTime now = System.DateTime.Now;
        string uniqueTimeStamp = now.ToString("HHmmssfff");
        newCard.name = $"{dataFile.id}_{uniqueTimeStamp}";

        // ====================================================================
        // 🎯 [★ 신규 기획: CardTem 부모 그룹으로 자동 정돈 엔진]
        // ====================================================================
        // 하이어라키 창에서 "CardTem"이라는 이름의 부모 폴더(오브젝트)를 찾습니다.
        GameObject cardTemGroup = GameObject.Find("CardTem");

        // 만약 씬에 CardTem이 없다면, 코드가 자동으로 새로 하나 만듭니다!
        if (cardTemGroup == null)
        {
            cardTemGroup = new GameObject("CardTem");
        }

        // 스폰된 카드의 부모를 CardTem으로 지정하여 안으로 쏙 집어넣습니다.
        newCard.transform.SetParent(cardTemGroup.transform);
        // ====================================================================

        // 2. 데이터 세부 스펙 완전 동기화
        CardController controller = newCard.GetComponent<CardController>();
        if (controller != null)
        {
            controller.cardData = dataFile;
            Debug.Log($"🧬 [CardTem 안착 완공] 유니크 이름 [{newCard.name}]이 CardTem 그룹 폴더 안으로 정돈되었습니다.");
        }
    }
}