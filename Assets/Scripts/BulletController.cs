using UnityEngine;

public class BulletController : MonoBehaviour
{
    public float timeLife  = 3f; // Thời gian sống của viên đạn
    public float speed = 10f; // Tốc độ di chuyển của viên đạn
    public int damageToDeal;
    public string myTag;
    UnitController targetUnit;
    UnitBase unitBase;

    public void InitializedBullet(UnitController target, int damage, string tag)
    {
       this.targetUnit = target;
       this.damageToDeal = damage;
       this.myTag = tag;
    }
    private void OnEnable()
    {
        // Bắt đầu đếm ngược thời gian sống của viên đạn
        Invoke(nameof(Deactive), timeLife);
    }
    private void Update()
    {
        if(targetUnit == null || targetUnit.transform == null)
        {
            Deactive();
            return;
        }
        float step = speed * Time.deltaTime; // Tính toán khoảng cách di chuyển trong một frame
        // Di chuyển viên đạn về phía mục tiêu
        transform.position = Vector2.MoveTowards(transform.position, targetUnit.transform.position, step);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra nếu va chạm với UnitController
        UnitController unit = collision.GetComponent<UnitController>();
        if (unit != null)
        {
            // Gọi phương thức xử lý va chạm với UnitController
            unit.unitBase.PlayAnimation("hit");
            // Sau khi va chạm, viên đạn sẽ bị vô hiệu hóa
        }
            Deactive();
    }

    private void Deactive()
    {
        CancelInvoke();
        ObjectPooling.Instance.ReturnToPool(myTag, gameObject);
    }
    
}
