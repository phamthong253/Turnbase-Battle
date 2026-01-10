using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
[System.Serializable]
[Tooltip("Dữ liệu các tướng đang sở hữu của player")]
public class PlayerUnitData
{
    public string UnitID;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public string Name;
    public int Level = 1;
    public float StarRank = 1;
    public int Rank = 1;
    public int CurrentExp = 0;

    public List<string> EquippedItemID = new List<string>();


}
// UnitRosterModel.cs (Phần bổ sung/cập nhật)
[System.Serializable]
public class UnitRosterModel
{
    public List<PlayerUnitData> PlayerUnits = new List<PlayerUnitData>();

    // Hàm cần thiết để Gacha Service kiểm tra Unit đã sở hữu chưa
    public bool HasUnit(string unitID)
    {
        return PlayerUnits.Exists(u => u.UnitID == unitID);
    }

    // Hàm cần thiết để thêm Unit Mới
    public void AddUnit(UnitSO unit)
    {
        if (HasUnit(unit.unitID))
        {
            Debug.LogWarning($"[UnitRosterModel] Unit {unit.unitID} đã tồn tại. Không thêm.");
            return;
        }

        // Tạo PlayerUnitData mới với các giá trị mặc định
        PlayerUnits.Add(new PlayerUnitData
        {
            UnitID = unit.unitID,
            Name = unit.name,
            Level = 1,
            StarRank = 1,
            Rank = 1,
            CurrentExp = 0
        });
        Debug.Log($"[UnitRosterModel] Unit MỚI {unit.unitID} đã được thêm vào đội hình.");
    }
    // LƯU Ý: Không cần hàm AddShard, Shard sẽ được chuyển cho InventoryModel.
}