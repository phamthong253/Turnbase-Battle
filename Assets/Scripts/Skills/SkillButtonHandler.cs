using UnityEngine;
using System.Collections.Generic;
using System.Collections; // Cần thiết cho Coroutine

public class SkillButtonHandler : MonoBehaviour
{
    private UnitController caster;
    private SkillUIManager uiManager;

    // Không cần Timeline Director nữa
    // [SerializeField] private PlayableDirector skillTimeline; 

    private void Awake()
    {
        uiManager = SkillUIManager.Instance;
    }

    public void LinkToSkill(UnitController caster)
    {
        this.caster = caster;
        // Debug.Log($"SkillButtonHandler đã liên kết với: {caster.name}");
    }

    public void OnSkillButtonPressed()
    {
        AudioManager.Instance.PlaySFX("skillSceneAudiostartBtn");

        if (uiManager == null) return;
        if (caster == null) return;

        // Kiểm tra Mana
        if (caster.UnitData.CurrentMana >= caster.skillSO.manaCost)
        {
            // BẮT ĐẦU QUY TRÌNH SKILL MỚI
            StartCoroutine(SkillSequenceRoutine());
        }
        else
        {
            Debug.LogWarning($"Không đủ mana! Cần: {caster.skillSO.manaCost}, Có: {caster.UnitData.CurrentMana}");
        }
    }

    /// <summary>
    /// Coroutine quản lý luồng: Cutscene -> Chờ -> Gây Damage
    /// </summary>
    private IEnumerator SkillSequenceRoutine()
    {
        // 1. Pause Game
        Time.timeScale = 0f;

        // 2. Gọi Cutscene (Hiệu ứng ảnh bay lượn)
        // Hệ thống sẽ tự tìm ảnh và hiển thị dựa trên CharacterID
        uiManager.TriggerSkillCutscene(caster.characterID);

        // 3. Chờ Cutscene chạy xong
        // Lấy tổng thời gian từ Manager để chờ cho khớp
        float waitTime = 0f;
        if (SkillCutsceneManager.Instance != null)
        {
            waitTime = SkillCutsceneManager.Instance.enterDuration
                     + SkillCutsceneManager.Instance.stayDuration
                     + SkillCutsceneManager.Instance.exitDuration;
        }
        else
        {
            waitTime = 2f; // Fallback nếu Manager lỗi
        }

        // Dùng WaitForSecondsRealtime vì timeScale đang = 0
        yield return new WaitForSecondsRealtime(waitTime);

        // 4. Resume Game
        Time.timeScale = 1f;

        // 5. Thực hiện Logic Skill (Gây damage, trừ mana...)
        UsePerformSkill();
    }

    private void UsePerformSkill()
    {
        if (caster.skillSO == null) return;

        // Lấy danh sách mục tiêu
        List<UnitController> currentTargets = SkillManager.Instance.GetTargets(caster.skillSO, caster);

        if (currentTargets == null || currentTargets.Count == 0)
        {
            Debug.LogWarning("Không có mục tiêu hợp lệ.");
            return;
        }

        // Camera Focus (Zoom vào tướng hoặc mục tiêu)
        CameraFocusManager.Instance.StartCoroutine(
             CameraFocusManager.Instance.FocusOnSkillRoutine(caster, currentTargets)
        );

        // Thực hiện kỹ năng (Trừ mana, chạy anim tấn công, tính damage)
        caster.PerformSkill(caster.skillSO, currentTargets, () =>
        {
            Debug.Log($"[SkillButtonHandler] Skill {caster.skillSO.skillName} hoàn tất.");

            // Trả Camera về bình thường sau 1.5s
            CameraFocusManager.Instance.ResetFocusDelayed(1.5f);
        });
    }
}