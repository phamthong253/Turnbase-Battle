using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnitSO; // Đảm bảo bạn có dòng này nếu enum AttackType nằm trong UnitSO
using DG.Tweening;

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
    private float currentAttackCooldown;
    private float attackTimer;
    private int basicAttackComboCount = 0;
    private Vector3 formationOffset; // Khoảng cách riêng của unit so với tâm của đội
    private Transform teamAnchor;    // Transform của cả đội (chính là teamFollowTarget hoặc playerTeamAnchor)
    public Transform fireTransform;
    private UnitCastSkill unitCastSkill;
    public ItemSO itemToDrop;
    public float dropChance;
    [Header("Knockback Effect")]
    private float knockbackStrength = 0.8f;
    private float maxKnockbackDistance = 3f;
    private Vector3 originalPos;
    private bool isKnockback = false;
    private Tween knockBackTween;

    [Header("Enhanced Attack Settings")]
    //public Transform effectMountPoint; // Kéo thả Transform (tay hoặc vũ khí) vào đây
    public string enhancedVfxTag; // Tên tag trong Object Pooling (VD: "SwordGlow")
    public float enhancedVfxDuration = 1f; // Thời gian hiệu ứng tồn tại (khớp với độ dài animation)
    public bool isCurrentlyEnhanced = false;
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

    public AudioClip attackAudioClip;
    public AudioClip hitAudioClip;
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
        //if (unitSO != null && unitSO.attackSpeed > 0)
        //{
        //    this.attackCooldown = unitSO.attackSpeed;
        //}
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
            if (unitSO.attackType == AttackType.Ranged || unitSO.attackType == AttackType.Magic || unitSO.attackType == AttackType.Support || unitSO.attackType == AttackType.Enemy )
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
                    // Play animation idle trong lúc chờ cooldown
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
        this.currentAttackCooldown = unitSO.attackCooldown > 0 ? unitSO.attackCooldown : 2f;
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

    public void TakeDamage(int damage, bool isCritical)
    {
        this.currentHealth -= damage;
        float force = isCritical ? knockbackStrength * 1.5f : knockbackStrength;
        ApplyCumulativeKnockback(force);
        unitBase?.PlayForceAnimation("hit", () => {
            if (this.currentHealth <= 0)
            {
                unitBase?.PlayAnimation("died"); // Hoặc gọi hàm xử lý cái chết
            }
        });
        BattleHUD.ShowDamagePopup(this.transform.position, damage, isPlayerUnit, isCritical, false);
        AudioManager.Instance.PlaySFX("HitAudioCliphit"); // Phát âm thanh khi bị đánh
    }
    private void ApplyCumulativeKnockback(float pushForce)
    {
        if (!isKnockback)
        {
            originalPos = transform.position;
            isKnockback = true;
        }
        if(knockBackTween.IsActive() && knockBackTween != null)
        {
            knockBackTween.Kill();
        }
        Vector3 knockBackDir = isPlayerUnit ? Vector3.left : Vector3.right;
        Vector3 targetPushPos = transform.position + (knockBackDir * pushForce);
        float distanceFromOriginal = Vector3.Distance(originalPos, targetPushPos);
        if(distanceFromOriginal > maxKnockbackDistance)
        {
            targetPushPos = originalPos + (targetPushPos - originalPos).normalized * maxKnockbackDistance;
        }
        Sequence seq = DOTween.Sequence();
        float pushDuration = Mathf.Clamp(pushForce * 0.1f, 0.3f, 0.5f);
        seq.Append(transform.DOMove(targetPushPos, pushDuration).SetEase(Ease.OutQuad));
        seq.Append(transform.DOMove(originalPos, 0.4f).SetEase(Ease.OutElastic));
        seq.OnComplete(() => { isKnockback = false; });
        knockBackTween = seq;
    }
    public void UseMana(int amount)
    {
        currentMana -= amount;
        if (currentMana < 0) currentMana = 0;
    }

    public void Delete()
    {
        isDestroyed = true;
        unitState = UnitState.DESTROYED;
        // Thêm animation chết và hiệu ứng ở đây nếu muốn
        unitBase?.PlayAnimation("died");
        Destroy(gameObject, 1.5f); // Hủy sau 1.5 giây
        OnUnitDestroyed?.Invoke(this);
        if (!isPlayerUnit)
        {
            if (RewardManager.Instance != null)
            {
                RewardManager.Instance.ItemReward();
            }
        }

    }
    #endregion

    #region Coroutines
    public IEnumerator PerformBasicAttack()
    {
        unitState = UnitState.BUSY;

        bool isEnoughStack = basicAttackComboCount >= unitSO.comboThreshold-1;
        bool targetIsWorthy = currentTarget != null && currentTarget.currentHealth > currentDamage;
        if (isEnoughStack && targetIsWorthy)
        {
            isCurrentlyEnhanced = true;
            basicAttackComboCount = 0; // Reset combo sau khi dùng đòn mạnh
                                       // --- CODE MỚI: KÍCH HOẠT HIỆU ỨNG CƯỜNG HÓA ---
            //if (!string.IsNullOrEmpty(enhancedVfxTag) && effectMountPoint != null)
            //{
            //    // 1. Lấy VFX từ Pool
            //    GameObject vfx = ObjectPooling.Instance.SpawnFromBool(enhancedVfxTag, effectMountPoint.position, effectMountPoint.rotation);

            //    if (vfx != null)
            //    {
            //        // 2. Gắn VFX vào điểm trên người nhân vật (để nó di chuyển theo tay/kiếm)
            //        vfx.transform.SetParent(effectMountPoint);
            //        vfx.transform.localPosition = Vector3.zero;
            //        vfx.transform.localRotation = Quaternion.identity;

            //        // 3. Hẹn giờ trả về Pool (và tách ra khỏi nhân vật)
            //        StartCoroutine(ReturnEnhancedVFX(vfx, enhancedVfxDuration));
            //    }
            //}
        }
        else
        {
            isCurrentlyEnhanced = false;
            basicAttackComboCount++;
            if (!isEnoughStack)
            {
                basicAttackComboCount++;
                Debug.Log($"<color=yellow>{unitSO.name} tích stack: {basicAttackComboCount}</color>");
            }
            else
            {
                Debug.Log($"<color=cyan>{unitSO.name} đang giữ đòn cường hóa (Địch quá yếu)...</color>");
            }
        }
        string attackAnimationKey = isCurrentlyEnhanced ? "enhancedAttack" : "attack";
        unitBase?.PlayAnimation(attackAnimationKey);
        //yield return new WaitForSeconds(attackCooldown);
        float animDuration = unitBase.GetAnimationDuration(attackAnimationKey);
        yield return new WaitForSeconds(animDuration);
        currentMana += unitSO.damage;
        if(currentMana >= unitSO.maxMP)
        {
            currentMana = unitSO.maxMP; // Đảm bảo không vượt quá max MP
        }
        attackTimer = currentAttackCooldown; // Reset timer ngay khi bắt đầu tấn công
        // Sau khi xong, quay về IDLE để Update có thể đánh giá lại tình hình
        unitState = UnitState.IDLE;
    }
    // Hàm phụ trợ để trả VFX về Pool
    private IEnumerator ReturnEnhancedVFX(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (vfx != null)
        {
            // Quan trọng: Phải tách VFX ra khỏi nhân vật trước khi trả về Pool
            // Nếu không lần sau lấy ra nó vẫn dính vào nhân vật cũ hoặc bị méo Scale
            vfx.transform.SetParent(null);

            // Trả về Pool
            if (ObjectPooling.Instance != null)
            {
                ObjectPooling.Instance.ReturnToPool(enhancedVfxTag, vfx);
            }
            else
            {
                vfx.SetActive(false);
            }
        }
    }
    private void MeleeAttack(UnitController target, bool isEnhanced)
    {
        if (target == null || target.isDestroyed) return;

        // 1. Tính toán sát thương
        int damage = DamageCalculator.CalculatorPhysicalDamage(currentDamage, target.currentArmor);
        bool isCritical = DamageCalculator.IsCriticalHit(unitSO.critChance);
        if (isCritical) damage = Mathf.RoundToInt(damage * 2f);
        if(isEnhanced) damage = Mathf.RoundToInt(damage * unitSO.damageEnhencedMultiplier);
        // --- LOGIC AOE MỚI ---
        // Mặc định là không AOE
        bool enableAOE = false;
        float radius = 0f;
        float dmgPercent = 0f;

        // Chỉ bật AOE nếu: Đang là đòn cường hóa VÀ UnitSO cho phép AOE
        if (isEnhanced && unitSO.useEnhancedAOE)
        {
            enableAOE = true;
            radius = unitSO.aoeRadius;
            dmgPercent = unitSO.aoeDamage;
        }
        // ---------------------

        // 2. Lấy tên Prefab "Vết chém" (Thay vì projectile bay, ta dùng projectile đứng yên)
        // Lưu ý: Bạn nên gán Prefab vết chém vào ô ProjectilePrefab của UnitSO cho tướng Melee
        // Hoặc tạo một biến riêng meleeSlashPrefab trong UnitSO
        string meleeTag = "";
        if (unitSO.projectilePrefab != null)
        {
            meleeTag = unitSO.projectilePrefab.name;
        }
        else
        {
            Debug.LogWarning("Chưa gán Prefab vết chém (Projectile) cho tướng Melee!");
            return;
        }

        // 3. Lấy tên Hit VFX (Hiệu ứng nổ/máu khi trúng)
        string vfxTag = (unitSO.hitVFXPrefab != null) ? unitSO.hitVFXPrefab.name : "";

        GameObject slashObject = ObjectPooling.Instance.SpawnFromBool(meleeTag, target.transform.position, Quaternion.identity);
        if(isEnhanced && !string.IsNullOrEmpty(unitSO.enhancedAttackVFXName))
        {
            vfxTag = unitSO.enhancedAttackVFXName;
        }

        if (slashObject != null)
        {
            BulletController slashScript = slashObject.GetComponent<BulletController>();
            if (slashScript != null)
            {
                slashScript.InitializedBullet(target, damage, meleeTag, vfxTag, isCritical, isPlayerUnit, enableAOE, radius, dmgPercent);
            }
        }
    }
    private void MagicAttack(UnitController target, bool isEnhanced)
    {
        if (target == null || target.isDestroyed) return;
        // Tính toán sát thương
        int damage = DamageCalculator.CalculatorPhysicalDamage(currentDamage, target.currentArmor);
        bool isCritical = DamageCalculator.IsCriticalHit(unitSO.critChance);
        if(isEnhanced) damage = Mathf.RoundToInt(damage * unitSO.damageEnhencedMultiplier);
        if (isCritical) damage = Mathf.RoundToInt(damage * 2f);

        // --- LOGIC AOE MỚI ---
        // Mặc định là không AOE
        bool enableAOE = false;
        float radius = 0f;
        float dmgPercent = 0f;

        // Chỉ bật AOE nếu: Đang là đòn cường hóa VÀ UnitSO cho phép AOE
        if (isEnhanced && unitSO.useEnhancedAOE)
        {
            enableAOE = true;
            radius = unitSO.aoeRadius;
            dmgPercent = unitSO.aoeDamage;
        }
        // ---------------------

        string bulletTag = unitSO.projectilePrefab.name;
        GameObject projectile = ObjectPooling.Instance.SpawnFromBool(bulletTag, fireTransform.position, fireTransform.rotation);
        string vfxTag = unitSO.hitVFXPrefab.name;
        if(isEnhanced && !string.IsNullOrEmpty(unitSO.enhancedAttackVFXName))
        {
            vfxTag = unitSO.enhancedAttackVFXName;
        }
        if (projectile != null)
        {
            // Nếu có prefab đạn, bắn nó về phía mục tiêu
            BulletController bullet = projectile.GetComponent<BulletController>();
            if (bullet != null)
            {
                bullet.InitializedBullet(target, damage, bulletTag, vfxTag, isCritical, isPlayerUnit, enableAOE, radius, dmgPercent);
                // Nếu là AOE, có thể scale đạn to lên cho hoành tráng
                if (enableAOE) bullet.transform.localScale = Vector3.one * 2f;
                else if (isEnhanced) bullet.transform.localScale = Vector3.one * 1.5f;
                else bullet.transform.localScale = Vector3.one;
            }
        }
    }
    private void RangedAttack(UnitController target, bool isEnhanced)
    {
        if (target == null || target.isDestroyed) return;
        // Tính toán sát thương
        int damage = DamageCalculator.CalculatorPhysicalDamage(currentDamage, target.currentArmor);
        bool isCritical = DamageCalculator.IsCriticalHit(unitSO.critChance);
        if (isEnhanced) damage = Mathf.RoundToInt(damage * unitSO.damageEnhencedMultiplier);
        if (isCritical) damage = Mathf.RoundToInt(damage * 2f);
        // --- LOGIC AOE MỚI ---
        // Mặc định là không AOE
        bool enableAOE = false;
        float radius = 0f;
        float dmgPercent = 0f;

        // Chỉ bật AOE nếu: Đang là đòn cường hóa VÀ UnitSO cho phép AOE
        if (isEnhanced && unitSO.useEnhancedAOE)
        {
            enableAOE = true;
            radius = unitSO.aoeRadius;
            dmgPercent = unitSO.aoeDamage;
        }
        // ---------------------

        string bulletTag = unitSO.projectilePrefab.name;
        GameObject projectile = ObjectPooling.Instance.SpawnFromBool(bulletTag, fireTransform.position, fireTransform.rotation);
        string vfxTag = unitSO.hitVFXPrefab.name;
        if (isEnhanced && !string.IsNullOrEmpty(unitSO.enhancedAttackVFXName))
        {
            vfxTag = unitSO.enhancedAttackVFXName;
        }
        if (projectile != null)
        {
            // Nếu có prefab đạn, bắn nó về phía mục tiêu
            BulletController bullet = projectile.GetComponent<BulletController>();
            if (bullet != null)
            {
                bullet.InitializedBullet(target, damage, bulletTag, vfxTag, isCritical, isPlayerUnit, enableAOE, radius, dmgPercent);
            }
        }
    }
    private void SupportAttack(UnitController target, bool isEnhanced)
    {
        // Biến lưu mục tiêu thực tế và chế độ bắn
        UnitController finalTarget = target;
        bool isHealingMode = false;
        int finalValue = 0; // Sát thương hoặc Hồi máu

        // --- LOGIC ĐỘC QUYỀN (Strategy Pattern dạng đơn giản) ---
        if (isEnhanced)
        {
            // 1. Đổi mục tiêu: Tìm đồng đội thấp máu nhất thay vì đánh địch
            finalTarget = GetLowestHealthAlly();

            // 2. Chuyển sang chế độ Hồi máu
            isHealingMode = true;

            // 3. Tính lượng hồi phục (Dựa trên Damage * Hệ số hồi phục)
            // Ví dụ: DamageEnhancedMultiplier lúc này đóng vai trò là % hồi máu
            finalValue = Mathf.RoundToInt(currentDamage * unitSO.damageEnhencedMultiplier);
        }
        else
        {
            // Đánh thường: Mục tiêu là Enemy (target ban đầu)
            if (target == null || target.isDestroyed) return;

            finalTarget = target;
            isHealingMode = false;

            // Tính sát thương vật lý lên địch
            finalValue = DamageCalculator.CalculatorPhysicalDamage(currentDamage, target.currentArmor);
        }

        // Tính Critical (Áp dụng cho cả Dame và Heal - Hồi máu chí mạng)
        bool isCritical = DamageCalculator.IsCriticalHit(unitSO.critChance);
        if (isCritical) finalValue = Mathf.RoundToInt(finalValue * 2f);

        // --- LOGIC AOE (Chỉ áp dụng nếu UnitSO cho phép) ---
        bool enableAOE = false;
        float radius = 0f;
        float dmgPercent = 0f;

        // Chỉ bật AOE nếu là Enhance
        if (isEnhanced && unitSO.useEnhancedAOE)
        {
            enableAOE = true;
            radius = unitSO.aoeRadius;
            dmgPercent = unitSO.aoeDamage;
        }

        // --- SPAWN ĐẠN ---
        // Nếu là Heal: Dùng Prefab đạn Heal (EnhancedProjectile). Nếu đánh thường: Dùng đạn thường.
        string bulletTag = unitSO.projectilePrefab.name;

        // VFX nổ
        string vfxTag = unitSO.hitVFXPrefab.name;
        if (isEnhanced && !string.IsNullOrEmpty(unitSO.enhancedAttackVFXName))
        {
            vfxTag = unitSO.enhancedAttackVFXName;
        }

        // Bắn
        GameObject projectile = ObjectPooling.Instance.SpawnFromBool(bulletTag, fireTransform.position, fireTransform.rotation);
        if (projectile != null)
        {
            BulletController bullet = projectile.GetComponent<BulletController>();
            if (bullet != null)
            {
                // Quan trọng: Truyền isHealingMode vào cuối hàm
                bullet.InitializedBullet(finalTarget, finalValue, bulletTag, vfxTag, isCritical, isPlayerUnit,
                                         enableAOE, radius, dmgPercent, isHealingMode);
            }
        }
    }
    private void EnemyAttack(UnitController target)
    {
        if (target == null || target.isDestroyed) return;
        // Tính toán sát thương
        int damage = DamageCalculator.CalculatorPhysicalDamage(currentDamage, target.currentArmor);
        bool isCritical = DamageCalculator.IsCriticalHit(unitSO.critChance);
        if (isCritical)
        {
            damage = Mathf.RoundToInt(damage * 2f);
        }
        // --- LOGIC AOE MỚI ---
        // Mặc định là không AOE
        bool enableAOE = false;
        float radius = 0f;
        float dmgPercent = 0f;

        //// Chỉ bật AOE nếu: Đang là đòn cường hóa VÀ UnitSO cho phép AOE
        //if (isEnhanced && unitSO.useEnhancedAOE)
        //{
        //    enableAOE = true;
        //    radius = unitSO.aoeRadius;
        //    dmgPercent = unitSO.aoeDamage;
        //}
        // ---------------------
        string bulletTag = unitSO.projectilePrefab.name;
        GameObject projectile = ObjectPooling.Instance.SpawnFromBool(bulletTag, fireTransform.position, fireTransform.rotation);
        string vfxTag = unitSO.hitVFXPrefab.name;
        if (projectile != null)
        {
            // Nếu có prefab đạn, bắn nó về phía mục tiêu
            BulletController bullet = projectile.GetComponent<BulletController>();
            if (bullet != null)
            {
                bullet.InitializedBullet(target, damage, bulletTag, vfxTag, isCritical, isPlayerUnit, enableAOE, radius, dmgPercent);
            }
        }
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
        AudioManager.Instance.PlaySFX("AttackAudioClipattackHitattackHit"); // Phát âm thanh tấn công
        // Dựa vào loại tấn công để thực hiện hành động tương ứng
        switch (unitSO.attackType)
        {
            case AttackType.Melee:
                MeleeAttack(currentTarget, isCurrentlyEnhanced);
                break;

            case AttackType.Ranged:
                RangedAttack(currentTarget, isCurrentlyEnhanced);
                break;

            case AttackType.Magic:
                MagicAttack(currentTarget, isCurrentlyEnhanced);
                break;

            case AttackType.Support:
                SupportAttack(currentTarget, isCurrentlyEnhanced);
                break;
            case AttackType.Enemy:
                EnemyAttack(currentTarget);
                break;
        }
    }
    #endregion

    public void PerformSkill(SkillSO skill, List<UnitController> targets, Action onComplete)
    {
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
    }

    /// <summary>
    /// Được gọi bởi callback từ UnitCastSkill để báo rằng hành động đã kết thúc,
    /// cho phép UnitController trở về trạng thái IDLE.
    /// </summary>
    public void OnActionFinished()
    {
        this.unitState = UnitState.IDLE;
    }

    public IEnumerator MoveCoroutineAndCallback(Vector3 destination, float duration, Action onFinished)
    {
        float startTime = Time.time;
        Vector3 startPos = transform.position;

        // Giả sử bạn kích hoạt hoạt ảnh chạy/đi bộ trong quá trình này
        // SetAnimationState(UnitAnimationState.Run); 

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            transform.position = Vector3.Lerp(startPos, destination, t);
            yield return null;
        }

        transform.position = destination;
        //unitBase.PlayAnimation("idle");
        // SetAnimationState(UnitAnimationState.Idle); // Trở về trạng thái tĩnh

        onFinished?.Invoke();
    }
    public void Heal(int amount, bool isCrit)
    {
        if (isDestroyed) return;

        currentHealth += amount;
        if (currentHealth > unitSO.maxHP) currentHealth = unitSO.maxHP;

        // Hiển thị text healing (Màu xanh lá)
        string text = isCrit ? $"<size=120%>{amount}!</size>" : amount.ToString();
        BattleHUD.ShowDamagePopup(this.transform.position, amount, isPlayerUnit, isCrit, true);

        Debug.Log($"{name} được hồi {amount} máu!");
    }
    // Hàm trả về đồng đội có % máu thấp nhất
    private UnitController GetLowestHealthAlly()
    {
        // 1. Tìm tất cả Unit đang hoạt động (Cách đơn giản nhất)
        // Lưu ý: Nếu game bạn có UnitManager quản lý List unit thì dùng List đó sẽ tối ưu hơn FindObjectsByType
        UnitController[] allUnits = FindObjectsByType<UnitController>(FindObjectsSortMode.None);

        UnitController lowestAlly = null;
        float minHealthPercent = 1.0f; // 100%

        foreach (var unit in allUnits)
        {
            // Điều kiện lọc:
            // 1. Phải cùng phe (isPlayerUnit giống mình)
            // 2. Không được là chính mình (tùy game, thường healer ko tự buff) -> bỏ điều kiện này nếu muốn tự buff
            // 3. Phải còn sống
            // 4. Máu chưa đầy (đầy rồi buff làm gì)
            if (unit.isPlayerUnit == this.isPlayerUnit && !unit.isDestroyed && unit.currentHealth < unit.unitSO.maxHP)
            {
                float hpPercent = (float)unit.currentHealth / unit.unitSO.maxHP;
                if (hpPercent < minHealthPercent)
                {
                    minHealthPercent = hpPercent;
                    lowestAlly = unit;
                }
            }
        }

        // Nếu không tìm thấy ai thấp máu (hoặc ai cũng đầy máu), return null hoặc return chính mình
        return lowestAlly != null ? lowestAlly : this;
    }
    // Trong UnitController.cs
    public void AddShield(int amount, float duration)
    {
        // Logic tạo khiên ảo. 
        // Bạn có thể tạo biến currentShield và trừ vào shield trước khi trừ máu.
        Debug.Log($"{gameObject.name} nhận lớp khiên {amount} trong {duration}s.");
        StartCoroutine(RemoveShieldAfterTime(amount, duration));
    }

    private IEnumerator RemoveShieldAfterTime(int amount, float duration)
    {
        yield return new WaitForSeconds(duration);
        // Logic hủy khiên
        Debug.Log($"Khiên của {gameObject.name} đã hết hiệu lực.");
    }

    public void ApplyStatModifier(string statType, int value, float duration)
    {
        StartCoroutine(StatModifierRoutine(statType, value, duration));
    }

    private IEnumerator StatModifierRoutine(string statType, int value, float duration)
    {
        // 1. Áp dụng chỉ số
        if (statType == "Armor") currentArmor += value;
        if (statType == "Damage") currentDamage += value;

        Debug.Log($"{gameObject.name} tăng {value} {statType} trong {duration}s.");

        // 2. Chờ hết thời gian
        yield return new WaitForSeconds(duration);

        // 3. Trả lại chỉ số cũ
        if (statType == "Armor") currentArmor -= value;
        if (statType == "Damage") currentDamage -= value;

        Debug.Log($"Buff {statType} của {gameObject.name} kết thúc.");
    }
}
