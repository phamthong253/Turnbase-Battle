using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UnitSlotUI : MonoBehaviour
{
    public Image unitAvt;
    public TextMeshProUGUI unitLevelText;
    public Image[] starsRank;
    public Sprite fillStar;
    public Sprite emptyStar;
    public Button cardBtn;

    public void Setup(UnitSO staticData, PlayerUnitData dynamicData, UnityAction onClickAction)
    {
        unitAvt.sprite = staticData.entireAvatar;
        unitLevelText.text = "Lvl. " + dynamicData.Level;

        for (int i = 0; i < starsRank.Length; i++)
        {
            starsRank[i].gameObject.SetActive(true);
            if (i < dynamicData.Rank)
            {
                starsRank[i].sprite = fillStar;
            }
            else
            {
                starsRank[i].sprite = emptyStar;
            }
        }
        cardBtn.onClick.RemoveAllListeners();
        cardBtn.onClick.AddListener(onClickAction);
    }
}
