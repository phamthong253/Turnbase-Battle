using UnityEngine;

[CreateAssetMenu(fileName = "ChampSO", menuName = "Scriptable Objects/ChampSO")]
public class ChampSO : ScriptableObject
{
    public GameObject playerGO;
    public GameObject enemyGO;
    [SerializeField] public new string name;
    public float hp;
    public float maxHP;
    public float mana;
    public float maxMana;
    public float armor;
    public float damage;
    public float magicDamage;
}
