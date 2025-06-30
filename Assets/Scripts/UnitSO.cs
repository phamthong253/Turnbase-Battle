using UnityEngine;

[CreateAssetMenu(fileName = "UnitSO", menuName = "Scriptable Objects/UnitSO")]
public class UnitSO : ScriptableObject
{
    public enum AttackType
    {
        Melee,
        Ranged,
        Magic,
        Support
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
    public float attackSpeed;
    public AttackType attackType;
    public float attackRange;
    public Sprite avatar;
    [Header("Behavior & Prefabs")]
    [Tooltip("Kéo Prefab của viên đạn (ví dụ: mũi tên, quả cầu lửa) vào đây. Để trống đối với tướng cận chiến.")]
    public GameObject projectilePrefab;
}
