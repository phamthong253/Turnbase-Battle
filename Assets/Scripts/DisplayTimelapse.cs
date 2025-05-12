using TMPro;
using UnityEngine;

public class DisplayTimelapse : MonoBehaviour
{
    public TextMeshProUGUI timeText;
    // Update is called once per frame
    void Update()
    {
        if(TimeLapse.Instance != null)
        {
            timeText.text = TimeLapse.Instance.GetFormattedTime();
        }
    }
}
