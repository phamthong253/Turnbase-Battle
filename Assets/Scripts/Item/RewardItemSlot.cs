using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardItemSlot : MonoBehaviour
{
    [Header("UI Components")]
    public Image iconImage;           // Kéo Image hiển thị icon vào đây
    public TextMeshProUGUI amountText; // Kéo Text hiển thị số lượng vào đây
    public Image rarityBackground;    // (Tùy chọn) Kéo Image nền để đổi màu theo độ hiếm

    // Hàm Setup đa năng: Dùng cho cả ItemSO và Crystal
    public void Setup(Sprite icon, int quantity, ItemSO.ItemRare rarity = ItemSO.ItemRare.B)
    {
        // 1. Gán hình ảnh
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.preserveAspect = true; // Giữ tỉ lệ ảnh ko bị méo
        }

        // 2. Gán số lượng
        if (amountText != null)
        {
            amountText.text = quantity > 1 ? $"x{quantity}" : ""; // Nếu là 1 thì ẩn số x1 đi cho đẹp (tùy bạn)
        }

        // 3. (Nâng cao) Đổi màu nền dựa trên độ hiếm từ ItemSO
        if (rarityBackground != null)
        {
            rarityBackground.color = GetColorByRarity(rarity);
        }
    }

    // Hàm phụ trợ để lấy màu theo độ hiếm
    private Color GetColorByRarity(ItemSO.ItemRare rarity)
    {
        switch (rarity)
        {
            case ItemSO.ItemRare.SSS: return Color.red;
            case ItemSO.ItemRare.S: return Color.yellow;
            case ItemSO.ItemRare.A: return Color.magenta;
            case ItemSO.ItemRare.B: return Color.blue;
            case ItemSO.ItemRare.C: return Color.grey;
            default: return Color.white;
        }
    }
}