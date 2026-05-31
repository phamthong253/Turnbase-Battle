using UnityEngine;
using UnityEngine.UI;

public class ScrollingEffect : MonoBehaviour
{
    public RawImage rawImage;
    public float speedX = 1.5f; // Tốc độ chạy ngang
    public float speedY = 0f;   // Tốc độ chạy dọc

    private void Update()
    {
        // Lấy Rect hiện tại
        Rect rect = rawImage.uvRect;

        // Di chuyển Rect (tạo hiệu ứng cuộn)
        rect.x += speedX * Time.unscaledDeltaTime;
        rect.y += speedY * Time.unscaledDeltaTime;

        // Gán ngược lại
        rawImage.uvRect = rect;
    }
}