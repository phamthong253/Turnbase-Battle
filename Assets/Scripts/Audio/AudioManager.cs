using System.Collections.Generic;
using UnityEngine;


public enum SoundType
{
    BattleMusic,
    SFX, // Sound Effects
    Attack,
    Hit,
    SkillScene
}
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; } // Singleton instance of AudioManager
    public Dictionary<string, Sound> sounds = new Dictionary<string, Sound>(); // Dictionary to hold audio clips by name
    public AudioSource musicAudioSource; // Reference to the AudioSource component
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; // Set the singleton instance
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }
    public void RegisterSound(Sound[] registerSound)
    {
        foreach (Sound sound in registerSound)
        {
            int count = 0;
            if (!sounds.ContainsKey(sound.name))
            {
                sounds.Add(sound.name, sound); // Add the sound to the dictionary
                sound.source = gameObject.AddComponent<AudioSource>(); // Create an AudioSource for the sound
                sound.source.clip = sound.clip; // Assign the clip to the AudioSource
                sound.source.loop = sound.loop; // Set loop property
                sound.source.volume = sound.volume; // Set volume
                sound.source.pitch = sound.pitch; // Set pitch
                sound.source.playOnAwake = false; // Prevent the sound from playing on awake
                sound.source.spatialBlend = 0.0f; // 2D sound
                count++;
            }
            else
            {
                Debug.LogWarning($"Sound '{sound.name}' is already registered. Skipping registration."); // Warn if sound is already registered
            }
        }
    }
    public void UnRegisterSound(Sound[] unRegisterSound)
    {
        foreach (Sound sound in unRegisterSound)
        {
            if (sounds.ContainsKey(sound.name))
            {
                sounds.Remove(sound.name); // Remove the sound from the dictionary
                Destroy(sound.source); // Destroy the AudioSource component
            }
        }
    }
    public void PlayBattleMusic(string name)
    {
        if (sounds.TryGetValue(name, out Sound foundSound))
        {
            musicAudioSource.clip = foundSound.clip; // Set the clip for the music AudioSource
            musicAudioSource.loop = foundSound.loop; // Set loop property
            musicAudioSource.volume = foundSound.volume; // Set volume
            musicAudioSource.pitch = foundSound.pitch; // Set pitch
            musicAudioSource.Play(); // Play the music
        }
        else
        {
            Debug.LogWarning($"Music '{name}' not found in the dictionary."); // Warn if music is not found
            var allKeys = sounds.Keys;
            // 2. Chuyển danh sách Keys thành một chuỗi duy nhất, phân cách bằng dấu phẩy
            string availableKeys = string.Join(", ", allKeys);

            // 3. Ghi log thông báo lỗi và danh sách Keys
            Debug.LogWarning($"[AudioManager] LỖI: Sound '{name}' not found in the dictionary.");
            Debug.LogWarning($"[AudioManager] Hiện tại có {sounds.Count} keys sau: {availableKeys}");
        }
    }

    public void PlaySFX(string name)
    {
        if (sounds.TryGetValue(name, out Sound foundSound)){
            foundSound.source.PlayOneShot(foundSound.clip, foundSound.volume); // Play the sound if found
            foundSound.source.spatialBlend = 0.0f; // Ensure it's a 2D sound
        }
        else
        {
            Debug.LogWarning($"Sound '{name}' not found in the dictionary."); // Warn if sound is not found
                                                                              // 1. Lấy danh sách Keys (là IEnumerable<string>)
            var allKeys = sounds.Keys;
            // 2. Chuyển danh sách Keys thành một chuỗi duy nhất, phân cách bằng dấu phẩy
            string availableKeys = string.Join(", ", allKeys);

            // 3. Ghi log thông báo lỗi và danh sách Keys
            Debug.LogWarning($"[AudioManager] LỖI: Sound '{name}' not found in the dictionary.");
            Debug.LogWarning($"[AudioManager] Hiện tại có {sounds.Count} keys sau: {availableKeys}");
        }
    }
}
