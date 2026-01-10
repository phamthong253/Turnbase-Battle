using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Gắn script này vào mỗi Button trong danh sách chọn tướng (ScrollView)
public class CharacterBtn : MonoBehaviour, IPointerEnterHandler
{
    [Tooltip("Kéo file asset UnitSO của tướng tương ứng vào đây")]
    public UnitSO characterData;

    private Button button;
    private FormationUIController uiController;

    void Start()
    {
        // Tự động tìm Controller chính trong Scene
        uiController = FindFirstObjectByType<FormationUIController>();
        if (uiController == null)
        {
            Debug.LogError("Không tìm thấy FormationUIController trong Scene!");
            return;
        }

        button = GetComponent<Button>();

        // Tự động gán sự kiện OnClick cho chính nút này
        button.onClick.AddListener(OnThisCharacterSelected);

        // Cập nhật hình ảnh cho nút từ avatar của UnitSO (tùy chọn)
        Image buttonImage = GetComponent<Image>();
        if (characterData != null && characterData.avatar != null)
        {
            buttonImage.sprite = characterData.avatar;
        }
    }

    /// <summary>
    /// Hàm được gọi khi người chơi nhấn vào nút này.
    /// </summary>
    void OnThisCharacterSelected()
    {
        if (uiController != null && characterData != null)
        {
            // Gọi hàm trong controller và truyền dữ liệu của chính mình vào
            uiController.SelectUnitFromRoster(characterData);
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (uiController != null && characterData != null)
        {
            AudioManager.Instance.PlaySFX("selectionSound"); // Phát âm thanh khi hover vào nút
        }
    }
}