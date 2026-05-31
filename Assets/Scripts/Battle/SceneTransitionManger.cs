using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum TransitionType
{
    Fade,
    SlideLeft,      // Vào từ trái, ra về trái (như cửa đóng mở)
    WipeRight,      // Quét một mạch từ Trái sang Phải (như lật trang sách/quét sơn)
    Zoom
}

public class SceneTransitionManager : MonoBehaviour // Đổi tên class cho khớp với ảnh của bạn
{
    [Header("Settings")]
    public TransitionType effectType = TransitionType.WipeRight; // Chọn cái này
    public float duration = 0.6f; // Tăng lên xíu cho mượt
    public Ease easeType = Ease.InOutQuad;

    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public RectTransform contentRect;

    private Vector2 hiddenPosLeft;
    private Vector2 centerPos = Vector2.zero;
    private Vector2 hiddenPosRight; // Vị trí ẩn bên phải

    private void Awake()
    {
        SetupInitialState();
    }

    private void SetupInitialState()
    {
        if (contentRect != null)
        {
            // Lấy chiều rộng màn hình (Canvas) thay vì chiều rộng ảnh
            // Để đảm bảo ảnh trượt hẳn ra ngoài màn hình
            float screenWidth = 1920f; // Hoặc lấy từ CanvasScaler nếu muốn chính xác tuyệt đối
            if (GetComponentInParent<Canvas>() != null)
            {
                screenWidth = GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect.width;
            }

            // Tính toán vị trí:
            // Ảnh cần dịch chuyển một khoảng đủ lớn để mép răng cưa đi khuất
            float moveDistance = screenWidth + (contentRect.rect.width / 2);

            hiddenPosLeft = new Vector2(-moveDistance, 0);
            hiddenPosRight = new Vector2(moveDistance, 0);
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        ResetToHidden();
    }

    private void ResetToHidden()
    {
        if (canvasGroup == null) return;

        switch (effectType)
        {
            case TransitionType.Fade:
                canvasGroup.alpha = 0f;
                break;
            case TransitionType.SlideLeft:
            case TransitionType.WipeRight: // Wipe cũng bắt đầu từ bên trái
                canvasGroup.alpha = 1f;
                if (contentRect != null) contentRect.anchoredPosition = hiddenPosLeft;
                break;
            case TransitionType.Zoom:
                canvasGroup.alpha = 1f;
                if (contentRect != null) contentRect.localScale = Vector3.zero;
                break;
        }
    }

    public Tween FadeIn()
    {
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        switch (effectType)
        {
            case TransitionType.SlideLeft:
            case TransitionType.WipeRight:
                // Cả 2 đều trượt từ trái vào giữa để che màn hình
                return contentRect.DOAnchorPos(centerPos, duration).SetEase(easeType);

            case TransitionType.Zoom:
                return contentRect.DOScale(1f, duration).SetEase(easeType);

            case TransitionType.Fade:
            default:
                return canvasGroup.DOFade(1f, duration).SetEase(easeType);
        }
    }

    public Tween FadeOut()
    {
        Tween t = null;

        switch (effectType)
        {
            case TransitionType.SlideLeft:
                // Lùi về trái (đóng cửa rồi mở lại cửa cũ)
                t = contentRect.DOAnchorPos(hiddenPosLeft, duration).SetEase(easeType);
                break;

            case TransitionType.WipeRight:
                // ĐI TIẾP SANG PHẢI (Quét qua luôn)
                // Sau khi quét xong, nó cần reset vị trí về bên trái ngay lập tức để chuẩn bị cho lần sau
                t = contentRect.DOAnchorPos(hiddenPosRight, duration).SetEase(easeType)
                    .OnComplete(() => {
                        // Reset âm thầm về bên trái để lần sau dùng tiếp
                        contentRect.anchoredPosition = hiddenPosLeft;
                    });
                break;

            case TransitionType.Zoom:
                t = contentRect.DOScale(0f, duration).SetEase(easeType);
                break;

            case TransitionType.Fade:
            default:
                t = canvasGroup.DOFade(0f, duration).SetEase(easeType);
                break;
        }

        if (t != null)
        {
            t.OnComplete(() => {
                if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
                // Nếu là WipeRight, logic reset đã nằm ở trên
            });
        }

        return t;
    }
}