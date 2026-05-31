using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryModel
{
    [System.NonSerialized]
    public Dictionary<string, int> ItemInventory = new Dictionary<string, int>();
    [SerializeField]
    private List<ItemInventoryData> serializedItems = new List<ItemInventoryData>();

    [System.NonSerialized]
    public Dictionary<string, int> UnitShardInventory = new Dictionary<string, int>();
    [SerializeField]
    private List<ItemInventoryData> serializedUnitShards = new List<ItemInventoryData>();

    [System.Serializable]
    private struct ItemInventoryData
    {
        public string itemID;
        public int quantity;
        public string itemName;
    }

    public void PrepareForSave()
    {
        serializedItems.Clear();
        foreach (var pair in ItemInventory)
        {
            serializedItems.Add(new ItemInventoryData { itemID = pair.Key, quantity = pair.Value });
        }

        serializedUnitShards.Clear();
        foreach (var pair in UnitShardInventory)
        {
            serializedUnitShards.Add(new ItemInventoryData { itemID = pair.Key, quantity = pair.Value });
        }
    }

    public void LoadFromSerializedData()
    {
        ItemInventory.Clear();
        foreach (var item in serializedItems)
        {
            ItemInventory[item.itemID] = item.quantity;
        }

        UnitShardInventory.Clear();
        foreach (var shard in serializedUnitShards)
        {
            UnitShardInventory[shard.itemID] = shard.quantity;
        }
    }

    public void AddItem(ItemSO item, int quantity)
    {
        if (item == null) return;
        if (quantity <= 0) return;

        if (string.IsNullOrEmpty(item.itemID))
        {
            Debug.LogError($"[FATAL ERROR] Item '{item.itemName}' (File: {item.name}) chưa có ItemID! Hãy điền ID trong Inspector ngay!");
            return;
        }

        string itemID = item.itemID;
        if (ItemInventory.ContainsKey(itemID)) ItemInventory[itemID] += quantity;
        else ItemInventory[itemID] = quantity;

        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowNotification($"You have received x{quantity} {item.itemName}!");
        }
        Debug.Log($"Current inventory contains ID [{itemID}] = {ItemInventory[itemID]}");
    }

    public bool HasItem(ItemSO itemData, int quantity)
    {
        if (itemData == null)
        {
            Debug.LogError("[CHECK FAILED] Item check bị Null!");
            return false;
        }

        string itemId = itemData.itemID;
        if (ItemInventory.TryGetValue(itemId, out int currentQuantity))
        {
            return currentQuantity >= quantity;
        }

        string allKeys = string.Join(", ", ItemInventory.Keys);
        Debug.LogError($"-> THẤT BẠI: Không tìm thấy ID '{itemId}' trong kho. KHO ĐANG CÓ: [{allKeys}]");
        return false;
    }

    public void RemoveItem(ItemSO item, int quantity)
    {
        if (item == null || quantity <= 0) return;
        string itemID = item.itemID;
        if (ItemInventory.ContainsKey(itemID))
        {
            ItemInventory[itemID] -= quantity;
            if (ItemInventory[itemID] <= 0) ItemInventory.Remove(itemID);
        }
        PrepareForSave();
    }

    public void AddUnitShard(UnitSO unitData, int quantity)
    {
        if (unitData == null || quantity <= 0) return;
        AddUnitShard(unitData.unitID, quantity);
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowNotification($"You have received x{quantity} shards {unitData.name}!");
        }
    }

    public void AddUnitShard(string unitId, int quantity)
    {
        if (string.IsNullOrEmpty(unitId) || quantity <= 0) return;
        if (UnitShardInventory.ContainsKey(unitId)) UnitShardInventory[unitId] += quantity;
        else UnitShardInventory[unitId] = quantity;
    }

    public void UpdateFullInventoryFromServer(List<PlayerItemData> serverItems)
    {
        ItemInventory.Clear();
        UnitShardInventory.Clear();

        if (serverItems == null)
        {
            Debug.LogWarning("[InventoryModel] Server inventory is null.");
            return;
        }

        foreach (var item in serverItems)
        {
            if (item == null || string.IsNullOrEmpty(item.itemID)) continue;
            if (string.Equals(item.itemType, "UnitShard", StringComparison.OrdinalIgnoreCase))
            {
                UnitShardInventory[item.itemID] = item.quantity;
            }
            else
            {
                ItemInventory[item.itemID] = item.quantity;
            }
        }

        Debug.Log($"[InventoryModel] Synced {ItemInventory.Count} items and {UnitShardInventory.Count} unit shards from server.");
    }
}
