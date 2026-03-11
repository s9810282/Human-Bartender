using System.IO;
using UnityEngine;

public static class JsonManager<T>
{
    public static bool SaveGame(T data, string saveFileName)
    {
        string filePath = Path.Combine(Application.persistentDataPath, saveFileName);
        string json = JsonUtility.ToJson(data, true);

        // 3. 파일 쓰기
        File.WriteAllText(filePath, json);

        Debug.Log($"저장 완료! 경로: {filePath}");

        return true;
    }
    public static T LoadGameData_PersistentDataPath(string fileName)
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            T data = JsonUtility.FromJson<T>(json);

            Debug.Log("데이터 로드 성공!");
            return data;
        }
        else
        {
            Debug.LogWarning("저장된 파일이 없습니다. 기본 데이터를 생성합니다.");
            return default(T);
        }
    }
    public static T LoadGameData_StreamingAssets(string fileName = "")
    {
        // 1. 경로 설정 (StreamingAssets 혹은 PersistentDataPath)
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);

        if (File.Exists(filePath))
        {
            string jsonText = File.ReadAllText(filePath);
            T data = JsonUtility.FromJson<T>(jsonText);
            return data;
        }
        else
        {
            Debug.LogError("파일을 찾을 수 없습니다: " + filePath);
            return default(T);
        }
    }
}