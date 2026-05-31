using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
[Tooltip("Liên kết unitSO và PlayerUnitData")]
public class RuntimeUnit
{
   public UnitSO BaseData { get; private set; }
    public PlayerUnitData DynamicData { get; private set; }
    public int CurrentHealth { get; private set; }
    public int CurrentMana { get; private set; }
    public List<BuffEffectSO> activeBuffs = new List<BuffEffectSO>();
    public List<PassiveSkillSO> activePassives = new List<PassiveSkillSO>();
    #region TÍNH TOÁN CHỈ SỐ CUỐI CÙNG (FINAL STATS)
    // Công thức chung: (Chỉ số cơ bản + (Level * Hệ số tăng trưởng) + Buff Cộng thẳng) * (1 + Buff Phần trăm)
    public int MaxHP
    {
        get
        {
            float baseValue = BaseData.hp + (DynamicData.Level * 15); // Mỗi level tăng 15 máu
            return CalculateFinalStat(BuffEffectSO.StatType.HealthRegen, baseValue);
        }
    }

    public int FinalDamage
    {
        get
        {
            float baseValue = BaseData.damage + (DynamicData.Level * 3); // Mỗi level tăng 3 sát thương
            return CalculateFinalStat(BuffEffectSO.StatType.Damage, baseValue);
        }
    }

    public int FinalArmor
    {
        get
        {
            float baseValue = BaseData.armor + (DynamicData.Level * 1); // Mỗi level tăng 1 giáp
            return CalculateFinalStat(BuffEffectSO.StatType.Armor, baseValue);
        }
    }
    public bool IsDead => CurrentHealth <= 0;
    public event Action OnStatsChanged;

    //Constructor
    public RuntimeUnit(UnitSO baseData, PlayerUnitData dynamicData)
    {
        BaseData = baseData;
        DynamicData = dynamicData;

        // Reset danh sách buff
        activeBuffs.Clear();
        activePassives.Clear();

        // Nạp passive skill từ SO vào (nếu có)
        if (baseData.passiveSkill != null)
        {
            activePassives.Add(baseData.passiveSkill);
        }

        // Khởi tạo máu và mana đầy khi bắt đầu trận
        CurrentHealth = MaxHP;
        CurrentMana = baseData.mp;
    }
    private int CalculateFinalStat(BuffEffectSO.StatType statType, float baseValue)
    {
        float flat = 0;
        float percent = 0;
        foreach (var buff in activeBuffs)
        {
            if (buff.statToBoost == statType)
            {
                if (buff.isPercent)
                {
                    percent += buff.amount;
                }
                else
                {
                    flat += buff.amount;
                }
            }
        }
        foreach (var passive in activePassives)
        {
            if (passive.targetStat.ToString() == statType.ToString())
            {
                if (passive.calcType == PassiveSkillSO.CalculationType.Percent)
                {
                    percent += passive.amount;
                }
                else
                {
                    flat += passive.amount;
                }
            }
        }
        float finalValue = (baseValue + flat) * (1f + percent / 100f);
        return Mathf.RoundToInt(finalValue);
    }
    public void TakeDamage(int damage)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0, MaxHP);
        OnStatsChanged?.Invoke(); // Báo cho UI update thanh máu
    }
    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount,0, MaxHP);
        OnStatsChanged?.Invoke();
    }
    public void UseMana(int amount)
    {
        CurrentMana = Mathf.Clamp(CurrentMana - amount, 0, BaseData.maxMP);
        OnStatsChanged?.Invoke(); // Báo cho UI update thanh mana
    }

    public void RegenMana(int amount)
    {
        CurrentMana = Mathf.Clamp(CurrentMana + amount, 0, BaseData.maxMP);
        OnStatsChanged?.Invoke();
    }

    // --- XỬ LÝ BUFF ---
    public void AddBuff(BuffEffectSO buff)
    {
        activeBuffs.Add(buff);
        OnStatsChanged?.Invoke(); // Chỉ số thay đổi, báo UI update text ATK/DEF
    }

    public void RemoveBuff(BuffEffectSO buff)
    {
        activeBuffs.Remove(buff);
        OnStatsChanged?.Invoke(); // Chỉ số thay đổi, báo UI trả lại số gốc
    }
    #endregion

}
