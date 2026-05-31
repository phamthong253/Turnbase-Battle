using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// Định nghĩa các loại Tab
public enum MenuTab
{
    Home,
    Inventory,
    Champs, // Heroes/Units
    Dungeon,
    Quests, // Main Story/Map
    Gacha
}

public class BottomNavigationHandler : MonoBehaviour
{
    [Header("Buttons Reference")]
    public Button btnHome;
    public Button btnInventory;
    public Button btnChamps;
    public Button btnDungeon;
    public Button btnQuests;
    public Button btnGacha;

    [Header("Settings")]
    // Tab hiện tại đang sáng (để biết mình đang ở đâu)
    public MenuTab currentTab;

    private void Start()
    {
        // 1. ÉP BUỘC cập nhật currentTab dựa trên Scene thực tế đang mở
        string currentSceneName = SceneManager.GetActiveScene().name;

        switch (currentSceneName)
        {
            case "MainScene": currentTab = MenuTab.Home; break;
            case "UnitScene": currentTab = MenuTab.Champs; break;
            case "MapScene": currentTab = MenuTab.Dungeon; break;
            case "QuestScene": currentTab = MenuTab.Quests; break;
            case "GachaScene": currentTab = MenuTab.Gacha; break;
        }
        // 1. Gán sự kiện click cho từng nút
        btnHome.onClick.AddListener(() => OnTabSelected(MenuTab.Home));
        btnInventory.onClick.AddListener(() => OnTabSelected(MenuTab.Inventory));
        btnChamps.onClick.AddListener(() => OnTabSelected(MenuTab.Champs));
        btnDungeon.onClick.AddListener(() => OnTabSelected(MenuTab.Dungeon));
        btnQuests.onClick.AddListener(() => OnTabSelected(MenuTab.Quests));
        btnGacha.onClick.AddListener(() => OnTabSelected(MenuTab.Gacha));

        // 2. Cập nhật giao diện (nút nào đang chọn thì sáng lên/tắt tương tác)
        UpdateVisualState();
    }

    private void OnTabSelected(MenuTab tab)
    {
        if (tab == MenuTab.Inventory)
        {
            if (InventoryUIHandler.Instance != null)
            {
                InventoryUIHandler.Instance.ToggleInventoryPanel();
            }
            else
            {
                Debug.LogError("Chưa tìm thấy InventoryUIHandler trong Scene!");
            }

            // Thoát hàm luôn, không thay đổi currentTab hay load Scene
            return;
        }
        // Nếu bấm vào đúng Tab đang đứng thì không làm gì cả (tránh load lại scene)
        if (tab == currentTab) return;

        string sceneToLoad = "";

        // 3. Mapping từ Enum sang tên Scene
        switch (tab)
        {
            case MenuTab.Home:
                sceneToLoad = "MainScene"; // Tên Scene Home của bạn
                break;
            case MenuTab.Champs:
                sceneToLoad = "UnitScene";
                break;
            case MenuTab.Dungeon:
                sceneToLoad = "MapScene";
                break;
            case MenuTab.Quests:
                sceneToLoad = "QuestScene";
                break;
            case MenuTab.Gacha:
                sceneToLoad = "GachaScene";
                break;
        }

        // 4. Chuyển Scene
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            // Có thể thêm hiệu ứng Fade màn hình đen ở đây nếu muốn
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private void UpdateVisualState()
    {
        // Reset trạng thái các nút (Ví dụ: nút đang chọn thì không bấm được nữa)
        btnHome.interactable = currentTab != MenuTab.Home;
        btnChamps.interactable = currentTab != MenuTab.Champs;
        btnDungeon.interactable = currentTab != MenuTab.Dungeon;
        btnQuests.interactable = currentTab != MenuTab.Quests;
        btnGacha.interactable = currentTab != MenuTab.Gacha;

        btnInventory.interactable = true; // Inventory luôn có thể bấm được vì nó là Popup
        // Ở đây bạn có thể đổi Sprite của nút (Ví dụ: icon sáng vs icon tối)
    }
}