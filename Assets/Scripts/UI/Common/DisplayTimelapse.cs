using TMPro;
using UnityEngine;

public class DisplayTimelapse : MonoBehaviour
{
    public TextMeshProUGUI timeText;
    private void OnEnable()
    {
        WaveScene.OnGameFinish += HideUI;
    }
    private void OnDisable()
    {
        WaveScene.OnGameFinish -= HideUI;
    }
    void HideUI()
    {
        gameObject.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        if(TimeLapse.Instance != null)
        {
            timeText.text = TimeLapse.Instance.GetFormattedTime();
        }
    }
}
