using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupDamage : MonoBehaviour
{
    [SerializeField] public float speed = 1f;
    [SerializeField] public float fadeTime = 15f;
    // ... các trường khác

    public TextMeshProUGUI textDamage;
    [SerializeField] private Image criticalIcon;
    [SerializeField] private Sprite criticalSprite;
    [SerializeField] private CanvasGroup canvasGroup;

    // Màu sắc và kích thước khi CHÍ MẠNG (Nên thêm để dễ tùy biến)
    public Color criticalColor = Color.red;
    public float criticalScale = 0.3f;
    // Màu sắc cho Healing
    public Color healColor = Color.green;

    private void Awake()
    {
        // Gán textDamage nếu chưa gán thủ công
        if (textDamage == null)
        {
            textDamage = GetComponentInChildren<TextMeshProUGUI>();
        }

        canvasGroup = GetComponent<CanvasGroup>();

        // SỬA: Dùng SetActive để ẩn GameObject chứa Image
        if (criticalIcon != null && criticalIcon.gameObject.activeSelf)
        {
            criticalIcon.gameObject.SetActive(false);
        }
    }

    public void SetupPopup(Vector3 position, int damage, Color normalColor, bool isCriticall, bool isHealing)
    {
        transform.position = position;

        // Tùy chỉnh Text cho Chí Mạng
        textDamage.text = damage.ToString();
        textDamage.color = normalColor;
        if(isHealing)
        {
            textDamage.text = "+" + damage.ToString();
            textDamage.color = healColor;
        }
        else
        {
            // HIỂN THỊ ICON CHÍ MẠNG
            if (criticalIcon != null)
            {
                if (isCriticall)
                {
                    // Set Sprite (nếu bạn có nhiều loại sprite)
                    if (criticalSprite != null)
                    {
                        criticalIcon.sprite = criticalSprite;
                        textDamage.transform.localScale = Vector3.one * (isCriticall ? criticalScale : 0.5f);
                        textDamage.color = isCriticall ? criticalColor : normalColor;
                    }

                    // Kích hoạt GameObject chứa Image
                    criticalIcon.gameObject.SetActive(true);
                }
                else
                {
                    // Vô hiệu hóa GameObject chứa Image
                    criticalIcon.gameObject.SetActive(false);
                }
            }
        }
        
        StartCoroutine(PopupEffect());
    }

    private IEnumerator PopupEffect()
    {
        float t = 0;
        // Lấy màu gốc của icon để giữ nguyên RGB, chỉ giảm Alpha
        Color startIconColor = criticalIcon.color;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float alpha = 1 - (t / fadeTime);

            // 1. Di chuyển
            transform.position += Vector3.up * speed * Time.deltaTime;

            // 2. Làm mờ CanvasGroup (Ưu tiên cách này nếu setup đúng)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
            else
            {
                // 3. (Dự phòng) Nếu không dùng CanvasGroup, phải làm mờ thủ công từng cái:

                // Làm mờ Text
                if (textDamage != null) textDamage.alpha = alpha;

                // Làm mờ Icon (QUAN TRỌNG)
                if (criticalIcon != null)
                {
                    Color newColor = startIconColor;
                    newColor.a = alpha; // Gán alpha mới
                    criticalIcon.color = newColor;
                }
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}