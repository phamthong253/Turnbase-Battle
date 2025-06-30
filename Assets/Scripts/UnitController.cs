using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitSO; // Đảm bảo bạn có dòng này nếu enum AttackType nằm trong UnitSO

public class UnitController : MonoBehaviour
{
    [Header("UI Skill")]
    public CharacterID characterID; // ID của tướng, để liên kết với UI

    public static event Action<UnitController> OnUnitDestroyed;
    #region Public References & Settings
    [Header("Data & Core Components")]
    public SkillSO skillSO; // Tham chiếu đến kỹ năng hiện tại của unit, nếu có
    public UnitSO unitSO;
    public UnitBase unitBase;

    [Header("Configuration")]
    [Tooltip("Đánh dấu nếu đây là unit của người chơi")]
    public bool isPlayerUnit;
    [HideInInspector] public int formationSlotIndex;
    #endregion

    #region State & Private Variables
    public enum UnitState { IDLE, MOVING_TO_TARGET, ATTACKING, USESKILL, RETURNING_HOME, BUSY, DESTROYED }
    public UnitState unitState { get; private set; }

    [SerializeField] public int currentHealth, currentMana, currentDamage, currentArmor;
    public bool isDestroyed = false;

    private UnitController currentTarget;
    private float attackCooldown;
    private float attackTimer;
    private Vector3 formationOffset; // Khoảng cách riêng của unit so với tâm của đội
    private Transform teamAnchor;    // Transform của cả đội (chính là teamFollowTarget hoặc playerTeamAnchor)
    public Transform fireTransform;
    private UnitCastSkill unitCastSkill;
    //public SkillSO skill; // Kỹ năng hiện tại của unit, nếu có
    public Vector3 HomePosition
    {
        get
        {
            // Nếu vì lý do nào đó không có anchor, "nhà" chính là vị trí hiện tại để tránh lỗi
            if (teamAnchor == null)
            {
                return transform.position;
            }

            // CÔNG THỨC VÀNG: Vị trí nhà = vị trí của cả đội + khoảng cách riêng của mình
            return teamAnchor.position + formationOffset;
        }
    }
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        if (unitBase == null) unitBase = GetComponent<UnitBase>();
        unitCastSkill = GetComponent<UnitCastSkill>();
    }

    void Start()
    {
        unitState = UnitState.IDLE;
        if (unitSO != null && unitSO.attackSpeed > 0)
        {
            this.attackCooldown = unitSO.attackSpeed;
        }
        //else
        //{
        //    this.attackCooldown = 2.0f; // Giá trị mặc định an toàn
        //}
        // Đặt attackTimer về 0 để unit có thể tấn công ngay khi có mục tiêu
        this.attackTimer = 0;
    }

    void Update()
    {
        // --- Các kiểm tra cơ bản ---
        if (isDestroyed || currentHealth <= 0)
        {
            if (!isDestroyed) Delete();
            return;
        }
        if (unitState == UnitState.BUSY) return; // Nếu đang bận (dùng skill, chuyển stage), không làm gì cả

        // --- LOGIC MỚI - ƯU TIÊN VỊ TRÍ ---

        // 1. XÁC ĐỊNH VỊ TRÍ ĐÍCH (DESTINATION)
        Vector3 destination;
        bool shouldReturnHome = (currentTarget == null || currentTarget.isDestroyed);

        if (shouldReturnHome)
        {
            // Nếu không có mục tiêu, vị trí đích luôn là "nhà"
            //destination = HomePosition;
            return; // Không làm gì nếu không có mục tiêu, tránh di chuyển về nhà liên tục
        }
        else // Nếu có mục tiêu
        {
            if (unitSO.attackType == AttackType.Ranged || unitSO.attackType == AttackType.Magic)
            {
                // Tướng tầm xa luôn muốn ở "nhà"
                destination = HomePosition;
            }
            else // Tướng cận chiến
            {
                // Vị trí đích là áp sát kẻ địch
                float yOffset = ((float)(formationSlotIndex % 3) - 1f) * -1.5f;
                Vector3 attackOffset = new Vector3(0, yOffset, 0);
                destination = currentTarget.transform.position - new Vector3(unitSO.attackRange * 0.8f, 0, 0) + attackOffset;
            }
        }

        // 2. XỬ LÝ DI CHUYỂN
        // Kiểm tra xem có cần di chuyển đến vị trí đích không
        if (Vector3.Distance(transform.position, destination) > 0.2f)
        {
            if (!isPlayerUnit) Debug.Log($"<color=white>[BƯỚC 1] {unitSO.name} đang di chuyển đến vị trí chiến đấu.</color>");
            unitState = (shouldReturnHome) ? UnitState.RETURNING_HOME : UnitState.MOVING_TO_TARGET;

            // Di chuyển đến đích
            transform.position = Vector3.MoveTowards(transform.position, destination, 10f * Time.deltaTime);
            unitBase?.PlayAnimation("move");
        }
        else // Nếu đã ở tại hoặc rất gần vị trí đích
        {
            // 3. XỬ LÝ TẤN CÔNG
            unitState = UnitState.ATTACKING;

            // Chỉ tấn công nếu có mục tiêu và ở trong tầm
            if (!shouldReturnHome && Vector3.Distance(transform.position, currentTarget.transform.position) <= unitSO.attackRange)
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0)
                {
                    StartCoroutine(PerformBasicAttack());
                }
                else
                {
                    // Chơi animation idle trong lúc chờ cooldown
                    unitBase?.PlayAnimation("idle");
                }
            }
            else
            {
                // Nếu đã về đến nhà và không có mục tiêu, hoặc là tướng tầm xa đã ở nhà nhưng địch ngoài tầm
                unitBase?.PlayAnimation("idle");
            }
        }
        // --- PHẦN 2: THI HÀNH HÀNH ĐỘNG ---
        // Dựa vào trạng thái vừa được quyết định ở trên, thực hiện hành động tương ứng.
        switch (unitState)
        {
            case UnitState.IDLE:
                unitBase?.PlayAnimation("idle");
                break;

            case UnitState.RETURNING_HOME:
                // Di chuyển về phía "nhà"
                transform.position = Vector3.MoveTowards(transform.position, HomePosition, 10f * Time.deltaTime);
                unitBase?.PlayAnimation("move");
                break;

            case UnitState.MOVING_TO_TARGET:
                transform.position = Vector3.MoveTowards(transform.position, currentTarget.transform.position, 5f * Time.deltaTime);
                unitBase?.PlayAnimation("move");
                break;

            case UnitState.ATTACKING:
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0)
                {
                    StartCoroutine(PerformBasicAttack());
                }
                else
                {
                    unitBase?.PlayAnimation("idle");
                }
                break;
            case UnitState.USESKILL:
                break;
        }
    }
    #endregion

    #region Public API & Helper Methods
    public void InitializeStatsFromSO()
    {
        if (unitSO == null) return;
        this.currentHealth = unitSO.hp;
        this.currentMana = unitSO.mp;
        this.currentDamage = unitSO.damage;
        this.currentArmor = unitSO.armor;
    }
    // Hàm này được BattleHandler gọi khi bắt đầu trận đấu
    public void SetFormationAnchor(Transform teamAnchor, Vector3 homePosition)
    {
        this.teamAnchor = teamAnchor;
        // Tính toán và lưu lại khoảng cách từ vị trí của mình đến vị trí của cả đội
        // Offset này sẽ không đổi trong suốt trận đấu.
        this.formationOffset = homePosition - teamAnchor.position;
    }
    // Trong UnitController.cs
    public void UpdateTeamAnchor(Transform newAnchor)
    {
        this.teamAnchor = newAnchor;
    }
    public UnitController GetCurrentTarget() { return currentTarget ; }
    public void SetTarget(UnitController target) { this.currentTarget = target; }

    public void TakeDamage(int damage)
    {
        this.currentHealth -= damage;
        unitBase?.PlayAnimation("hit");
        BattleHUD.ShowDamagePopup(this.transform.position, damage, isPlayerUnit);
    }
    public void UseMana(int amount)
    {
        currentMana -= amount;
        if (currentMana < 0) currentMana = 0;
    }

    public void Delete()
    {
        Debug.Log($"<color=red>{unitSO.name} đã bị hạ! Bắn tín hiệu OnUnitDefeated!</color>");
        isDestroyed = true;
        unitState = UnitState.DESTROYED;
        // Thêm animation chết và hiệu ứng ở đây nếu muốn
        unitBase?.PlayAnimation("died");
        Destroy(gameObject, 1.5f); // Hủy sau 1.5 giây
        OnUnitDestroyed?.Invoke(this);
    }
    #endregion

    #region Coroutines
    public IEnumerator PerformBasicAttack()
    {
        unitState = UnitState.BUSY;
        attackTimer = attackCooldown; // Reset timer ngay khi bắt đầu tấn công

        unitBase?.PlayAnimation("attack");
        yield return new WaitForSeconds(attackCooldown);
        currentMana += unitSO.damage;
        if(currentMana >= unitSO.maxMP)
        {
            currentMana = unitSO.maxMP; // Đảm bảo không vượt quá max MP
        }
        // Sau khi xong, quay về IDLE để Update có thể đánh giá lại tình hình
        unitState = UnitState.IDLE;
    }
    private void MeleeAttack(UnitController target)
    {
        if (target == null || target.isDestroyed) return;
        // Tính toán sát thương
        int damage = DamageCalculator.CalculatorPhysicalDamage(currentDamage, target.currentArmor);
        target.TakeDamage(damage);
    }
    private void RangedAttack(UnitController target)
    {
        if (target == null || target.isDestroyed) return;
        // Tính toán sát thương
        int damage = DamageCalculator.CalculatorPhysicalDamage(currentDamage, target.currentArmor);
        target.TakeDamage(damage);
        string objectTile = unitSO.projectilePrefab.name;
        GameObject projectile = ObjectPooling.Instance.SpawnFromBool(objectTile, fireTransform.position, fireTransform.rotation);
        if (projectile != null)
        {
            // Nếu có prefab đạn, bắn nó về phía mục tiêu
            BulletController bullet = projectile.GetComponent<BulletController>();
            bullet.transform.LookAt(target.transform);
            if (bullet != null)
            {
                bullet.InitializedBullet(target, damage, objectTile);
            }
        }
    }
    private void MagicAttack(UnitController target)
    {
        if (target == null || target.isDestroyed) return;
        // Tính toán sát thương
        int damage = DamageCalculator.CalculatorPhysicalDamage(currentDamage, target.currentArmor);
        target.TakeDamage(damage);
        string objectTile = unitSO.projectilePrefab.name;
        Debug.Log(unitSO.name + " Đã bắn đạn từ unit: " + objectTile + " vào kẻ địch " + target);
        GameObject projectile = ObjectPooling.Instance.SpawnFromBool(objectTile, fireTransform.position, fireTransform.rotation);
        if (projectile != null)
        {
            // Nếu có prefab đạn, bắn nó về phía mục tiêu
            BulletController bullet = projectile.GetComponent<BulletController>();
            if (bullet != null)
            {
                bullet.InitializedBullet(target, damage, objectTile);
            }
        }
    }
    private void SupportAttack(UnitController target)
    {
        if (target == null || target.isDestroyed) return;
        // Tính toán hiệu ứng hỗ trợ
        // Ví dụ: hồi máu, tăng giáp, v.v.
        int healAmount = 10; // Giả sử là 10
        target.currentHealth += healAmount;
        target.currentHealth = Mathf.Min(target.currentHealth, target.unitSO.hp); // Đảm bảo không vượt quá max HP
        BattleHUD.ShowDamagePopup(target.transform.position, healAmount, true); // Hiển thị hiệu ứng hồi máu
    }
    public IEnumerator MoveCoroutineAndCallback(Vector3 targetPosition, Action onComplete)
    {
        unitState = UnitState.BUSY;
        unitBase?.PlayAnimation("move");

        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, 15f * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
        unitBase?.PlayAnimation("idle");

        unitState = UnitState.IDLE;
        onComplete?.Invoke();
    }
    #endregion
    #region Animation Event Receivers

    // Hàm này sẽ được gọi bởi Animation Event đặt trên "hit frame" của animation tấn công
    public void AnimationTrigger_OnAttackAction()
    {
        // Nếu không có mục tiêu hoặc mục tiêu đã bị hủy, không làm gì cả
        if (currentTarget == null || currentTarget.isDestroyed) return;

        Debug.Log($"<color=cyan>Animation Event 'OnAttackAction' được kích hoạt cho {unitSO.name}</color>");

        // Dựa vào loại tấn công để thực hiện hành động tương ứng
        switch (unitSO.attackType)
        {
            case AttackType.Melee:
                MeleeAttack(currentTarget);
                break;

            case AttackType.Ranged:
                RangedAttack(currentTarget);
                break;

            case AttackType.Magic:
                MagicAttack(currentTarget);
                break;

            case AttackType.Support:
                SupportAttack(currentTarget);
                break;
        }
    }
    #endregion

    public void PerformSkill(SkillSO skill, List<UnitController> targets, Action onComplete)
    {
        // 1. KIỂM TRA TRẠNG THÁI CỦA CHÍNH MÌNH (SĨ QUAN CHỈ HUY)
        if (unitState == UnitState.DESTROYED)
        {
            Debug.LogWarning($"[UnitController] {name} đang bận hoặc đã chết, không thể thực hiện hành động mới.");
            onComplete?.Invoke(); // Báo lại ngay là hành động thất bại
            return;
        }

        // 2. ỦY QUYỀN CHO CHUYÊN GIA VŨ KHÍ CHUẨN BỊ
        // Gọi hàm PrepareToUseSkill, truyền đầy đủ thông tin cho nó
        bool canPerform = unitCastSkill.PrepareToUseSkill(skill, targets, onComplete);

        // 3. NẾU CHUYÊN GIA BÁO LẠI LÀ ĐÃ SẴN SÀNG...
        if (canPerform)
        {
            // 4. ...THÌ SĨ QUAN MỚI CHUYỂN TRẠNG THÁI VÀ RA LỆNH CHO BỘ PHẬN ANIMATION
            this.unitState = UnitState.USESKILL;
            Debug.Log($"[UnitController] {name} chuyển sang trạng thái USESKILL.");

            // Ra lệnh cho UnitBase chạy animation tương ứng với skill,
            // sử dụng hệ thống Skill Animation Map.
            unitBase.PlayAnimation(skill.skillAnimationString);
        }
        else
        {
            // Nếu không thể thực hiện, UnitCastSkill đã log lý do và gọi onComplete.
            // UnitController không cần làm gì thêm.
            Debug.Log($"[UnitController] {name} không thể kích hoạt animation {skill.skillAnimationString} do không đủ điều kiện.");
            Debug.Log($"[UnitController] {name} không thể sử dụng kỹ năng {skill.skillName} do không đủ điều kiện.");
        }
    }

    /// <summary>
    /// Được gọi bởi callback từ UnitCastSkill để báo rằng hành động đã kết thúc,
    /// cho phép UnitController trở về trạng thái IDLE.
    /// </summary>
    public void OnActionFinished()
    {
        Debug.Log($"[UnitController] {name} đã hoàn thành hành động, trở về trạng thái IDLE.");
        this.unitState = UnitState.IDLE;
    }
}
