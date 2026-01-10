using UnityEngine;
[System.Serializable]
public class CurrencyModel
{
   public int Crystal = 9999999;
    public void AddCrystal(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[CurrencyModel] Số lượng Crystal thêm phải lớn hơn 0.");
            return;
        }
        Crystal += amount;
        // Kích hoạt sự kiện để UI tự cập nhật (Sự kiện này phải nằm trong PlayerDataManager)
        // PlayerDataManager.Instance.OnCurrencyChanged?.Invoke(); 
    }
    public bool CanAffordGacha(int requiredAmount)
    {
        if (requiredAmount < 0) return true;
        if (Crystal >= requiredAmount)
        {
            return true;
        }
        return false;
    }
    public bool SpendForGacha(int costAmount)
    {
        if (!CanAffordGacha(costAmount))
        {
            Debug.LogError("[CurrencyModel] Lỗi: SpendForGacha được gọi mà không đủ tiền.");
            return false;
        }

        int remainingCost = costAmount;

        // 1. Ưu tiên trừ Free Crystals
        if (Crystal >= remainingCost)
        {
            Crystal -= remainingCost;
        }
        else
        {
            remainingCost -= Crystal;
            Crystal = 0;

        }
        // Kích hoạt sự kiện để UI tự cập nhật (Sự kiện này phải nằm trong PlayerDataManager)
        // PlayerDataManager.Instance.OnCurrencyChanged?.Invoke(); 
        return true;
    }
    public int GetTotalCrystals()
    {
        return Crystal;
    }
}
