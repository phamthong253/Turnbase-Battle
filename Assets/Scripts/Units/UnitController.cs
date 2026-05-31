using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using static UnitSO; // Đảm bảo bạn có dòng này để lấy AttackType

public class UnitController : MonoBehaviour
{
    [Header("UI Skill")]
    public CharacterID characterID;

    public static event Action<UnitController> OnUnitDestroyed;

    #region MVC - Data Model (Dữ liệu)
    [Header("Data Aggregator (Lõi dữ liệu)")]
    // [REF] Toàn bộ dữ liệu UnitSO, CurrentHealth, Buff... nằm hết trong này
    public RuntimeUnit UnitData { get; private set; }
    #endregion

    #region Public References & Settings
    [Header("Core Components")]
    public UnitBase unitBase;
    public SkillSO skillSO;
    public UnitSO configSO;

    [Header("Configuration")]
    [Tooltip("Đánh dấu nếu đây là unit của người chơi")]
    public bool isPlayerUnit;
    [HideInInspector] public int formationSlotIndex;
    #endregion

    #region State & Private Variables
    public enum UnitState { IDLE, MOVING_TO_TARGET, ATTACKING, USESKILL, RETURNING_HOME, BUSY, DESTROYED }
    public UnitState unitState { get; private set; }

    public bool isDestroyed = false;

    private UnitController currentTarget;
    private float currentAttackCooldown;
    private float attackTimer;
    private int basicAttackComboCount = 0;
    private Vector3 formationOffset;
    private Transform teamAnchor;
    public Transform fireTransform;
    private UnitCastSkill unitCastSkill;

    // Item drop settings
    public ItemSO itemToDrop;
    public float dropChance;

    [Header("Knockback Effect")]
    private float knockbackStrength = 0.8f;
    private float maxKnockbackDistance = 3f;
    private Vector3 originalPos;
    private bool isKnockback = false;
    private Tween knockBackTween;

    [Header("Enhanced Attack Settings")]
    public bool isCurrentlyEnhanced = false;

    public Vector3 HomePosition
    {
        get
        {
            if (teamAnchor == null) return transform.position;
            return teamAnchor.position + formationOffset;
        }
    }

    public AudioClip attackAudioClip;
    public AudioClip hitAudioClip;
    #endregion

    #region Unity Lifecycle & Initialization
    private void Awake()
    {
        if (unitBase == null) unitBase = GetComponent<UnitBase>();
        unitCastSkill = GetComponent<UnitCastSkill>();
    }

    /// <summary>
    /// [REF] Hàm mới: Được BattleManager/Spawner gọi để bơm dữ liệu vào
    /// </summary>
    public void SetupUnit(RuntimeUnit data)
    {
        this.UnitData = data;
        unitState = UnitState.IDLE;
        attackTimer = 0;

        // Cập nhật lại cooldown đánh tay dựa trên dữ liệu mới
        currentAttackCooldown = UnitData.BaseData.attackCooldown > 0 ? UnitData.BaseData.attackCooldown : 2f;
    }

