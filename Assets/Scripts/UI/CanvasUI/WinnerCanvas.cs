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

    [Header("Win UI")]
    [Tooltip("Kéo 3 hình ngôi sao (Image) vào đây theo thứ tự 1, 2, 3")]
    public GameObject[] stars; // <-- MỚI THÊM: Mảng chứa 3 ngôi sao

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
        if (winCanvasRoot != null) winCanvasRoot.SetActive(false);

        // Gán sự kiện nút bấm
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(ClickNext);
        }
    }

    // --- HÀM MỚI: NHẬN SỐ SAO TỪ WAVE SCENE ---
    public void SetupWinScreen(int starCount)
    {
        // Reset: Tắt hết các ngôi sao trước khi bật lại
        if (stars != null)
        {
            foreach (var s in stars)
            {
                if (s != null) s.SetActive(false);
            }

            // Bật số sao tương ứng
            // Nếu starCount = 3 -> i chạy 0, 1, 2 -> Bật sao[0], sao[1], sao[2]
            for (int i = 0; i < starCount; i++)
            {
                if (i < stars.Length && stars[i] != null)
                {
                    stars[i].SetActive(true);
                }
            }
        }

        Debug.Log($"WinnerCanvas: Đã setup hiển thị {starCount} sao.");
    }

    // --- HÀM NÀY SẼ ĐƯỢC GỌI KHI HẾT TRẬN ---
    public void ActivateWinScreen()
    {
        Debug.Log("WinnerCanvas: Đã nhận lệnh hiển thị chiến thắng!");

        // 1. Hiện giao diện lên
        if (winCanvasRoot != null) winCanvasRoot.SetActive(true);

        // 2. Ẩn UI cũ của gameplay đi (nếu cần)
        if (WaveScene.Instance != null && WaveScene.Instance.WinnerUI != null)
        {
            WaveScene.Instance.WinnerUI.SetActive(false);
        }

        // 3. Render danh sách vật phẩm
        ShowRewardList();
    }

    public void ShowRewardList()
    {
        if (rewardDisplayPanel != null) rewardDisplayPanel.SetActive(true);

        // Lấy danh sách từ RewardManager
        if (RewardManager.Instance != null)
        {
            List<ItemRewardEntry> finalRewards = RewardManager.Instance.GetFinalRewardListForDisplay();
            // Render ra màn hình
            DisplayFinalRewardList(finalRewards);
        }
    }

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

            // TRƯỜNG HỢP 1: Là Crystal
            if (entry.ItemID == "CRYSTAL_KEY" && RewardManager.Instance != null)
            {
                displayIcon = RewardManager.Instance.crystalIcon;
                displayRare = ItemSO.ItemRare.S;
            }
            // TRƯỜNG HỢP 2: Là Item thật
            else if (entry.ItemData != null)
            {
                displayIcon = entry.ItemData.itemAvatar;
                displayRare = entry.ItemData.itemRare;
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

        // Logic bấm Next:
        // Chốt sổ, lưu vào kho
        if (RewardManager.Instance != null)
        {
            RewardManager.Instance.FinalizeReward();
            RewardManager.Instance.StartNewReward();
        }

        // Chuyển Scene (Ví dụ về MapScene hoặc GachaScene)
        Debug.Log("Chuyển về MapScene...");
        SceneManager.LoadScene("MapScene"); // Đổi tên Scene đích của bạn tại đây
    }
}