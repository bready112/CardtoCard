using UnityEngine;
using UnityEditor;
using System.IO;

public class CsvToScriptableObject : EditorWindow
{
    [MenuItem("Tools/CSV to CardData Assets Burner")]
    public static void ShowWindow()
    {
        GetWindow<CsvToScriptableObject>("Data Burner");
    }

    private void OnGUI()
    {
        GUILayout.Label("CSV -> ScriptableObject 굽기 엔진", EditorStyles.boldLabel);

        if (GUILayout.Button("144개 데이터 에셋 파일로 굽기 시작"))
        {
            string path = "Assets/Resources/CardData.csv";
            BurnCsvToAssets(path);
        }
    }

    public static void BurnCsvToAssets(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"❌ CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        // 저장될 폴더 자동 생성 확인
        string dirPath = "Assets/Resources/Cards";
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

        string[] lines = File.ReadAllLines(csvPath);
        int burnCount = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i])) continue;

            string[] row = lines[i].Split(',');
            if (row.Length < 3) continue;

            string cardId = row[0].Trim();
            if (string.IsNullOrEmpty(cardId)) continue;

            // 이미 구워진 파일이 있으면 불러오고, 없으면 새로 만듭니다 (데이터 덮어쓰기 최적화)
            string assetPath = $"{dirPath}/{cardId}.asset";
            CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);

            bool isNew = false;
            if (cardData == null)
            {
                cardData = ScriptableObject.CreateInstance<CardData>();
                isNew = true;
            }

            // 엑셀 열 데이터 정밀 이식
            cardData.id = cardId;
            cardData.cardName = row[1].Trim();

            // ====================================================================
            // 🎯 [★ string -> GameObject 매칭 핵심 구역]
            // 엑셀 Category 칸에 적힌 프리팹 이름을 가져와서 실제 프로젝트 안의 에셋을 조준합니다.
            // ====================================================================
            string prefabName = row[2].Trim();

            // 💡 [!] 만약 카드 프리팹들이 'Assets/Prefabs/'가 아닌 다른 폴더에 들어있다면, 
            // 아래 경로("Assets/Prefabs/")를 실제 프리팹이 모여있는 폴더 경로로 꼭 맞춰주세요!
            string totalPrefabPath = $"Assets/Prefabs/{prefabName}.prefab";
            GameObject foundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(totalPrefabPath);

            if (foundPrefab != null)
            {
                // 성공적으로 실물 프리팹을 찾았다면 GameObject형 category 변수에 완벽 이식!
                cardData.category = foundPrefab;
            }
            else
            {
                // 엑셀에 오타가 났거나 경로가 틀렸을 때 명확하게 범인을 짚어주는 방어 로그
                Debug.LogWarning($"⚠️ [공정 경고] {cardId} 카드를 굽는 중, 엑셀에 적힌 '{prefabName}' 프리팹을 [{totalPrefabPath}] 경로에서 찾을 수 없어 널(Null) 처리되었습니다.");
                cardData.category = null;
            }
            // ====================================================================

            float.TryParse(row[3], out cardData.hp);
            cardData.isPermanence = (row[4].Trim() == "True" || row[4].Trim() == "1");
            cardData.beTrash = (row[5].Trim() == "True" || row[5].Trim() == "1");
            int.TryParse(row[6], out cardData.trashPower);
            float.TryParse(row[7], out cardData.possessionTime);
            float.TryParse(row[8], out cardData.workScale);
            float.TryParse(row[9], out cardData.powerConsumption);
            cardData.recipe = row[10].Trim();
            cardData.cardSlot = row[11].Trim();
            cardData.yesBox = row[12].Trim();
            cardData.isConsumable = (row[13].Trim() == "True" || row[13].Trim() == "1");
            cardData.isTransitive = (row[14].Trim() == "True" || row[14].Trim() == "1");
            cardData.inputMaterial = row[15].Trim();
            cardData.outputMaterial = row[16].Trim();
            int.TryParse(row[17], out cardData.gathering);
            if (row.Length > 18) cardData.outputPercent = row[18].Trim();

            // 유니티 데이터 파일로 파일 저장 시스템 가동
            if (isNew)
            {
                AssetDatabase.CreateAsset(cardData, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(cardData);
            }
            burnCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"🔥 [굽기 완료] 총 {burnCount}개의 카드 데이터 파일이 '{dirPath}'에 안전하게 보존되었습니다!");
    }
}