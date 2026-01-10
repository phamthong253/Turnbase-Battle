using System;
using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
public partial class PlayerDataManager : MonoBehaviour // Sử dụng 'partial' nếu class quá lớn
{
    public static PlayerDataManager Instance;
    public CurrencyModel CurrencyModel;
    // Giả định các Models đã được khởi tạo trong PlayerDataManager:
    //public CurrencyModel CurrencyModel;
    public UnitRosterModel UnitRosterModel;
    public InventoryModel InventoryModel;

    // Giả định GachaService là một class khác để xử lý việc chọn ngẫu nhiên phần thưởng

    // Sự kiện cần thiết để thông báo cập nhật
    public event Action<int> OnCurrencyChanged;
    public event Action<UnitSO> OnNewUnitAcquired;
    public event Action OnInventoryChanged;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;

            // 🌟 BƯỚC 1: KHỞI TẠO TẤT CẢ CÁC MODEL MẶC ĐỊNH 🌟
            // Đảm bảo chúng không phải là NULL, ngay cả khi Load thất bại.
            if (CurrencyModel == null) CurrencyModel = new CurrencyModel();
            if (InventoryModel == null) InventoryModel = new InventoryModel();
            if (UnitRosterModel == null) UnitRosterModel = new UnitRosterModel();

            // (Cần thêm các Model khác nếu có)

            // BƯỚC 2: TẢI DỮ LIỆU
            LoadPlayerData();

            // BƯỚC 3: GIỮ ĐỐI TƯỢNG GIỮA CÁC SCENE
            DontDestroyOnLoad(this.gameObject);
        }
    }
    /// <summary>
    /// Thực hiện giao dịch Gacha an toàn.
    /// </summary>
    /// <param name="costData">Dữ liệu chi phí (GachaCostSO).</param>
    /// <param name="gachaService">Dịch vụ Gacha chịu trách nhiệm Pull.</param>
    /// <returns>True nếu giao dịch thành công, False nếu không đủ tiền hoặc thất bại.</returns>
    public bool TryGacha(GachaCostSO costData, IGachaService gachaService)
    {
        // --- 1. KIỂM TRA ĐỦ TIỀN ---
        if (!CurrencyModel.CanAffordGacha(costData.CrystalCost))
        {
            Debug.LogWarning("[Gacha] Thất bại: Không đủ Crystal.");
            return false;
        }

        // --- 2. THỰC HIỆN GACHA (START TRANSACTION) ---

        // 2a. Trừ tiền
        if (!CurrencyModel.SpendForGacha(costData.CrystalCost))
        {
            Debug.LogError("[Gacha] Lỗi trừ tiền không xác định. Giao dịch bị hủy.");
            return false;
        }

        // 2b. Thực hiện Roll và nhận thưởng
        List<GachaReward> rewards = gachaService.Roll(costData);

        // 2c. Xử lý phần thưởng
        try
        {
            foreach (var reward in rewards)
            {
                ProcessGachaReward(reward);
            }

            // --- 3. LƯU VÀ CẬP NHẬT (COMMIT) ---
            SavePlayerData();

            // Kích hoạt Events để UI cập nhật (Currency luôn cập nhật)
            OnCurrencyChanged?.Invoke(CurrencyModel.GetTotalCrystals());
            OnInventoryChanged?.Invoke();

            return true;

        }
        catch (Exception e)
        {
            // --- 4. XỬ LÝ LỖI NGHIỆP VỤ (ROLLBACK) ---
            Debug.LogError($"[Gacha] Lỗi xử lý phần thưởng: {e.Message}. Đang hoàn tiền.");

            // Hoàn lại tiền cho người chơi (Mô phỏng Rollback)
            CurrencyModel.AddCrystal(costData.CrystalCost);
            OnCurrencyChanged?.Invoke(CurrencyModel.GetTotalCrystals());

            return false;
        }
    }

    /// <summary>
    /// Xử lý việc thêm từng loại phần thưởng (Unit, Shard, Item).
    /// </summary>
    private void ProcessGachaReward(GachaReward reward)
    {
        // 1. Phần thưởng là Unit (UnitSO)
        if (reward.UnitData != null)
        {
            // Thêm Unit mới vào Roster. Nếu đã có, UnitRosterModel sẽ chuyển thành Shard.
            //UnitRosterModel.AcquireUnit(reward.UnitData);
            OnNewUnitAcquired?.Invoke(reward.UnitData);
        }

        // 2. Phần thưởng là Shard (Mảnh Unit)
        if (reward.ShardData != null)
        {
            InventoryModel.AddUnitShard(reward.ShardData, reward.Quantity);
        }

        // 3. Phần thưởng là Item/Resource
        if (reward.ItemData != null)
        {
            InventoryModel.AddItem(reward.ItemData, reward.Quantity);
        }
    }
    private void SavePlayerData()
    {
        this.InventoryModel.PrepareForSave();
        // Cần phải có các Model được đánh dấu [Serializable]
        PlayerSaveData data = new PlayerSaveData
        {
            currencyModelData = this.CurrencyModel,
            unitRosterModelData = this.UnitRosterModel,
            inventoryModelData = this.InventoryModel
        };

        string jsonData = JsonUtility.ToJson(data);
        // 3. Ghi vào file (sử dụng Application.persistentDataPath cho nơi lưu an toàn)
        string path = Application.persistentDataPath + "/playerdata.json";
        System.IO.File.WriteAllText(path, jsonData);

        Debug.Log("[Data Manager] Dữ liệu đã được lưu ở đường dẫn." + path);
    }

    private void LoadPlayerData()
    {
        string path = Application.persistentDataPath + "/playerdata.json";
        if (!System.IO.File.Exists(path))
        {
            Debug.Log($"[Data Manager] Không tìm thấy dữ liệu lưu tại path {path}. Sử dụng dữ liệu mặc định.");
            return;
        }

        string json = System.IO.File.ReadAllText(path);

        try
        {
            PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
            this.CurrencyModel = data.currencyModelData;
            this.UnitRosterModel = data.unitRosterModelData;
            this.InventoryModel = data.inventoryModelData;
            this.InventoryModel.LoadFromSerializedData();
            Debug.Log("[Data Manager] Dữ liệu đã được tải thành công.");
            Debug.Log($"[Data Manager] Crystal sau khi tải: {this.CurrencyModel.Crystal}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Data Manager] Lỗi tải dữ liệu: {e.Message}. Sử dụng dữ liệu mặc định.");
        }
    }
}