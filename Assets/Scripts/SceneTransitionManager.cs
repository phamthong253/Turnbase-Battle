using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;
    public Animator animator;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Gắn Animator lần đầu nếu có trong scene đầu tiên
            AssignAnimatorInScene();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Gắn lại animator mỗi khi scene mới được load
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AssignAnimatorInScene();
    }

    private void AssignAnimatorInScene()
    {
        GameObject fadeCanvas = GameObject.Find("FadeCanvas");
        if (fadeCanvas != null)
        {
            animator = fadeCanvas.GetComponent<Animator>();
        }
        else
        {
            Debug.LogWarning("Không tìm thấy FadeCanvas trong scene!");
        }
    }

    

    public IEnumerator FadeAndLoadScene(int sceneIndex)
    {
        if (animator != null)
        {
            animator.SetTrigger("StartTransition");
            yield return new WaitForSeconds(1f);
        }

        SceneManager.LoadScene(sceneIndex);
    }
}
