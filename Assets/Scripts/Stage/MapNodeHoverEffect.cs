using UnityEngine;
using UnityEngine.EventSystems; // Cần thư viện này để bắt sự kiện chuột
using DG.Tweening;
using UnityEngine.UI; // Cần DOTween

public class MapNodeHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Settings")]
    public float hoverScale = 1.2f; // Phóng to lên 1.2 lần
    public float duration = 0.2f;   // Thời gian phóng to

    [Header("Sound (Optional)")]
    // public string hoverSfxName = "ui_hover";
    // public string clickSfxName = "ui_click";

    private Vector3 originalScale;
    private RectTransform rectTransform;
    private Button targetButton;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        targetButton = GetComponent<Button>();
    }

    // Khi chuột rê vào
    public void OnPointerEnter(PointerEventData eventData)
    {
        if(targetButton != null && !targetButton.interactable) return; // Không làm gì nếu nút không tương tác được
        // Dừng các tween cũ để tránh xung đột nếu rê chuột quá nhanh
        rectTransform.DOKill();

        // Phóng to với hiệu ứng nảy nhẹ (OutBack) cho sinh động
        rectTransform.DOScale(originalScale * hoverScale, duration).SetEase(Ease.OutBack);

        // AudioManager.Instance.PlaySFX(hoverSfxName); // Bật tiếng nếu cần
    }

    // Khi chuột rời đi
    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.DOKill();

        // Trả về kích thước gốc
        rectTransform.DOScale(originalScale, duration).SetEase(Ease.OutQuad);
    }

    // Khi click chuột (Hiệu ứng nhấn xuống)
    public void OnPointerClick(PointerEventData eventData)
    {
        rectTransform.DOKill();
        // Nhún xuống một chút rồi nảy lên
        rectTransform.DOScale(originalScale * 0.9f, 0.1f).OnComplete(() =>
        {
            rectTransform.DOScale(originalScale, 0.1f);
        });

        // AudioManager.Instance.PlaySFX(clickSfxName);
    }
}