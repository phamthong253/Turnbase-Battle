using System.Data;
using System.Linq;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script đa năng và tự quản. Sau khi được liên kết với một Unit,
/// nó sẽ tự động cập nhật trạng thái và vị trí của chính mình.
/// </summary>
public class BattleHUD : MonoBehaviour
{
    // Biến static để các hệ thống khác có thể truy cập
    public static GameObject _popupDamagePrefab;

    [Header("Component References")]
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
    [Header("Passive & Buff UI")]
    public Transform statusIconContainer; // Kéo một Empty Object có Horizontal Layout Group vào đây
    public GameObject iconPrefab;

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
        if (linkedUnit != null)
        {
            // [REF] ĐĂNG KÝ SỰ KIỆN: UI sẽ tự động update Thanh Máu/Mana/Icon mỗi khi Data thay đổi.
            linkedUnit.UnitData.OnStatsChanged += UpdateStatsUI;
            linkedUnit.UnitData.OnStatsChanged += RefreshStatusIcons;

            // Chạy lần đầu tiên để hiển thị UI
            UpdateStatsUI();
            RefreshStatusIcons();
        }
    }

    /// <summary>
    /// Thiết lập các giá trị không đổi như tên, level, max HP/MP.
    /// </summary>
    void Initialize()
    {
        if (linkedUnit == null || linkedUnit.UnitData == null) return;

        //unitNameText.text = linkedUnit.unitSO.name;
        unitLevelText.text = "Lv " + linkedUnit.UnitData.DynamicData.Level;
        hpSlider.maxValue = linkedUnit.UnitData.MaxHP;

        if (isEnemyHUD)
        {
            // Ẩn các thành phần không cần thiết cho Enemy
            if (mpSlider != null) mpSlider.gameObject.SetActive(false);
            if (avatarImage != null) avatarImage.gameObject.SetActive(false);
            if (fireImageEffect != null) fireImageEffect.SetActive(false);
        }
        else // Nếu là HUD của Player
        {
            if (avatarImage != null) avatarImage.sprite = linkedUnit.UnitData.BaseData.avatar;
            if (mpSlider != null)
            {
                mpSlider.gameObject.SetActive(true);
                mpSlider.maxValue = linkedUnit.UnitData.BaseData.maxMP;
                fireImageEffect.SetActive(false); // Tắt hiệu ứng lửa ban đầu
            }
        }
    }

    // UPDATE SẼ TỰ ĐỘNG CHẠY MỖI FRAME
    void Update()
    {

        if (linkedUnit == null || linkedUnit.isDestroyed || linkedUnit.UnitData.IsDead)
        {
            // Hủy event trước khi tự hủy để tránh rác bộ nhớ (Memory Leak)
            if (linkedUnit != null && linkedUnit.UnitData != null)
            {
                linkedUnit.UnitData.OnStatsChanged -= UpdateStatsUI;
                linkedUnit.UnitData.OnStatsChanged -= RefreshStatusIcons;
            }

            Destroy(this.gameObject);
            return;
        }
        if (targetToFollow != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(targetToFollow.position + new Vector3(0, 2.5f, 0));
            transform.position = screenPos;
        }
    }
    /// <summary>
    /// [REF] Hàm này CHỈ CHẠY khi UnitData bắn tín hiệu OnStatsChanged (nhận sát thương, hồi máu).
    /// </summary>
    private void UpdateStatsUI()
    {
        if (linkedUnit == null || linkedUnit.UnitData == null) return;

        // Cập nhật giá trị các thanh slider từ RuntimeUnit
        hpSlider.value = linkedUnit.UnitData.CurrentHealth;

        if (!isEnemyHUD && mpSlider != null)
        {
            mpSlider.value = linkedUnit.UnitData.CurrentMana;
            CheckFireEffect();
        }
    }
    public void SetTargetToFollow(Transform unitTransform)
    {
        this.targetToFollow = unitTransform;
    }

    private void CheckFireEffect()
    {
        if (fireImageEffect == null) return;
        bool showFire = linkedUnit.UnitData.CurrentMana >= linkedUnit.UnitData.BaseData.maxMP;
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
    public void RefreshStatusIcons()
    {
        if (statusIconContainer == null || linkedUnit == null || linkedUnit.UnitData == null) return;

        // 1. Xóa sạch icon cũ (Dùng logic xóa ngược an toàn)
        for (int i = statusIconContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(statusIconContainer.GetChild(i).gameObject);
        }

        // 2. LOGIC GOM NHÓM (STACKING)
        // Thay vì foreach trực tiếp, ta gom nhóm các Buff giống nhau lại
        var passiveGroup = linkedUnit.UnitData.activePassives
            .GroupBy(buff => buff) // Gom theo object Buff (PassiveSkillSO)
            .Select(group => new {
                BuffData = group.Key,
                Count = group.Count()
            });

        // 3. Vẽ Icon dựa trên các nhóm đã gom
        foreach (var group in passiveGroup)
        {
            if (group.BuffData != null && group.BuffData.passiveIcon != null)
            {
                // Truyền thêm số lượng (Count) vào hàm tạo icon
                CreateIcon(group.BuffData.passiveIcon, group.Count);
            }
        }
        // 3. --- MỚI: VẼ BUFF EFFECT (List mới) ---
        if (linkedUnit.UnitData.activeBuffs != null)
        {
            var buffGroups = linkedUnit.UnitData.activeBuffs
                .GroupBy(b => b)
                .Select(g => new { Data = g.Key, Count = g.Count() });

            foreach (var g in buffGroups)
            {
                // BuffEffectSO dùng biến 'icon' thay vì 'passiveIcon'
                if (g.Data != null && g.Data.icon != null)
                    CreateIcon(g.Data.icon, g.Count);
            }
        }
    }

    // Cập nhật hàm này nhận thêm biến 'count'
    private void CreateIcon(Sprite iconSprite, int count)
    {
        if (iconPrefab == null) return;

        GameObject newIcon = Instantiate(iconPrefab, statusIconContainer);

        // 1. Set Ảnh
        Image img = newIcon.GetComponentInChildren<Image>();
        if (img != null) img.sprite = iconSprite;

        // 2. Set Số lượng Stack (Logic mới)
        // Tìm TextMeshProUGUI trong icon (bạn vừa tạo ở Bước 1)
        TextMeshProUGUI stackText = newIcon.GetComponentInChildren<TextMeshProUGUI>();

        if (stackText != null)
        {
            if (count > 1)
            {
                stackText.gameObject.SetActive(true);
                stackText.text = "x" + count; // Hiển thị x2, x3
            }
            else
            {
                stackText.gameObject.SetActive(false); // Nếu là 1 thì ẩn đi cho gọn
            }
        }

        // Hiệu ứng scale
        newIcon.transform.localScale = Vector3.zero;
        newIcon.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }
}
