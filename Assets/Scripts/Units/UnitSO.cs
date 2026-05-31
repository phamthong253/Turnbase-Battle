using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitSO", menuName = "Scriptable Objects/UnitSO")]
public class UnitSO : ScriptableObject
{
    public string unitID;
    public enum AttackType
    {
        Melee,
        Ranged,
        Magic,
        Support,
        Enemy
    }
    [SerializeField] public new string name;
    public int currentExp { get; private set; }
    public int expToNextLevel { get; private set; }
    public int hp;
    public int maxHP;
    public int mp;
    public int maxMP;
    public int armor;
    public int damage;
    public int magicDamage;
    public int level;
    public float attackCooldown; // sau 1 khoảng thời gian sẽ đánh 1 hit
    public AttackType attackType;
    public float attackRange;
    public int comboThreshold;
    public float critChance; // Tỷ lệ chí mạng (0-100)
    public Sprite avatar;
    public Sprite entireAvatar;
    [Header("Behavior & Prefabs")]
    [Tooltip("Kéo Prefab của viên đạn (ví dụ: mũi tên, quả cầu lửa) vào đây. Để trống đối với tướng cận chiến.")]
    public GameObject projectilePrefab;
    public GameObject hitVFXPrefab;
    [Header("Passive Skills")] // Nội tại kỹ năng thụ động của unit
    public PassiveSkillSO passiveSkill;
    [Header("Enhanced Buff Skills")] // Kỹ năng buff nâng cao dành cho tướng hỗ trợ
    public BuffEffectSO enhancedBuffEffect;
    public enum EnhencedAttackType
    {
        None,
        Critical,
        MultiTarget,
        Stun,
        Heal
    }
    [Header("Enhenced Attack Configuration")]
    public EnhencedAttackType enhencedAttackType;
    public bool hasEnhancedAttack = false;
    public int attackToTriggerEnhanced;
    public float damageEnhencedMultiplier = 2.0f;
    public string enhancedAttackVFXName;
    public bool useEnhancedAOE;
    public float aoeRadius;
    public float aoeDamage;

    // --- THÊM PHẦN NÀY ---
    [Header("Cinematic Theme")]
    [Tooltip("Màu nền màn hình (Nên chọn màu tối/đậm)")]
    public Color cinematicBackdropColor;

    [Tooltip("Màu của tia tốc độ (Nên chọn màu sáng/rực rỡ)")]
    public Color cinematicSpeedLineColor;
    // ---------------------

    [Header("Growth Stats (Chỉ số tăng mỗi cấp)")]
    public float hpGrowth = 10f;
    public float damageGrowth = 2f;
    public float armorGrowth = 0.5f;

    // Cấu hình Rank (Sao)
    [Header("Rank Multiplier")]
    public float rankMultiplierStep = 0.1f; // Mỗi sao tăng 10% chỉ số
    // Yêu cầu cấp Rank để mặc trang bị

    [System.Serializable]
    public struct RankRequirement
    {
        public ItemSO[] requiredItems;
        public int rankLevel; 
    }
    public List<RankRequirement> rankConfig;
    public ItemSO[] GetItemForRank(int rank)
    {
        if (rankConfig == null) {
            Debug.LogError($"[UnitSO - {name}] LỖI: List RankConfig đang bị RỖNG hoàn toàn!");
            return new ItemSO[6];
        }

        foreach (var config in rankConfig)
        {
            if (config.rankLevel == rank)
            {
                // Đảm bảo luôn trả về mảng đủ 6 phần tử (phòng trường hợp config sai)
                if (config.requiredItems == null || config.requiredItems.Length < 6)
                {
                    Debug.LogWarning($"UnitSO {name}: Rank {rank} config items thiếu hoặc null!");
                    return new ItemSO[6];
                }
                return config.requiredItems;
            }
        }
        // Không tìm thấy Rank -> Trả về mảng rỗng (toàn null)
        string availableRanks = "";
        foreach (var r in rankConfig) availableRanks += r.rankLevel + ", ";

        Debug.LogError($"[UnitSO - {name}] LỖI: Game đang yêu cầu đồ cho Rank {rank}, nhưng trong Config chỉ có các Rank: [{availableRanks}]");

        return new ItemSO[6];
    }
    // Lấy 1 món đồ tại ô chỉ định (0-5)
    public ItemSO GetItemAtSlot(int currentRank, int slotIndex)
    {
        var items = GetItemForRank(currentRank);

        if (items != null && slotIndex >= 0 && slotIndex < items.Length)
        {
            return items[slotIndex];
        }
        return null;
    }
}

