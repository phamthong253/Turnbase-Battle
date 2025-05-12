using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WaveScene : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static WaveScene Instance;

    [Header("Wave Settings")]
    [SerializeField] public int currentWave;
    [SerializeField] public int baseEnemyCount;
    [SerializeField] public int maxWave;
    public int totalEnemyInPool;
    public int enemySpawnedSoFar;
    public int enemyKilled;
    public GameObject WinnerUI;
    public bool isCompleteWave;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);// Không bị phá hủy khi load scene
            if(WinnerUI != null)
            {
                DontDestroyOnLoad(WinnerUI);
            }
        }

        else
        {
            Destroy(gameObject);
        }
    }

    // Trả về số enemy tương ứng với wave hiện tại
    public int GetEnemyCountForWave()
    {
        // số enemy còn lại 
        int remainingEnemy = totalEnemyInPool - enemySpawnedSoFar;
        if (remainingEnemy <= 0)
        {
            return 0; // Không còn enemy để spawn
        }
        // Số lượng enemy spawn sẽ dựa trên tổng số còn lại trong bể
        int spawnThisWave = Mathf.Min(baseEnemyCount + currentWave - 1, remainingEnemy);
        return spawnThisWave;
    }

    // Tăng wave lên (không vượt quá maxWave)
    public void NextWave()
    {
        if (currentWave < maxWave)
        {
            currentWave++;
            StartCoroutine(LoadTransitionNextScene(SceneManager.GetActiveScene().buildIndex + 1));
        }
    }
    public void DisplayWinnerUI()
    {
        isCompleteWave = true;
        if (WinnerUI != null)
        {
            WinnerUI.SetActive(true);
            Debug.Log("UI dang true");
        }
        else
        {
            Debug.Log("ko tim thay winnerUI");
        }
        Debug.Log("Hoàn thành toàn bộ 3 wave" + currentWave + " / " + maxWave);
    }
    IEnumerator LoadTransitionNextScene(int nextSceneIndex)
    {
        StartCoroutine(SceneTransitionManager.Instance.FadeAndLoadScene(nextSceneIndex));
        yield return new WaitForSeconds(1f); // Thời gian chuyển cảnh
    }

    // Gọi khi chơi lại từ đầu
    public void ResetWaves()
    {
        currentWave = 1;
        SceneManager.LoadScene(0);
    }
}
