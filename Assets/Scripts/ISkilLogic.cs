using UnityEngine;
using System.Collections.Generic;

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
            ExecuteMultiTargetAttack(skill, caster, targets);
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
                enemyUnit.TakeDamage(skill.damage);
            }
        }
    }

    /// <summary>
    /// Logic cho kỹ năng đa mục tiêu cũ: nhiều hiệu ứng, mỗi cái trúng 1 mục tiêu.
    /// </summary>
    private void ExecuteMultiTargetAttack(SkillSO skill, UnitController caster, List<UnitController> targets)
    {
        Debug.Log($"[AttackSkillLogic] Kích hoạt kỹ năng đa mục tiêu (non-AOE).");
        foreach (var target in targets)
        {
            if (target != null && !target.isDestroyed)
            {
                target.TakeDamage(skill.damage);
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
                // Gây sát thương cho từng mục tiêu trong danh sách
                Debug.Log($"[AttackSkillLogic] Gây {skill.damage} sát thương lên {target.name}");
                target.TakeDamage(skill.damage);
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
        // Kiểm tra ngay trên skill được truyền vào
        if (caster == null || targets == null) return;
        // Kiểm tra loại kỹ năng
        if (skill.skillType != SkillSO.SkillType.Buff) return;
        // Thực hiện buff
        //target.ApplyBuff(skill.buffEffect);
        //Debug.Log($"Đã áp dụng buff {skill.buffEffect.name} cho {target.name} bằng kỹ năng {skill.skillName}");
    }
}
public class TankSkillLogic : ISkillLogic
{
    public void ExecuteSkill(SkillSO skill, UnitController caster, List<UnitController> targets)
    {
        // Kiểm tra ngay trên skill được truyền vào
        if (caster == null || targets == null) return;
        // Kiểm tra loại kỹ năng
        if (skill.skillType != SkillSO.SkillType.Tank) return;
        // Thực hiện debuff
        //target.ApplyDebuff(skill.debuffEffect);
        //Debug.Log($"Đã áp dụng debuff {skill.debuffEffect.name} cho {target.name} bằng kỹ năng {skill.skillName}");
    }
}

public class SummonSkillLogic : ISkillLogic
{
    public void ExecuteSkill(SkillSO skill, UnitController caster, List<UnitController> targets)
    {
        BattleHandler battleHandler = Object.FindAnyObjectByType<BattleHandler>();
        // Kiểm tra ngay trên skill được truyền vào
        if (caster == null || targets == null) return;
        // Kiểm tra loại kỹ năng
        if (skill.skillType != SkillSO.SkillType.Summon) return;
        // Thực hiện triệu hồi
        if (skill.summonPrefab != null)
        {
            Vector3 spawnPos = caster.transform.position + caster.transform.right * 2f;
            GameObject golemObject = Object.Instantiate(skill.summonPrefab, spawnPos, Quaternion.identity);
            UnitController golemController = golemObject.GetComponent<UnitController>();
            if (golemController == null)
            {
                    Debug.LogError("Prefab được triệu hồi thiếu component UnitController!");
                    Object.Destroy(golemObject);
                    return;
            }
            battleHandler.RegisterNewUnit(golemController);
            SummonUnit summonUnit = golemController.GetComponent<SummonUnit>();
            if (summonUnit != null)
            {
                summonUnit.Initialize(skill.summonDuration);
            }
            else
            {
                Debug.LogError("Prefab được triệu hồi thiếu component SummonUnit!");
            }
            Debug.Log($"Đã triệu hồi {golemController.name} với thời gian tồn tại {skill.summonDuration} giây.");
        }

    }
}