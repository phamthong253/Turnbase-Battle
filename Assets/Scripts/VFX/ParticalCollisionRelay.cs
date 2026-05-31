using UnityEngine;

public class ParticleCollisionRelay : MonoBehaviour
{
    private BulletController parentController;
    private ParticleSystem myParticleSystem; // Thêm biến này

    private void Start()
    {
        parentController = GetComponentInParent<BulletController>();
        myParticleSystem = GetComponent<ParticleSystem>(); // Lấy component này
    }

    private void OnParticleCollision(GameObject other)
    {
        if (parentController != null && myParticleSystem != null)
        {
            // Truyền cả đối tượng bị va chạm VÀ hệ thống hạt sang
            parentController.OnParticleCollisionFromChild(other, myParticleSystem);
        }
    }
}