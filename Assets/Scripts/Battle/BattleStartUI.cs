using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // Cần thiết cho DOTween
using System.Collections;

public class BattleStartUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Kéo GameObject cha chứa cả chữ và hình ảnh (kiếm, sao) vào đây")]
    public CanvasGroup contentCanvasGroup;
    [Tooltip("Kéo Transform của chữ/hình ảnh để scale")]
    public RectTransform contentTransform;

    [Header("Animation Settings")]
    public float scaleDuration = 0.5f;
    public float stayDuration = 2.5f;
    public float fadeOutDuration = 0.3f;

    [Header("Audio")]
    public AudioClip startSfx;

    private void Awake()
    {
        // Đảm bảo ban đầu nó ẩn đi
        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.alpha = 0;
            contentCanvasGroup.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Hàm này trả về Tween để Coroutine bên WaveScene có thể chờ nó chạy xong
    /// </summary>
    public Tween PlayStartSequence()
    {
        if (contentCanvasGroup == null || contentTransform == null) return null;

        contentCanvasGroup.gameObject.SetActive(true);

        // 1. Reset trạng thái ban đầu
        contentCanvasGroup.alpha = 1; // Hiện rõ
        contentTransform.localScale = Vector3.zero; // Thu nhỏ về 0

        // Phát âm thanh
        //if (startSfx != null && AudioManager.Instance != null)
        //{
        //    AudioManager.Instance.PlaySFX(startSfx);
        //}

        // 2. Tạo một Sequence (Chuỗi hành động)
        Sequence mySequence = DOTween.Sequence();

        // GIAI ĐOẠN 1: Xuất hiện (Pop up)
        // Scale từ 0 lên 1, dùng Ease.OutBack để nó nảy ra ngoài một chút rồi thu lại (đàn hồi)
        mySequence.Append(contentTransform.DOScale(1f, scaleDuration).SetEase(Ease.OutBack));

        // GIAI ĐOẠN 2: Hiệu ứng chấn động (Punch)
        // Rung nhẹ cái chữ để tạo cảm giác lực mạnh
        mySequence.Append(contentTransform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 10, 1));

        // GIAI ĐOẠN 3: Giữ nguyên để người chơi đọc (Interval)
        mySequence.AppendInterval(stayDuration);

        // GIAI ĐOẠN 4: Biến mất (Fade Out + Scale Up nhẹ)
        // Vừa mờ dần...
        mySequence.Append(contentCanvasGroup.DOFade(0f, fadeOutDuration));
        // ...Vừa phóng to ra thêm chút nữa cho kịch tính (Join chạy song song với Append trên)
        mySequence.Join(contentTransform.DOScale(1.5f, fadeOutDuration));

        // Khi chạy xong thì tắt object đi
        mySequence.OnComplete(() =>
        {
            contentCanvasGroup.gameObject.SetActive(false);
        });

        return mySequence;
    }
}