using UnityEngine;

public static class DamageCalculator
{
    private const float K_Constant = 100f; // Hằng số K trong công thức tính sát thương

    public static int CalculatorPhysicalDamage(int baseDamage, int targetArmor)
    {
        // Tính toán sát thương vật lý dựa trên công thức
        float reduceDamage = (float)targetArmor / (targetArmor * K_Constant);
        int finalDamage =  Mathf.RoundToInt(baseDamage * (1 - reduceDamage));

        return Mathf.Max(1, finalDamage); // Đảm bảo sát thương không âm
    }


}
