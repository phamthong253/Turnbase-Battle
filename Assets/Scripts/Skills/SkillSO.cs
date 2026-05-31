using UnityEngine;
using UnityEngine.Playables;

[CreateAssetMenu(fileName = "SkillSO", menuName = "Scriptable Objects/SkillSO")]

public class SkillSO : ScriptableObject
{
    [Header("Skill Basic Info")]
    public string skillName;
    public string description;

    [Header("Skill Stats")]
    public int damage;
    public int manaCost;
    public float cooldownTime;
    public float aoeRadius; // lớn hơn 0 thì là AOE, nhỏ hơn 0 thì là Single Target
    public GameObject summonPrefab; // prefab của quái vật được triệu hồi, nếu có
    public float summonDuration; // thời gian tồn tại của quái vật được triệu hồi, nếu có
    public SkillType skillType;
    public TargetType targetType;

    [Header("Buff/Healing Setting")]
    public int shieldAmount;
    public int healAmount;
    public int damageBonus;
    public float buffDuration;

    [Header("Skill Effects")]
    public GameObject skillEffect;
    public string skillAnimationString; // có thẻe phải sửa lại 
    public AudioClip skillSound;
    [SerializeField] public PlayableAsset skillTimeline;

    [Header("Passive SO")]
    public PassiveSkillSO buffData;
    public BuffEffectSO buffEffectData;

    public enum SkillType
    {
        Attack,
        Summon,
        Heal,
        Buff,
        Tank,
    }
    public enum TargetType
    {
        SingleTarget,
        MultiTarget,
        Self,
        AllAllies,
    }
}
