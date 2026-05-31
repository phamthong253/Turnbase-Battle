using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    private Transform cameraTransform;
    private Vector3 lastCameraPosition;
    public float parallaxEffectSpeed = 0.5f; // Tốc độ hiệu ứng parallax

    void Start()
    {
        cameraTransform = Camera.main.transform;
        lastCameraPosition = cameraTransform.position;
    }
    void LateUpdate()
    {
        // Tính toán sự thay đổi vị trí của camera kể từ frame trước
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        // Di chuyển layer này một khoảng bằng delta * tốc độ parallax
        transform.position += deltaMovement * parallaxEffectSpeed;
        // Cập nhật lại vị trí cuối cùng của camera
        lastCameraPosition = cameraTransform.position;
    }
}
