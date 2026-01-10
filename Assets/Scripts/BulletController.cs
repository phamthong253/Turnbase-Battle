using UnityEngine;
using System.Collections.Generic;

public class BulletController : MonoBehaviour
{
    [Header("Basic Settings")]
    public float speed = 15f; // Nếu là Particle/Laser đứng yên thì set = 0
    public float timeLife = 3f;

    // --- DỮ LIỆU NHẬN TỪ UNIT ---
    private UnitController targetUnit;
    private int damageToDeal;
    private string myBulletTag;
    private string hitVfxTag;
    private bool isCriticalHit;
    private bool isFromPlayer; // Để tránh quân ta bắn quân mình

    // --- DỮ LIỆU AOE ---
    private bool isAOE = false;
    private float aoeRadius = 0f;
    private float aoeDamagePercent = 0f;

    //Healing Mode
    private bool isHealing = false;

    // --- CỜ TRẠNG THÁI ---
    private bool isInitialized = false;
    private bool isReturning = false;
    // Biến tạm để lưu các sự kiện va chạm (Tránh new List liên tục gây lag)
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    // --- SỬA HÀM NÀY: Nhận thêm ParticleSystem ---

    // --- HÀM KHỞI TẠO (GỌI TỪ UNIT) ---
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

    private void OnEnable()
    {
        isInitialized = false;
        isReturning = false;
        Invoke(nameof(Deactive), timeLife);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void Update()
    {
        // 1. Nếu là Particle System (Speed = 0) hoặc chưa Init -> Không di chuyển
        if (!isInitialized || isReturning || speed <= 0) return;

        // 2. Logic dẫn đường cho đạn thường (Target chết thì hủy)
        if (targetUnit == null || targetUnit.isDestroyed || targetUnit.gameObject == null)
        {
            Deactive();
            return;
        }

        // 3. Di chuyển tới mục tiêu (Khóa Z để tránh lỗi 2D)
        Vector3 targetPos = targetUnit.transform.position;
        targetPos.z = transform.position.z; // Giữ nguyên độ sâu Z của đạn

        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
    }

    // ==========================================================
    // TRƯỜNG HỢP 1: ĐẠN THƯỜNG (Dùng Collider 2D + Rigidbody 2D)
    // ==========================================================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isInitialized || isReturning || !gameObject.activeSelf) return;

        UnitController hitUnit = collision.GetComponentInParent<UnitController>();
        HandleHitLogic(hitUnit, transform.position, collision);
    }

    // ==========================================================
    // TRƯỜNG HỢP 2: ĐẠN PARTICLE (Laser mưa, Thiên thạch...)
    // Yêu cầu: Particle System bật module Collision (Mode 2D, Send Messages)
    // ==========================================================
    // ĐỔI TÊN HÀM VÀ CHUYỂN THÀNH PUBLIC ĐỂ CON GỌI
    public void OnParticleCollisionFromChild(GameObject other, ParticleSystem part)
    {
        if (!isInitialized || isReturning || !gameObject.activeSelf) return;

        int numCollisionEvents = part.GetCollisionEvents(other, collisionEvents);
        UnitController hitUnit = other.GetComponentInParent<UnitController>();
        // Duyệt qua từng điểm va chạm(thường chỉ có 1, nhưng cứ for cho chắc)
            for (int i = 0; i < numCollisionEvents; i++)
            {
                // Đây chính là tọa độ chính xác nơi hạt chạm vào quái
                Vector3 hitPosition = collisionEvents[i].intersection;

                if (hitUnit != null)
                {
                    // Gọi xử lý logic với vị trí chính xác
                    HandleHitLogic(hitUnit, hitPosition, isParticle: true);
                }
            }
        // Gọi xử lý logic, báo hiệu đây là Particle
        if (hitUnit != null)
        {
            HandleHitLogic(hitUnit,transform.position, true); // true = là Particle
        }

    }
    // --- HÀM XỬ LÝ CHUNG KHI TRÚNG MỤC TIÊU ---
    private void HandleHitLogic(UnitController hitUnit,Vector3 hitPos, bool isParticle = false)
    {
        if (hitUnit != null)
        {
            if(isHealing)
            {
                if(hitUnit.isPlayerUnit = this.isFromPlayer)
                {
                    hitUnit.Heal(damageToDeal, isCriticalHit);
                    SpawnHitVFX(hitPos);
                    if (!isParticle) Deactive();
                }
                return;
            }
            // Kiểm tra phe phái: Quân ta không bắn quân mình
            if (hitUnit.isPlayerUnit == this.isFromPlayer) return;

            // 1. Gây Damage Chính
            // Nếu là Particle, có thể bạn muốn giảm damage đi vì nó hit nhiều lần (ví dụ chia 5)
            // int finalDmg = isParticle ? Mathf.Max(1, damageToDeal / 5) : damageToDeal;
            hitUnit.TakeDamage(damageToDeal, isCriticalHit);

            // 2. Gây Damage AOE (Nếu có)
            if (isAOE)
            {
                ApplyAOEDamage(hitUnit, hitPos);
            }

            // 3. Hiệu ứng & Thu hồi
            if (!isParticle)
            {
                // Nếu là đạn thường: Trúng là nổ -> Hủy ngay
                SpawnHitVFX(hitPos);
                Deactive();
            }
            else
            {
                // Nếu là Particle (Laser/Mưa): 
                // KHÔNG GỌI Deactive() Ở ĐÂY!
                // Vì laser cần tồn tại hết 3 giây để bắn tiếp các hạt khác.
                // Nó sẽ tự hủy khi hết timeLife (nhờ Invoke ở OnEnable).

                // (Tùy chọn) Spawn VFX nổ nhỏ tại chỗ va chạm
                SpawnHitVFX(hitUnit.transform.position);
            }
        }
    }

    private void ApplyAOEDamage(UnitController mainTarget, Vector3 centerPos)
    {
        // Chỉ quét Layer Unit
        //int layerMask = 1 << LayerMask.NameToLayer("Unit");
        Collider2D[] colliders = Physics2D.OverlapCircleAll(centerPos, aoeRadius);

        if (colliders.Length > 0)
        {
            int aoeDamage = Mathf.RoundToInt(damageToDeal * aoeDamagePercent);

            foreach (Collider2D col in colliders)
            {
                UnitController nearbyUnit = col.GetComponentInParent<UnitController>();

                // Trừ ông mục tiêu chính ra, và phải khác phe
                if (nearbyUnit != null && nearbyUnit != mainTarget && nearbyUnit.isPlayerUnit != isFromPlayer)
                {
                    nearbyUnit.TakeDamage(aoeDamage, false);
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

    // Vẽ Gizmos để chỉnh AOE
    private void OnDrawGizmosSelected()
    {
        if (isAOE)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, aoeRadius);
        }
    }
}