using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnitSO;

public class FormationPopupManager : MonoBehaviour
{
    public static FormationPopupManager Instance;
    //public UnitDatabase unitDatabase;
    [Header("UI References")]
    [Tooltip("Kéo GameObject 'Content' (có Grid Layout Group) trong ScrollView vào đây")]
    public Transform rosterContent;

    [Tooltip("Kéo Prefab 'UnitIconItem' (có script RosterItemUI) vào đây")]
    public GameObject rosterItemPrefab;

    [Header("Role Icons")]
    public Sprite meleeRoleIcon; // Kéo ảnh mũi tên -> (Front) vào đây
    public Sprite magicRoleIcon; // Kéo ảnh mũi tên <- (Back) vào đây
    public Sprite supportRoleIcon; // Kéo ảnh mũi tên -> (Front) vào đây
    public Button battleButton; // Nút bấm "Ra trận"
    public Button closeButton; // Nút bấm "Đóng"

    // Mảng 5 ô slot bên dưới (đã làm ở bước trước)
    public PartySlotUI[] partySlots;

    // Đội hình hiện tại (5 slot)
    public UnitSO[] currentTeam = new UnitSO[5];

    private StageSO currentSelectedStage;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        this.gameObject.SetActive(false); // Ẩn popup khi bắt đầu
    }
    private void Start()
    {
        // Gán sự kiện cho nút Ra trận
        if (battleButton != null)
        {
            battleButton.onClick.RemoveAllListeners();
            battleButton.onClick.AddListener(OnStartClicked);
        }
    }

    private void OnEnable()
    {
        if (PlayerDataManager.Instance == null)
        {
            StartCoroutine(WaitForPlayerDataThenLoad());
            return;
        }

        LoadRosterData();
        RefreshBottomSlots();
    }

    private IEnumerator WaitForPlayerDataThenLoad()
    {
        yield return new WaitUntil(() => PlayerDataManager.Instance != null);
        LoadRosterData();
        RefreshBottomSlots();
    }

    public void LoadRosterData()
    {
        foreach (Transform child in rosterContent) Destroy(child.gameObject);
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning("[FormationPopup] PlayerDataManager chưa sẵn sàng, sẽ thử lại khi có instance.");
            return;
        }

        if (PlayerDataManager.Instance.UnitRosterModel == null)
        {
            Debug.LogError("[FormationPopup] LỖI: UnitRosterModel bị Null!");
            return;
        }

        List<UnitSO> ownedUnits = null;
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.UnitRosterModel != null)
        {
            ownedUnits = PlayerDataManager.Instance.UnitRosterModel.GetOwnedUnits();
        }

        if (ownedUnits == null || ownedUnits.Count == 0)
        {
            Debug.LogWarning("[FormationPopup] Kho tướng RỖNG! Không có gì để hiển thị.");
            return;
        }
        Debug.Log($"[FormationPopup] Đã tìm thấy {ownedUnits.Count} tướng trong kho. Đang hiển thị...");

        // --- LOGIC SẮP XẾP (MỚI) ---
        // Sắp xếp: Melee đứng trước, Magic đứng sau. 
        // Nếu cùng loại thì xếp theo tên hoặc Level.
        ownedUnits.Sort((a, b) =>
        {
            int roleComparison = a.attackType.CompareTo(b.attackType);
            if (roleComparison != 0) return roleComparison;

            // Nếu cùng Role, thằng nào Level cao hơn đứng trước
            return b.level.CompareTo(a.level);
        });

        // --- RENDER ---
        foreach (var unit in ownedUnits)
        {
            GameObject newIconObj = Instantiate(rosterItemPrefab, rosterContent);
            RosterItemUI itemUI = newIconObj.GetComponent<RosterItemUI>();

            if (itemUI != null)
            {
                bool isInTeam = IsUnitInTeam(unit);

                // --- CHỌN ICON DỰA TRÊN ROLE ---
                Sprite roleSpriteToDisplay = null;
                if (unit.attackType == AttackType.Melee)
                {
                    roleSpriteToDisplay = meleeRoleIcon; // Icon ->
                }
                else if (unit.attackType == AttackType.Magic)
                {
                    roleSpriteToDisplay = magicRoleIcon; // Icon <-
                }
                else if (unit.attackType == AttackType.Support)
                {
                    roleSpriteToDisplay = supportRoleIcon; // Icon ->
                }


                // Truyền thêm roleSpriteToDisplay vào hàm Setup
                itemUI.Setup(unit, isInTeam, roleSpriteToDisplay, OnRosterItemClicked);
            }
        }
    }
    public void OpenStagePopup(StageSO stageData)
    {
        currentSelectedStage = stageData;
        // Hiển thị popup đội hình (nếu chưa hiển thị)
        this.gameObject.SetActive(true);
        LoadRosterData();
        RefreshBottomSlots();
    }
    public void CloseStagePopup()
    {
        // Ẩn popup đội hình
        this.gameObject.SetActive(false);
    }

    // --- CÁC HÀM XỬ LÝ LOGIC ---

    // Hàm được gọi khi bấm vào 1 icon tướng trên danh sách
    private void OnRosterItemClicked(UnitSO unit)
    {
        if (IsUnitInTeam(unit))
        {
            // Nếu đã có -> Gỡ ra
            RemoveFromTeam(unit);
        }
        else
        {
            // Nếu chưa có -> Thêm vào
            AddToTeam(unit);
        }

        // Cập nhật lại giao diện
        LoadRosterData(); // Để hiển thị trạng thái "đang chọn" (lớp mờ)
        RefreshBottomSlots(); // Để hiển thị dưới 5 ô slot
    }

    private bool IsUnitInTeam(UnitSO unit)
    {
        foreach (var u in currentTeam) if (u == unit) return true;
        return false;
    }

    private void AddToTeam(UnitSO unit)
    {
        for (int i = 0; i < currentTeam.Length; i++)
        {
            if (currentTeam[i] == null) // Tìm ô trống đầu tiên
            {
                currentTeam[i] = unit;
                return;
            }
        }
        Debug.Log("Đội hình đã đầy!");
    }

    private void RemoveFromTeam(UnitSO unit)
    {
        for (int i = 0; i < currentTeam.Length; i++)
        {
            if (currentTeam[i] == unit)
            {
                currentTeam[i] = null;
                return;
            }
        }
    }

    // Hàm này được PartySlotUI gọi khi bấm vào slot để gỡ tướng
    public void RemoveUnitAtSlot(int index)
    {
        if (index >= 0 && index < currentTeam.Length)
        {
            currentTeam[index] = null;
            LoadRosterData();
            RefreshBottomSlots();
        }
    }

    private void RefreshBottomSlots()
    {
        for (int i = 0; i < partySlots.Length; i++)
        {
            if (partySlots[i] != null)
                partySlots[i].Setup(currentTeam[i], i);
        }
    }
    private void OnStartClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("startBtnAudiostartBtn");

        // 1. Kiểm tra điều kiện: Phải có ít nhất 1 tướng
        bool hasUnit = false;
        foreach (var unit in currentTeam)
        {
            if (unit != null)
            {
                hasUnit = true;
                break;
            }
        }

        if (!hasUnit)
        {
            Debug.LogWarning("Đội hình trống! Vui lòng chọn ít nhất 1 tướng.");
            return;
        }

        // --- SỬA LỖI TẠI ĐÂY ---
        // 2. Lưu đội hình vào biến Tĩnh (Static)

        // Cách cũ (Không an toàn): PlayerDataManager.Instance.battleTeamData = currentTeam;

        // Cách mới (An toàn hơn): Tạo mảng mới và copy dữ liệu sang
        UnitSO[] savedTeam = new UnitSO[currentTeam.Length];
        System.Array.Copy(currentTeam, savedTeam, currentTeam.Length);

        // Gán mảng đã copy vào PlayerDataManager
        PlayerDataManager.Instance.battleTeamData = savedTeam;
        if(PlayerDataManager.Instance != null && currentSelectedStage != null)
        {
            PlayerDataManager.Instance.currentStageSO = currentSelectedStage;
        }

        // 3. Chuyển Scene
        SceneManager.LoadScene("Wave1");
    }

}
