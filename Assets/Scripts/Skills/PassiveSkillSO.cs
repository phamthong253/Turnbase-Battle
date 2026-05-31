using UnityEngine;

[CreateAssetMenu(fileName = "New Passive", menuName = "Skill/Passive Skill")]
public class PassiveSkillSO : ScriptableObject
{
    public string passiveName;

    public enum PassiveTargetStat { None, Damage, Armor, MaxHP, AttackSpeed }
    public enum CalculationType { Flat, Percent } // Cộng thẳng (Flat) hay cộng % (Percent)

    [Header("Cấu hình Buff")]
    public PassiveTargetStat targetStat; // Muốn buff chỉ số nào?
    public CalculationType calcType;     // Cộng thẳng hay %?
    public float amount;
    public Sprite passiveIcon;
    [Header("Visual Effects")]
    public string activationVFXTag; // Tên VFX trong Object Pool (VD: "BuffArmorEffect")
    public bool isPersistent = false;       // True = Aura (tồn tại mãi), False = Nổ 1 cái rồi tắt (2 giây)// Giá trị bao nhiêu? (VD: 10 hoặc 0.2 cho 20%)

    // Hàm tính toán giá trị cuối cùng
    public int GetModifiedValue(int baseValue)
    {
        if (targetStat == PassiveTargetStat.None) return baseValue;

        if (calcType == CalculationType.Flat)
        {
            return baseValue + (int)amount; // VD: 100 + 10 = 110
        }
        else
        {
            return baseValue + (int)(baseValue * amount); // VD: 100 + (100 * 0.1) = 110
        }
    }
}