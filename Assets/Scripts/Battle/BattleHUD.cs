using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;

/// <summary>
/// Script đa năng và tự quản. Sau khi được liên kết với một Unit,
/// nó sẽ tự động cập nhật trạng thái và vị trí của chính mình.
/// </summary>
public class BattleHUD : MonoBehaviour
{
    // Biến static để các hệ thống khác có thể truy cập
    public static GameObject _popupDamagePrefab;

    [Header("Component References")]
    public TextMeshProUGUI unitNameText;
    public TextMeshProUGUI unitLevelText;
    public Slider hpSlider;
    public Slider mpSlider;
    public Slider durationBarFill;
    public Image avatarImage;
    public GameObject fireImageEffect;

    [Header("Configuration")]
    [Tooltip("Tick vào nếu đây là HUD dành cho kẻ địch")]
    public bool isEnemyHUD = false;

    private Transform targetToFollow;
    private UnitController linkedUnit;

    /// <summary>
    /// Hàm liên kết HUD này với một Unit cụ thể.
    /// Sẽ được gọi MỘT LẦN DUY NHẤT bởi BattleHandler.
    /// </summary>
    /// 
    //private void Awake()
    //{
    //    durationBarFill.gameObject.SetActive(false); // Ẩn thanh thời gian nếu không cần thiết
    //}
    public void HideDurationBar()
    {
        if (durationBarFill != null)
        {
            durationBarFill.gameObject.SetActive(false);
        }
    }
    public void UpdateDurationBar(float duration, float maxTimeline)
    {
        if (durationBarFill == null) return;
        if (!durationBarFill.gameObject.activeSelf)
        {
            durationBarFill.gameObject.SetActive(true); // Hiện thanh thời gian nếu nó đang ẩn
        }
        // Cập nhật giá trị hiện tại
        durationBarFill.value = duration / maxTimeline;
    }
    public void LinkToUnit(UnitController unit, GameObject popupPrefab)
    {
        this.linkedUnit = unit;

        // Lưu lại prefab popup một lần cho cả hệ thống
        if (_popupDamagePrefab == null && popupPrefab != null)
        {
            _popupDamagePrefab = popupPrefab;
        }

        Initialize(); // Gọi hàm thiết lập ban đầu
    }

    /// <summary>
    /// Thiết lập các giá trị không đổi như tên, level, max HP/MP.
    /// </summary>
    void Initialize()
    {
        if (linkedUnit == null || linkedUnit.unitSO == null) return;

        unitNameText.text = linkedUnit.unitSO.name;
        unitLevelText.text = "Lv " + linkedUnit.unitSO.level;
        hpSlider.maxValue = linkedUnit.unitSO.hp;

        if (isEnemyHUD)
        {
            if (unitNameText != null) unitNameText.text = linkedUnit.unitSO.name;
            // Ẩn các thành phần không cần thiết cho Enemy
            if (mpSlider != null) mpSlider.gameObject.SetActive(false);
            if (avatarImage != null) avatarImage.gameObject.SetActive(false);
            if (fireImageEffect != null) fireImageEffect.SetActive(false);
        }
        else // Nếu là HUD của Player
        {
            if (avatarImage != null) avatarImage.sprite = linkedUnit.unitSO.avatar;
            if (mpSlider != null)
            {
                mpSlider.gameObject.SetActive(true);
                mpSlider.maxValue = linkedUnit.unitSO.maxMP;
                fireImageEffect.SetActive(false); // Tắt hiệu ứng lửa ban đầu
            }
        }
    }

    // UPDATE SẼ TỰ ĐỘNG CHẠY MỖI FRAME
    void Update()
    {
        
        // Nếu không được liên kết với unit nào, hoặc unit đã chết, thì không làm gì cả
        if (linkedUnit == null || linkedUnit.isDestroyed)
        {
            // Tự hủy HUD nếu unit liên kết đã chết
            Destroy(this.gameObject);
            return;
        }

        // Tự cập nhật giá trị các thanh slider
        hpSlider.value = linkedUnit.currentHealth;
        if (!isEnemyHUD && mpSlider != null)
        {
            mpSlider.value = linkedUnit.currentMana;
            CheckFireEffect(); // Tự kiểm tra hiệu ứng
        }
        if (targetToFollow != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(targetToFollow.position + new Vector3(0, 2.5f, 0));
            transform.position = screenPos;
        }
    }

    public void SetTargetToFollow(Transform unitTransform)
    {
        this.targetToFollow = unitTransform;
    }

    private void CheckFireEffect()
    {
        if (fireImageEffect == null) return;
        bool showFire = linkedUnit.currentMana >= linkedUnit.unitSO.maxMP;
        if (fireImageEffect.activeSelf != showFire)
        {
            fireImageEffect.SetActive(showFire);
        }
    }

    // Hàm static để tạo popup sát thương vẫn giữ nguyên
    public static void ShowDamagePopup(Vector3 worldPosition, int damageAmount, bool isPlayer, bool isCritical, bool isHealing)
    {
        if (_popupDamagePrefab == null)
        {
            return;
        }

        GameObject popupInstance = Instantiate(_popupDamagePrefab, worldPosition, Quaternion.identity);
        PopupDamage popupScript = popupInstance.GetComponent<PopupDamage>();
        popupScript.SetupPopup(worldPosition, damageAmount, Color.white, isCritical, isHealing);


        //Destroy(popupInstance, popupScript.fadeTime);

    }
}