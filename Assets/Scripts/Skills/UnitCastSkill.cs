using System;
using System.Collections.Generic;
using System.Linq; // Thêm để sử dụng .Keys.ToList() cho an toàn
using UnityEngine;

/// <summary>
/// Module chuyên môn chịu trách nhiệm kiểm tra và thực thi logic của một kỹ năng.
/// Hoạt động dưới sự chỉ huy của UnitController.
/// </summary>
[RequireComponent(typeof(UnitController))] // Đảm bảo luôn có UnitController đi kèm
public class UnitCastSkill : MonoBehaviour
{
    // --- CÁC THAM CHIẾU NỘI BỘ ---
    private UnitController _unitController;
    private AnimationEventReceiver animationEventReceiver;

    // --- CÁC BIẾN TRẠNG THÁI (STATE) ---
    private SkillSO skillToUse;
    private List<UnitController> targets;
    private Action onActionComplete;
    private Dictionary<SkillSO, float> cooldowns = new Dictionary<SkillSO, float>();

    #region --- Khởi tạo và Quản lý Vòng đời ---

    private void Awake()
    {
        // Tự động lấy các component cần thiết cho hoạt động
        _unitController = GetComponent<UnitController>();
        animationEventReceiver = GetComponent<AnimationEventReceiver>();

        // Kiểm tra lỗi ngay từ đầu để gỡ lỗi dễ dàng hơn
        if (_unitController == null)
            Debug.LogError($"[UnitCastSkill] Không tìm thấy UnitController trên {gameObject.name}!");

        if (animationEventReceiver == null)
            Debug.LogError($"[UnitCastSkill] Không tìm thấy AnimationEventReceiver trên {gameObject.name}! Các sự kiện animation sẽ không hoạt động.");
    }

    private void OnEnable()
    {
        // Đăng ký lắng nghe sự kiện từ animation khi script được kích hoạt
        if (animationEventReceiver != null)
        {
            animationEventReceiver.OnAnimationActionTrigger += HandleAnimationAction;
        }
    }

    private void OnDisable()
    {
        // Hủy đăng ký để tránh lỗi khi script bị vô hiệu hóa
        if (animationEventReceiver != null)
        {
            animationEventReceiver.OnAnimationActionTrigger -= HandleAnimationAction;
        }
    }

    private void Update()
    {
        // Xử lý giảm thời gian cooldown mỗi frame
        HandleCooldowns();
    }

    #endregion

    #region --- API Công khai (Giao tiếp với UnitController) ---

    /// <summary>
    /// Được gọi bởi UnitController để chuẩn bị sử dụng một kỹ năng.
    /// Kiểm tra các điều kiện và lưu lại thông tin nếu hợp lệ.
    /// </summary>
    /// <returns>Trả về true nếu có thể dùng skill, ngược lại trả về false.</returns>
    public bool PrepareToUseSkill(SkillSO skill, List<UnitController> targets, Action onComplete)
    {
        if (!CanUseSkill(skill))
        {
            onComplete?.Invoke(); // Báo cho hệ thống là hành động đã kết thúc (do thất bại)
            return false;
        }

        // Nếu đủ điều kiện, lưu lại thông tin để HandleAnimationAction sử dụng sau
        this.skillToUse = skill;
        this.targets = targets;
        this.onActionComplete = onComplete;

        return true;
    }

    #endregion

    #region --- Logic Nội bộ và Xử lý Sự kiện ---

    /// <summary>
    /// Hàm kiểm tra các điều kiện nội bộ để xem có thể dùng skill không.
    /// </summary>
    private bool CanUseSkill(SkillSO skill)
    {
        if (skill == null)
        {
            Debug.LogError("Yêu cầu kiểm tra một skill null!");
            return false;
        }

        // Kiểm tra Mana
        if (_unitController.UnitData.CurrentMana < skill.manaCost)
        {
            Debug.Log($"[UnitCastSkill] Không đủ mana để dùng {skill.skillName}. Cần {skill.manaCost}, có {_unitController.UnitData.CurrentMana}");
            return false;
        }

        // Kiểm tra Cooldown
        if (cooldowns.ContainsKey(skill))
        {
            Debug.Log($"[UnitCastSkill] Kỹ năng {skill.skillName} đang trong thời gian hồi chiêu.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Hàm này CHỈ được gọi bởi Animation Event.
    /// Đây là lúc hiệu ứng của kỹ năng được áp dụng.
    /// </summary>
    private void HandleAnimationAction()
    {
        if (skillToUse == null)
        {
            Debug.Log("[UnitCastSkill] Không có kỹ năng nào được chuẩn bị để sử dụng. Vui lòng gọi PrepareToUseSkill trước.");
            // Animation event có thể bị gọi nhầm, thoát ra để an toàn
            return;
        }
        Debug.Log($"[UnitCastSkill] Animation Event kích hoạt! Thực thi hiệu ứng của {skillToUse.skillName}.");

        // 1. Trừ mana và đặt cooldown
        _unitController.UseMana(skillToUse.manaCost);
        cooldowns[skillToUse] = Time.time + skillToUse.cooldownTime;

        // 2. Thực thi hiệu ứng kỹ năng (ví dụ: gây sát thương, hồi máu)
        // Đây là nơi lý tưởng để dùng một hệ thống SkillManager hoặc Strategy Pattern
        // Giả sử target có một hàm TakeDamage để nhận sát thương
        ISkillLogic skillLogic = SkillManager.Instance.GetSkillLogic(skillToUse);
        if (skillLogic != null)
        {
            skillLogic.ExecuteSkill(skillToUse, _unitController, targets);
            Debug.Log($"[UnitCastSkill] đã thực hiện kỹ năng {skillLogic}");
        }
        else
        {
            Debug.LogError($"[UnitCastSkill] Không tìm thấy logic cho kỹ năng {skillToUse.skillName}. Vui lòng kiểm tra SkillManager.");
        }

        // 3. Gọi callback để báo cho UnitController và BattleHandler là hành động đã hoàn tất
        onActionComplete?.Invoke();

        // 4. Reset các biến trạng thái để chuẩn bị cho lần tiếp theo
        skillToUse = null;
        targets = null;
        onActionComplete = null;
    }

    /// <summary>
    /// Xử lý việc giảm và loại bỏ các kỹ năng hết thời gian hồi chiêu.
    /// </summary>
    private void HandleCooldowns()
    {
        // Tạo một danh sách các key cần xóa để tránh lỗi khi sửa đổi Dictionary trong vòng lặp
        List<SkillSO> skillsToRemove = new List<SkillSO>();
        foreach (var skill in cooldowns.Keys)
        {
            if (cooldowns[skill] <= Time.time)
            {
                skillsToRemove.Add(skill);
            }
        }

        foreach (var skill in skillsToRemove)
        {
            cooldowns.Remove(skill);
            Debug.Log($"[UnitCastSkill] Kỹ năng '{skill.skillName}' đã hết hồi chiêu.");
        }
    }

    #endregion
}