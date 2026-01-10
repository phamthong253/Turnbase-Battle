using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SoundLibrary : MonoBehaviour
{
    public Sound[] sounds;

    private void Awake()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.RegisterSound(sounds); // Register sounds with AudioManager
        }
        else
        {
            Debug.LogError("AudioManager instance is null.");
        }
    }

    private void OnDestroy()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UnRegisterSound(sounds); // Unregister sounds from AudioManager
        }
    }
}
