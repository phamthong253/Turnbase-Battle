using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Cần dùng để xử lý List tiện hơn

[Tooltip("Quản lý danh sách unit người chơi (kiểm tra sở hữu, add, unlock, equip)")]
[System.Serializable]
public class UnitRosterModel
{
    // Đây là LIST DUY NHẤT cần Save xuống ổ cứng
    public List<PlayerUnitData> PlayerUnits = new List<PlayerUnitData>();

    // Dictionary dùng để chạy game cho nhanh (tra cứu O(1)), không cần Save
    private Dictionary<string, PlayerUnitData> _unitLookup = new Dictionary<string, PlayerUnitData>();

    // Hàm khởi tạo lại Dictionary sau khi Load Game
    public void Initialize()
    {
        _unitLookup.Clear();
        foreach (var unit in PlayerUnits)
        {
            if (!_unitLookup.ContainsKey(unit.UnitID))
                _unitLookup.Add(unit.UnitID, unit);
        }
    }

    // ========================================================================
    // 1. QUẢN LÝ SỞ HỮU (OWNERSHIP)
    // ========================================================================

    public bool HasUnit(string unitID)
    {
        // Kiểm tra trong Dictionary nhanh hơn List.Exists
        if (_unitLookup.Count == 0 && PlayerUnits.Count > 0) Initialize();
        return _unitLookup.ContainsKey(unitID);
    }

    public PlayerUnitData GetUnitData(string unitID)
    {
        if (HasUnit(unitID)) return _unitLookup[unitID];
        return null;
    }

    // Gộp UnlockUnit và AddUnit làm 1 để tránh nhầm lẫn
    public void AddUnit(UnitSO unit)
    {
        if (unit == null || HasUnit(unit.unitID)) return;

        // Tạo data mới (Constructor đã lo việc khởi tạo mảng isEquipped)
        var newData = new PlayerUnitData(unit.unitID)
        {
            Name = unit.name, // LƯU Ý: Dùng unitName (tên hiển thị), đừng dùng unit.name (tên file)
            Level = 1,
            StarRank = 1,
            Rank = 1,
            CurrentExp = 0,
            // isEquipped = new bool[6] // Không cần dòng này nữa nếu Constructor đã làm
        };

        // Thêm vào cả List và Dictionary
        PlayerUnits.Add(newData);
        _unitLookup.Add(unit.unitID, newData);

        Debug.Log($"Đã mở khóa Unit: {unit.name}");
    }
    public void UpdateFullUnit(List<PlayerUnitData> serverDataList)
    {
        PlayerUnits.Clear();
        PlayerUnits.AddRange(serverDataList);
        Initialize();

        Debug.Log($"[UnitRosterModel] Đã đồng bộ {PlayerUnits.Count} tướng từ Server.");
    }

    /// <summary>
    /// Cập nhật MỘT tướng duy nhất (Dùng khi gọi API Mặc đồ, Lên cấp, hoặc Mở khóa 1 tướng mới)
    /// </summary>
    public void UpdateSingleUnit(PlayerUnitData updatedUnitFromServer)
    {
        if (updatedUnitFromServer == null || string.IsNullOrEmpty(updatedUnitFromServer.UnitID)) return;

        PlayerUnitData existingUnit = GetUnitData(updatedUnitFromServer.UnitID);

        if (existingUnit != null)
        {
            // 1. NẾU ĐÃ CÓ: Cập nhật đè các chỉ số mới nhất từ Server
            existingUnit.Level = updatedUnitFromServer.Level;
            existingUnit.Rank = updatedUnitFromServer.Rank;
            existingUnit.CurrentExp = updatedUnitFromServer.CurrentExp;
            existingUnit.StarRank = updatedUnitFromServer.StarRank;

            if (updatedUnitFromServer.isEquipped != null && updatedUnitFromServer.isEquipped.Length == 6)
            {
                existingUnit.isEquipped = updatedUnitFromServer.isEquipped;
            }

            // Gọi lại hàm chạy hậu kỳ để tính lại BonusStats cho tướng này
            UnitSO so = PlayerDataManager.Instance.GetUnitSO(existingUnit.UnitID);
            existingUnit.InitializeRuntimeData(so);

            Debug.Log($"[UnitRosterModel] Đã cập nhật trạng thái mới cho Tướng: {existingUnit.UnitID}");
        }
        else
        {
            // 2. NẾU CHƯA CÓ (Tức là API vừa mở khóa thành công thẻ tướng mới)
            PlayerUnits.Add(updatedUnitFromServer);
            _unitLookup.Add(updatedUnitFromServer.UnitID, updatedUnitFromServer);

            // Gọi hàm hậu kỳ
            UnitSO so = PlayerDataManager.Instance.GetUnitSO(updatedUnitFromServer.UnitID);
            updatedUnitFromServer.InitializeRuntimeData(so);

            Debug.Log($"[UnitRosterModel] Đã thêm tướng mới vào kho: {updatedUnitFromServer.UnitID}");
        }
    }
    public List<UnitSO> GetOwnedUnits()
    {
        List<UnitSO> result = new List<UnitSO>();

        // Duyệt qua danh sách tướng mà người chơi đang sở hữu (lưu trong Save)
        foreach (var unitData in PlayerUnits)
        {
            // Lấy UnitSO từ PlayerDataManager dựa trên ID
            if (PlayerDataManager.Instance != null)
            {
                UnitSO so = PlayerDataManager.Instance.GetUnitSO(unitData.UnitID);

                if (so != null)
                {
                    result.Add(so);
                }
                else
                {
                    Debug.LogWarning($"[UnitRosterModel] Bất thường: Người chơi có ID '{unitData.UnitID}' nhưng không tìm thấy file UnitSO gốc!");
                }
            }
        }

        return result;
    }
}