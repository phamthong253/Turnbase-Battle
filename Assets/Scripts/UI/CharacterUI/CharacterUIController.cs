using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterUIController : MonoBehaviour
{
    [Header("Model Configuration")]
    // Database ánh xạ giữa UnitSO và Prefab 3D/2D
    public List<UnitPrefabMapping> modelDatabase;

    [Header("UI Elements")]
    public Transform characterListContainer;
    public GameObject unitSlotPrefab;

    // Tham chiếu đến Script CharacterDetailUI chứa hàm SetupAndOpen
    public CharacterDetailUI detailPanel;

    // Cache danh sách hiển thị để truyền vào DetailPanel (làm nút Next/Pre)
    private List<PlayerUnitData> _currentDisplayList;
    private void OnEnable()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnUnitRosterUpdated += LoadDataAndRender;
        }
    }
    private void OnDisable()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnUnitRosterUpdated -= LoadDataAndRender;
        }
    }

    private void Start()
    {
        // Đóng panel chi tiết khi bắt đầu
        if (detailPanel != null) detailPanel.ClosePanel();

        // Tự động load dữ liệu từ Singleton
        LoadDataAndRender();
    }

    public void LoadDataAndRender()
    {
        // 1. Lấy dữ liệu ĐỘNG từ PlayerDataManager (Kho tổng)
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.UnitRosterModel != null)
        {
            _currentDisplayList = PlayerDataManager.Instance.UnitRosterModel.PlayerUnits;
            RenderCharacterList();
        }
        else
        {
            Debug.LogWarning("PlayerDataManager chưa sẵn sàng hoặc không có dữ liệu Unit!");
        }
    }

    public void RenderCharacterList()
    {
        // Xóa các item cũ
        foreach (Transform child in characterListContainer)
        {
            Destroy(child.gameObject);
        }

        if (_currentDisplayList == null) return;

        foreach (var playerUnit in _currentDisplayList)
        {
            // 2. Lấy dữ liệu TĨNH (UnitSO) từ PlayerDataManager bằng ID
            UnitSO staticData = PlayerDataManager.Instance.GetUnitSO(playerUnit.UnitID);

            if (staticData == null)
            {
                Debug.LogWarning($"Không tìm thấy UnitSO cho ID: {playerUnit.UnitID}");
                continue;
            }

            // Tạo Slot UI
            GameObject unitSlot = Instantiate(unitSlotPrefab, characterListContainer);
            UnitSlotUI slotUI = unitSlot.GetComponent<UnitSlotUI>();

            if (slotUI != null)
            {
                // Setup hiển thị slot bên ngoài
                slotUI.Setup(staticData, playerUnit, () =>
                {
                    OnUnitSlotClicked(playerUnit);
                });
            }
        }
    }

    // Hàm xử lý khi click vào 1 slot
    private void OnUnitSlotClicked(PlayerUnitData clickedUnitData)
    {
        if (detailPanel != null)
        {
            // GỌI HÀM SETUPANDOPEN CỦA BẠN
            // Tham số 1: Data của unit vừa click
            // Tham số 2: Toàn bộ danh sách (để nút Next/Pre hoạt động)
            // Tham số 3: Logic lấy UnitSO và Prefab (Delegate Func)
            detailPanel.SetupAndOpen(
                clickedUnitData,
                _currentDisplayList,
                GetUnitInfoLogic // Truyền hàm logic vào đây
            );
        }
    }

    // --- HÀM LOGIC (DELEGATE) ---
    // Hàm này sẽ được CharacterDetailUI gọi mỗi khi nó cần hiển thị 1 unit
    // Input: PlayerUnitData -> Output: (UnitSO, GameObject)
    private (UnitSO, GameObject) GetUnitInfoLogic(PlayerUnitData data)
    {
        // A. Lấy UnitSO từ kho tổng
        UnitSO so = PlayerDataManager.Instance.GetUnitSO(data.UnitID);

        // B. Lấy Prefab từ modelDatabase cục bộ
        GameObject prefab = GetPrefabBySO(so);

        // C. Trả về kết quả dạng Tuple
        return (so, prefab);
    }

    // Hàm phụ trợ tìm Prefab
    private GameObject GetPrefabBySO(UnitSO staticData)
    {
        if (staticData == null) return null;

        var mapping = modelDatabase.FirstOrDefault(m => m.unitData == staticData);
        if (mapping != null && mapping.unitPrefab != null)
        {
            return mapping.unitPrefab;
        }

        // Fallback nếu không tìm thấy mapping
        Debug.LogWarning($"Không tìm thấy Prefab cho Unit: {staticData.name}");
        return null;
    }
}