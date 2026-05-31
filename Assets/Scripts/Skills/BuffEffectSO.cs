using UnityEngine;

[CreateAssetMenu(fileName = "New Effect", menuName = "Skill/Status Effect")]
public class BuffEffectSO : ScriptableObject
{
    public string effectName;
    public Sprite icon;
    public string vfxTag; // VFX nổ ra khi nhận buff

    public enum StatType { Damage, Armor, AttackSpeed, HealthRegen }
    public StatType statToBoost;

    public float amount;     // Lượng tăng
    public bool isPercent;   // Tăng theo % hay số thẳng
    public float duration;   // Thời gian tồn tại (Nếu < 0 là vĩnh viễn/Passive)
}