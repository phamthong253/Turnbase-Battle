using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
[System.Serializable]
public class InventoryModel
{
    [System.NonSerialized]
    // Lưu trữ vật phẩm tiêu hao, Potion, Exp.
    public Dictionary<string, int> ItemInventory = new Dictionary<string, int>();
    [SerializeField]
    private List<ItemInventoryData> serializedItems = new List<ItemInventoryData>();

    // RẤT QUAN TRỌNG: Lưu trữ Unit Shard (Mảnh Unit)
    // Key: UnitID, Value: Số lượng mảnh
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
    [System.Serializable]
    private struct UnitShardData
    {
        public string unitID;
        public int quantity;
        public string unitName;
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

    // Hàm gọi khi tải (Được gọi trong PlayerDataManager.LoadPlayerData() HOẶC ngay sau khi FromJson)
    public void LoadFromSerializedData()
    {
        ItemInventory.Clear();
        foreach (var item in serializedItems)
        {
            ItemInventory.Add(item.itemID, item.quantity);
        }
        foreach (var shard in serializedUnitShards)
        {
            UnitShardInventory.Add(shard.itemID, shard.quantity);
        }
    }
    public void AddItem(ItemSO item, int quantity)
    {
        if (item == null || quantity <= 0)
        {
            Debug.LogWarning("Không có item nào để thêm.");
            return;
        }
        string itemID = item.itemID;
        if(ItemInventory.ContainsKey(itemID))
        {
            ItemInventory[itemID] += quantity;
        }
        else
        {
            ItemInventory[itemID] = quantity;
        }
        // Giả sử bạn có một danh sách hoặc kho để lưu trữ các item
        // AddItemToInventory(item);
        Debug.Log($"Thêm item {item.name} vào dữ liệu người chơi.");
    }
    public void RemoveItem(ItemSO item) {
                if (item == null) {
            Debug.LogWarning("Không có item nào để xóa.");
            return;
        }
        string itemID = item.itemID;
        if (ItemInventory.ContainsKey(itemID)) {
            ItemInventory.Remove(itemID);
            Debug.Log($"Xóa item {item.name} khỏi dữ liệu người chơi.");
        } else {
            Debug.LogWarning($"Item {item.name} không tồn tại trong kho.");
        }
    }
    public bool HasItem(ItemSO itemData, int quantity)
    {
        if (itemData == null || quantity <= 0) return false;

        // Tra cứu bằng ItemID
        string itemId = itemData.itemID;
        if (ItemInventory.TryGetValue(itemId, out int currentQuantity))
        {
            return currentQuantity >= quantity;
        }
        return false;
    }
    public void AddUnitShard(UnitSO unitData, int quantity)
    {
        if (unitData == null || quantity <= 0) return;
        string unitId = unitData.unitID;
        if (UnitShardInventory.ContainsKey(unitId))
        {
            UnitShardInventory[unitId] += quantity;
        }
        else
        {
            UnitShardInventory[unitId] = quantity;
        }
        Debug.Log($"Thêm {quantity} mảnh cho Unit: {unitData.name} vào dữ liệu người chơi.");
    }
    public void RemoveUnitShard(string unitID, int quantity)
    {
        if (string.IsNullOrEmpty(unitID) || quantity <= 0)
        {
            Debug.LogWarning("Không có UnitID hợp lệ để xóa mảnh.");
            return;
        }
        if (UnitShardInventory.ContainsKey(unitID))
        {
            UnitShardInventory[unitID] -= quantity;
            if (UnitShardInventory[unitID] <= 0)
            {
                UnitShardInventory.Remove(unitID);
            }
            Debug.Log($"Xóa {quantity} mảnh cho UnitID: {unitID} khỏi dữ liệu người chơi.");
        }
        else
        {
            Debug.LogWarning($"UnitID: {unitID} không tồn tại trong kho mảnh.");
        }
    }
    public int GetShardCount(string unitID)
    {
        if (string.IsNullOrEmpty(unitID))
        {
            Debug.LogWarning("Không có UnitID hợp lệ để kiểm tra mảnh.");
            return 0;
        }
        if (UnitShardInventory.TryGetValue(unitID, out int count))
        {
            return count;
        }
        return 0;
    }
}
