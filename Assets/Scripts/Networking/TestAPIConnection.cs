using UnityEngine;
using System.Collections.Generic;

public class TestAPIConnection : MonoBehaviour
{
    private List<PlayerUnitData> loadedUnits = new List<PlayerUnitData>();
    void Start()
    {
        Debug.Log("Bắt đầu gọi API...");
        foreach (var unit in loadedUnits)
        {
            Debug.Log("--- BẮT ĐẦU KIỂM TRA API LƯU TƯỚNG ---");
            // 1. Thử lưu một con tướng mới
            PlayerUnitData newUnit = new PlayerUnitData(unit.UnitID);
            newUnit.Name = "Warrior_01";
            newUnit.Level = 10;
            newUnit.Rank = 2;

            //    APIManager.Instance.SaveUnit(newUnit,
            //    (success) =>
            //    {
            //        Debug.Log("Lưu thành công! Đang tải lại danh sách...");

            //        // 2. Lưu xong thì thử tải về xem có thấy nó không
            //        LoadAllUnits();
            //    },
            //    (error) => Debug.LogError("Lưu thất bại: " + error)
            //);
            //}

            //void LoadAllUnits()
            //{
            //    APIManager.Instance.LoadUnits(
            //        (units) =>
            //        {
            //            Debug.Log($"Đã tải về {units.Count} nhân vật:");
            //            foreach (var u in units)
            //            {
            //                Debug.Log($"- {u.UnitID} (Lv.{u.Level})");
            //            }
            //        },
            //        (error) => Debug.LogError("Tải thất bại: " + error)
            //    );
        }
    }
}