using UnityEngine;

public enum StatType
{
    HP,
    Attack,
    Armor,
    AttackSpeed,
    // Thêm các loại stat khác nếu cần
}
[System.Serializable]
public class StatsModifier 
{
    public StatType statType;
    public float value;

    public StatsModifier() { }
    public StatsModifier(StatType statType, float value)
    {
        this.statType = statType;
        this.value = value;
    }
}
