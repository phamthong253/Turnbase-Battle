using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class EquipmentSlotUI : MonoBehaviour
{
    [Header("UI Components")]
    public Image iconImage;       // Ảnh vật phẩm
    public Image frameImage;      // Ảnh khung viền
    public GameObject plusIcon;   // Dấu cộng
    public GameObject selectedBorder; // Viền chọn
    public TextMeshProUGUI howToObtain;

    [Header("Default Icon Image")]
    private int _slotIndex;
    private System.Action<int> _onClickAction;

    private Tween _pulseTween;


    public void Setup(ItemSO item, bool isEquipped, bool hasInInventory, int slotIndex, System.Action<int> onClick, Sprite defaultSprite)
    {
        _slotIndex = slotIndex;
        _onClickAction = onClick;
        if (_pulseTween != null) _pulseTween.Kill();
        transform.DOKill();
        transform.localScale = Vector3.one; // Trả về kích thước gốc

        // Check null an toàn cho các component UI
        if (iconImage == null || frameImage == null) return;

        // LOGIC MỚI: Xử lý hiển thị dựa trên trạng thái đã mặc hay chưa
        if (isEquipped)
        {
            // --- TRƯỜNG HỢP 1: ĐÃ MẶC ---
            // Hiển thị ảnh thật của Item
            if (item != null) iconImage.sprite = item.itemAvatar;

            // Hiển thị khung và lấy màu theo độ hiếm
            frameImage.gameObject.SetActive(true);
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.rarityConfig != null && item != null)
            {
                frameImage.sprite = PlayerDataManager.Instance.rarityConfig.GetRarityIcon(item.itemRare);
            }

            // Tắt các hiệu ứng khác
            iconImage.color = Color.white;
            if (howToObtain != null) howToObtain.gameObject.SetActive(false);
            if (plusIcon) plusIcon.SetActive(false);
        }
        else
        {
            // --- TRƯỜNG HỢP 2: CHƯA MẶC ---
            // Hiển thị ảnh mặc định (placeholder)
            if (defaultSprite != null)
            {
                iconImage.sprite = defaultSprite;
                iconImage.rectTransform.sizeDelta = new Vector2(130f, 130f);
            }
            else
            {
                // Fallback nếu quên kéo ảnh mặc định: vẫn hiện ảnh item nhưng tối đi
                if (item != null) iconImage.sprite = item.itemAvatar;
            }

            // ẨN KHUNG ĐI (Theo yêu cầu của bạn)
            frameImage.gameObject.SetActive(false);
            
            if (!isEquipped && hasInInventory && item != null)
            {
                _pulseTween = iconImage.transform.DOScale(1.08f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            }
        }

        // Gắn sự kiện Click
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);
        }
    }


    // Hàm này sẽ được Button gọi khi người chơi bấm vào
    public void OnClick()
    {
        _onClickAction?.Invoke(_slotIndex);
    }
    public void SetSelected(bool isSelected)
    {
        if (selectedBorder != null) selectedBorder.SetActive(isSelected);
    }
}