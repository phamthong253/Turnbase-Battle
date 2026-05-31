using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PartySlotUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Image hiển thị mặt của tướng")]
    public Image unitIcon;

    [Tooltip("GameObject hiển thị khi ô này trống (VD: Hình dấu cộng hoặc khung rỗng)")]
    public GameObject emptyVisual;

    [Tooltip("Nút bấm của slot này")]
    public Button btnSlot;

    // Biến lưu trạng thái nội bộ
    private int mySlotIndex;
    private UnitSO currentUnit;

    private void Start()
    {
        // Gán sự kiện click
        if (btnSlot != null)
        {
            btnSlot.onClick.AddListener(OnSlotClicked);
        }
    }

    /// <summary>
    /// Hàm này được FormationPopupManager gọi để cập nhật dữ liệu
    /// </summary>
    /// <param name="unit">Dữ liệu tướng (có thể null)</param>
    /// <param name="index">Số thứ tự của slot này (0, 1, 2, 3, 4)</param>
    public void Setup(UnitSO unit, int index)
    {
        this.currentUnit = unit;
        this.mySlotIndex = index;

        if (unit != null)
        {
            // TRƯỜNG HỢP CÓ TƯỚNG
            if (unitIcon != null)
            {
                unitIcon.gameObject.SetActive(true);
                unitIcon.sprite = unit.avatar; // Đảm bảo UnitSO có biến 'icon'
            }

            if (emptyVisual != null)
            {
                emptyVisual.SetActive(false); // Ẩn cái dấu cộng đi
            }
        }
        else
        {
            // TRƯỜNG HỢP Ô TRỐNG
            if (unitIcon != null)
            {
                unitIcon.gameObject.SetActive(false); // Ẩn icon tướng đi
            }

            if (emptyVisual != null)
            {
                emptyVisual.SetActive(true); // Hiện cái dấu cộng/khung rỗng lên
            }
        }
    }

    /// <summary>
    /// Khi người chơi bấm vào slot này
    /// </summary>
    private void OnSlotClicked()
    {
        // Nếu slot này đang có tướng, bấm vào nghĩa là muốn GỠ RA
        if (currentUnit != null)
        {
            // Gọi hàm Remove trong Manager (Bạn cần đảm bảo Manager có hàm này)
            FormationPopupManager.Instance.RemoveUnitAtSlot(mySlotIndex);
        }
    }
}