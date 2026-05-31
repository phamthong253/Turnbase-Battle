//using UnityEngine;
//using UnityEngine.SceneManagement;
//using UnityEngine.UI;

//public class FormationUIController : MonoBehaviour
//{
//    // Kéo 5 hoặc 6 cái Button UI_Slot của bạn vào đây trong Inspector
//    public Button[] formationSlotButtons;
//    public Button startBtn;

//    // Biến để lưu trữ tướng đang được chọn từ danh sách bên trái
//    private UnitSO currentlySelectedUnit;
//    private SkillSO currentlySelectedSkill; // Biến để lưu trữ kỹ năng đang được chọn (nếu cần)
//    public GameObject warningPopup; // Popup cảnh báo nếu đội hình chưa đầy đủ

//    void Start()
//    {
//        if (FormationManager.Instance != null)
//        {
//            FormationManager.Instance.ClearUnitFromFormation();
//        }
//        // Gán sự kiện OnClick cho từng nút một cách tự động
//        for (int i = 0; i < formationSlotButtons.Length; i++)
//        {
//            // Cần tạo một biến tạm thời để tránh lỗi closure trong lambda
//            int slotIndex = i;
//            formationSlotButtons[i].onClick.AddListener(() => OnSlotClicked(slotIndex));
//        }
//        if (startBtn != null)
//        {
//            startBtn.onClick.AddListener(OnStartButtonClicked);
//        }
//        StartBtnUpdateState(); // Cập nhật trạng thái nút Start ngay khi bắt đầu
//    }

//    // Hàm được gọi khi người chơi nhấn vào một tướng trong danh sách
//    public void SelectUnitFromRoster(UnitSO unit)
//    {
//        this.currentlySelectedUnit = unit;
//        Debug.Log("Đã chọn tướng: " + unit.name);
//    }

//    // Hàm được gọi khi người chơi nhấn vào một ô trong đội hình
//    private void OnSlotClicked(int slotIndex)
//    {
//        ToopTipTrigger toolTipTrigger = formationSlotButtons[slotIndex].GetComponent<ToopTipTrigger>();
//        if (currentlySelectedUnit != null)
//        {
//            // Gán tướng đang chọn vào vị trí tương ứng trong FormationManager
//            FormationManager.Instance.SetUnitInFormation(currentlySelectedUnit, slotIndex);

//            // Cập nhật hình ảnh của Button để hiển thị avatar của tướng
//            Image slotImage = formationSlotButtons[slotIndex].GetComponent<Image>();
//            if (slotImage != null && currentlySelectedUnit.avatar != null) // Giả sử UnitSO có Sprite avatar
//            {
//                slotImage.sprite = currentlySelectedUnit.avatar;
//            }
//            if (toolTipTrigger != null)
//            {
//                // Cập nhật dữ liệu cho ToopTipTrigger
//                toolTipTrigger.SetUnitData(currentlySelectedUnit, currentlySelectedSkill); // Giả sử không có SkillSO ở đây
//            }
//            // Xóa lựa chọn hiện tại để người chơi phải chọn lại tướng khác
//            currentlySelectedUnit = null;
//        }
//        else
//        {
//            // Logic để xóa tướng khỏi vị trí nếu người chơi nhấn vào ô đã có tướng
//            //FormationManager.Instance.ClearUnitFromFormation(slotIndex);
//            // Cập nhật lại hình ảnh ô trống...
//            toolTipTrigger.SetUnitData(null, null); // Xóa dữ liệu tướng trong tooltip
//        }
//        StartBtnUpdateState(); // Cập nhật trạng thái nút Start mỗi khi có thay đổi đội hình
//    }
//    private void OnStartButtonClicked()
//    {
//        // Logic để bắt đầu trận đấu, có thể chuyển sang scene khác hoặc gọi hàm khởi tạo trận đấu
//        AudioManager.Instance.PlaySFX("startBtnAudiostartBtn"); // Phát âm thanh khi nhấn nút Start
//        if (IsFormationInvalid())
//        {
//            SceneManager.LoadScene("Wave1"); // Thay "BattleScene" bằng tên scene bạn muốn chuyển đến
//        }
//        else
//        {
//            WarningPopup(true); // Hiển thị cảnh báo nếu đội hình chưa đầy đủ
//        }
//        // Ví dụ: SceneManager.LoadScene("BattleScene");
//    }
//    private bool IsFormationInvalid()
//    {
//        // Kiểm tra xem có đủ tướng trong đội hình không
//        foreach (var unit in FormationManager.Instance.selectedFormation)
//        {
//            if (unit != null) return true; // Nếu có vị trí trống, đội hình chưa đầy đủ
//        }
//        return false; // Tất cả vị trí đều đã có tướng
//    }
//    private void StartBtnUpdateState()
//    {
//        // Nút Start chỉ có thể tương tác khi đội hình hợp lệ
//        if (startBtn != null)
//        {
//            startBtn.interactable = IsFormationInvalid();
//        }
//    }
//    public void WarningPopup(bool show)
//    {
//        Debug.LogWarning("Đội hình chưa đầy đủ, không thể bắt đầu trận đấu!");
//        // Hiển thị một thông báo hoặc popup cảnh báo cho người chơi
//        // Ví dụ: có thể sử dụng một UI Text hoặc một Popup prefab để hiển thị thông báo này
//        warningPopup.SetActive(show);
//    }
//}