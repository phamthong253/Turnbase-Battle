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
    [Header("Behavior & Prefabs")]
    [Tooltip("Kéo Prefab của viên đạn (ví dụ: mũi tên, quả cầu lửa) vào đây. Để trống đối với tướng cận chiến.")]
    public GameObject projectilePrefab;
    public GameObject hitVFXPrefab;
    
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
}

