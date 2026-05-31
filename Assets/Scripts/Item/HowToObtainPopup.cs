using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HowToObtainPopupUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI txtItemName;
    public Image imgItemIcon;
    public Button btnClose;

    [Header("Simple Text List")]
    [Tooltip("Kéo một TextMeshProUGUI to vào đây để hiển thị danh sách ải")]
    public TextMeshProUGUI txtLocationsList;

    private void Awake()
    {
        if (btnClose != null) btnClose.onClick.AddListener(ClosePopup);
    }

    // Chỉ nhận vào ItemSO, không cần tham chiếu đến UI Cha nữa
    public void SetupAndShow(ItemSO item)
    {
        this.gameObject.SetActive(true);

        // 1. Hiển thị thông tin cơ bản của Item
        if (txtItemName != null) txtItemName.text = item.itemName;
        if (imgItemIcon != null) imgItemIcon.sprite = item.itemAvatar;

        // 2. Gom danh sách ải thành một chuỗi văn bản (String)
        if (txtLocationsList != null)
        {
            if (item.dropLocations != null && item.dropLocations.Count > 0)
            {
                // Dùng \n để xuống dòng
                string locationsStr = "<color=#FFCC00>Can find at:</color>\n\n";

                foreach (var stage in item.dropLocations)
                {
                    // Lưu ý: Đổi stage.name thành biến chứa tên ải trong StageSO của bạn nếu cần
                    locationsStr += $"- {stage.name}\n";
                }

                txtLocationsList.text = locationsStr;
            }
            else
            {
                txtLocationsList.text = "This item can't find anywhere.";
            }
        }
    }

    public void ClosePopup()
    {
        this.gameObject.SetActive(false);
    }
}