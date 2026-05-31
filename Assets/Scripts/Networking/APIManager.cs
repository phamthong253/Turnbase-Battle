using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class PlayerResponse
{
    public int id;
    public string displayName;
    public int exp;
    public int crystals;
    public int stamina;
    public int level;
}

public class EquipItemRequest
{
    public int playerId;
    public string unitID;
    public string itemType;
    public string itemID;
    public int slotIndex;
}

public class UnEquipItemRequest
{
    public int playerId;
    public string unitID;
    public string itemType;
    public string itemID;
    public int slotIndex;
}

public class MapProgressRequest
{
    public int playerId;
    public int stageId;
    public double progress;
    public int? stars;
    public bool isCompleted;
    public bool incrementAttempt = true;
}

[System.Serializable]
public class MapProgressDTO
{
    public int id;
    public int playerId;
    public int stageId;
    public bool isCompleted;
    public int? stars;
    public double progress;
    public int attempts;
    public string completedAt;
    public string lastUpdated;
}

public class GachaRollRequest
{
    public int playerId;
    public string bannerId;
    public int rollCount;
}

[System.Serializable]
public class GachaRewardResponse
{
    public string rewardType;
    public string id;
    public int quantity;
    public bool isNew;
}

[System.Serializable]
public class GachaRollResponse
{
    public int crystals;
    public List<GachaRewardResponse> rewards = new List<GachaRewardResponse>();
    public List<PlayerUnitData> units = new List<PlayerUnitData>();
    public List<PlayerItemData> inventory = new List<PlayerItemData>();
}

public class APIManager : MonoBehaviour
{
    public static APIManager Instance;
    private string baseUrl = "https://localhost:7122/api";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    public void GetPlayer(int id, Action<PlayerResponse> onSuccess, Action<string> onError)
    {
        StartCoroutine(SendGetRequest($"Player/{id}", onSuccess, onError));
    }

    public void SaveUnit(PlayerUnitData data, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(SendPostRequest("save", data, onSuccess, onError));
    }

    public void LoadInventoryForPlayer(int playerId, Action<List<PlayerItemData>> onSuccess, Action<string> onError)
    {
        StartCoroutine(SendGetRequest($"Inventory/{playerId}", onSuccess, onError));
    }

    public void LoadUnitsForPlayer(int playerId, Action<List<PlayerUnitData>> onSuccess, Action<string> onError)
    {
        StartCoroutine(SendGetRequest($"PlayerUnit/{playerId}", onSuccess, onError));
    }

    public void EquipItem(int playerId, string unitID, string itemType, string itemID, int slotIndex, Action<string> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(unitID))
        {
            onError?.Invoke("unitID is null or empty");
            return;
        }
        if (string.IsNullOrEmpty(itemID))
        {
            onError?.Invoke("itemID is null or empty");
            return;
        }

        var req = new EquipItemRequest
        {
            playerId = playerId,
            unitID = unitID,
            itemType = itemType,
            itemID = itemID,
            slotIndex = slotIndex
        };

        StartCoroutine(SendPostRequest<string, EquipItemRequest>("PlayerUnit/equip", req, onSuccess, onError));
    }

    public void UnEquipItem(int playerId, string unitId, string itemId, string itemType, int slotIndex, Action<string> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(unitId))
        {
            onError?.Invoke("unitId is null or empty");
            return;
        }
        if (string.IsNullOrEmpty(itemId))
        {
            onError?.Invoke("itemId is null or empty");
            return;
        }

        var req = new UnEquipItemRequest
        {
            playerId = playerId,
            unitID = unitId,
            itemID = itemId,
            itemType = itemType,
            slotIndex = slotIndex
        };

        StartCoroutine(SendPostRequest<string, UnEquipItemRequest>("PlayerUnit/unequip", req, onSuccess, onError));
    }

    public void LoadMapProgressForPlayer(int playerId, Action<List<MapProgressDTO>> onSuccess, Action<string> onError)
    {
        StartCoroutine(SendGetRequest($"MapProgress/{playerId}", onSuccess, onError));
    }

    public void SaveMapProgressForPlayer(int playerId, int stageId, double progress, bool isCompleted, int stars, Action<string> onSuccess, Action<string> onError)
    {
        var progressData = new MapProgressRequest
        {
            playerId = playerId,
            stageId = stageId,
            progress = progress,
            isCompleted = isCompleted,
            stars = stars,
            incrementAttempt = true
        };
        Debug.Log($"[API] SaveMapProgressForPlayer: {playerId}, Stage: {stageId}, Progress: {progress}, Stars: {stars}, Completed: {isCompleted}");

        StartCoroutine(SendPostRequest<string, MapProgressRequest>("MapProgress/upsert", progressData, onSuccess, onError));
    }

    public void RollGacha(int playerId, string bannerId, int rollCount, Action<GachaRollResponse> onSuccess, Action<string> onError)
    {
        var request = new GachaRollRequest
        {
            playerId = playerId,
            bannerId = string.IsNullOrEmpty(bannerId) ? "standard" : bannerId,
            rollCount = rollCount
        };

        StartCoroutine(SendPostRequest<GachaRollResponse, GachaRollRequest>("Gacha/roll", request, onSuccess, onError));
    }

    private IEnumerator SendGetRequest<T_Response>(string endpoint, Action<T_Response> onSuccess, Action<string> onError)
    {
        string url = $"{baseUrl}/{endpoint}";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.certificateHandler = new BypassCertificate();
            yield return webRequest.SendWebRequest();
            HandleResponse(webRequest, onSuccess, onError);
        }
    }

    private IEnumerator SendPostRequest<T_Response, T_Request>(string endpoint, T_Request requestData, Action<T_Response> onSuccess, Action<string> onError)
    {
        string url = $"{baseUrl}/{endpoint}";
        string jsonBody = JsonConvert.SerializeObject(requestData);
        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.certificateHandler = new BypassCertificate();
            Debug.Log($"[API POST] URL: {url}\nBody: {jsonBody}");

            yield return webRequest.SendWebRequest();
            HandleResponse(webRequest, onSuccess, onError);
        }
    }

    private void HandleResponse<T>(UnityWebRequest request, Action<T> onSuccess, Action<string> onError)
    {
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[API Error] {request.url}: {request.error}\n{request.downloadHandler?.text}");
            onError?.Invoke(request.error);
            return;
        }

        string jsonResult = request.downloadHandler.text;
        Debug.Log($"[API Success] {request.url}\nData: {jsonResult}");

        try
        {
            if (typeof(T) == typeof(string))
            {
                onSuccess?.Invoke((T)(object)jsonResult);
            }
            else
            {
                T data = JsonConvert.DeserializeObject<T>(jsonResult);
                onSuccess?.Invoke(data);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[JSON Parse Error] {e.Message}");
            onError?.Invoke("Lỗi xử lý dữ liệu từ Server.");
        }
    }

    public class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}

