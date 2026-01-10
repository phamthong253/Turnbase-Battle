using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinnerCanvas : MonoBehaviour
{
    // 1. Tạo Singleton để dễ gọi từ bất cứ đâu
    public static WinnerCanvas Instance { get; private set; }

    [Header("Main Panels")]
    public GameObject winCanvasRoot;          // Kéo cái Panel chứa tất cả UI vào đây
    public GameObject rewardDisplayPanel;     // Panel hiển thị danh sách item

    [Header("Reward UI")]
    public Transform itemContainer;
    public RewardItemSlot itemSlotPrefab;
    public Button nextButton;

    private int clickCount = 0;

    private void Awake()
    {
        // Setup Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 2. TỰ ĐỘNG ẨN GIAO DIỆN KHI VÀO GAME
        // GameObject WinnerCanvas vẫn Active, nhưng hình ảnh thì ẩn đi
        if (winCanvasRoot != null) winCanvasRoot.SetActive(false);

        // Gán sự kiện nút bấm
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(ClickNext);
    }

    // --- HÀM NÀY SẼ ĐƯỢC GỌI KHI HẾT TRẬN ---
    public void ActivateWinScreen()
    {
        Debug.Log("WinnerCanvas: Đã nhận lệnh hiển thị chiến thắng!");

        // 1. Hiện giao diện lên
        winCanvasRoot.SetActive(true);

        // 2. Ẩn UI cũ của gameplay đi (nếu cần)
        if (WaveScene.Instance != null && WaveScene.Instance.WinnerUI != null)
        {
            WaveScene.Instance.WinnerUI.SetActive(false);
        }

        // 3. Render danh sách vật phẩm NGAY LẬP TỨC
        ShowRewardList();
    }

    public void ShowRewardList()
    {
        rewardDisplayPanel.SetActive(true);

        // Lấy danh sách từ RewardManager
        List<ItemRewardEntry> finalRewards = RewardManager.Instance.GetFinalRewardListForDisplay();

        // Render ra màn hình
        DisplayFinalRewardList(finalRewards);
        
    }

    // ... (Giữ nguyên hàm DisplayFinalRewardList và ClickNext của bạn ở đây) ...
    public void DisplayFinalRewardList(List<ItemRewardEntry> finalRewards)
    {
        // --- CHECK 1: Kiểm tra Container ---
        if (itemContainer == null)
        {
            Debug.LogError("LỖI: Bạn chưa kéo thả 'Item Container' vào WinnerCanvas trong Inspector!");
            return;
        }

        // --- CHECK 2: Kiểm tra Prefab ---
        if (itemSlotPrefab == null)
        {
            Debug.LogError("LỖI: Bạn chưa kéo thả Prefab 'RewardItemSlot' vào WinnerCanvas trong Inspector!");
            return;
        }
        // 1. Xóa item cũ
        if (itemContainer.childCount > 0)
        {
            foreach (Transform child in itemContainer) Destroy(child.gameObject);
        }
        if (finalRewards == null) return;

        // 2. Tạo UI từng item
        foreach (var entry in finalRewards)
        {
            // Tạo ra slot mới
            RewardItemSlot newSlot = Instantiate(itemSlotPrefab, itemContainer);

            Sprite displayIcon = null;
            ItemSO.ItemRare displayRare = ItemSO.ItemRare.B; // Mặc định

            // TRƯỜNG HỢP 1: Là Crystal (Không có ItemSO, lấy icon từ Manager)
            if (entry.ItemID == "CRYSTAL_KEY")
            {
                displayIcon = RewardManager.Instance.crystalIcon;
                displayRare = ItemSO.ItemRare.S; // Crystal cho màu xịn chút
            }
            // TRƯỜNG HỢP 2: Là Item thật (Có ItemSO)
            else if (entry.ItemData != null)
            {
                displayIcon = entry.ItemData.itemAvatar; // Lấy ảnh từ ItemSO của bạn
                displayRare = entry.ItemData.itemRare;   // Lấy độ hiếm từ ItemSO của bạn
            }

            // GỌI HÀM SETUP ĐỂ HIỂN THỊ
            if (displayIcon != null)
            {
                newSlot.Setup(displayIcon, entry.Quantity, displayRare);
                newSlot.gameObject.SetActive(true);
            }
        }
    }
    void ClickNext()
    {
        clickCount++;

        // Lần click 1: Hiển thị danh sách vật phẩm
        if (clickCount == 1)
        {
            // Chốt sổ, lưu vào kho
            RewardManager.Instance.FinalizeReward();
            RewardManager.Instance.StartNewReward();
            // Nếu bạn muốn bấm Next mới hiện list thì dùng dòng này
            // Nhưng logic hiện tại là hiện luôn khi thắng, nên nút Next này có thể dùng để chuyển Scene luôn
            SceneManager.LoadScene("GachaScene");
            return;
        }
    }
}