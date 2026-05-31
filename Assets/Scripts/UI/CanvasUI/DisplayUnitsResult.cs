using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // Sử dụng DOTween để thanh EXP chạy mượt

public class DisplayUnitsResult : MonoBehaviour
{
    [Header("Player UI")]
    public TextMeshProUGUI levelPlayerText;
    public Slider expPlayerBar;
    public TextMeshProUGUI expPlayerIncrease;

    [Header("Units Settings")]
    public Transform unitStatsContainer;
    public GameObject unitResultSlotPrefab; // Kéo Prefab thẻ tướng vào đây

    public void SetupPlayerExp(int level, int currentExp, int expToNextLevel, int expGained)
    {
        levelPlayerText.text = "Player Lv " + level;
        expPlayerBar.maxValue = expToNextLevel;
        expPlayerBar.value = currentExp;
        expPlayerIncrease.text = "+" + expGained + " EXP";

        // Hiệu ứng thanh EXP chạy
        expPlayerBar.DOValue(currentExp + expGained, 1.5f).SetEase(Ease.OutCubic);
    }

    //public void DisplayUnitStats(int expForEachUnit)
    //{
    //    // 1. Xóa các slot cũ
    //    foreach (Transform child in unitStatsContainer) Destroy(child.gameObject);

    //    // 2. Lấy danh sách tướng từ Manager
    //    var battleUnits = PlayerDataManager.Instance.battleTeamData;

    //    for (int i = 0; i < battleUnits.Length; i++)
    //    {
    //        if (battleUnits[i] != null)
    //        {
    //            // 3. Tạo ra Slot UI cho tướng
    //            GameObject slotGo = Instantiate(unitResultSlotPrefab, unitStatsContainer);
    //            UnitResultSlot slotScript = slotGo.GetComponent<UnitResultSlot>();

    //            // 4. Lưu dữ liệu cũ để làm hiệu ứng chạy thanh (nếu muốn)
    //            int oldLevel = battleUnits[i].level;
    //            int oldExp = battleUnits[i].currentExp;

    //            // 5. CỘNG EXP VÀO DATA THẬT
    //            battleUnits[i].AddExp(expForEachUnit);

    //            // 6. HIỂN THỊ LÊN UI
    //            slotScript.Setup(
    //                battleUnits[i].level,
    //                battleUnits[i].currentExp,
    //                battleUnits[i].expToNextLevel,
    //                expForEachUnit
    //            );
    //        }
    //    }
    //}
}