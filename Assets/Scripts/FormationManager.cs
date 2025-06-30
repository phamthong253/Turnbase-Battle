using UnityEngine;

/// <summary>
/// Script Singleton sử dụng DontDestroyOnLoad để lưu trữ đội hình được người chơi chọn
/// và mang nó qua các scene (từ màn hình chọn đội hình sang màn hình chiến đấu).
/// </summary>
public class FormationManager : MonoBehaviour
{
    public static FormationManager Instance;

    // Giả sử bạn có 5 vị trí trong đội hình
    // Mảng này sẽ lưu UnitSO của tướng được chọn cho từng vị trí.
    // Index 0 -> PlayerPos1, Index 1 -> PlayerPos2, ...
    public UnitSO[] selectedFormation = new UnitSO[5];

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

    /// <summary>
    /// Gán một tướng vào một vị trí trong đội hình.
    /// </summary>
    /// <param name="unitData">Dữ liệu của tướng (ScriptableObject)</param>
    /// <param name="positionIndex">Vị trí trong đội hình (0-4)</param>
    public void SetUnitInFormation(UnitSO unitData, int positionIndex)
    {
        if (positionIndex >= 0 && positionIndex < selectedFormation.Length)
        {
            selectedFormation[positionIndex] = unitData;
        }
    }

    /// <summary>
    /// Xóa một tướng khỏi một vị trí.
    /// </summary>
    public void ClearUnitFromFormation()
    {
        for (int i = 0; i < selectedFormation.Length; i++)
        {
            selectedFormation[i] = null;
        }
    }
}