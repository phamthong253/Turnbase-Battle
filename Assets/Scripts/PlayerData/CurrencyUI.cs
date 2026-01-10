using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro; // Cần cho Action

public class CurrencyUI : MonoBehaviour
{
    public TextMeshProUGUI crystalCountText; // Kéo thả Text UI vào đây

    private void Start()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("PlayerDataManager chưa được khởi tạo!");
            return;
        }

        // Cập nhật giá trị ban đầu
        UpdateCrystalUI(PlayerDataManager.Instance.CurrencyModel.GetTotalCrystals());

        // Đăng ký lắng nghe sự kiện khi số Crystal thay đổi
        PlayerDataManager.Instance.OnCurrencyChanged += UpdateCrystalUI;
    }

    private void OnDestroy()
    {
        // Hủy đăng ký để tránh lỗi
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnCurrencyChanged -= UpdateCrystalUI;
        }
    }

    /// <summary>
    /// Hàm được gọi khi số Crystal thay đổi, để cập nhật UI.
    /// </summary>
    /// <param name="newCrystalCount">Số Crystal mới.</param>
    private void UpdateCrystalUI(int newCrystalCount)
    {
        if (crystalCountText != null)
        {
            crystalCountText.text = newCrystalCount.ToString();
        }
    }
}