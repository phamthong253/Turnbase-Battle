using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaUIHandler : MonoBehaviour
{
    [Header("Gacha Buttons")]
    public Button roll1xButton;
    public Button roll10xButton;

    [Header("Gacha Costs Display")]
    public TextMeshProUGUI roll1xCostText;
    public TextMeshProUGUI roll10xCostText;

    [Header("Gacha Cost Scriptable Objects")]
    public GachaCostSO gachaCost1x;
    public GachaCostSO gachaCost10x;

    [Header("Backend Banner")]
    public string bannerId = "standard";

    private void Start()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("PlayerDataManager chưa được khởi tạo! Hãy đảm bảo nó tồn tại trong Scene đầu tiên hoặc được DontDestroyOnLoad.");
            return;
        }

        PlayerDataManager.Instance.OnCurrencyChanged += OnCurrencyChangedHandler;

        if (roll1xButton != null) roll1xButton.onClick.AddListener(() => OnRollButtonClicked(gachaCost1x));
        if (roll10xButton != null) roll10xButton.onClick.AddListener(() => OnRollButtonClicked(gachaCost10x));

        if (roll1xCostText != null && gachaCost1x != null) roll1xCostText.text = $"{gachaCost1x.CrystalCost} Crystal";
        if (roll10xCostText != null && gachaCost10x != null) roll10xCostText.text = $"{gachaCost10x.CrystalCost} Crystal";

        UpdateGachaButtonsState();
    }

    private void OnDestroy()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnCurrencyChanged -= OnCurrencyChangedHandler;
        }
    }

    private void OnRollButtonClicked(GachaCostSO costData)
    {
        if (costData == null)
        {
            Debug.LogError("GachaCostSO chưa được gán!");
            return;
        }

        int playerId = PlayerDataManager.Instance.CurrentPlayerId;
        APIManager.Instance.RollGacha(playerId, bannerId, costData.RollCount,
            onSuccess: (response) =>
            {
                PlayerDataManager.Instance.ApplyGachaRollResponse(response);
                if (response != null && response.rewards != null)
                {
                    foreach (var reward in response.rewards)
                    {
                        Debug.Log($"[Gacha] {reward.rewardType}: {reward.id} x{reward.quantity} New={reward.isNew}");
                    }
                }
                UpdateGachaButtonsState();
            },
            onError: (error) =>
            {
                Debug.LogError("[Gacha] Roll failed: " + error);
                UpdateGachaButtonsState();
            });
    }

    private void OnCurrencyChangedHandler(int newCount)
    {
        UpdateGachaButtonsState();
    }

    private void UpdateGachaButtonsState()
    {
        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.CurrencyModel == null) return;

        if (gachaCost1x != null && roll1xButton != null)
        {
            roll1xButton.interactable = PlayerDataManager.Instance.CurrencyModel.CanAffordGacha(gachaCost1x.CrystalCost);
        }
        if (gachaCost10x != null && roll10xButton != null)
        {
            roll10xButton.interactable = PlayerDataManager.Instance.CurrencyModel.CanAffordGacha(gachaCost10x.CrystalCost);
        }
    }
}
