using UnityEngine;
using UnityEngine.EventSystems; // Bắt buộc phải có để dùng Pointer Events
using DG.Tweening;

public class NavigationUIEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Settings")]
    public float hoverScale = 1.2f;
    public float animationDuration = 0.2f;
    public float selectedPunch = 0.1f; // Độ nảy khi click
    private bool isSelected = false;

    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    // Khi di chuột vào (Hover Enter)
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Phóng to icon
        transform.DOScale(originalScale * hoverScale, animationDuration)
                 .SetEase(Ease.OutBack); // Hiệu ứng nảy nhẹ
    }

    // Khi di chuột ra (Hover Exit)
    public void OnPointerExit(PointerEventData eventData)
    {
        // Thu nhỏ về lại ban đầu
        transform.DOScale(originalScale, animationDuration)
                 .SetEase(Ease.InQuad);
    }

    // Khi nhấp chuột vào (Click/Selected)
    public void OnPointerClick(PointerEventData eventData)
    {
        // Tạo hiệu ứng nhấn (Punch) để báo hiệu đã chọn
        transform.DOPunchScale(new Vector3(selectedPunch, selectedPunch, 0), animationDuration, 10, 1);

        // Bạn có thể thêm logic chuyển Scene hoặc bật Tab ở đây
        Debug.Log("Button Clicked!");
    }

    public void SetSelected(bool state)
    {
        isSelected = state;
        if (isSelected)
        {
            transform.DOScale(originalScale * hoverScale, animationDuration);
            // Có thể đổi màu icon sang vàng ở đây
        }
        else
        {
            transform.DOScale(originalScale, animationDuration);
        }
    }
}