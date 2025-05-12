using TMPro;
using UnityEngine;

public class TimeLapse : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static TimeLapse Instance;

    float timeElapsed;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Không bị phá hủy khi load scene
        }
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(timeElapsed / 60);
        int seconds = Mathf.FloorToInt(timeElapsed % 60);
        return string.Format("{0}m {1:00}s", minutes, seconds);
    }

    public void ResetTimer()
    {
        timeElapsed = 0f;
    }
}
