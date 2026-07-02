using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Networking;

/// <summary>
/// JSON 데이터를 저장/로드하는 제네릭 정적 유틸리티.
/// StreamingAssets 동기 로드, PersistentDataPath 저장/로드, UnityWebRequest 비동기 로드,
/// Addressables 비동기 로드를 지원한다.
/// </summary>
public static class JsonManager<T>
{
    public static bool SaveGame(T data, string saveFileName)
    {
        string filePath = Path.Combine(Application.persistentDataPath, saveFileName);
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
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
            T data = JsonConvert.DeserializeObject<T>(json);

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
            T data = JsonConvert.DeserializeObject<T>(jsonText);
            return data;
        }
        else
        {
            Debug.LogError("파일을 찾을 수 없습니다: " + filePath);
            return default(T);
        }
    }

    // JsonManager에 비동기 버전 추가
    public static async UniTask<T> LoadAsync<T>(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

        using var req = UnityWebRequest.Get("file://" + path);

        await req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"로드 실패: {fileName} / {req.error}");
            return default;
        }

        return JsonConvert.DeserializeObject<T>(req.downloadHandler.text);
    }

    public static async UniTask<T> LoadDataAsync<T>(string addressableKey)
    {
        var handle = Addressables.LoadAssetAsync<TextAsset>(addressableKey);
        TextAsset textAsset = await handle.ToUniTask();

        if (textAsset == null)
        {
            Debug.LogError($"어드레서블 로드 실패: {addressableKey}");
            return default;
        }

        T data = JsonConvert.DeserializeObject<T>(textAsset.text);

        Addressables.Release(handle);

        return data;
    }
}