using TMPro;
using UnityEngine;

public class PopupDamage : MonoBehaviour
{
   [SerializeField] public float speed = 1f;
   [SerializeField] public float fadeTime = 15f;
    private PopupDamage current;
    public GameObject prefab;
    private void Awake()
    {
        current = this;
    }
   public void CreatePopup(Vector3 position, int damage, Color color)
    {
        GameObject popup = Instantiate(prefab, position, Quaternion.identity);
        popup.GetComponentInChildren<TextMeshProUGUI>().text = damage.ToString();
        Destroy(popup, fadeTime);
    }
}

