using UnityEngine;

[CreateAssetMenu(fileName = "NewCardData", menuName = "Card System/Advanced Card Data")]
public class CardData : ScriptableObject
{
    [Header("기본 정보")]
    public string id;                      // ID: 고유 키
    public string cardName;                // 이름 (Name)
    public GameObject category;                // 분류 (Category): 유니티 오브젝트 카드 종류

    [Header("체력 및 지속성 관련")]
    public float hp;                       // HP/Durability: 카드 체력 또는 내구도
    public bool isPermanence;              // Permanence: 시간에 의해 사라지지 않는다
    public bool beTrash;                   // BeTrash/Tombstone: 파괴 시 쓰레기가 된다
    public int trashPower;                 // TrashPower: 버리면 채워지는 힌트 보너스 점수

    [Header("시간 및 작업 연산")]
    public float possessionTime;           // possession time: 기본 시간
    public float workScale;                // work scale: 작업 배수
    public float powerConsumption;         // Power Consumption/Fuel: 연료 소모량

    [Header("상호작용 및 조합")]
    public string recipe;                  // Recipe: 필요한 혼합 재료
    public string cardSlot;                // CardSlot: 슬롯 장착 조건
    public string yesBox;                  // YesBox: 내부 상자 진입 여부
    public bool isConsumable;              // IsConsumable: 소모성 여부
    public bool isTransitive;              // IsTransitive: 이동 가능 여부

    [Header("전선 및 자원 아웃풋 기믹")]
    public string inputMaterial;           // InputMaterial: 전선 흡입 재료
    public string outputMaterial;          // OutputMaterial: 전선 배출 재료
    public int gathering;                  // Gathering: 채집 가능 횟수
    [TextArea(2, 5)]
    public string outputPercent;           // Output(%): 확률 및 기본 재료
}