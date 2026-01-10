using UnityEngine;
using UnityEngine.UI; // Cần thiết cho các component UI
using System.Collections.Generic;
using TMPro; // Nếu bạn muốn hiển thị nhiều kết quả

public class GachaUIHandler : MonoBehaviour
{
    // --- KHAI BÁO UI COMPONENTS ---
    [Header("Gacha Buttons")]
    public Button roll1xButton;
    public Button roll10xButton;

    [Header("Gacha Costs Display")]
    public TextMeshProUGUI roll1xCostText;
    public TextMeshProUGUI roll10xCostText;

    // --- KHAI BÁO SCRIPTABLE OBJECTS (Chi phí Gacha) ---
    [Header("Gacha Cost Scriptable Objects")]
    public GachaCostSO gachaCost1x;
    public GachaCostSO gachaCost10x;

    // --- SERVICE GACHA (Để thực hiện Roll) ---
    [Header("Gacha Service (Assign in Inspector)")]
    public GachaServiceManager gachaService; // Kéo thả đối tượng GachaService vào đây

    private void Start()
    {
        // Kiểm tra xem PlayerDataManager đã có Instance chưa
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("PlayerDataManager chưa được khởi tạo! Hãy đảm bảo nó tồn tại trong Scene đầu tiên hoặc được DontDestroyOnLoad.");
            return;
        }
        PlayerDataManager.Instance.OnCurrencyChanged += OnCurrencyChangedHandler;

        // --- CẤU HÌNH CÁC NÚT BẤM ---
        if (roll1xButton != null)
        {
            roll1xButton.onClick.AddListener(() => OnRollButtonClicked(gachaCost1x));
        }
        if (roll10xButton != null)
        {
            roll10xButton.onClick.AddListener(() => OnRollButtonClicked(gachaCost10x));
        }

        // --- CẬP NHẬT TEXT CHI PHÍ BAN ĐẦU ---
        if (roll1xCostText != null && gachaCost1x != null)
        {
            roll1xCostText.text = $"{gachaCost1x.CrystalCost} Crystal";
        }
        if (roll10xCostText != null && gachaCost10x != null)
        {
            roll10xCostText.text = $"{gachaCost10x.CrystalCost} Crystal";
        }

        // Cập nhật trạng thái nút ban đầu (mở/khóa)

        // Đăng ký lắng nghe sự kiện thay đổi tiền tệ để cập nhật trạng thái nút
        //PlayerDataManager.Instance.OnCurrencyChanged += UpdateGachaButtonsState;
    }

    private void OnDestroy()
    {
        // Hủy đăng ký để tránh lỗi khi Scene bị hủy
        if (PlayerDataManager.Instance != null)
        {
            //PlayerDataManager.Instance.OnCurrencyChanged -= UpdateGachaButtonsState;
        }
    }

    /// <summary>
    /// Hàm xử lý khi nhấn nút Roll.
    /// </summary>
    /// <param name="costData">Dữ liệu chi phí của lần Roll này.</param>
    private void OnRollButtonClicked(GachaCostSO costData)
    {
        if (costData == null || gachaService == null)
        {
            Debug.LogError("GachaCostSO hoặc GachaService chưa được gán!");
            return;
        }

        Debug.Log($"Đang thử Gacha: {costData.GachaName} với chi phí {costData.CrystalCost}");

        // Gọi hàm TryGacha từ PlayerDataManager
        bool success = PlayerDataManager.Instance.TryGacha(costData, gachaService);

        if (success)
        {
            Debug.Log("Gacha thành công!");
            // TODO: Hiển thị kết quả Gacha (Unit mới, Shard, Item)
            // Bạn sẽ cần một Panel/Popup để hiển thị list GachaReward
        }
        else
        {
            Debug.LogWarning("Gacha thất bại: Không đủ Crystal hoặc lỗi khác.");
            // TODO: Hiển thị thông báo "Không đủ Crystal" trên UI
        }
    }
    private void OnCurrencyChangedHandler(int newCount)
    {
        // Bỏ qua tham số và gọi hàm không tham số
        UpdateGachaButtonsState();
    }
    private void UpdateGachaButtonsState()
    {
        int currentCrystal = (PlayerDataManager.Instance.CurrencyModel.GetTotalCrystals());
        if (gachaCost1x != null && roll1xButton != null)
        {
            roll1xButton.interactable = PlayerDataManager.Instance.CurrencyModel.CanAffordGacha(gachaCost1x.CrystalCost);
        }
        if (gachaCost10x != null && roll10xButton != null)
        {
            roll10xButton.interactable = PlayerDataManager.Instance.CurrencyModel.CanAffordGacha(gachaCost10x.CrystalCost);
        }
    }
}