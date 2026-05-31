using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public ISkillLogic GetSkillLogic(SkillSO skill)
    {
        switch (skill.skillType)
        {
            case SkillSO.SkillType.Attack:
                return new AttackSkillLogic();
            case SkillSO.SkillType.Heal:
                return new HealSkillLogic();
            case SkillSO.SkillType.Buff:
                return new BuffSkillLogic();
            case SkillSO.SkillType.Tank:
                return new TankSkillLogic();
            case SkillSO.SkillType.Summon:
                return new SummonSkillLogic();
            default:
                Debug.LogWarning($"Không tìm thấy logic nào cho loại skill: {skill.skillType}");
                return null;
        }
    }

    public List<UnitController> GetTargets(SkillSO skill, UnitController caster)
    {
        List<UnitController> targets = new List<UnitController>();

        BattleHandler battleHandler = FindAnyObjectByType<BattleHandler>();
        if (battleHandler == null)
        {
            Debug.LogError("Không tìm thấy BattleHandler trong scene.");
            return targets;
        }
        switch (skill.targetType)
        {
            case SkillSO.TargetType.SingleTarget:
                // Tìm kiếm mục tiêu đơn lẻ
                UnitController singleTarget = caster.GetCurrentTarget();
                if (singleTarget != null)
                {
                    //targets = new List<UnitController>() { targets[0]}; // tính lấy 1 mục tiêu đầu tiên
                    targets.Add(singleTarget);
                }
                break;
            case SkillSO.TargetType.MultiTarget:
                // Tìm kiếm tất cả mục tiêu trong phạm vi
                targets = battleHandler.GetOpponentListFor(caster).Where(u=> u != null && !u.isDestroyed).ToList();
                break;
            case SkillSO.TargetType.Self:
                // Thêm chính caster vào danh sách mục tiêu
                if (caster != null && !caster.isDestroyed)
                {
                    targets.Add(caster);
                }
                break;
            case SkillSO.TargetType.AllAllies:
                // Thêm tất cả đồng minh vào danh sách mục tiêu
                targets = battleHandler.GetAlliedListFor(caster).Where(u => u != null && !u.isDestroyed).ToList();
                break;

        }
        return targets;
    }
}