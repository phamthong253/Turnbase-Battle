using TMPro;
using UnityEngine;

public class WaveText : MonoBehaviour
{
    public TextMeshProUGUI waveText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        waveText.text = "Wave " + WaveScene.Instance.currentWave + " / " + WaveScene.Instance.maxWave;
    }

    // Update is called once per frame
    
}
