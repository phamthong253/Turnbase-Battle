using UnityEngine;
[CreateAssetMenu(fileName = "GachaCostSO", menuName = "Scriptable Objects/GachaCostSO")]
public class GachaCostSO : ScriptableObject
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public string GachaName;
    public int CrystalCost;
    public string GachaType; // Loại Gacha để GachaService biết pull ở pool nào
    public int PullsPerTicket;
    public int RollCount
    {
        get { return PullsPerTicket; }
    }
}
