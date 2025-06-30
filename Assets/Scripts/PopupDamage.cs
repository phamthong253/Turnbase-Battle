using TMPro;
using UnityEngine;

public class PopupDamage : MonoBehaviour
{
   [SerializeField] public float speed = 1f;
   [SerializeField] public float fadeTime = 15f;
    public GameObject prefab;
    public TextMeshProUGUI textDamage;
    private void Awake()
    {
        textDamage = GetComponentInChildren<TextMeshProUGUI>();
    }
   public void CreatePopup(Vector3 position, int damage, Color color)
    {
        // Đặt vị trí của cả GameObject popup
        transform.position = position;

        // Thiết lập nội dung và màu sắc cho text
        textDamage.text = damage.ToString();
        textDamage.color = color;
    }
}

