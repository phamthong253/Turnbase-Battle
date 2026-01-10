using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/ItemSO")]

public class ItemSO : ScriptableObject
{
    public string itemID;
   public enum ItemType
    {
        Weapon,
        Armor,
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
}
