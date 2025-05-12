using UnityEngine;

public class Unit : MonoBehaviour
{
    public int currentHP;
    public int maxHP;
    public string unitName;
    public int unitLevel;
    public int damage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public bool TakeDamage(int damage)
    {   
        currentHP -= damage;
        if(currentHP <= 0)
        {
            currentHP = 0;
            Debug.Log(unitName + " đã chết!");
            Destroy(gameObject);
            return true;
        }
        return false;
    }
}
