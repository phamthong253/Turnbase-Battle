using UnityEngine;

public class InventoryUIHandler : MonoBehaviour
{
    public static InventoryUIHandler Instance;

    [Header("Panel Giao diện Túi đồ")]
    public GameObject inventoryPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    public void RegisterPanel(GameObject newPanel)
    {
        inventoryPanel = newPanel;
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            Debug.Log("[InventoryUIHandler] Đã nhận được Panel Túi đồ từ Scene mới!");
        }
    }

    public void ToggleInventoryPanel()
    {
        if (inventoryPanel == null)
        {
            TryResolvePanelInScene();
        }

        if (inventoryPanel == null)
        {
            Debug.LogWarning("[InventoryUIHandler] Chưa tìm thấy Inventory panel trong scene hiện tại.");
            return;
        }

        bool newState = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(newState);
    }

    private void TryResolvePanelInScene()
    {
        InventoryStorageUI[] candidates = Resources.FindObjectsOfTypeAll<InventoryStorageUI>();
        foreach (var candidate in candidates)
        {
            if (candidate == null) continue;
            GameObject candidateObject = candidate.gameObject;
            if (!candidateObject.scene.IsValid()) continue;
            RegisterPanel(candidateObject);
            return;
        }
    }
}
