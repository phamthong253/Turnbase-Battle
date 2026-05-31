using System;
using UnityEngine;

[Serializable]
public class PlayerItemData
{
    public int playerId;
    public string itemID;
    public string itemType;
    public int quantity;

    [NonSerialized]
    public ItemSO StaticInfo;

    public PlayerItemData() { }

    public PlayerItemData(string id, int qty)
    {
        itemID = id;
        quantity = qty;
    }

    public void InitializeRuntimeData(ItemSO staticData)
    {
        StaticInfo = staticData;
        if (StaticInfo == null)
        {
            Debug.LogError($"[Thiếu Data] Item ID '{itemID}' có trong Database nhưng không tìm thấy file ItemSO!");
        }
    }
}
