using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public interface ISkillLogic
{
    void ExecuteSkill(SkillSO skill, UnitController caster, List<UnitController> targets) {
        if(skill == null || caster == null || targets == null)
        {
            Debug.LogError("Skill, caster, or targets cannot be null.");
            return;
        }
        // Kiểm tra loại kỹ năng
        if(skill.aoeRadius > 0)
        {
            // Nếu kỹ năng có phạm vi AoE, thực hiện logic AoE
            ExecuteAoeAttack(skill, caster, targets[0]);
        }
        else
        {
            // Nếu không, thực hiện logic tấn công đơn lẻ
            ExecuteSingleTargetAttack(skill, caster, targets);
        }
    }
    /// <summary>
    /// Logic cho kỹ năng AOE: 1 hiệu ứng, nhiều mục tiêu trong bán kính.
    /// </summary>
    private void ExecuteAoeAttack(SkillSO skill, UnitController caster, UnitController primaryTarget)
    {
        if (primaryTarget == null) return;

        Vector3 centerPoint = primaryTarget.transform.position;
        Debug.Log($"[AttackSkillLogic] Kích hoạt AOE tại vị trí {centerPoint} với bán kính {skill.aoeRadius}");

        // 1. TẠO RA MỘT HIỆU ỨNG DUY NHẤT TẠI TÂM ĐIỂM
        if (skill.skillEffect != null)
        {
            Object.Instantiate(skill.skillEffect, centerPoint, Quaternion.identity);
        }
        // 2. "QUÉT" TẤT CẢ CÁC COLLIDER TRONG BÁN KÍNH
        // Lấy LayerMask của layer "Enemy" mà chúng ta đã tạo
        int enemyLayerMask = LayerMask.GetMask("Enemy");
        Collider2D[] hits = Physics2D.OverlapCircleAll(centerPoint, skill.aoeRadius, enemyLayerMask);

        Debug.Log($"Đã tìm thấy {hits.Length} kẻ địch trong vùng ảnh hưởng.");

        // 3. GÂY SÁT THƯƠNG CHO TẤT CẢ KẺ ĐỊCH TÌM THẤY
        foreach (var hit in hits)
        {
            UnitController enemyUnit = hit.GetComponent<UnitController>();
            if (enemyUnit != null && !enemyUnit.isDestroyed)
            {
                bool isCritical = DamageCalculator.IsCriticalHit(caster.UnitData.BaseData.critChance);
                int finalDamage = isCritical ? skill.damage * 2 : skill.damage;
                enemyUnit.TakeDamage(finalDamage, isCritical);
            }
        }
    }

    /// <summary>
    /// Logic cho kỹ năng đa mục tiêu cũ: nhiều hiệu ứng, mỗi cái trúng 1 mục tiêu.
    /// </summary>
    private void ExecuteSingleTargetAttack(SkillSO skill, UnitController caster, List<UnitController> targets)
    {
        Debug.Log($"[AttackSkillLogic] Kích hoạt kỹ năng đa mục tiêu (non-AOE).");
        foreach (var target in targets)
        {
            if (target != null && !target.isDestroyed)
            {
                bool isCritical = DamageCalculator.IsCriticalHit(caster.UnitData.BaseData.critChance);
                int finalDamage = isCritical ? skill.damage * 2 : skill.damage;
                target.TakeDamage(finalDamage, isCritical);
                if (skill.skillEffect != null)
                {
                    Object.Instantiate(skill.skillEffect, target.transform.position, Quaternion.identity);
                }
            }
        }
    }
}
public class AttackSkillLogic : ISkillLogic
{
    public void ExecuteSkill(SkillSO skill, UnitController caster, List<UnitController> targets)
    {
        // Kiểm tra ngay trên skill được truyền vào
        if (caster == null || targets == null) return;

        // Lặp qua danh sách mục tiêu nhận được
        foreach (var target in targets)
        {
            if (target != null && !target.isDestroyed)
            {
                // Kiểm tra loại kỹ năng
                if (skill.skillType != SkillSO.SkillType.Attack) return;
                // Thực hiện tấn công
                Transform spawnPoint = caster.fireTransform;
                bool isCritical = DamageCalculator.IsCriticalHit(caster.UnitData.BaseData.critChance);
                int finalDamage = isCritical ? skill.damage * 2 : skill.damage;
                // Gây sát thương cho từng mục tiêu trong danh sách
                Debug.Log($"[AttackSkillLogic] Gây {skill.damage} sát thương lên {target.name}");
                target.TakeDamage(finalDamage, isCritical);
            }
        GameObject projectile = Object.Instantiate(skill.skillEffect, target.transform.position, Quaternion.identity);
            Object.Destroy(projectile, 2f); // Hủy hiệu ứng sau 2 giây
        }
    }
}

