using TMPro;
using UnityEngine;

public class StatsRowUI : MonoBehaviour
{
    public TextMeshProUGUI statName;
    public TextMeshProUGUI statValue;

    public void Setup(string name, string value, bool isPercent = false)
    {
        statName.text = name;
        statValue.text = isPercent ? $"{value}%" : value.ToString();
    }
}
