using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class RosterItemUI : MonoBehaviour
{
    [Header("UI References")]
    public Image avatarImage;      // Kéo Image con "Avatar" vào đây
    public GameObject selectedOverlay; // Kéo lớp mờ "SelectedOverlay" vào đây
    public Button btnClick;        // Kéo nút Button vào đây

    public Image roleIconImage;      // Kéo Image RoleIcon vào đây
    public TextMeshProUGUI levelText;

    private UnitSO myData;
    private System.Action<UnitSO> onClickCallback;

    // Hàm này sẽ được Manager gọi để điền dữ liệu
    public void Setup(UnitSO data, bool isSelected,Sprite roleSprite, System.Action<UnitSO> clickAction)
    {
        myData = data;
        onClickCallback = clickAction;

        // 1. Hiển thị ảnh
        if (data.avatar != null)
            avatarImage.sprite = data.avatar; // Đảm bảo UnitSO của bạn có biến 'icon' kiểu Sprite

        // 2. Hiển thị trạng thái (Đã chọn hay chưa)
        if (selectedOverlay != null)
            selectedOverlay.SetActive(isSelected);

        if (levelText != null)
        {
            // Sau này lấy level từ SaveData, tạm thời lấy từ SO
            levelText.text = "Lv." + data.level.ToString();
        }

        // 3. Hiển thị Role Icon (Mũi tên)
        if (roleIconImage != null)
        {
            if (roleSprite != null)
            {
                roleIconImage.sprite = roleSprite;
                roleIconImage.gameObject.SetActive(true);
            }
            else
            {
                roleIconImage.gameObject.SetActive(false);
            }
        }

        // 3. Gán sự kiện click
        btnClick.onClick.RemoveAllListeners();
        btnClick.onClick.AddListener(() =>
        {
            onClickCallback?.Invoke(myData);
        });
    }
}