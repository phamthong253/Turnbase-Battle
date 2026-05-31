using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // Bắt buộc
using TMPro;

public class SkillCutsceneManager : MonoBehaviour
{
    public static SkillCutsceneManager Instance;

    [Header("UI References")]
    public CanvasGroup mainCanvasGroup;    // Để Fade In/Out toàn bộ Canvas
    public RectTransform cutInContainer;   // Container chứa Thanh + Tướng
    public Image characterImage;           // Ảnh tướng
    public RawImage speedLines;            // Vệt tốc độ

    [Header("Settings")]
    public float enterDuration = 0.3f;     // Thời gian bay vào
    public float stayDuration = 1.5f;      // Thời gian dừng lại để ngầu
    public float exitDuration = 0.25f;     // Thời gian bay ra
    public Ease enterEase = Ease.OutBack;  // Hiệu ứng nảy khi vào
    public Ease exitEase = Ease.InCubic;   // Hiệu ứng bay ra nhanh

    private Vector2 originalPosition;      // Vị trí giữa màn hình (đích đến)
    private float screenWidth;

    private void Awake()
    {
        Instance = this;
        // Tắt Canvas lúc đầu
        mainCanvasGroup.alpha = 0;
        mainCanvasGroup.blocksRaycasts = false;

        // Lưu lại vị trí giữa màn hình và chiều rộng để tính toán
        originalPosition = cutInContainer.anchoredPosition;
        screenWidth = Screen.width;
    }

    /// <summary>
    /// Hàm gọi Cutscene. Gọi hàm này từ UnitController khi dùng Skill.
    /// </summary>
    /// <param name="unitSprite">Ảnh của tướng</param>
    public void PlayCutscene(Sprite unitSprite)
    {
        // 1. Setup dữ liệu
        characterImage.sprite = unitSprite;

        // 2. Reset vị trí về bên trái ngoài màn hình
        // Giả sử Container neo ở giữa, ta dịch nó sang trái quá khổ màn hình
        cutInContainer.anchoredPosition = new Vector2(-screenWidth * 1.5f, originalPosition.y);

        // Bật vệt tốc độ (Nếu bạn dùng script ScrollingEffect, đảm bảo nó dùng unscaledDeltaTime)
        speedLines.gameObject.SetActive(true);

        // 3. Bắt đầu DOTween Sequence
        Sequence seq = DOTween.Sequence();

        // QUAN TRỌNG: SetUpdate(true) để chạy được ngay cả khi Time.timeScale = 0
        seq.SetUpdate(true);

        // --- GIAI ĐOẠN 1: XUẤT HIỆN ---
        // Bật Canvas lên
        seq.Append(mainCanvasGroup.DOFade(1, 0.2f));

        // Container bay từ trái vào giữa
        seq.Join(cutInContainer.DOAnchorPos(originalPosition, enterDuration).SetEase(enterEase));

        // (Tùy chọn) Punch Scale: Nhân vật nảy lên 1 chút khi dừng lại cho cảm giác lực
        seq.Append(characterImage.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.2f));

        // --- GIAI ĐOẠN 2: DỪNG LẠI (SHOW OFF) ---
        seq.AppendInterval(stayDuration);

        // --- GIAI ĐOẠN 3: BIẾN MẤT ---
        // Container bay tiếp sang phải ra khỏi màn hình
        seq.Append(cutInContainer.DOAnchorPos(new Vector2(screenWidth * 1.5f, originalPosition.y), exitDuration).SetEase(exitEase));

        // Fade tắt Canvas
        seq.Join(mainCanvasGroup.DOFade(0, exitDuration));

        // Callback khi xong
        seq.OnComplete(() => {
            mainCanvasGroup.blocksRaycasts = false;
            // Nếu bạn có Pause game, nhớ Resume lại ở đây hoặc dùng Callback riêng
            // Time.timeScale = 1; 
        });
    }
    public void SetThemeColor(Color color)
    {
        speedLines.color = new Color(color.r, color.g, color.b, 1f);
    }
}