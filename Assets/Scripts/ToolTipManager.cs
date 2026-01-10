using TMPro;
using UnityEngine;

public class ToolTipManager : MonoBehaviour
{
    public static ToolTipManager instance { get; private set; } // Biến tĩnh để lưu trữ instance của ToolTipManager

    public GameObject toolTipPrefab; // Prefab của tooltip để hiển thị thông tin tướng
    private GameObject toolTipInstance; // Instance của tooltip được tạo từ prefab
    private TextMeshProUGUI nameChampText;
    private TextMeshProUGUI statusChampText;
    private TextMeshProUGUI skillChampText;
    private CanvasGroup canvasGroup; // CanvasGroup để điều khiển hiển thị của tooltip
    private RectTransform toolTipRectTransform; // RectTransform của tooltip để điều chỉnh vị trí
    private void Awake()
    {
        if(instance == null)
        {
        instance = this;
        }
        else
        {
            Destroy(gameObject); // Đảm bảo chỉ có một instance của ToolTipManager
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        toolTipInstance = Instantiate(toolTipPrefab, transform);
        toolTipRectTransform = toolTipInstance.GetComponent<RectTransform>();
        canvasGroup = toolTipInstance.GetComponent<CanvasGroup>();

        // Lấy các thành phần TextMeshProUGUI từ tooltip instance
        nameChampText = toolTipInstance.transform.Find("NameChampText").GetComponent<TextMeshProUGUI>();
        statusChampText = toolTipInstance.transform.Find("StatusChampText").GetComponent<TextMeshProUGUI>();
        skillChampText = toolTipInstance.transform.Find("SkillChampText").GetComponent<TextMeshProUGUI>();
        //HideToolTip(); // Ẩn tooltip ngay từ đầu

    }

    // Update is called once per frame
    void Update()
    {
        if(toolTipInstance.activeSelf)
        {
            // Cập nhật vị trí của tooltip theo vị trí chuột
            Vector2 mousePosition = Input.mousePosition;
            toolTipRectTransform.position = mousePosition + new Vector2(15, -15); // Thêm một chút offset để tooltip không bị che khuất bởi con trỏ chuột
        }
    }

    public void ShowToolTip(UnitSO unit, SkillSO unitSkill)
    {
        toolTipInstance.SetActive(true); // Hiển thị tooltip
        canvasGroup.alpha = 1; // Đặt alpha của CanvasGroup về 1 để hiển thị tooltip

        // Cập nhật nội dung của tooltip
        // Điền dữ liệu từ UnitSO vào các Text
        nameChampText.text = unit.name;
        statusChampText.text = $"HP: {unit.hp}\nDamage: {unit.damage}\nArmor: {unit.armor}";
        skillChampText.text = $"<color=yellow>{unitSkill.skillName}</color>\n{unitSkill.description}";
        Debug.Log("Showing tooltip for: " + unit.name);
    }
    public void HideToolTip()
    {
        toolTipInstance.SetActive(false); // Ẩn tooltip
        canvasGroup.alpha = 0; // Đặt alpha của CanvasGroup về 0 để ẩn tooltip
    }
}
