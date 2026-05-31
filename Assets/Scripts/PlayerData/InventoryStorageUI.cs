// InventoryStorageUI.cs
using System.Collections.Generic;
using System.Linq;
using TMPro; // Nếu dùng TextMeshPro
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InventoryStorageUI : MonoBehaviour
{
    [Header("UI References")]
    //public GameObject itemSlotPrefab; // Prefab của Slot UI
    public Transform slotContainer; // Parent Transform
    private List<Transform> contentParent = new List<Transform>();
    [Header("Detail Panel References")]
    public RectTransform selectTranform; // Cái khung viền sáng để highlight ô đang chọn
    public GameObject detailPanel; // Cái bảng to để hiện thông tin
    public Image detailIcon;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailDescText;
    public TextMeshProUGUI detailStatsText; // Sẽ dùng hàm GetStatsInfo() của bạn

    [Header("Button")]
    public Button actionBtn;
    public TextMeshProUGUI actionNameText;

    private Transform currentlySelectedSlot; // Biến lưu trữ ô đang được chọn để dễ dàng truy cập khi cần
    private void Awake()
    {
        if (InventoryUIHandler.Instance != null)
        {
        }
        if (slotContainer == null)
        {
            Debug.LogError("slotContainer chưa được gán trong InventoryStorageUI!");
            return;
        }
        contentParent.Clear();
        foreach (Transform child in slotContainer)
            {
            if (child.name.Contains("Highlight")) continue;
            contentParent.Add(child);
        }
        if (InventoryUIHandler.Instance != null)
        {
            // "this.gameObject" chính là cái Panel đang gắn script này
            InventoryUIHandler.Instance.RegisterPanel(this.gameObject);
        }
    }
    private void OnEnable()
    {
        if(PlayerDataManager.Instance == null)
        {
            Debug.Log("PlayerDataManager chưa được khởi tạo");
            return;
        }
        RenderInventory();
        if(PlayerDataManager.Instance.InventoryModel != null)
        {
            PlayerDataManager.Instance.OnInventoryUpdated += RenderInventory;
        }
    }
    private void OnDisable()
    {
        if(PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnInventoryUpdated -= RenderInventory;
        }
    }

    public void RenderInventory()
    {
        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.InventoryModel == null) return;

        // --- BƯỚC 1: LÀM SẠCH TOÀN BỘ SLOTS (Fix lỗi Bóng Ma) ---
        foreach (Transform slot in slotContainer)
        {
            TextMeshProUGUI quantityText = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (quantityText != null) quantityText.text = "";

            // Phải tắt (hoặc làm trong suốt) cái Icon đi
            Transform iconTransform = slot.Find("Image");
            if (iconTransform != null)
            {
                iconTransform.gameObject.SetActive(false); // Ẩn hoàn toàn icon
            }
            // Nhớ TẮT luôn cái Highlight đi khi làm sạch
            Transform highlight = slot.Find("Highlight");
            if (highlight != null) highlight.gameObject.SetActive(false);

            // Xóa sự kiện click cũ để tránh bị gọi đúp
            Button btn = slot.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners();
        }

        var inventory = PlayerDataManager.Instance.InventoryModel;
        int slotIndex = 0;
        int totalSlot = contentParent.Count;

        Debug.Log("--- BẮT ĐẦU RENDER KHO ĐỒ ---");

        // --- BƯỚC 2: HIỂN THỊ ITEMS/EQUIPMENT ---
        foreach (var item in inventory.ItemInventory)
        {
            // Bỏ qua các item đã xài hết (SL <= 0)
            if (item.Value <= 0) continue;

            if (slotIndex >= totalSlot)
            {
                Debug.LogWarning("Số lượng slot UI không đủ để chứa tất cả vật phẩm!");
                break;
            }
            CreateItemSlot(contentParent[slotIndex], item.Key, item.Value, isShard: false);
            slotIndex++;
        }

        // --- BƯỚC 3: HIỂN THỊ UNIT SHARDS ---
        foreach (var pair in inventory.UnitShardInventory)
        {
            if (pair.Value <= 0) continue;

            if (slotIndex >= totalSlot) break;
            CreateItemSlot(contentParent[slotIndex], pair.Key, pair.Value, isShard: true);
            slotIndex++;
        }

        Debug.Log($"--- KẾT THÚC RENDER: Hiển thị {slotIndex}/{totalSlot} ô ---");
    }

    private void CreateItemSlot(Transform slotTransform, string id, int quantity, bool isShard)
    {
        Sprite iconToUse = null;

        if (GameDataService.Instance == null) return;

        // 1. TRA CỨU DỮ LIỆU TĨNH
        if (isShard)
        {
            UnitSO unitSO = GameDataService.Instance.GetUnitSO(id);
            if (unitSO != null) iconToUse = unitSO.avatar; // Hoặc bạn có avatar mảnh ghép riêng
        }
        else
        {
            ItemSO itemSO = GameDataService.Instance.GetItemSO(id);
            if (itemSO != null) iconToUse = itemSO.itemAvatar;
        }

        if (iconToUse == null)
        {
            Debug.LogError($"[ASSET MISSING] Không tìm thấy Sprite cho ID '{id}'");
            return;
        }

        // 2. RENDER LÊN UI (Bật lại Icon)
        Image iconImage = slotTransform.Find("Image")?.GetComponent<Image>();
        if (iconImage != null)
        {
            iconImage.sprite = iconToUse;
            iconImage.gameObject.SetActive(true); // BẬT SÁNG ICON LÊN!
        }

        TextMeshProUGUI quantityText = slotTransform.GetComponentInChildren<TextMeshProUGUI>();
        if (quantityText != null)
        {
            quantityText.text = quantity.ToString();
        }
        Button slotBtn = slotTransform.GetComponent<Button>();
        if (slotBtn != null)
        {
            slotBtn.onClick.RemoveAllListeners(); // Xóa rác cũ

            // LƯU Ý: Phải gán id và quantity ra một biến local để lambda hiểu đúng (Fix lỗi Closure trong C#)
            string clickId = id;
            int clickQty = quantity;
            bool clickIsShard = isShard;

            slotBtn.onClick.AddListener(() =>
            {
                Debug.Log($"[Click Test] Bạn vừa bấm vào ô chứa ID: {clickId}");
                OnSlotClicked(slotTransform, clickId, clickQty, clickIsShard);
            });
        }
        else
        {
            Debug.LogError($"[Thiếu Button] Lỗi: Ô đồ tên '{slotTransform.name}' không có component Button!");
        }
    }
    private void OnSlotClicked(Transform clickedSlot, string id, int quantity, bool isShard)
    {
        // 1. XỬ LÝ HIỆU ỨNG VIỀN SÁNG (HIGHLIGHT)
        if (selectTranform != null)
        {
            selectTranform.gameObject.SetActive(true); // Bật nó lên

            // Ép nó làm con của ô vừa click để nó di chuyển theo nếu cuộn (Scroll)
            selectTranform.SetParent(clickedSlot, false);

            // Căn giữa nó hoàn hảo so với ô đồ
            selectTranform.localPosition = Vector3.zero;

            // Đặt nó ở vị trí cuối cùng trong Hierarchy của ô đồ để nó vẽ ĐÈ lên trên Icon
            selectTranform.SetAsLastSibling();
        }

        // Lưu lại ô này làm ô đang được chọn
        currentlySelectedSlot = clickedSlot;

        // 2. HIỂN THỊ THÔNG TIN LÊN DETAIL PANEL
        if (detailPanel != null)
        {
            detailPanel.SetActive(true); // Bật panel lên

            if (isShard)
            {
                UnitSO unitSO = GameDataService.Instance.GetUnitSO(id);
                if (unitSO != null)
                {
                    detailIcon.sprite = unitSO.avatar;
                    detailNameText.text = unitSO.name + " Shard";
                    detailDescText.text = "Thu thập đủ mảnh để triệu hồi hoặc thăng sao cho tướng này.";
                    actionNameText.text = "Go to Unit"; // Ví dụ: Nút này có thể dẫn đến trang chi tiết tướng hoặc trang nâng cấp
                    actionBtn.onClick.AddListener(() => {SceneManager.LoadScene("UnitScene");}); // Ví dụ: Chuyển sang Scene Tướng khi bấm nút
                }
            }
            else
            {
                ItemSO itemSO = GameDataService.Instance.GetItemSO(id);
                if (itemSO != null)
                {
                    detailIcon.sprite = itemSO.itemAvatar;
                    detailNameText.text = itemSO.itemName;
                    detailDescText.text = itemSO.description;
                    actionNameText.text = itemSO.isEquipment ? "Equip Now" : "Use Now"; //
                    actionBtn.onClick.AddListener(() => {SceneManager.LoadScene("UnitScene");}); // Ví dụ: Chuyển sang Scene Tướng khi bấm nút)

                    // Ở đây tôi gọi chính xác cái hàm GetStatsInfo() mà bạn đã viết rất hay trong ItemSO!
                    if (detailStatsText != null)
                    {
                        detailStatsText.text = itemSO.GetStatsInfo();
                    }
                }
            }

            // (Tùy chọn) Thêm số lượng đang sở hữu vào tên hoặc góc panel
            // detailNameText.text += $" (Đang có: {quantity})";
        }
    }
    public void CloseInventoryPanel()
    {
        // 1. Tắt cái Bảng chi tiết và viền sáng đi (để lần sau mở lên nó gọn gàng)
        if (detailPanel != null) detailPanel.SetActive(false);
        if (selectTranform != null) selectTranform.gameObject.SetActive(false);

        // 2. Tắt chính cái Panel Giao diện Túi đồ này đi
        gameObject.SetActive(false);
        // (Lưu ý: Nếu script này không nằm trên Panel gốc mà nằm ở con, 
        // hãy đổi thành myPanel.SetActive(false); với myPanel là biến GameObject bạn đã khai báo)
    }
}