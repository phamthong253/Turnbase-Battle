using UnityEngine;

[CreateAssetMenu(fileName = "UnitSO", menuName = "Scriptable Objects/UnitSO")]
public class UnitSO : ScriptableObject
{
    [SerializeField] public new string name;
    public int hp;
    public int maxHP;
    public int mp;
    public int maxMP;
    public int armor;
    public int damage;
    public int magicDamage;
    public int level;
}
