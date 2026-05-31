using System.Collections.Generic;
using System.Text;
using Microsoft.Unity.VisualStudio.Editor;
using NUnit.Framework;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/ItemSO")]

public class ItemSO : ScriptableObject
{
    public string itemID;
    public enum ItemType
    {
        Weapon,
        Shield,
        Armor,
        Shoes,
        Ring,
        Chain,
        Crystal,
    }
    public ItemType itemType;
    public float dropRate; // Tỷ lệ rơi của item này
    public string itemName; // Tên của item
    public string description; // Mô tả của item
    public enum ItemRare
    {
        SSS,
        S,
        A,
        B,
        C,
    }
    public ItemRare itemRare;
    public bool isEquipment;
    public Sprite itemAvatar;
    [Header("Stats Item")]
    public List<StatsModifier> statModifiers = new List<StatsModifier>(); // Danh sách các chỉ số mà item này cung cấp
    public List<StageSO> dropLocations; // Danh sách các StageSO mà item này có thể rơi ra
    public string GetStatsInfo()
    {
        if (statModifiers == null || statModifiers.Count == 0)
            return "Vật phẩm không có chỉ số";

        string colorTag = "<color=#52FF33>";
        string endColor = "</color>";
        string result = "";

        foreach (var mod in statModifiers)
        {
            // Tự động in ra theo loại chỉ số
            string statName = mod.statType.ToString(); // Có thể viết 1 hàm dịch sang Tiếng Việt sau
            result += $"{statName}: {colorTag}+{mod.value}{endColor}\n";
        }
        return result;
    }
}

