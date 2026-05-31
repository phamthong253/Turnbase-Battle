// Scriptable Object chứa danh sách toàn bộ tướng trong game
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Manager/Unit Database")]
public class UnitDatabaseSO : ScriptableObject
{
    public List<UnitSO> allUnits;

    // Hàm tìm kiếm SO dựa vào ID
    public UnitSO GetUnitByID(string id)
    {
        return allUnits.Find(u => u.unitID == id); // Giả sử trong UnitSO có trường unitID
    }
}