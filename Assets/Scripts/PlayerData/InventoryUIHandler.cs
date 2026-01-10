// InventoryUIHandler.cs
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIHandler : MonoBehaviour
{
    [Header("Panel Giao diện Túi đồ")]
    // Kéo thả Panel chứa toàn bộ giao diện túi đồ vào đây
    public GameObject inventoryPanel;

    [Header("Component Render Vật phẩm")]
    // Kéo thả script InventoryStorageUI.cs vào đây
    public InventoryStorageUI inventoryRenderer;

    private bool isPanelOpen = false;

    void Start()
    {
        // Đảm bảo Panel bị tắt khi game bắt đầu (Phòng trường hợp quên tắt trong Editor)
        inventoryPanel.SetActive(false);
    }

    // Hàm này được gọi khi bấm nút
    public void ToggleInventoryPanel()
    {
        isPanelOpen = !isPanelOpen;

        // 1. Bật/Tắt Panel
        inventoryPanel.SetActive(isPanelOpen);

        // 2. Kích hoạt Render khi mở Panel
        if (isPanelOpen)
        {
            // Kiểm tra và gọi hàm render đã viết trước đó
            if (inventoryRenderer != null)
            {
                inventoryRenderer.RenderInventory();
                Debug.Log("Đang hiển thị vật phẩm vào Túi đồ.");
            }
            else
            {
                Debug.LogError("Chưa gán script InventoryStorageUI!");
            }
        }
    }
}