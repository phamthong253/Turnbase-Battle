// InventoryStorageUI.cs
using System.Collections.Generic;
using System.Linq;
using TMPro; // Nếu dùng TextMeshPro
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InventoryStorageUI : MonoBehaviour
{
    [Header("UI References")]
    //public GameObject itemSlotPrefab; // Prefab của Slot UI
    public Transform slotContainer; // Parent Transform
    private List<Transform> contentParent = new List<Transform>();

    private void Awake()
    {
        if(slotContainer == null)
        {
            Debug.LogError("slotContainer chưa được gán trong InventoryStorageUI!");
            return;
        }
        contentParent.Clear();
        foreach (Transform child in slotContainer)
            {
                contentParent.Add(child);
            }
        if (contentParent.Count == 0)
        {
            Debug.LogWarning("[SETUP WARNING] Không tìm thấy slot con nào trong Slots Container!");
        }
        else
        {
            Debug.Log($"[SETUP SUCCESS] Đã tìm thấy {contentParent.Count} slot cố định.");
        }
    }
    private void OnEnable()
    {
        if(PlayerDataManager.Instance == null)
        {
            Debug.Log("PlayerDataManager chưa được khởi tạo");
            return;
        }
        RenderInventory();
    }

    public void RenderInventory()
    {
        if (PlayerDataManager.Instance == null) return;
        foreach (Transform slot in slotContainer)
        {
            // Ẩn Sprite và Text để làm trống slot
            TextMeshProUGUI quantityText = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (quantityText != null) quantityText.text = "";
        }
        var inventory = PlayerDataManager.Instance.InventoryModel;
        int slotIndex = 0;
        int totalSlot = contentParent.Count;

        Debug.Log("--- BẮT ĐẦU RENDER KHO ĐỒ ---");


        // 1. HIỂN THỊ ITEMS/EQUIPMENT (ItemInventory)
        foreach (var item in inventory.ItemInventory)
        {
            if(slotIndex >= totalSlot)
            {
                Debug.LogError("Số lượng contentParent không đủ để hiển thị tất cả vật phẩm.");
                break;
            }
            CreateItemSlot(contentParent[slotIndex], item.Key, item.Value, isShard:false);
            slotIndex++;
        }

        // 2. HIỂN THỊ UNIT SHARDS (UnitShardInventory)
        foreach (var pair in inventory.UnitShardInventory)
        {
            if (slotIndex >= totalSlot) break;
            CreateItemSlot(contentParent[slotIndex], pair.Key, pair.Value, isShard: true);
            slotIndex++;
        }

        Debug.Log("--- KẾT THÚC RENDER KHO ĐỒ ---");
    }

    // InventoryStorageUI.cs (Hàm CreateItemSlot đã làm sạch)

    private void CreateItemSlot(Transform slotTransform,string id, int quantity, bool isShard)
    {
        // KHÔNG CẦN KHAI BÁO CÁC BIẾN TẠM THỜI TẠI ĐÂY NỮA
        Sprite iconToUse = null;
        string nameToUse = "N/A";

        // --- 1. TRA CỨU TÀI NGUYÊN ĐỘC LẬP ---

        if (GameDataService.Instance == null) {
            Debug.LogError("[DATA SERVICE MISSING] GameDataService.Instance bị null. Không thể tra cứu dữ liệu.");
            return; 
        };

        if (isShard)
        {
            // CHỈ TRA CỨU UNIT CHO SHARD
            UnitSO unitSO = GameDataService.Instance.GetUnitSO(id);
            if (unitSO != null)
            {
                iconToUse = unitSO.avatar;
                nameToUse = unitSO.name + " Shard";
            }
        }
        else // isShard == FALSE -> Đây là Item/Equipment
        {
            // CHỈ TRA CỨU ITEM CHO ITEM/EQUIPMENT
            ItemSO itemSO = GameDataService.Instance.GetItemSO(id);
            if (itemSO != null)
            {
                iconToUse = itemSO.itemAvatar;
                nameToUse = itemSO.itemName;
            }
        }

        // --- 2. KIỂM TRA LỖI VÀ CHUẨN BỊ RENDER ---

        // Nếu không tìm thấy SO HOẶC Sprite bị thiếu trong SO, dừng lại.
        if (iconToUse == null)
        {
            Debug.LogError($"[ASSET MISSING] Không tìm thấy Sprite cho ID '{id}' (Loại: {(isShard ? "SHARD" : "ITEM")}). Kiểm tra dữ liệu SO.");
            return;
        }

        

        // --- 3. TẠO SLOT VÀ GÁN DỮ LIỆU ---

        Image borderImage = slotTransform.GetComponent<Image>();
        Image iconImage = slotTransform.transform.Find("Image").GetComponent<Image>();
        TextMeshProUGUI quantityText = slotTransform.GetComponentInChildren<TextMeshProUGUI>();

        if (iconToUse != null)
        {
            // 🌟 HÀNH ĐỘNG 1: Gán Icon Sprite 🌟
            if (iconImage != null)
            {
                iconImage.sprite = iconToUse;
                iconImage.color = Color.white; // Đảm bảo không trong suốt
            }

            // 🌟 HÀNH ĐỘNG 2: XÓA ICON TRÊN KHUNG VIỀN (Đảm bảo Khung Viền vẫn là khung viền) 🌟
            if (borderImage != null)
            {
                // Đặt Sprite của Khung Viền về lại Sprite mặc định (ví dụ: hình vuông trắng hoặc rỗng)
                borderImage.sprite = borderImage.sprite; // Giữ nguyên hình ảnh Border
            }
        }
        else
        {
            // Nếu không có Item, làm sạch cả hai
            if (iconImage != null) iconImage.sprite = null;
        }

        //iconImage.sprite = iconToUse;

        if (quantityText != null)
        {
            quantityText.text = quantity.ToString();
        }
    }
}