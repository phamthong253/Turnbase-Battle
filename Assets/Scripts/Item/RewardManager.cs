using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class ItemRewardEntry
{
    public string ItemID;
    public ItemSO ItemData;
    public int Quantity;
}
public class RewardManager : MonoBehaviour
{
    private static RewardManager instance;
    public static RewardManager Instance => instance;
    public static event Action<int, string> OnItemRewarded;
    // --- CONFIGURATION ---
    [Header("Reward Settings")]
    [Tooltip("Số Crystal TỐI THIỂU nhận được mỗi Wave")]
    public int minCrystalPerWave = 5;
    [Tooltip("Số Crystal TỐI ĐA nhận được mỗi Wave")]
    public int maxCrystalPerWave = 15;
    public Sprite crystalIcon; // Sprite của Crystal để hiển thị trong UI
    // Giả sử bạn có một danh sách Item có thể nhận ngẫu nhiên
    [Tooltip("Kéo thả danh sách các ItemSO có thể nhận ngẫu nhiên vào đây")]
    public List<ItemSO> randomRewardItems;
    [Range(0f, 1f)]
    public float randomItemDropChance = 0.3f; // 30% tỷ lệ nhận Item ngẫu nhiên
    public int sessionItemCount = 0;
    private int rewardCrystalSession;
    private Dictionary<string, int> rewardItemsSession = new Dictionary<string, int>();

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Đã tồn tại một instance của RewardManager. Destroy duplicate instance.");
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void StartNewReward()
    {
        rewardCrystalSession = 0;
        rewardItemsSession.Clear();
    }

    /// <summary>
    /// Hàm để làm rơi một item tại một vị trí cụ thể.
    /// </summary>
    /// <param name="itemData">Dữ liệu của item cần rơi (ScriptableObject).</param>
    /// <param name="position">Vị trí sẽ làm rơi item.</param>
    public void ItemReward()
    {
        int crystalAmount = UnityEngine.Random.Range(minCrystalPerWave, maxCrystalPerWave + 1);
        rewardCrystalSession += crystalAmount;

        ItemSO randomItem = null;

        if (UnityEngine.Random.value < randomItemDropChance && randomRewardItems.Count > 0)
        {
            randomItem = randomRewardItems[UnityEngine.Random.Range(0, randomRewardItems.Count)];

            if (randomItem != null)
            {
                if (!rewardItemsSession.ContainsKey(randomItem.itemID))
                    rewardItemsSession[randomItem.itemID] = 0;

                rewardItemsSession[randomItem.itemID]++;
                sessionItemCount++;
            }
        }
        OnItemRewarded?.Invoke(crystalAmount, randomItem != null ? randomItem.itemName : string.Empty);
        // Luôn in ra / tính FinalReward
        FinalReward();
    }


    public void FinalizeReward()
    {
        Debug.Log("<color=green>--- TỔNG KẾT PHẦN THƯỞNG TRẬN ĐẤU ---</color>");

        // 1. If PlayerDataManager exists, use its ApplyRewards helper to update models and notify UI
        if (PlayerDataManager.Instance != null)
        {
            // Copy the dictionary to avoid mutation side-effects
            var rewardsCopy = new Dictionary<string, int>(rewardItemsSession);
            int crystals = rewardCrystalSession;

            // Apply rewards centrally in PlayerDataManager (triggers OnCurrency/OnInventory events)
            PlayerDataManager.Instance.ApplyRewards(crystals, rewardsCopy, () =>
            {
                Debug.Log("[RewardManager] Rewards applied to PlayerDataManager.");

                // Optional: after applied you might want to trigger other game flows (analytics, popup)
                // StartNewReward() to clear current session after applying
                StartNewReward();
            });
        }
        else
        {
            // Fallback: apply locally (legacy behavior)
            Debug.LogWarning("[RewardManager] PlayerDataManager.Instance is null — applying rewards locally.");

            // 1. Cộng Crystal vào tài khoản
            if (PlayerDataManager.Instance?.CurrencyModel != null)
                PlayerDataManager.Instance.CurrencyModel.AddCrystal(rewardCrystalSession);

            // 2. Cộng Item vào Inventory
            foreach (var itemEntry in rewardItemsSession)
            {
                ItemSO itemData = GameDataService.Instance.GetItemSO(itemEntry.Key);
                if (itemData != null)
                {
                    PlayerDataManager.Instance.InventoryModel.AddItem(itemData, itemEntry.Value);
                }
            }

            StartNewReward();
        }

        // Debug: print summary as before
        int totalItemCount = GetTotalItemCount();
        Debug.Log($"Tổng số item nhận được trong phiên: {totalItemCount}");
        Dictionary<string, int> itemRewards = GetItemRewardInSession();
        foreach (var itemEntry in itemRewards)
        {
            Debug.Log($"ItemID: {itemEntry.Key}, Số lượng: {itemEntry.Value}");
        }
    }

    public int GetSessionItemDropCount()
    {
        Debug.Log($"Tổng số item nhận được trong phiên: {sessionItemCount}");
        return sessionItemCount;
    }
    public int GetTotalItemCount()
    {
        return rewardItemsSession.Values.Sum();
    }
    public int GetTotalCrystalCount()
    {
        return rewardCrystalSession;
    }
    public Dictionary<string,int> GetItemRewardInSession()
    {
        return new Dictionary<string, int>(rewardItemsSession);
    }
    public void FinalReward()
    {
        int totalItemCount = GetTotalItemCount();
        Debug.Log($"Tổng số item nhận được trong phiên: {totalItemCount}");
        Dictionary<string, int> itemRewards = GetItemRewardInSession();
        foreach (var itemEntry in itemRewards)
        {
            Debug.Log($"ItemID: {itemEntry.Key}, Số lượng: {itemEntry.Value}");
        }
    }
    public List<ItemRewardEntry> GetFinalRewardListForDisplay()
    {
        List<ItemRewardEntry> displayList = new List<ItemRewardEntry>();

        // 1. Xử lý Crystal
        if (rewardCrystalSession > 0 && crystalIcon != null)
        {
            // Thêm một Entry đặc biệt cho Crystal
            displayList.Add(new ItemRewardEntry
            {
                ItemID = "CRYSTAL_KEY",
                ItemData = null, // Vì không dùng ItemSO cho Crystal
                Quantity = rewardCrystalSession
            });
        }

        // 2. Xử lý các Item ngẫu nhiên khác
        foreach (var itemEntry in rewardItemsSession)
        {
            // *Giả định GameDataService.Instance.GetItemSO(id) tồn tại và hoạt động*
            ItemSO itemData = GameDataService.Instance.GetItemSO(itemEntry.Key);
            Debug.Log("Lấy ItemSO cho ItemID: " + itemEntry.Key);
            if (itemData != null)
            {
                // Thêm một Entry cho Item thông thường
                displayList.Add(new ItemRewardEntry
                {
                    ItemID = itemEntry.Key,
                    ItemData = itemData,
                    Quantity = itemEntry.Value
                });
            }
        }

        return displayList;
    }
}
