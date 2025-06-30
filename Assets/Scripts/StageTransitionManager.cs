using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class StageTransitionManager : MonoBehaviour
{
    public static StageTransitionManager Instance;
    public Animator animator;
    [SerializeField] public float transitionTime = 3f; // Thời gian chuyển cảnh mặc định
    // Biến để lưu hash của các state animation
    private int startStateHash = Animator.StringToHash("StartTransition");
    private int endStateHash = Animator.StringToHash("EndTransition");

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public IEnumerator Transition(IEnumerator actionDuringBlackout)
    {
        // --- 1. BẮT ĐẦU FADE OUT ---
        animator.SetTrigger("StartTransition");

        // Chờ một frame để Animator bắt đầu chuyển state
        yield return null;

        // --- 2. CHỜ CHO ANIMATION FADE-OUT KẾT THÚC ---
        // Vòng lặp này sẽ chạy cho đến khi animation không còn là "Fade_Out" nữa
        // hoặc animation "Fade_Out" đã chạy xong 100%
        while (animator.GetCurrentAnimatorStateInfo(0).shortNameHash == endStateHash &&
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            yield return null; // Chờ frame tiếp theo
        }
        Debug.Log("Fade-out hoàn tất.");

        // --- 3. THỰC HIỆN LOGIC GAME KHI MÀN HÌNH ĐEN ---
        // Di chuyển camera, dịch chuyển người chơi, v.v...
        yield return StartCoroutine(actionDuringBlackout);

        // --- 4. BẮT ĐẦU FADE IN ---
        animator.SetTrigger("EndTransition");

        // Chờ một frame để Animator bắt đầu chuyển state
        yield return null;

        // --- 5. (TÙY CHỌN) CHỜ CHO ANIMATION FADE-IN KẾT THÚC ---
        while (animator.GetCurrentAnimatorStateInfo(0).shortNameHash != startStateHash)
        {
            yield return null; // Chờ cho đến khi quay về state Idle
        }
        Debug.Log("Chuyển cảnh hoàn tất.");
    }

    public void StartTransition()
    {
        animator.SetTrigger("StartTransition");
    }
    public void EndTransition()
    {
        animator.SetTrigger("EndTransition");
    }
}
