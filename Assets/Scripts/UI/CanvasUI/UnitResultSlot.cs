using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // Cần thiết cho hiệu ứng chạy thanh EXP

public class UnitResultSlot : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI levelText;
    public Slider expSlider;
    public TextMeshProUGUI expGainedText;

    /// <summary>
    /// Hàm này được gọi từ DisplayUnitsResult
    /// </summary>
    public void Setup(int level, int currentExp, int maxExp, int expGained)
    {
        // 1. Hiển thị Avatar và Level
        if (levelText != null) levelText.text = "Lv." + level;

        // 2. Hiển thị số EXP nhận được
        if (expGainedText != null)
        {
            expGainedText.text = $"+{expGained} XP";
            // Hiệu ứng nhảy chữ nhẹ (Optional)
            expGainedText.transform.localScale = Vector3.one;
            expGainedText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f);
        }

        // 3. Xử lý thanh Slider EXP
        if (expSlider != null)
        {
            expSlider.maxValue = maxExp;

            // --- LOGIC HIỆU ỨNG ---
            // Để thanh EXP chạy mượt, ta giả bộ đặt giá trị cũ trước khi cộng
            // (Nếu EXP hiện tại < EXP nhận được nghĩa là vừa lên cấp, ta cứ cho chạy từ 0)
            float startVal = Mathf.Max(0, currentExp - expGained);

            expSlider.value = startVal; // Đặt vị trí bắt đầu
            expSlider.DOValue(currentExp, 1.5f).SetEase(Ease.OutQuad); // Chạy đến vị trí hiện tại
        }
    }
}