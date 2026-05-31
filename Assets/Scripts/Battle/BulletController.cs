using UnityEngine;
using System.Collections.Generic;

public class BulletController : MonoBehaviour
{
    [Header("Basic Settings")]
    public float speed = 15f;
    public float timeLife = 3f;

    // --- DỮ LIỆU NHẬN TỪ UNIT ---
    private UnitController targetUnit;
    private int damageToDeal;
    private string myBulletTag;
    private string hitVfxTag;
    private bool isCriticalHit;
    private bool isFromPlayer;

    // --- [MỚI] DỮ LIỆU BUFF ---
    private BuffEffectSO buffPayload; // Gói buff mang theo

    // --- DỮ LIỆU AOE ---
    private bool isAOE = false;
    private float aoeRadius = 0f;
    private float aoeDamagePercent = 0f;

    //Healing Mode
    private bool isHealing = false;

    // --- CỜ TRẠNG THÁI ---
    private bool isInitialized = false;
    private bool isReturning = false;

    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    // --- HÀM KHỞI TẠO ---
    public void InitializedBullet(UnitController target, int damage, string bulletTag, string vfxTag,
                                  bool isCritical, bool isPlayerSide,
                                  bool enableAOE = false, float radius = 0f, float dmgPercent = 0f, bool isHealingMode = false)
    {
        this.targetUnit = target;
        this.damageToDeal = damage;
        this.myBulletTag = bulletTag;
        this.hitVfxTag = vfxTag;
        this.isCriticalHit = isCritical;
        this.isFromPlayer = isPlayerSide;

        // Setup AOE
        this.isAOE = enableAOE;
        this.aoeRadius = radius;
        this.aoeDamagePercent = dmgPercent;

        //Setup Healing Mode
        this.isHealing = isHealingMode;

        this.isInitialized = true;
        this.isReturning = false;
    }

    // --- [MỚI] HÀM NHẬN BUFF TỪ UNIT ---
    // Hàm này được UnitController gọi ngay sau khi Spawn đạn
    public void SetBuffPayload(BuffEffectSO buff)
    {
        this.buffPayload = buff;
    }

    private void OnEnable()
    {
        isInitialized = false;
        isReturning = false;
        // [MỚI] Reset buff để tránh đạn sau dùng lại buff của đạn trước
        buffPayload = null;
        Invoke(nameof(Deactive), timeLife);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void Update()
    {
        if (!isInitialized || isReturning || speed <= 0) return;

        if (targetUnit == null || targetUnit.isDestroyed || targetUnit.gameObject == null)
        {
            Deactive();
            return;
        }

        Vector3 targetPos = targetUnit.transform.position;
        targetPos.z = transform.position.z;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isInitialized || isReturning || !gameObject.activeSelf) return;

        UnitController hitUnit = collision.GetComponentInParent<UnitController>();
        HandleHitLogic(hitUnit, transform.position, collision);
    }

    public void OnParticleCollisionFromChild(GameObject other, ParticleSystem part)
    {
        if (!isInitialized || isReturning || !gameObject.activeSelf) return;

        int numCollisionEvents = part.GetCollisionEvents(other, collisionEvents);
        UnitController hitUnit = other.GetComponentInParent<UnitController>();

        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 hitPosition = collisionEvents[i].intersection;

            if (hitUnit != null)
            {
                HandleHitLogic(hitUnit, hitPosition, isParticle: true);
            }
        }

        if (hitUnit != null)
        {
            HandleHitLogic(hitUnit, transform.position, true);
        }
    }

    // --- HÀM XỬ LÝ CHUNG KHI TRÚNG MỤC TIÊU ---
    private void HandleHitLogic(UnitController hitUnit, Vector3 hitPos, bool isParticle = false)
    {
        if (hitUnit != null)
        {
            // --- [SỬA LỖI] Logic Hồi Máu ---
            if (isHealing)
            {
                // Lưu ý: Code cũ của bạn là "=", phải là "==" để so sánh
                if (hitUnit.isPlayerUnit == this.isFromPlayer)
                {
                    hitUnit.Heal(damageToDeal, isCriticalHit);

                    // --- [MỚI] GIAO BUFF (Nếu có) ---
                    // Chỉ giao buff cho đồng đội
                    DeliverBuff(hitUnit);
                    // --------------------------------

                    SpawnHitVFX(hitPos);
                    if (!isParticle) Deactive();
                }
                return;
            }

            // --- Logic Gây Dame ---
            if (hitUnit.isPlayerUnit == this.isFromPlayer) return; // Quân ta ko bắn quân mình

            hitUnit.TakeDamage(damageToDeal, isCriticalHit);

            // --- [MỚI] GIAO DEBUFF (Nếu muốn mở rộng sau này) ---
            // Nếu sau này bạn muốn bắn địch gây choáng/độc, dùng dòng này:
            // if (hitUnit.isPlayerUnit != this.isFromPlayer) DeliverBuff(hitUnit);
            // Hiện tại ta chỉ buff hỗ trợ nên tạm thời không cần.

            if (isAOE)
            {
                ApplyAOEDamage(hitUnit, hitPos);
            }

            if (!isParticle)
            {
                SpawnHitVFX(hitPos);
                Deactive();
            }
            else
            {
                SpawnHitVFX(hitUnit.transform.position);
            }
        }
    }

    // --- [MỚI] Hàm phụ trợ giao Buff ---
    private void DeliverBuff(UnitController target)
    {
        if (this.buffPayload != null && target != null && !target.isDestroyed)
        {
            // Gọi hàm nhận BuffEffectSO mới bên UnitController
            target.ApplyBuffEffect(this.buffPayload);
        }
    }

    private void ApplyAOEDamage(UnitController mainTarget, Vector3 centerPos)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(centerPos, aoeRadius);

        if (colliders.Length > 0)
        {
            int aoeDamage = Mathf.RoundToInt(damageToDeal * aoeDamagePercent);

            foreach (Collider2D col in colliders)
            {
                UnitController nearbyUnit = col.GetComponentInParent<UnitController>();

                if (nearbyUnit != null && nearbyUnit != mainTarget && nearbyUnit.isPlayerUnit != isFromPlayer)
                {
                    nearbyUnit.TakeDamage(aoeDamage, false);

                    // [TÙY CHỌN] Nếu bạn muốn AOE cũng gây Debuff/Buff thì gọi DeliverBuff(nearbyUnit) ở đây
                }
            }
        }
    }

    private void SpawnHitVFX(Vector3 pos)
    {
        if (!string.IsNullOrEmpty(hitVfxTag) && ObjectPooling.Instance != null)
        {
            ObjectPooling.Instance.SpawnFromBool(hitVfxTag, pos, Quaternion.identity);
        }
    }

    private void Deactive()
    {
        if (isReturning) return;
        isReturning = true;
        CancelInvoke();

        if (!string.IsNullOrEmpty(myBulletTag) && ObjectPooling.Instance != null)
        {
            ObjectPooling.Instance.ReturnToPool(myBulletTag, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (isAOE)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, aoeRadius);
        }
    }
}