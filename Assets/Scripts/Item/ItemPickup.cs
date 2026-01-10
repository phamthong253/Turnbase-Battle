using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    private ItemSO itemData;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        // Lấy component SpriteRenderer để hiển thị hình ảnh item
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Đảm bảo Collider là trigger để không va chạm vật lý với các vật khác
    }

    /// <summary>
    /// Gán dữ liệu ItemSO cho đối tượng item này và cập nhật hình ảnh.
    /// </summary>
    public void Initialize(ItemSO data)
    {
        this.itemData = data;
    }

    /// <summary>
    /// Được gọi khi có đối tượng khác đi vào trigger.
    /// </summary>
}