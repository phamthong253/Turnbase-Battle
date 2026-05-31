using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[DefaultExecutionOrder(-100)]
public partial class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;
    public CurrencyModel CurrencyModel;
    public UnitRosterModel UnitRosterModel;
    public InventoryModel InventoryModel;
    public MapProgressModel MapProgressModel;

    public int CurrentPlayerId = 2;

    [Header("Game Config")]
    public List<UnitSO> startingUnits;
    [Header("Item Rare")]
    public RarityAssetSO rarityConfig;
    [Header("Battle Data")]
    public UnitSO[] battleTeamData = new UnitSO[5];
    public StageSO currentStageSO;

    public event Action<int> OnCurrencyChanged;
    public event Action<UnitSO> OnNewUnitAcquired;
    public event Action OnInventoryChanged;
    public event Action OnUnitRosterUpdated;
    public event Action OnInventoryUpdated;
    public event Action OnMapProgressUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        if (CurrencyModel == null) CurrencyModel = new CurrencyModel();
        if (InventoryModel == null) InventoryModel = new InventoryModel();
        if (UnitRosterModel == null) UnitRosterModel = new UnitRosterModel();
        if (MapProgressModel == null) MapProgressModel = new MapProgressModel();
        DontDestroyOnLoad(this.gameObject);
    }

    public void ApplyPlayerProfile(PlayerResponse playerData)
    {
        if (playerData == null) return;
        CurrentPlayerId = playerData.id;
        if (CurrencyModel == null) CurrencyModel = new CurrencyModel();
        CurrencyModel.SetCrystal(playerData.crystals);
        OnCurrencyChanged?.Invoke(CurrencyModel.GetTotalCrystals());
    }

    public void FetchAndMatchItemsFromServer(int playerId)
    {
        Debug.Log("Đang tải danh sách vật phẩm từ Server...");
        APIManager.Instance.LoadInventoryForPlayer(playerId,
            onSuccess: (userItems) =>
            {
                if (InventoryModel == null) InventoryModel = new InventoryModel();
                InventoryModel.UpdateFullInventoryFromServer(userItems);
                OnInventoryUpdated?.Invoke();
                OnInventoryChanged?.Invoke();
                Debug.Log($"[PlayerDataManager] Đã đồng bộ inventory từ Server.");
            },
            onError: (errorMsg) =>
            {
                Debug.LogError("Không thể tải danh sách vật phẩm: " + errorMsg);
            }
        );
    }

    public void FetchAndMatchUnitsFromServer(int playerId)
    {
        Debug.Log("Đang tải danh sách tướng từ Server...");
        APIManager.Instance.LoadUnitsForPlayer(playerId,
            onSuccess: (userUnits) =>
            {
                if (UnitRosterModel == null) UnitRosterModel = new UnitRosterModel();
                if (userUnits == null) userUnits = new List<PlayerUnitData>();

                foreach (var unit in userUnits)
                {
                    UnitSO staticData = GetUnitSO(unit.UnitID);
                    if (staticData != null)
                    {
                        unit.InitializeRuntimeData(staticData);
                        Debug.Log($"[MATCH THÀNH CÔNG] ID: {unit.UnitID} | Tên gốc từ SO: {staticData.name} | Cấp độ: {unit.Level}");
                    }
                    else
                    {
                        Debug.LogError($"[CẢNH BÁO LỆCH DATA] Server trả về Tướng có ID '{unit.UnitID}' nhưng Unity không có UnitSO này!");
                    }
                }

                UnitRosterModel.UpdateFullUnit(userUnits);
                OnUnitRosterUpdated?.Invoke();
            },
            onError: (errorMsg) =>
            {
                Debug.LogError("Không thể tải danh sách tướng: " + errorMsg);
            }
        );
    }

    public void EquipItemUnitsFromServer(int playerId, string unitID, string itemType, string itemID, int slotIndex, Action onSuccess = null, Action<string> onError = null)
    {
        Debug.Log($"Đang gửi yêu cầu trang bị Item {itemID} cho Unit {unitID} vào slot {slotIndex} lên Server...");
        APIManager.Instance.EquipItem(playerId, unitID, itemType, itemID, slotIndex,
            onSuccess: (responseMsg) =>
            {
                Debug.Log("Trang bị thành công! Server trả về: " + responseMsg);
                FetchAndMatchUnitsFromServer(playerId);
                FetchAndMatchItemsFromServer(playerId);
                onSuccess?.Invoke();
            },
            onError: (errorMsg) =>
            {
                Debug.LogError("Không thể trang bị item: " + errorMsg);
                onError?.Invoke(errorMsg);
            }
        );
    }

    public void UnEquipItemUnitsFromServer(int playerId, string unitID, string itemType, string itemID, int slotIndex, Action onSuccess = null, Action<string> onError = null)
    {
        Debug.Log($"Sending unequip request for item {itemID} (type:{itemType}) -> unit {unitID}, slot {slotIndex} ...");
        APIManager.Instance.UnEquipItem(playerId, unitID, itemID, itemType, slotIndex,
            onSuccess: (responseMsg) =>
            {
                Debug.Log("Unequip success. Server response: " + responseMsg);
                FetchAndMatchUnitsFromServer(playerId);
                FetchAndMatchItemsFromServer(playerId);
                onSuccess?.Invoke();
            },
            onError: (errorMsg) =>
            {
                Debug.LogError("Unequip failed: " + errorMsg);
                onError?.Invoke(errorMsg);
            }
        );
    }

    public void LoadMapProgressFromServer(Action onSuccess = null, Action<string> onError = null)
    {
        if (CurrentPlayerId <= 0)
        {
            onError?.Invoke("PlayerId chưa được khởi tạo.");
            return;
        }

        APIManager.Instance.LoadMapProgressForPlayer(CurrentPlayerId,
            onSuccess: (resp) =>
            {
                if (MapProgressModel == null) MapProgressModel = new MapProgressModel();
                var entries = new List<StageDataEntry>();
                if (resp != null)
                {
                    foreach (var s in resp)
                    {
                        entries.Add(new StageDataEntry
                        {
                            stageID = s.stageId,
                            starsEarned = s.stars ?? 0,
                            isCompleted = s.isCompleted,
                            attempts = s.attempts,
                            progress = s.progress,
                            completedAt = null
                        });
                    }
                }

                MapProgressModel.FromServerStageList(entries);
                var completed = entries.Where(e => e.isCompleted).ToList();
                if (completed.Count > 0)
                {
                    MapProgressModel.currentMaxCompleteStageID = Mathf.Max(MapProgressModel.currentMaxCompleteStageID, completed.Max(e => e.stageID) + 1);
                }

                OnMapProgressUpdated?.Invoke();
                if (MapUIManager.Instance != null) MapUIManager.Instance.RefreshMapNodes();
                onSuccess?.Invoke();
            },
            onError: (err) =>
            {
                Debug.LogError("[LoadMapProgressFromServer] " + err);
                onError?.Invoke(err);
            });
    }

    public void SaveMapProgressToServer(int stageId, int stars, Action onSuccess = null, Action<string> onError = null)
    {
        if (CurrentPlayerId <= 0)
        {
            onError?.Invoke("PlayerId chưa được khởi tạo.");
            return;
        }
        if (MapProgressModel == null)
        {
            onError?.Invoke("MapProgressModel rỗng, không có gì để lưu.");
            return;
        }

        var entry = MapProgressModel.stageList.Find(s => s.stageID == stageId);
        double progress = entry != null ? entry.progress : 1.0;
        bool isCompleted = entry != null ? entry.isCompleted : true;

        APIManager.Instance.SaveMapProgressForPlayer(CurrentPlayerId, stageId, progress, isCompleted, stars,
            onSuccess: (resp) =>
            {
                Debug.Log("[SaveMapProgressToServer] Server response: " + resp);
                LoadMapProgressFromServer();
                onSuccess?.Invoke();
            },
            onError: (err) =>
            {
                Debug.LogError("[SaveMapProgressToServer] " + err);
                onError?.Invoke(err);
            }
        );
    }

    public UnitSO GetUnitSO(string unitID)
    {
        if (string.IsNullOrEmpty(unitID)) return null;
        UnitSO fromService = GameDataService.Instance != null ? GameDataService.Instance.GetUnitSO(unitID) : null;
        if (fromService != null) return fromService;
        return startingUnits != null ? startingUnits.Find(u => u != null && u.unitID == unitID) : null;
    }

    public bool TryGacha(GachaCostSO costData, IGachaService gachaService)
    {
        Debug.LogWarning("[Gacha] Local gacha path is debug-only. Production gacha uses APIManager.RollGacha.");
        return false;
    }

    public void ApplyGachaRollResponse(GachaRollResponse response)
    {
        if (response == null) return;
        if (CurrencyModel == null) CurrencyModel = new CurrencyModel();
        CurrencyModel.SetCrystal(response.crystals);
        OnCurrencyChanged?.Invoke(CurrencyModel.GetTotalCrystals());

        if (response.units != null)
        {
            foreach (var unit in response.units)
            {
                UnitSO staticData = GetUnitSO(unit.UnitID);
                if (staticData != null) unit.InitializeRuntimeData(staticData);
            }
            if (UnitRosterModel == null) UnitRosterModel = new UnitRosterModel();
            UnitRosterModel.UpdateFullUnit(response.units);
            OnUnitRosterUpdated?.Invoke();
        }

        if (response.inventory != null)
        {
            if (InventoryModel == null) InventoryModel = new InventoryModel();
            InventoryModel.UpdateFullInventoryFromServer(response.inventory);
            OnInventoryUpdated?.Invoke();
            OnInventoryChanged?.Invoke();
        }
    }

    private void GrantStartingUnits()
    {
        if (startingUnits != null)
        {
            foreach (var unit in startingUnits)
            {
                UnitRosterModel.AddUnit(unit);
            }
        }
    }

    private void ProcessGachaReward(GachaReward reward)
    {
        if (reward.UnitData != null) OnNewUnitAcquired?.Invoke(reward.UnitData);
        if (reward.ShardData != null) InventoryModel.AddUnitShard(reward.ShardData, reward.Quantity);
        if (reward.ItemData != null) InventoryModel.AddItem(reward.ItemData, reward.Quantity);
    }

    public void ApplyRewards(int crystals, Dictionary<string, int> itemRewards, Action onComplete = null)
    {
        if (crystals > 0)
        {
            if (CurrencyModel == null) CurrencyModel = new CurrencyModel();
            CurrencyModel.AddCrystal(crystals);
            OnCurrencyChanged?.Invoke(CurrencyModel.GetTotalCrystals());
        }

        if (itemRewards != null && itemRewards.Count > 0)
        {
            if (InventoryModel == null) InventoryModel = new InventoryModel();
            foreach (var kv in itemRewards)
            {
                if (kv.Value <= 0) continue;
                ItemSO itemSO = GameDataService.Instance?.GetItemSO(kv.Key);
                if (itemSO != null) InventoryModel.AddItem(itemSO, kv.Value);
                else Debug.LogWarning($"[ApplyRewards] Cannot resolve ItemSO for id '{kv.Key}'. Skipping.");
            }
            OnInventoryUpdated?.Invoke();
            OnInventoryChanged?.Invoke();
        }
        onComplete?.Invoke();
    }

    public void CompleteStageAndSave(int currentStageID, int stars, int nextStageID)
    {
        MapProgressModel.StageCompleted(currentStageID, stars, nextStageID);
        SaveMapProgressToServer(currentStageID, stars);
    }

    public int GetTotalPlayerCombatPower()
    {
        int totalAccountCP = 0;
        if (UnitRosterModel != null && UnitRosterModel.PlayerUnits != null)
        {
            foreach (var unitData in UnitRosterModel.PlayerUnits)
            {
                UnitSO staticData = GetUnitSO(unitData.UnitID);
                if (staticData != null) totalAccountCP += unitData.GetCombatPower(staticData);
            }
        }
        return totalAccountCP;
    }
}

