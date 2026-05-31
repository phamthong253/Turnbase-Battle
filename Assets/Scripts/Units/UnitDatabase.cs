using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitDatabase", menuName = "System/UnitDatabase")]
public class UnitDatabase : ScriptableObject
{
    public List<UnitSO> allUnits; // Kéo thả tất cả UnitSO vào đây trong Editor

    // Dictionary để tìm kiếm siêu nhanh (O(1))
    private Dictionary<string, UnitSO> _lookup;

    private void OnEnable()
    {
        // Xây dựng Dictionary mỗi khi game chạy hoặc load lại
        _lookup = new Dictionary<string, UnitSO>();
        foreach (var unit in allUnits)
        {
            if (unit != null && !_lookup.ContainsKey(unit.unitID))
            {
                _lookup.Add(unit.unitID, unit);
            }
        }
    }

    public UnitSO GetUnitByID(string id)
    {
        if (_lookup == null) OnEnable(); // Đề phòng trường hợp chưa init

        if (_lookup.TryGetValue(id, out UnitSO unit))
        {
            return unit;
        }
        return null;
    }
}