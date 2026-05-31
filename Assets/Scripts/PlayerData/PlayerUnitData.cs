using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[Tooltip("Dữ liệu các tướng đang sở hữu của player")]
public class PlayerUnitData
{
    public string UnitID;
    public string Name;
    public int Level = 1;
    public float StarRank = 1;
    public int Rank = 1;
    public int CurrentExp = 0;
    public string EquippedSlot;
    public string[] EquippedItemIds = new string[6];
    public bool[] isEquipped = new bool[6];
    public Dictionary<StatType, float> BonusStats = new Dictionary<StatType, float>();

    public PlayerUnitData(string unitID)
    {
        UnitID = unitID;
        EnsureEquipmentArrays();
    }

    public PlayerUnitData()
    {
        EnsureEquipmentArrays();
    }

    public void InitializeRuntimeData(UnitSO staticData)
    {
        EnsureEquipmentArrays();
        if (staticData == null) return;
        Name = staticData.name;
        RebuildBonusStats(staticData);
    }

    private void EnsureEquipmentArrays()
    {
        if (isEquipped == null || isEquipped.Length != 6) isEquipped = new bool[6];
        if (EquippedItemIds == null || EquippedItemIds.Length != 6) EquippedItemIds = new string[6];
    }

    private void RebuildBonusStats(UnitSO staticData)
    {
        if (BonusStats == null) BonusStats = new Dictionary<StatType, float>();
        BonusStats.Clear();
        EnsureEquipmentArrays();

        for (int i = 0; i < isEquipped.Length; i++)
        {
            if (isEquipped[i])
            {
                ItemSO item = staticData.GetItemAtSlot(Rank, i);
                if (item != null)
                {
                    foreach (var mod in item.statModifiers)
                    {
                        AddStatModifier(mod, true);
                    }
                }
            }
        }
    }

    public void AddStatModifier(StatsModifier mod, bool isEquip)
    {
        if (mod == null) return;
        if (BonusStats == null) BonusStats = new Dictionary<StatType, float>();
        if (!BonusStats.ContainsKey(mod.statType)) BonusStats[mod.statType] = 0f;
        float modifierValue = isEquip ? mod.value : -mod.value;
        BonusStats[mod.statType] += modifierValue;
        if (BonusStats[mod.statType] < 0f) BonusStats[mod.statType] = 0f;
    }

    public float GetTotalStat(StatType statType, float baseValue)
    {
        if (BonusStats == null) return baseValue;
        float bonus = BonusStats.ContainsKey(statType) ? BonusStats[statType] : 0f;
        return baseValue + bonus;
    }

    public int GetCombatPower(UnitSO staticData)
    {
        if (staticData == null) return 0;

        float totalCP = 0f;
        float totalHP = GetTotalStat(StatType.HP, staticData.hp);
        float totalAttack = GetTotalStat(StatType.Attack, staticData.damage);
        float totalArmor = GetTotalStat(StatType.Armor, staticData.armor);
        float attackCooldown = GetTotalStat(StatType.AttackSpeed, staticData.attackCooldown);
        float attackSpeedScore = attackCooldown > 0 ? 1f / attackCooldown : 0f;

        totalCP += totalHP * 0.1f;
        totalCP += totalAttack * 2.0f;
        totalCP += totalArmor * 1.5f;
        totalCP += attackSpeedScore * 50.0f;

        return Mathf.RoundToInt(totalCP);
    }

    public bool CanRankUp()
    {
        EnsureEquipmentArrays();
        for (int i = 0; i < isEquipped.Length; i++)
        {
            if (!isEquipped[i]) return false;
        }
        return true;
    }

    public void PerformRankUp()
    {
        if (CanRankUp())
        {
            Rank++;
            for (int i = 0; i < isEquipped.Length; i++) isEquipped[i] = false;
        }
        else
        {
            Debug.LogWarning($"[PlayerUnitData] Không thể thăng hạng cho UnitID: {UnitID} vì chưa trang bị đủ.");
        }
    }
}
