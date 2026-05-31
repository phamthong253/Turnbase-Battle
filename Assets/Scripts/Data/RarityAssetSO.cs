using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
[CreateAssetMenu(fileName = "RarityAssetSO", menuName = "Scriptable Objects/System/RarityAssetsSO")]

public class RarityAssetSO : ScriptableObject
{
    [System.Serializable]
    public struct RarityVisual
    {
        public ItemSO.ItemRare rarity; // độ hiếm
        public Color rarityColor;
        public Sprite rarityIcon;

    }
        public List<RarityVisual> rarityList;
    public Sprite GetRarityIcon(ItemSO.ItemRare rarity)
    {
        foreach (var rarityVisual in rarityList)
        {
            if (rarityVisual.rarity == rarity)
            {
                return rarityVisual.rarityIcon;
            }
        }
        return null; // Hoặc trả về một giá trị mặc định nếu không tìm thấy
    }
    public Color GetRarityColor(ItemSO.ItemRare rarity)
    {
        foreach (var rarityVisual in rarityList)
        {
            if (rarityVisual.rarity == rarity)
            {
                return rarityVisual.rarityColor;
            }
        }
        return Color.white; // Hoặc trả về một giá trị mặc định nếu không tìm thấy
    }
}
