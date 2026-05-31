using DG.Tweening;
using TMPro;
using UnityEngine;

public class NotificationItem : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI msgText;
    public CanvasGroup canvasGroup;
    [Header("Animation Settings")]
    public float fadeInDuration = 0.3f;
    public float displayDuration = 2f;
    public void Setup(string message)
    {
        msgText.text = message;

        // Bắt đầu từ trạng thái trong suốt
        canvasGroup.alpha = 0f;

        // Đảm bảo vị trí bắt đầu luôn ở ngay tâm của Container (điểm gốc 0,0)
        transform.localPosition = Vector3.zero;

        Sequence seq = DOTween.Sequence();

        // 1. Hiện rõ lên nhanh chóng
        seq.Append(canvasGroup.DOFade(1f, 0.2f));

        // 2. Trôi lên trên liên tục trong suốt vòng đời của nó (Ví dụ: bay lên 150 pixel)
        // Dùng Ease.OutLinear hoặc Ease.OutQuad để tốc độ bay đều đặn, không bị khựng
        seq.Join(transform.DOLocalMoveY(250f, displayDuration + fadeInDuration).SetEase(Ease.OutQuad));

        // 3. Đợi nó hiển thị rõ ràng xong rồi từ từ mờ đi
        seq.Insert(displayDuration, canvasGroup.DOFade(0f, fadeInDuration));

        // 4. Tiêu hủy (Lúc này nó đã vô hình (alpha = 0) nên biến mất sẽ không ai thấy giật)
        seq.OnComplete(() => Destroy(gameObject));

        seq.SetUpdate(true);
    }
    public void SetupRollingNumber(string prefix, int startValue, int endValue, bool isIncrease)
    {
        // 1. Cấu hình màu sắc và mũi tên
        string colorTag = isIncrease ? "<color=#52FF33>" : "<color=#FF3333>"; // Xanh nếu tăng, Đỏ nếu giảm
        string arrow = isIncrease ? "▲" : "▼";
        string endColor = "</color>";

        canvasGroup.alpha = 0f;
        transform.localPosition = Vector3.zero;

        Sequence seq = DOTween.Sequence();

        // 2. Hiện rõ và Bay lên (giống hệt cũ)
        seq.Append(canvasGroup.DOFade(1f, 0.2f));
        seq.Join(transform.DOLocalMoveY(150f, displayDuration + fadeInDuration).SetEase(Ease.OutQuad));

        // 3. HIỆU ỨNG NHẢY SỐ (Chạy song song với lúc bay lên)
        int tempValue = startValue;

        // Gán text khởi điểm
        msgText.text = $"{prefix} {colorTag}{tempValue:N0} {arrow}{endColor}";

        // DOTween.To sẽ từ từ thay đổi biến tempValue từ startValue -> endValue
        seq.Join(DOTween.To(() => tempValue, x =>
        {
            tempValue = x;
            msgText.text = $"{prefix} {colorTag}{tempValue:N0} {arrow}{endColor}";
        }, endValue, displayDuration).SetEase(Ease.OutQuad)); // Chậm dần về cuối

        // 4. Mờ dần và Tiêu hủy
        seq.Insert(displayDuration, canvasGroup.DOFade(0f, fadeInDuration));
        seq.OnComplete(() => Destroy(gameObject));

        seq.SetUpdate(true);
    }
}