public class HealSkillLogic : ISkillLogic
{
    public void ExecuteSkill(SkillSO skill, UnitController caster, List<UnitController> targets)
    {
        // Kiểm tra ngay trên skill được truyền vào
        if (caster == null || targets == null) return;
        // Kiểm tra loại kỹ năng
        if (skill.skillType != SkillSO.SkillType.Heal) return;
        // Thực hiện hồi máu
        //target.Heal(skill.healAmount);
    }
}
public class BuffSkillLogic : ISkillLogic
{
    public void ExecuteSkill(SkillSO skill, UnitController caster, List<UnitController> targets)
    {
        if (caster == null || targets == null || targets.Count == 0) return;

        // Cho phép dùng logic này cho cả loại Heal và Buff
        if (skill.skillType != SkillSO.SkillType.Buff && skill.skillType != SkillSO.SkillType.Heal) return;

        Debug.Log($"[BuffSkillLogic] {caster.name} kích hoạt hỗ trợ toàn đội!");

        foreach (var target in targets)
        {
            if (target != null && !target.isDestroyed && target.isPlayerUnit)
            {
                // 1. VFX
                if (skill.skillEffect != null)
                {
                    GameObject vfx = Object.Instantiate(skill.skillEffect, target.transform.position, Quaternion.identity);
                    target.StartCoroutine(DestroyVFX(vfx, 2.5f)); // Hàm hủy VFX phụ trợ
                }

                // 2. Hồi máu (Nếu Heal Amount > 0)
                if (skill.healAmount > 0)
                {
                    target.Heal(skill.healAmount, false);
                }

                // 3. Tăng Damage (Nếu Damage Bonus > 0)
                if (skill.damageBonus > 0)
                {
                    target.ApplyBuffEffect(skill.buffEffectData);
                }
            }
        }
    }

    private System.Collections.IEnumerator DestroyVFX(GameObject vfx, float time)
    {
        yield return new WaitForSeconds(time);
        if (vfx != null) Object.Destroy(vfx);
    }
}
public class TankSkillLogic : ISkillLogic
{
    public void ExecuteSkill(SkillSO skill, UnitController caster, List<UnitController> targets)
    {
        if (caster == null || targets == null || targets.Count == 0) return;
        // Kiểm tra đúng loại skill chưa (để an toàn)
        if (skill.skillType != SkillSO.SkillType.Tank) return;

        Debug.Log($"[TankSkillLogic] {caster.name} kích hoạt kỹ năng bảo vệ đồng minh!");

        foreach (var target in targets)
        {
            if (target != null && !target.isDestroyed && target.isPlayerUnit)
            {
                // 1. Tạo hiệu ứng hình ảnh (VFX) trên mỗi mục tiêu
                if (skill.skillEffect != null)
                {
                    GameObject vfx = Object.Instantiate(skill.skillEffect, target.transform.position, Quaternion.identity);
                    Object.Destroy(vfx, 2f); // Hủy VFX sau 2s
                }

                // 2. Buff Shield (Nếu có set thông số > 0)
                //if (skill.shieldAmount > 0)
                //{
                //    target.AddShield(skill.shieldAmount, skill.buffDuration);
                //}

            }
        }
    }
}

public class SummonSkillLogic : ISkillLogic
{
    public void ExecuteSkill(SkillSO skill, UnitController caster, List<UnitController> targets)
    {
        // 1. Kiểm tra điều kiện đầu vào
        if (caster == null || skill == null || skill.skillType != SkillSO.SkillType.Summon) return;

        BattleHandler battleHandler = Object.FindAnyObjectByType<BattleHandler>();
        if (battleHandler == null) return;

        if (skill.summonPrefab != null)
        {
            // 2. TÍNH TOÁN VỊ TRÍ XUẤT HIỆN
            // Xuất hiện phía trước mặt Caster một chút
            Vector3 spawnDir = caster.isPlayerUnit ? Vector3.right : Vector3.left;
            Vector3 spawnPos = caster.transform.position + spawnDir * 2f;

            // 3. SPAWN XÁC (Tạo Game Object)
            GameObject golemObject = Object.Instantiate(skill.summonPrefab, spawnPos, Quaternion.identity);
            UnitController golemController = golemObject.GetComponent<UnitController>();

            if (golemController == null)
            {
                Debug.LogError("Prefab được triệu hồi thiếu component UnitController!");
                Object.Destroy(golemObject);
                return;
            }

            // --- [MỚI] 4. BƠM LINH HỒN (DỮ LIỆU) VÀO XÁC ---

            // A. Lấy dữ liệu gốc từ Prefab (Biến configSO mà ta vừa tạo ở bước trước)
            UnitSO baseData = golemController.configSO;
            if (baseData == null)
            {
                Debug.LogError($"Prefab Summon {golemObject.name} chưa được kéo file UnitSO vào biến ConfigSO!");
                return;
            }

            // B. Tạo dữ liệu động: Kế thừa Level từ Caster
            PlayerUnitData dynamicData = new PlayerUnitData(baseData.unitID)
            {
                Level = caster.UnitData.DynamicData.Level, // Caster cấp bao nhiêu, đệ cấp bấy nhiêu
                Rank = caster.UnitData.DynamicData.Rank
            };

            // C. Gộp lại và bơm vào Golem
            RuntimeUnit golemRuntimeData = new RuntimeUnit(baseData, dynamicData);
            golemController.SetupUnit(golemRuntimeData);

            // D. Quan trọng: Set phe phái cho Golem giống hệt phe của Caster
            golemController.isPlayerUnit = caster.isPlayerUnit;
            // -----------------------------------------------


            // 5. ĐĂNG KÝ VÀO TRẬN
            // BattleHandler sẽ tự động tạo HUD máu có thanh thời gian cho con quái này
            battleHandler.RegisterNewUnit(golemController);

            // 6. KÍCH HOẠT THỜI GIAN SỐNG
            SummonUnit summonUnit = golemController.GetComponent<SummonUnit>();
            if (summonUnit != null)
            {
                summonUnit.Initialize(skill.summonDuration);
            }
            else
            {
                Debug.LogError("Prefab được triệu hồi thiếu component SummonUnit!");
            }

            Debug.Log($"Đã triệu hồi {golemController.UnitData.BaseData.name} (Lv {golemRuntimeData.DynamicData.Level}) - Tồn tại: {skill.summonDuration}s.");
        }
    }
}