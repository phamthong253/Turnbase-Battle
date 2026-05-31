using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UnitsRosterUI : MonoBehaviour
{
    public GameObject unitSlotPrefab;
    public Transform contentParent;

    private void OnEnable()
    {
        RenderUnit();
    }
    private void RenderUnit()
    {
        if (PlayerDataManager.Instance == null || GameDataService.Instance == null)
        {
            Debug.LogWarning("PlayerDataManager hoặc GameDataService chưa được khởi tạo.");
            return;
        }
        // Xóa các đơn vị hiện có trong danh sách trước khi hiển thị lại
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        var unitsRoster = PlayerDataManager.Instance.UnitRosterModel.PlayerUnits;
        foreach (var playerUnitData in unitsRoster)
        {
            // 1. Tra cứu UnitSO gốc
            UnitSO unitSO = GameDataService.Instance.GetUnitSO(playerUnitData.UnitID);

            if (unitSO != null && unitSlotPrefab != null)
            {
                // 2. Tạo Slot và hiển thị
                GameObject slotGO = Instantiate(unitSlotPrefab, contentParent);

                // Giả định Slot có component Image và Text
                Image iconImage = slotGO.GetComponent<Image>();
                // TextMeshProUGUI nameText = slotGO.GetComponentInChildren<TextMeshProUGUI>();

                // Giả định UnitSO có một trường Sprite icon
                if (unitSO.avatar != null && iconImage != null)
                {
                    iconImage.sprite = unitSO.avatar; // ⬅️ Cần thêm public Sprite Icon vào UnitSO
                    // nameText.text = playerUnitData.Name; 
                }

                // TODO: Hiển thị Level, Rank, EquippedItem dựa trên PlayerUnitData
            }
        }
    }
}
