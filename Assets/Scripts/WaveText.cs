using TMPro;
using UnityEngine;

public class WaveText : MonoBehaviour
{
    public static WaveText Instance { get; private set; }
    public TextMeshProUGUI waveText;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void UpdateWaveText(int currentStage, int MaxStage)
    {
        waveText.text = "Wave " + currentStage + " / " + MaxStage;
    }
}
