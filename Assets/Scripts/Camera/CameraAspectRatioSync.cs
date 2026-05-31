using UnityEngine;
using Unity.Cinemachine;

[ExecuteAlways] // Chạy ngay cả trong Editor khi bạn kéo giãn cửa sổ Game
public class CinemachineFixedWidth : MonoBehaviour
{
    public CinemachineCamera vcam;

    [Tooltip("Độ rộng (theo Unit) mà bạn muốn luôn hiển thị trọn vẹn trên mọi màn hình")]
    public float targetWidth = 20f;

    void LateUpdate()
    {
        if (vcam == null) vcam = GetComponent<CinemachineCamera>();
        if (vcam == null) return;

        // 1. Tính toán Aspect Ratio hiện tại (Bảo vệ chia cho 0)
        float currentAspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1.77f;

        // 2. Tính toán OrthoSize cần thiết để giữ cố định chiều ngang
        // Công thức: Size = Width / (2 * AspectRatio)
        float newOrthoSize = targetWidth / (2f * currentAspect);

        // 3. QUAN TRỌNG: Quy trình "Lấy ra -> Sửa -> Gán lại"
        var lensSettings = vcam.Lens; // Lấy bản sao struct

        // Kiểm tra thay đổi để tối ưu hiệu năng (tránh gán liên tục nếu không cần)
        if (!Mathf.Approximately(lensSettings.OrthographicSize, newOrthoSize))
        {
            lensSettings.OrthographicSize = newOrthoSize; // Sửa trên bản sao
            vcam.Lens = lensSettings; // GÁN NGƯỢC LẠI VÀO CAMERA (Bước này bạn bị thiếu)
        }
    }
}