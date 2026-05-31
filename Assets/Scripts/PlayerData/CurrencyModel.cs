using UnityEngine;

[System.Serializable]
public class CurrencyModel
{
    public int Crystal = 0;

    public void SetCrystal(int amount)
    {
        Crystal = Mathf.Max(0, amount);
    }

    public void AddCrystal(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[CurrencyModel] Số lượng Crystal thêm phải lớn hơn 0.");
            return;
        }
        Crystal += amount;
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowNotification($"You have received x{amount} Crystal!");
        }
    }

    public bool CanAffordGacha(int requiredAmount)
    {
        if (requiredAmount < 0) return true;
        return Crystal >= requiredAmount;
    }

    public bool SpendForGacha(int costAmount)
    {
        if (!CanAffordGacha(costAmount))
        {
            Debug.LogError("[CurrencyModel] Lỗi: SpendForGacha được gọi mà không đủ tiền.");
            return false;
        }

        Crystal -= costAmount;
        return true;
    }

    public int GetTotalCrystals()
    {
        return Crystal;
    }
}
