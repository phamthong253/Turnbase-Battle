using DG.Tweening;
using TMPro;
using UnityEngine;

public class BattleRewardUI : MonoBehaviour
{
    public TextMeshProUGUI rewardCrystalText;
    public TextMeshProUGUI rewardItemText;
    public Transform iconCrystalTransform;
    public Transform iconItemTransform;
    public float animDuration = 0.5f;
    public float punchScale = 1.2f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        WaveScene.OnGameFinish += HideUI;
        DOTween.Init();
        RewardManager.OnItemRewarded += DisplayReward;
    }
    private void OnDisable()
    {
        WaveScene.OnGameFinish -= HideUI;
        RewardManager.OnItemRewarded -= DisplayReward;
    }
    public void DisplayReward(int crystalAmount, string itemName)
    {
        if(rewardCrystalText != null)
        {
            int totalCrystals = RewardManager.Instance.GetTotalCrystalCount();
            rewardCrystalText.text = $"{totalCrystals}";
            if(crystalAmount > 0)
            {
                CrystalAnimation();
            }
        }

        if(rewardItemText != null)
        {
            if(!string.IsNullOrEmpty(itemName))
            {
                rewardItemText.text = $"x{RewardManager.Instance.GetSessionItemDropCount()}";

            }
            else
            {
                rewardItemText.text = $"x{RewardManager.Instance.GetSessionItemDropCount()}";
            }
        }

    }
    public void CrystalAnimation()
    {
        if(iconCrystalTransform == null) return;
        iconCrystalTransform.DOKill(true);
        iconItemTransform.DOKill(true);
        //iconCrystalTransform.localScale = Vector3.one;
        iconCrystalTransform.DOPunchScale(new Vector3(punchScale -1f, punchScale -1f, punchScale -1f),animDuration,1,0.5f).SetEase(Ease.OutBack);
        iconItemTransform.DOPunchScale(new Vector3(punchScale - 1f, punchScale - 1f, punchScale - 1f), animDuration, 1, 0.5f).SetEase(Ease.OutBack);
    }
    void HideUI()
    {
        gameObject.SetActive(false);
    }
}
