using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening.Core.Easing;

public class StageNodeUI : MonoBehaviour
{
    [Header("Data")]
    public StageSO stageData;

    [Header("UI References")]
    public Button btnStage;
    public Image stageImage;      // <-- Kéo cái Image ngôi nhà vào đây
    public GameObject lockIcon;   // Kéo icon ổ khóa vào đây
    public GameObject[] stars;
    public GameObject[] emptyStars;

    // Màu khi bị khóa (Xám tối)
    private Color lockedColor = new Color(113, 113, 113, 255);
    // Màu bình thường (Trắng - tức là giữ nguyên màu ảnh gốc)
    private Color unlockedColor = Color.white;

    private void Start()
    {
        if (btnStage != null)
            btnStage.onClick.AddListener(OnStageClicked);

        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (stageData == null) return;

        // 1. Lấy trạng thái mở khóa
        var mapModel = PlayerDataManager.Instance.MapProgressModel;
        bool isUnlocked = mapModel.IsStageUnlock(stageData.stageID);
        bool isCompleted = mapModel.IsStageCompleted(stageData.stageID);
        int starsEarned = mapModel.GetStarsForStage(stageData.stageID);

        // 2. Cập nhật Nút bấm và Ổ khóa
        if (btnStage != null) btnStage.interactable = isUnlocked;
        if (lockIcon != null) lockIcon.SetActive(!isUnlocked);

        // 3. Cập nhật Màu sắc ngôi nhà (QUAN TRỌNG)
        if (stageImage != null)
        {
            stageImage.color = isUnlocked ? unlockedColor : lockedColor;
        }

        // 4. Hiển thị sao
        if (stars != null)
        {
            foreach (var star in stars) star.SetActive(false); // Ẩn hết trước
            if (isCompleted)
            {
                // Nếu đã thắng: Bật sao thật tương ứng với điểm, sao rỗng cho phần thiếu
                for (int i = 0; i < 3; i++)
                {
                    if (i < starsEarned && stars != null && i < stars.Length)
                        stars[i].SetActive(true);
                    else if (emptyStars != null && i < emptyStars.Length)
                        emptyStars[i].SetActive(true); // Sao bị rớt (không đạt tối đa)
                }
            }
            else if (isUnlocked)
            {
                // Nếu mới mở khóa (chưa đánh thắng): Hiện toàn bộ sao rỗng
                if (emptyStars != null)
                {
                    foreach (var es in emptyStars) es.SetActive(true);

                }
            }
            else
            {
                // Nếu bị khóa: Ẩn hết sao
                if (emptyStars != null)
                {
                    foreach (var es in emptyStars) es.SetActive(false);
                }
            }
        }
        Debug.Log(stageData.stageID + " - Unlocked: " + isUnlocked + ", Completed: " + isCompleted + ", Stars: " + starsEarned);
    }

    private void OnStageClicked()
    {
        // Gọi Popup manager...
        MapUIManager.Instance.ShowStageInfo(stageData);
    }
}