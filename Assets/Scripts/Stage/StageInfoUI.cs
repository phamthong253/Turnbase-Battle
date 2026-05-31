using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class StageInfoUI : MonoBehaviour
{

    private StageSO currentStageData;
    [Header("Stage Info UI Components")]
    public TextMeshProUGUI stageNameText;
    public TextMeshProUGUI stageID;
    [Header("Enemy List Display")]
    [Tooltip("Kéo Prefab icon quái vật vào đây")]
    public List<Image> enemyIconSlots;

    [Header("Reward Limited")]
    public List<Image> rewardIconSlots;

    [Header("Daily Rewards")]
    public GameObject crystalIcon;
    public GameObject expIcon;
    [Header("Buttons")]
    public Button beginBtn;
    public Button cancelBtn;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(beginBtn != null) beginBtn.onClick.AddListener(OnBeginButtonClicked);
        if(cancelBtn != null) cancelBtn.onClick.AddListener(OnCancelButtonClicked);
    }

    public void SetupAndShow(StageSO currentStageData)
    {
        this.currentStageData = currentStageData;
        stageNameText.text = currentStageData.stageName;
        stageID.text = "Stage " + currentStageData.stageID.ToString();
        if(expIcon != null) expIcon.SetActive(currentStageData.hasExpReward);
        if(crystalIcon != null) crystalIcon.SetActive(currentStageData.hasCrystalReward);
        FillRewardSlots(currentStageData);
        FillEnemySlots(currentStageData);
        this.gameObject.SetActive(true);
    }


    private void FillEnemySlots(StageSO data)
    {
        // Bước A: Lọc danh sách quái duy nhất (không trùng lặp)
        HashSet<UnitSO> uniqueEnemies = new HashSet<UnitSO>();
        if (data.waves != null)
        {
            foreach (var wave in data.waves)
            {
                foreach (var enemy in wave.enemies)
                {
                    if (enemy != null) uniqueEnemies.Add(enemy);
                }
            }
        }

        // Chuyển sang List để dễ truy cập theo index
        List<UnitSO> enemyList = new List<UnitSO>(uniqueEnemies);

        // Bước B: Duyệt qua từng Slot có sẵn để gán ảnh
        for (int i = 0; i < enemyIconSlots.Count; i++)
        {
            if (i < enemyList.Count)
            {
                // Trường hợp CÓ quái: Hiển thị và gán Sprite
                UnitSO enemy = enemyList[i];
                enemyIconSlots[i].gameObject.SetActive(true); // Bật lên

                // Giả sử UnitSO có biến 'icon'
                 enemyIconSlots[i].sprite = enemy.avatar;
                enemyIconSlots[i].color = new Color(255, 255, 255, 255);
            }
            else
            {
                // Trường hợp KHÔNG có quái (Slot thừa): Ẩn đi
                enemyIconSlots[i].gameObject.SetActive(false);
                // Hoặc set sprite = null và chỉnh alpha = 0 nếu muốn giữ khung
                enemyIconSlots[i].sprite = null;
                enemyIconSlots[i].color = new Color(0, 0, 0, 0); // Trong suốt
            }
        }
    }
    private void FillRewardSlots(StageSO data)
    {
        // Lấy danh sách vật phẩm từ StageSO
        List<ItemSO> rewards = data.rewardItems;

        if (rewards == null) rewards = new List<ItemSO>(); // Tránh lỗi null

        for (int i = 0; i < rewardIconSlots.Count; i++)
        {
            if (i < rewards.Count)
            {
                // CÓ vật phẩm -> Hiển thị
                ItemSO item = rewards[i];
                rewardIconSlots[i].gameObject.SetActive(true);

                // Gán icon (Giả sử ItemSO có biến 'icon' kiểu Sprite)
                 rewardIconSlots[i].sprite = item.itemAvatar;
                rewardIconSlots[i].color = new Color(255, 255, 255, 255);
            }
            else
            {
                // KHÔNG có vật phẩm -> Ẩn đi (để trơ lại cái khung)
                rewardIconSlots[i].gameObject.SetActive(false);
                rewardIconSlots[i].sprite = null;
                rewardIconSlots[i].color = new Color(0, 0, 0, 0);
            }
        }
    }
    private void OnBeginButtonClicked()
    {
        if (currentStageData != null)
        {
            gameObject.SetActive(false);
            MapUIManager.Instance.ShowStagePopup(currentStageData);
        }
    }
    private void OnCancelButtonClicked()
    {
        this.gameObject.SetActive(false);
    }
}
