using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Timeline; // Quan trọng: Thêm namespace này

public class SkillButtonHandler : MonoBehaviour
{
    private UnitController caster;
    private SkillUIManager uiManager; // Kéo SkillUIManager_GO trong Hierarchy vào đây
    public BattleHUD battleHUD;
    [SerializeField] private GameObject skillSceneGO; // Biến này có thể dùng để chứa GameObject liên quan đến Skill Scene nếu cần
    [SerializeField] private PlayableDirector skillTimeline;


    private void Awake()
    {

        uiManager = SkillUIManager.Instance; // Lấy instance của SkillUIManager
        if (uiManager == null)
        {
            return;
        }

        GameObject directorGO = GameObject.FindGameObjectWithTag("SkillTimelineDirector");
        if (directorGO != null)
        {
            skillTimeline = directorGO.GetComponent<PlayableDirector>();
        }
    }
    public void LinkToSkill(UnitController caster)
    {
        this.caster = caster;
        Debug.Log($"SkillButtonHandler đã liên kết với caster: {caster.name} và sẽ sử dụng kỹ năng {caster.skillSO.skillName}");
        Debug.Log($"SkillButtonHandler đã liên kết với skill: {caster.skillSO.skillName} và kỹ năng hao {caster.skillSO.manaCost} mana");
    }

    public void OnSkillButtonPressed()
    {
        if (uiManager == null)
        {
            return;
        }
        if (caster.currentMana >= caster.skillSO.manaCost)
        {
            BindAndPlayTimeline();
            UsePerformSkill();
        }
        else
        {
            Debug.LogWarning($"Không đủ mana để sử dụng {caster.skillSO.skillName}.");
            return; // Không đủ mana, không thực hiện gì cả
        }
    }
    private void BindAndPlayTimeline()
    {
        // Lấy UI Set đúng cho caster hiện tại từ Manager
        SkillUISet currentUISet = uiManager.GetUISet(caster.characterID);
        if (currentUISet == null)
        {
            return;
        }

        skillTimeline.playableAsset = caster.skillSO.skillTimeline;
        var timelineAsset = (TimelineAsset)skillTimeline.playableAsset;

        foreach (var track in timelineAsset.GetOutputTracks())
        {
            // Gán binding DỰA TRÊN UI LẤY TỪ MANAGER
            switch (track.name) // Vẫn dùng tên track đã thống nhất
            {
                case "Skill_Animation_In":
                    skillTimeline.SetGenericBinding(track, currentUISet.skillUI_In.GetComponent<Animator>());
                    break;
                case "Skill_Activation_In":
                    skillTimeline.SetGenericBinding(track, currentUISet.skillUI_In);
                    break;
                case "Skill_Animation_Out":
                    skillTimeline.SetGenericBinding(track, currentUISet.skillUI_Out.GetComponent<Animator>());
                    break;
                case "Skill_Activation_Out":
                    skillTimeline.SetGenericBinding(track, currentUISet.skillUI_Out);
                    break;
            }
        }

        // ... code Play() giữ nguyên
        Time.timeScale = 0f;
        skillTimeline.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        skillTimeline.stopped += OnTimelineFinished;
        skillTimeline.Play();
    }
    // 5. Hàm này sẽ được tự động gọi khi Timeline kết thúc
    private void OnTimelineFinished(PlayableDirector director)
    {
        // Chỉ xử lý nếu đúng là director này đã kết thúc
        if (skillTimeline == director)
        {
            // 6. Trả game về tốc độ bình thường
            Time.timeScale = 1f;

            // 7. Rất quan trọng: Hủy đăng ký sự kiện để tránh gọi lại nhiều lần
            skillTimeline.stopped -= OnTimelineFinished;

            // Tại đây bạn có thể thực hiện logic gây sát thương cuối cùng nếu muốn
        }
    }
    private void UsePerformSkill()
    {
        List<UnitController> currentTarget = SkillManager.Instance.GetTargets(caster.skillSO, caster);
        if(caster.skillSO == null)
        {
            Debug.LogError("SkillSO không được gán cho caster.");
            return;
        }
        if (currentTarget.Count == 0)
        {
            Debug.LogWarning("Không có mục tiêu để sử dụng kỹ năng.");
            return;
        }
        var castSkillModule = caster.GetComponent<UnitCastSkill>();
        if (castSkillModule == null)
        {
            Debug.LogError("Không tìm thấy UnitCastSkill trên caster.");
            return;
        }

        // Gọi hàm PrepareToUseSkill để chuẩn bị sử dụng kỹ năng
        caster.PerformSkill(caster.skillSO, currentTarget, () =>
        {
            // Callback khi kỹ năng đã được sử dụng
            Debug.Log($"[SkillButtonHandler] {caster.name} đã hoàn tất sử dụng skill {caster.skillSO.name}.");
            // Cập nhật HUD nếu cần

        });

    }
}