    void Update()
    {
        // [REF] Kiểm tra điều kiện sống chết qua UnitData
        if (isDestroyed || UnitData == null || UnitData.IsDead)
        {
            if (!isDestroyed) Delete();
            return;
        }
        if (unitState == UnitState.BUSY) return;

        // 1. XÁC ĐỊNH VỊ TRÍ ĐÍCH
        Vector3 destination;
        bool shouldReturnHome = (currentTarget == null || currentTarget.isDestroyed);

        if (shouldReturnHome)
        {
            return;
        }
        else
        {
            // [REF] Lấy attackType từ UnitData.BaseData
            AttackType type = UnitData.BaseData.attackType;
            if (type == AttackType.Ranged || type == AttackType.Magic || type == AttackType.Support || type == AttackType.Enemy)
            {
                destination = HomePosition;
            }
            else // Cận chiến
            {
                float yOffset = ((float)(formationSlotIndex % 3) - 1f) * -1.5f;
                Vector3 attackOffset = new Vector3(0, yOffset, 0);
                // [REF] Lấy attackRange từ UnitData.BaseData
                destination = currentTarget.transform.position - new Vector3(UnitData.BaseData.attackRange * 0.8f, 0, 0) + attackOffset;
            }
        }

        // 2. XỬ LÝ DI CHUYỂN
        if (Vector3.Distance(transform.position, destination) > 0.2f)
        {
            unitState = (shouldReturnHome) ? UnitState.RETURNING_HOME : UnitState.MOVING_TO_TARGET;
            transform.position = Vector3.MoveTowards(transform.position, destination, 10f * Time.deltaTime);
            unitBase?.PlayAnimation("move");
        }
        else
        {
            // 3. XỬ LÝ TẤN CÔNG
            unitState = UnitState.ATTACKING;

            // [REF] Kiểm tra tầm đánh qua UnitData.BaseData
            if (!shouldReturnHome && Vector3.Distance(transform.position, currentTarget.transform.position) <= UnitData.BaseData.attackRange)
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0)
                {
                    StartCoroutine(PerformBasicAttack());
                }
                else
                {
                    unitBase?.PlayAnimation("idle");
                }
            }
            else
            {
                unitBase?.PlayAnimation("idle");
            }
        }

        // --- STATE MACHINE THI HÀNH HÀNH ĐỘNG ---
        switch (unitState)
        {
            case UnitState.IDLE: unitBase?.PlayAnimation("idle"); break;
            case UnitState.RETURNING_HOME:
                transform.position = Vector3.MoveTowards(transform.position, HomePosition, 10f * Time.deltaTime);
                unitBase?.PlayAnimation("move");
                break;
            case UnitState.MOVING_TO_TARGET:
                transform.position = Vector3.MoveTowards(transform.position, currentTarget.transform.position, 5f * Time.deltaTime);
                unitBase?.PlayAnimation("move");
                break;
            case UnitState.ATTACKING:
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0) StartCoroutine(PerformBasicAttack());
                else unitBase?.PlayAnimation("idle");
                break;
            case UnitState.USESKILL: break;
        }
    }
    #endregion

    #region Core Combat Methods
    public void SetFormationAnchor(Transform teamAnchor, Vector3 homePosition)
    {
        this.teamAnchor = teamAnchor;
        this.formationOffset = homePosition - teamAnchor.position;
    }
    public void UpdateTeamAnchor(Transform newAnchor) { this.teamAnchor = newAnchor; }
    public UnitController GetCurrentTarget() { return currentTarget; }
    public void SetTarget(UnitController target) { this.currentTarget = target; }

    public void TakeDamage(int damage, bool isCritical)
    {
        // [REF] 1. Gọi sang Model để trừ máu (Model sẽ tự lo trừ khiên, trừ máu)
        UnitData.TakeDamage(damage);

        // 2. Controller xử lý phần hiển thị: Nảy, Animation, UI Text, m thanh
        float force = isCritical ? knockbackStrength * 1.5f : knockbackStrength;
        ApplyCumulativeKnockback(force);

        unitBase?.PlayForceAnimation("hit", () => {
            if (UnitData.IsDead)
            {
                unitBase?.PlayAnimation("died");
            }
        });

        BattleHUD.ShowDamagePopup(this.transform.position, damage, isPlayerUnit, isCritical, false);
        AudioManager.Instance.PlaySFX("HitAudioCliphit");
    }

    public void Heal(int amount, bool isCrit)
    {
        if (isDestroyed || UnitData.IsDead) return;

        // [REF] 1. Gọi sang Model để cộng máu
        UnitData.Heal(amount);

        // 2. Hiển thị UI
        BattleHUD.ShowDamagePopup(this.transform.position, amount, isPlayerUnit, isCrit, true);
        Debug.Log($"{name} được hồi {amount} máu!");
    }

    private void ApplyCumulativeKnockback(float pushForce)
    {
        if (!isKnockback)
        {
            originalPos = transform.position;
            isKnockback = true;
        }
        if (knockBackTween.IsActive() && knockBackTween != null) knockBackTween.Kill();

        Vector3 knockBackDir = isPlayerUnit ? Vector3.left : Vector3.right;
        Vector3 targetPushPos = transform.position + (knockBackDir * pushForce);
        float distanceFromOriginal = Vector3.Distance(originalPos, targetPushPos);

        if (distanceFromOriginal > maxKnockbackDistance)
            targetPushPos = originalPos + (targetPushPos - originalPos).normalized * maxKnockbackDistance;

        Sequence seq = DOTween.Sequence();
        float pushDuration = Mathf.Clamp(pushForce * 0.1f, 0.3f, 0.5f);
        seq.Append(transform.DOMove(targetPushPos, pushDuration).SetEase(Ease.OutQuad));
        seq.Append(transform.DOMove(originalPos, 0.4f).SetEase(Ease.OutElastic));
        seq.OnComplete(() => { isKnockback = false; });
        knockBackTween = seq;
    }

    public void UseMana(int amount)
    {
        // [REF] Gọi sang Model
        UnitData.UseMana(amount);
    }

    public void Delete()
    {
        isDestroyed = true;
        unitState = UnitState.DESTROYED;
        unitBase?.PlayAnimation("died");
        Destroy(gameObject, 1.5f);
        OnUnitDestroyed?.Invoke(this);
        if (!isPlayerUnit && RewardManager.Instance != null)
        {
            RewardManager.Instance.ItemReward();
        }
    }
    #endregion

    #region Attack Routines & Logic
    public IEnumerator PerformBasicAttack()
    {
        unitState = UnitState.BUSY;

        // [REF] Lấy ngưỡng combo từ UnitData
        bool isEnoughStack = basicAttackComboCount >= UnitData.BaseData.comboThreshold - 1;
        // [REF] Lấy sát thương từ UnitData
        bool targetIsWorthy = currentTarget != null && currentTarget.UnitData.CurrentHealth > UnitData.FinalDamage;

        if (isEnoughStack && targetIsWorthy)
        {
            isCurrentlyEnhanced = true;
            basicAttackComboCount = 0;
        }
        else
        {
            isCurrentlyEnhanced = false;
            basicAttackComboCount++;
            if (!isEnoughStack) basicAttackComboCount++;
        }

        string attackAnimationKey = isCurrentlyEnhanced ? "enhancedAttack" : "attack";
        unitBase?.PlayAnimation(attackAnimationKey);

        float animDuration = unitBase.GetAnimationDuration(attackAnimationKey);
        yield return new WaitForSeconds(animDuration);

        // Hồi mana sau khi đánh
        UnitData.RegenMana(UnitData.BaseData.damage); // Bạn có thể tạo hàm RegenMana trong RuntimeUnit

        attackTimer = currentAttackCooldown;
        unitState = UnitState.IDLE;
    }

    // Animation Event Receiver
    public void AnimationTrigger_OnAttackAction()
    {
        if (currentTarget == null || currentTarget.isDestroyed) return;
        AudioManager.Instance.PlaySFX("AttackAudioClipattackHitattackHit");

        // [REF] Lấy kiểu đánh từ UnitData
        switch (UnitData.BaseData.attackType)
        {
            case AttackType.Melee: MeleeAttack(currentTarget, isCurrentlyEnhanced); break;
            case AttackType.Ranged: RangedAttack(currentTarget, isCurrentlyEnhanced); break;
            case AttackType.Magic: MagicAttack(currentTarget, isCurrentlyEnhanced); break;
            case AttackType.Support: SupportAttack(currentTarget, isCurrentlyEnhanced); break;
            case AttackType.Enemy: EnemyAttack(currentTarget); break;
        }
    }

    private void MeleeAttack(UnitController target, bool isEnhanced)
    {
        if (target == null || target.isDestroyed) return;

        // [REF] TÍNH TOÁN SÁT THƯƠNG VỚI RUNTIME UNIT
        int damage = DamageCalculator.CalculatorPhysicalDamage(UnitData.FinalDamage, target.UnitData.FinalArmor);
        bool isCritical = DamageCalculator.IsCriticalHit(UnitData.BaseData.critChance);
        if (isCritical) damage = Mathf.RoundToInt(damage * 2f);
        if (isEnhanced) damage = Mathf.RoundToInt(damage * UnitData.BaseData.damageEnhencedMultiplier);

        // --- LOGIC AOE ---
        bool enableAOE = isEnhanced && UnitData.BaseData.useEnhancedAOE;
        float radius = enableAOE ? UnitData.BaseData.aoeRadius : 0f;
        float dmgPercent = enableAOE ? UnitData.BaseData.aoeDamage : 0f;

        string meleeTag = UnitData.BaseData.projectilePrefab != null ? UnitData.BaseData.projectilePrefab.name : "";
        string vfxTag = UnitData.BaseData.hitVFXPrefab != null ? UnitData.BaseData.hitVFXPrefab.name : "";
        if (isEnhanced && !string.IsNullOrEmpty(UnitData.BaseData.enhancedAttackVFXName)) vfxTag = UnitData.BaseData.enhancedAttackVFXName;

        GameObject slashObject = ObjectPooling.Instance.SpawnFromBool(meleeTag, target.transform.position, Quaternion.identity);
        if (slashObject != null)
        {
            slashObject.GetComponent<BulletController>()?.InitializedBullet(target, damage, meleeTag, vfxTag, isCritical, isPlayerUnit, enableAOE, radius, dmgPercent);
        }
    }

    private void MagicAttack(UnitController target, bool isEnhanced)
    {
        if (target == null || target.isDestroyed) return;

        int damage = DamageCalculator.CalculatorPhysicalDamage(UnitData.FinalDamage, target.UnitData.FinalArmor);
        bool isCritical = DamageCalculator.IsCriticalHit(UnitData.BaseData.critChance);
        if (isEnhanced) damage = Mathf.RoundToInt(damage * UnitData.BaseData.damageEnhencedMultiplier);
        if (isCritical) damage = Mathf.RoundToInt(damage * 2f);

        bool enableAOE = isEnhanced && UnitData.BaseData.useEnhancedAOE;
        float radius = enableAOE ? UnitData.BaseData.aoeRadius : 0f;
        float dmgPercent = enableAOE ? UnitData.BaseData.aoeDamage : 0f;

        string bulletTag = UnitData.BaseData.projectilePrefab.name;
        string vfxTag = isEnhanced && !string.IsNullOrEmpty(UnitData.BaseData.enhancedAttackVFXName) ? UnitData.BaseData.enhancedAttackVFXName : UnitData.BaseData.hitVFXPrefab.name;

        GameObject projectile = ObjectPooling.Instance.SpawnFromBool(bulletTag, fireTransform.position, fireTransform.rotation);
        if (projectile != null)
        {
            BulletController bullet = projectile.GetComponent<BulletController>();
            if (bullet != null)
            {
                bullet.InitializedBullet(target, damage, bulletTag, vfxTag, isCritical, isPlayerUnit, enableAOE, radius, dmgPercent);
                bullet.transform.localScale = enableAOE ? Vector3.one * 2f : (isEnhanced ? Vector3.one * 1.5f : Vector3.one);
            }
        }
    }

    private void RangedAttack(UnitController target, bool isEnhanced)
    {
        // Tương tự Magic Attack, đã được rút gọn code tính toán
        MagicAttack(target, isEnhanced);
    }

    private void SupportAttack(UnitController target, bool isEnhanced)
    {
        UnitController finalTarget = target;
        bool isHealingMode = false;
        int finalValue = 0;

        if (isEnhanced)
        {
            finalTarget = GetLowestHealthAlly();
            isHealingMode = true;
            // [REF] Tính hồi máu dựa trên FinalDamage của Model
            finalValue = Mathf.RoundToInt(UnitData.FinalDamage * UnitData.BaseData.damageEnhencedMultiplier);
        }
        else
        {
            if (target == null || target.isDestroyed) return;
            finalTarget = target;
            finalValue = DamageCalculator.CalculatorPhysicalDamage(UnitData.FinalDamage, target.UnitData.FinalArmor);
        }

        bool isCritical = DamageCalculator.IsCriticalHit(UnitData.BaseData.critChance);
        if (isCritical) finalValue = Mathf.RoundToInt(finalValue * 2f);

        bool enableAOE = isEnhanced && UnitData.BaseData.useEnhancedAOE;
        float radius = enableAOE ? UnitData.BaseData.aoeRadius : 0f;
        float dmgPercent = enableAOE ? UnitData.BaseData.aoeDamage : 0f;

        string bulletTag = UnitData.BaseData.projectilePrefab.name;
        string vfxTag = isEnhanced && !string.IsNullOrEmpty(UnitData.BaseData.enhancedAttackVFXName) ? UnitData.BaseData.enhancedAttackVFXName : UnitData.BaseData.hitVFXPrefab.name;

        GameObject projectile = ObjectPooling.Instance.SpawnFromBool(bulletTag, fireTransform.position, fireTransform.rotation);
        if (projectile != null)
        {
            BulletController bullet = projectile.GetComponent<BulletController>();
            if (bullet != null)
            {
                bullet.InitializedBullet(finalTarget, finalValue, bulletTag, vfxTag, isCritical, isPlayerUnit, enableAOE, radius, dmgPercent, isHealingMode);
                if (isEnhanced && UnitData.BaseData.enhancedBuffEffect != null)
                {
                    bullet.SetBuffPayload(UnitData.BaseData.enhancedBuffEffect);
                }
            }
        }
    }

    private void EnemyAttack(UnitController target)
    {
        MeleeAttack(target, false); // Có thể gộp nếu logic giống nhau
    }
    #endregion

    #region Buff & Skill Logic
    public void PerformSkill(SkillSO skill, List<UnitController> targets, Action onComplete)
    {
        if (unitState == UnitState.DESTROYED || unitCastSkill == null)
        {
            onComplete?.Invoke();
            return;
        }

        bool canPerform = unitCastSkill.PrepareToUseSkill(skill, targets, onComplete);
        if (canPerform)
        {
            this.unitState = UnitState.USESKILL;
            unitBase.PlayAnimation(skill.skillAnimationString);
        }
    }

    public void OnActionFinished() { this.unitState = UnitState.IDLE; }

    /// <summary>
    /// [REF] Xử lý Buff mới: Controller chỉ lo bật VFX và đếm giờ. Tính toán do Model lo.
    /// </summary>
    public void ApplyBuffEffect(BuffEffectSO buff)
    {
        if (buff == null) return;
        StartCoroutine(BuffEffectRoutine(buff));
    }

    private IEnumerator BuffEffectRoutine(BuffEffectSO buff)
    {
        // 1. Báo cho Model nhận buff (Model sẽ tự tính lại FinalStats)
        UnitData.AddBuff(buff);

        // 2. Xử lý phần "Nhìn thấy" (VFX)
        GameObject vfx = null;
        if (!string.IsNullOrEmpty(buff.vfxTag))
        {
            vfx = ObjectPooling.Instance.SpawnFromBool(buff.vfxTag, transform.position, Quaternion.identity);
            if (vfx != null) vfx.transform.SetParent(this.transform);
        }

        // 3. Chờ thời gian buff
        yield return new WaitForSeconds(buff.duration);

        // 4. Hết thời gian, báo Model xóa buff
        UnitData.RemoveBuff(buff);

        // 5. Tắt VFX
        if (vfx != null)
        {
            vfx.transform.SetParent(null);
            ObjectPooling.Instance.ReturnToPool(buff.vfxTag, vfx);
        }
    }
    #endregion

    #region Helper Methods
    private UnitController GetLowestHealthAlly()
    {
        UnitController[] allUnits = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        UnitController lowestAlly = null;
        float minHealthPercent = 1.0f;

        foreach (var unit in allUnits)
        {
            // [REF] So sánh qua UnitData thay vì biến local
            if (unit.isPlayerUnit == this.isPlayerUnit && !unit.isDestroyed && unit.UnitData.CurrentHealth < unit.UnitData.MaxHP)
            {
                float hpPercent = (float)unit.UnitData.CurrentHealth / unit.UnitData.MaxHP;
                if (hpPercent < minHealthPercent)
                {
                    minHealthPercent = hpPercent;
                    lowestAlly = unit;
                }
            }
        }
        return lowestAlly != null ? lowestAlly : this;
    }
    #endregion
    /// <summary>
    /// Di chuyển đến mục tiêu với TỐC ĐỘ cố định. Dùng khi chuyển Wave.
    /// </summary>
    public IEnumerator MoveCoroutineAndCallback(Vector3 targetPosition, Action onComplete)
    {
        unitState = UnitState.BUSY;
        unitBase?.PlayAnimation("move");

        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            // Tốc độ 15f có thể tùy chỉnh hoặc lấy từ UnitData nếu bạn muốn tướng có tốc độ chạy khác nhau
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, 15f * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
        unitBase?.PlayAnimation("idle");

        unitState = UnitState.IDLE;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Di chuyển đến mục tiêu trong một KHOẢNG THỜI GIAN cố định. Dùng lúc tạo dáng chiến thắng.
    /// </summary>
    public IEnumerator MoveCoroutineAndCallback(Vector3 destination, float duration, Action onFinished)
    {
        unitState = UnitState.BUSY;
        unitBase?.PlayAnimation("move");

        float startTime = Time.time;
        Vector3 startPos = transform.position;

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            transform.position = Vector3.Lerp(startPos, destination, t);
            yield return null;
        }

        transform.position = destination;
        unitBase?.PlayAnimation("idle");

        unitState = UnitState.IDLE;
        onFinished?.Invoke();
    }
    public void PlayPassiveVisuals()
    {
        // [REF] Truy xuất passiveSkill thông qua UnitData.BaseData
        if (UnitData != null && UnitData.BaseData.passiveSkill != null && !string.IsNullOrEmpty(UnitData.BaseData.passiveSkill.activationVFXTag))
        {
            // 1. Lấy VFX từ Object Pooling
            GameObject vfx = ObjectPooling.Instance.SpawnFromBool(
                UnitData.BaseData.passiveSkill.activationVFXTag,
                transform.position,
                Quaternion.identity
            );

            if (vfx != null)
            {
                // 2. Gắn VFX vào dưới chân nhân vật
                vfx.transform.SetParent(this.transform);
                vfx.transform.localPosition = new Vector3(0, 0.1f, 0);
                vfx.transform.localScale = Vector3.one * 5f;

                // 3. Nếu không phải hiệu ứng vĩnh viễn, hẹn giờ tắt nó đi
                if (!UnitData.BaseData.passiveSkill.isPersistent)
                {
                    StartCoroutine(ReturnPassiveVFX(vfx, UnitData.BaseData.passiveSkill.activationVFXTag, 2.0f));
                }
            }

            Debug.Log($"<color=cyan>[Passive]</color> {UnitData.BaseData.name} đã kích hoạt hiệu ứng: {UnitData.BaseData.passiveSkill.passiveName}");
        }
    }

    private IEnumerator ReturnPassiveVFX(GameObject vfxObj, string tag, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (vfxObj != null)
        {
            vfxObj.transform.SetParent(null); // Tách khỏi nhân vật trước khi trả về pool
            ObjectPooling.Instance.ReturnToPool(tag, vfxObj);
        }
    }
}