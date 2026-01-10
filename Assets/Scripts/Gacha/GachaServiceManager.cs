// GachaServiceManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GachaServiceManager : MonoBehaviour, IGachaService
{
    // --- KHAI BÁO THAM CHIẾU ---
    [Header("Gacha Reward Pools")]
    public List<UnitSO> unitPool;
    public List<ItemSO> equipmentPool;

    // Các Model (Được gán trong Awake/Start của script này)
    private UnitRosterModel _unitRosterModel;
    private InventoryModel _inventoryModel;
    private CurrencyModel _currencyModel; // NEW: Thêm Model quản lý tiền tệ

    // --- HÀM KHỞI TẠO ---
    private void Awake()
    {
        if (PlayerDataManager.Instance != null)
        {
            _unitRosterModel = PlayerDataManager.Instance.UnitRosterModel;
            _inventoryModel = PlayerDataManager.Instance.InventoryModel;
            _currencyModel = PlayerDataManager.Instance.CurrencyModel; // NEW: Gán CurrencyModel
        }
    }

    // --- HÀM ROLL CHÍNH (TRIỂN KHAI INTERFACE) ---
    public List<GachaReward> Roll(GachaCostSO costData)
    {
        // Kiểm tra Models đã sẵn sàng
        if (_unitRosterModel == null || _inventoryModel == null || _currencyModel == null)
        {
            Debug.LogError("[GachaServiceManager] Lỗi: Các Model (UnitRosterModel, InventoryModel, CurrencyModel) chưa được khởi tạo trong PDM.Awake().");
            return new List<GachaReward>();
        }

        int rolls = costData.RollCount;
        List<GachaReward> rewards = new List<GachaReward>();

        for (int i = 0; i < rolls; i++)
        {
            GachaReward reward = DetermineReward();
            rewards.Add(reward);
        }

        ProcessRewards(rewards);
        return rewards;
    }

    // --- LOGIC XÁC ĐỊNH PHẦN THƯỞNG ---
    private GachaReward DetermineReward()
    {
        float rollValue = Random.value;

        // 1. Phân chia tỷ lệ (Ví dụ: 60% Unit, 40% Equipment)
        if (rollValue < 0.6f && unitPool.Count > 0)
        {
            // --- ROLL UNIT ---
            UnitSO chosenUnit = unitPool[Random.Range(0, unitPool.Count)];
            return new GachaReward { UnitData = chosenUnit, Quantity = 1 };
        }
        else // Roll Item/Equipment
        {
            // --- XỬ LÝ EQUIPMENT ĐẶC BIỆT (ĐẢM BẢO MỚI) ---
            List<ItemSO> unownedEquipment = equipmentPool
                .Where(eq => !_inventoryModel.HasItem(eq, 1))
                .ToList();

            if (unownedEquipment.Count > 0)
            {
                // TÍCH HỢP dropRate VÀO ĐÂY:

                // 1. Tính tổng tỷ lệ rơi của tất cả Trang bị CHƯA sở hữu
                float totalRate = unownedEquipment.Sum(eq => eq.dropRate);

                // Nếu tổng tỷ lệ bằng 0, chọn ngẫu nhiên đơn giản
                if (totalRate <= 0)
                {
                    Debug.LogWarning("[GachaService] Tổng dropRate của các Trang bị chưa sở hữu bằng 0. Chọn ngẫu nhiên đơn giản.");
                    ItemSO newEquipment = unownedEquipment[Random.Range(0, unownedEquipment.Count)];

                    return new GachaReward
                    {
                        ItemData = newEquipment,
                        Quantity = 1,
                        IsEquipment = true
                    };
                }

                // 2. Roll ngẫu nhiên dựa trên tổng tỷ lệ
                float randomPoint = Random.Range(0, totalRate);
                ItemSO chosenEquipment = null;

                foreach (var eq in unownedEquipment)
                {
                    if (randomPoint < eq.dropRate)
                    {
                        chosenEquipment = eq;
                        break;
                    }
                    randomPoint -= eq.dropRate;
                }

                // Đảm bảo có item được chọn (nếu có lỗi làm tròn, chọn item cuối cùng)
                if (chosenEquipment == null)
                {
                    chosenEquipment = unownedEquipment.Last();
                }

                return new GachaReward
                {
                    ItemData = chosenEquipment,
                    Quantity = 1,
                    IsEquipment = true
                };
            }
            else
            {
                // Người chơi đã sở hữu TẤT CẢ Trang bị. ĐỀN BÙ BẰNG CRYSTAL.
                int crystalAmount = Random.Range(100, 1000);
                Debug.Log("[Gacha] Đã sở hữu hết Trang bị! Chuyển đổi Crystal.");

                return new GachaReward
                {
                    Crystal = crystalAmount,
                    Quantity = 1 // Giá trị Quantity không liên quan đến Crystal
                };
            }
        }
    }

    // --- LOGIC PHÂN PHỐI PHẦN THƯỞNG ---
    private void ProcessRewards(List<GachaReward> rewards)
    {
        // Kiểm tra Models đã được gán
        if (_unitRosterModel == null || _inventoryModel == null || _currencyModel == null)
        {
            Debug.LogError("[GachaServiceManager] Lỗi: Các Model chưa sẵn sàng!");
            return;
        }

        foreach (var reward in rewards)
        {
            // 1. XỬ LÝ UNIT (TƯỚNG)
            if (reward.UnitData != null)
            {
                if (!_unitRosterModel.HasUnit(reward.UnitData.unitID))
                {
                    // ✅ TRƯỜNG HỢP 1: UNIT MỚI
                    _unitRosterModel.AddUnit(reward.UnitData);
                    Debug.Log($"[Gacha] ✨ Unit MỚI: {reward.UnitData.unitID}");
                    // Sau khi nhận unit mới, có thể tặng kèm một ít shard cho unit đó
                    _inventoryModel.AddUnitShard(reward.UnitData, 5); // Ví dụ: 5 Shard khởi đầu
                }
                else
                {
                    // 🔄 TRƯỜNG HỢP 2: UNIT TRÙNG -> CHUYỂN THÀNH SHARD
                    int shardQuantity = Random.Range(20, 50); // Số lượng Shard bù đắp
                    _inventoryModel.AddUnitShard(reward.UnitData, shardQuantity);
                    Debug.Log($"[Gacha] Unit trùng. Đền bù: Shard {reward.UnitData.unitID} x{shardQuantity}");

                    // Tùy chọn (Giống PriConne): Thêm Tượng Công Chúa/Tiền Tệ Chung nếu Unit có độ hiếm cao
                    // int amuletQuantity = 5; 
                    // _inventoryModel.AddItem(amuletItemSO, amuletQuantity); 
                }
            }

            // 2. XỬ LÝ EQUIPMENT (TRANG BỊ)
            else if (reward.ItemData != null && reward.IsEquipment)
            {
                // Do logic DetermineReward đã đảm bảo Roll ra trang bị mới (nếu chưa hết pool),
                // ta chỉ cần kiểm tra xem item này có được Roll ra không.

                // LƯU Ý: Nếu DetermineReward đã bị thay đổi để không đảm bảo mới:
                bool hasItem = _inventoryModel.HasItem(reward.ItemData, 1);

                if (!hasItem)
                {
                    // ✅ TRƯỜNG HỢP 3: EQUIPMENT MỚI
                    _inventoryModel.AddItem(reward.ItemData, reward.Quantity);
                    Debug.Log($"[Gacha] ⚙️ Trang bị MỚI: {reward.ItemData.itemID}");
                }
                else
                {
                    // 🔄 TRƯỜNG HỢP 4: EQUIPMENT TRÙNG -> CHUYỂN THÀNH CRYSTAL
                    int crystalAmount = Random.Range(50, 200); // Số lượng Crystal bù đắp
                    _currencyModel.AddCrystal(crystalAmount);
                    Debug.Log($"[Gacha] Trang bị trùng. Đền bù: Crystal x{crystalAmount}");
                }
            }

            // 3. XỬ LÝ CRYSTAL ĐỀN BÙ (Khi Roll hết Pool Equipment)
            else if (reward.Crystal > 0)
            {
                _currencyModel.AddCrystal(reward.Crystal);
                Debug.Log($"[Gacha] Đền bù Crystal: x{reward.Crystal} (Do hết Pool)");
            }

            // 4. XỬ LÝ ITEM KHÁC (Item tiêu chuẩn không phải Equipment)
            else if (reward.ItemData != null && !reward.IsEquipment)
            {
                _inventoryModel.AddItem(reward.ItemData, reward.Quantity);
                Debug.Log($"[Gacha] Item tiêu chuẩn: {reward.ItemData.itemID} x{reward.Quantity}");
            }

            // Cần đảm bảo rằng sau khi Gacha Service xử lý xong, PlayerDataManager gọi SaveGame()
        }
    }
}