using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Spriter2UnityDX;
using DG.Tweening;
using System; // <--- NHỚ THÊM DÒNG NÀY

public class CharacterDetailUI : MonoBehaviour
{
    [Header("UI Slots")]
    public EquipmentSlotUI[] equipmentSlots; // Kéo 6 ô vào đây theo thứ tự 1->6
    [Header("Default Icon")]
    public Sprite[] defaultItemIcons; // Danh sách icon mặc định cho các slot (nếu muốn)
    [Header("DoTween Settings")]
    public float animDuration = 0.3f;
    public Ease popupEase = Ease.OutBack; // Hiệu ứng nảy

    [Header("Model Display")]
    public Transform modelSpawnPoint;
    public Vector3 modelOffSet = new Vector3(-0.3f, -2.5f, 4.5f);
    private GameObject _currentModelInstance;

    [Header("Static Info")]
    public Image avatarImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI powerText;

    [Header("Navigation")]
    public Button backBtn;
    public Button nextBtn;
    public Button prevBtn;

    [Header("Tabs Buttons")]
    public Button btnTabEquipment;
    public Button btnTabLevelUp;
    public Button btnTabSkills;
    public Button btnTabAscension;

    [Header("Tab Pages")]
    public GameObject pageEquipment;
    public GameObject pageLevelUp;
    public GameObject pageSkills;
    public GameObject pageAscension;

    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = Color.gray;

    [Header("Side Panel Info")]
    public GameObject sidePanel;
    public Image sideItemIcon;
    public Image borderItemIcon;
    public TextMeshProUGUI sideItemName;
    public Button btnEquip;
    [Header("Stats UI")]
    public Transform statsContainer; // Kéo GameObject chứa Vertical Layout Group vào đây
    public GameObject statRowPrefab;

    [Header("Drop Item Location")]
    public HowToObtainPopupUI howToObtainPopup;

    // Cache lại item đang được chọn để xử lý nút Equip
    private ItemSO _selectedItem;
    private int _selectedSlotIndex;

    private List<PlayerUnitData> _unitList;
    private UnitSO _currentStaticData;
    private int _currentIndex;
    private System.Func<PlayerUnitData, (UnitSO, GameObject)> _getPrefabFunc; // Lấy model từ callback
    // Component để làm mờ
    private CanvasGroup _canvasGroup;
    private Tween _cpTween; // Khai báo ở đầu class

    private void Awake()
    {
        // Tự động lấy CanvasGroup, nếu chưa có thì thêm vào
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (backBtn != null) backBtn.onClick.AddListener(ClosePanel);
        if (nextBtn != null) nextBtn.onClick.AddListener(ShowNext);
        if (prevBtn != null) prevBtn.onClick.AddListener(ShowPrevious);

        // Setup Tabs (Sửa lại một chút để truyền nút bấm vào hàm SwitchTab)
        btnTabEquipment.onClick.AddListener(() => SwitchTab(TabType.Equipment, btnTabEquipment));
        btnTabLevelUp.onClick.AddListener(() => SwitchTab(TabType.LevelUp, btnTabLevelUp));
        btnTabSkills.onClick.AddListener(() => SwitchTab(TabType.Skills, btnTabSkills));
        btnTabAscension.onClick.AddListener(() => SwitchTab(TabType.Ascension, btnTabAscension));
    }

    public enum TabType { Equipment, LevelUp, Skills, Ascension }

    public void SetupAndOpen(PlayerUnitData dynamicData, List<PlayerUnitData> allUnits, System.Func<PlayerUnitData, (UnitSO, GameObject)> getPrefabLogic)
    {
        _unitList = allUnits;
        _getPrefabFunc = getPrefabLogic;
        _currentIndex = _unitList.FindIndex(u => u == dynamicData);

        // 1. Lấy dữ liệu UnitSO
        var result = _getPrefabFunc.Invoke(dynamicData);
        UnitSO unitSO = result.Item1;
        GameObject prefab = result.Item2;

        if (unitSO == null)
        {
            Debug.LogError($"[CRITICAL] Không tìm thấy UnitSO cho ID: {dynamicData.UnitID}");
            return;
        }
        _currentStaticData = unitSO;

        // 2. Fix lỗi mảng trang bị (isEquipped) bị null
        if (dynamicData.isEquipped == null || dynamicData.isEquipped.Length < 6)
        {
            dynamicData.isEquipped = new bool[6];
        }

        // 3. Hiển thị UI
        gameObject.SetActive(true);
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1f, animDuration).SetUpdate(true);
        }
        transform.localScale = Vector3.one * 0.8f;
        transform.DOScale(1f, animDuration).SetEase(popupEase).SetUpdate(true);

        RefreshContent();
        SwitchTab(TabType.Equipment, btnTabEquipment);

        RefreshEquipmentSlots();

        if (equipmentSlots.Length > 0 && equipmentSlots[0] != null && equipmentSlots[0].gameObject.activeSelf)
        {
            OnSlotClicked(0); // Tự động chọn ô số 1
        }
    }
    private void OnSlotClicked(int slotIndex)
    {
        Debug.Log("Bạn vừa Clicked vào: " + slotIndex);
        // 1. Kiểm tra an toàn dữ liệu cơ bản
        if (_unitList == null || _unitList.Count == 0 || _currentIndex < 0) return;
        if (_currentStaticData == null)
        {
            Debug.LogError("LỖI: _currentStaticData bị null. RefreshContent chưa chạy thành công?");
            return;
        }

        // 2. Lấy Unit hiện tại (KHÔNG DÙNG FOREACH)
        PlayerUnitData currentUnit = _unitList[_currentIndex];

        // 3. Lấy Item từ UnitSO
        ItemSO item = _currentStaticData.GetItemAtSlot(currentUnit.Rank, slotIndex);

        // Nếu slot này không có item (null), ẩn bảng bên phải và thoát
        if (item == null)
        {
            if (sidePanel != null) sidePanel.SetActive(false);
            return;
        }

        // 4. HIỂN THỊ UI (Kèm Check Null từng món để tìm ra cái nào chưa kéo)
        if (sidePanel == null) { Debug.LogError("LỖI: Chưa kéo 'Side Panel' vào Inspector!"); return; }
        sidePanel.SetActive(true);

        if (sideItemName != null) sideItemName.text = item.itemName;
        if (sideItemIcon != null) sideItemIcon.sprite = item.itemAvatar;
        foreach (Transform child in statsContainer)
        {
            Destroy(child.gameObject);
        }

        // --- SINH DÒNG CHỈ SỐ MỚI ---
        if (item.statModifiers != null && item.statModifiers.Count > 0)
        {
            foreach (var mod in item.statModifiers)
            {
                GameObject rowObj = Instantiate(statRowPrefab, statsContainer);
                rowObj.transform.localScale = Vector3.one;

                StatsRowUI rowUI = rowObj.GetComponent<StatsRowUI>();
                if (rowUI != null)
                {
                    // Dịch tên Enum sang Tiếng Việt (hoặc Tiếng Anh tùy ý)
                    string translatedName = TranslateStatName(mod.statType);

                    // Kiểm tra xem chỉ số này có hiển thị dạng % không
                    bool isPercent = (mod.statType == StatType.AttackSpeed);

                    rowUI.Setup(translatedName, mod.value.ToString(), isPercent);
                }
            }
        }
        else
        {
            // Có thể sinh ra 1 dòng chữ "Không có chỉ số" nếu muốn
        }
        // --- HIỂN THỊ KHUNG VIỀN (RARITY BORDER) ---
        if (borderItemIcon != null)
        {
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.rarityConfig != null)
            {
                // Lấy khung từ Config dựa trên độ hiếm của Item
                Sprite rarityFrame = PlayerDataManager.Instance.rarityConfig.GetRarityIcon(item.itemRare);
                if (rarityFrame != null)
                {
                    borderItemIcon.sprite = rarityFrame;
                    borderItemIcon.gameObject.SetActive(true); // Đảm bảo nó được bật
                }
                else
                {
                    // Nếu không có khung (item thường), có thể ẩn hoặc để mặc định
                    borderItemIcon.gameObject.SetActive(false);
                }
            }
        }

            // 5. LOGIC NÚT BẤM
            bool isEquipped = currentUnit.isEquipped[slotIndex];
        bool hasItem = false;

        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.InventoryModel != null)
        {
            hasItem = PlayerDataManager.Instance.InventoryModel.HasItem(item, 1);
            Debug.Log($"Kiểm tra kho cho item {item.itemName}: {(hasItem ? "CÓ HÀNG" : "KHÔNG CÓ HÀNG")}");
        }
        if (isEquipped)
        {
            // TRƯỜNG HỢP 1: ĐÃ MẶC -> Hiện ảnh thật + Hiện khung
            if (sideItemIcon != null)
            {
                sideItemIcon.sprite = item.itemAvatar;
                equipmentSlots[slotIndex].iconImage.sprite = item.itemAvatar; // Cập nhật ảnh thật cho ô bên trái
                equipmentSlots[slotIndex].iconImage.rectTransform.sizeDelta = new Vector2(100f, 100f); // Điều chỉnh kích thước nếu cần
            }

            if (borderItemIcon != null)
            {
                borderItemIcon.gameObject.SetActive(true); // Bật khung
                // Lấy màu khung từ Config
                if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.rarityConfig != null)
                {
                    borderItemIcon.sprite = PlayerDataManager.Instance.rarityConfig.GetRarityIcon(item.itemRare);
                }
            }
        }
        else
        {
            // TRƯỜNG HỢP 2: CHƯA MẶC -> Hiện ảnh mặc định (Default) + Ẩn khung
            if (sideItemIcon != null)
            {
                // Lấy ảnh mặc định từ mảng defaultItemIcons dựa theo slotIndex
                if (defaultItemIcons != null && slotIndex < defaultItemIcons.Length)
                {
                    sideItemIcon.sprite = defaultItemIcons[slotIndex];
                    sideItemIcon.rectTransform.sizeDelta = new Vector2(200f, 200f);
                }
                else
                {
                    // Fallback: Nếu quên kéo ảnh default thì hiện ảnh item nhưng mờ đi
                    sideItemIcon.sprite = item.itemAvatar;
                }

            }

            // Ẩn khung viền đi vì chưa mặc
            if (borderItemIcon != null)
            {
                borderItemIcon.gameObject.SetActive(false);
            }
        }

        if (btnEquip != null)
        {
            btnEquip.gameObject.SetActive(true);
            TextMeshProUGUI btnText = btnEquip.GetComponentInChildren<TextMeshProUGUI>();
            Image btnImg = btnEquip.GetComponent<Image>();

            // Xóa hết sự kiện cũ để tránh bấm 1 lần ăn 2 lệnh
            btnEquip.onClick.RemoveAllListeners();

            if (isEquipped)
            {
                // TRƯỜNG HỢP 1: ĐÃ MẶC -> HIỆN NÚT UNEQUIP (THÁO)
                if (btnText) btnText.text = "UNEQUIP";

                btnEquip.onClick.AddListener(() => PerformUnequip(slotIndex, item));
            }
            else
            {
                // CHƯA MẶC
                if (hasItem)
                {
                    // TRƯỜNG HỢP 2: CÓ HÀNG -> HIỆN NÚT EQUIP (MẶC)
                    if (btnText) btnText.text = "EQUIP";

                    btnEquip.onClick.AddListener(() => PerformEquip(slotIndex, item));
                }
                else
                {
                    // TRƯỜNG HỢP 3: KHÔNG CÓ HÀNG -> HIỆN NÚT TÌM KIẾM
                    if (btnText) btnText.text = "HOW TO OBTAIN";

                    btnEquip.onClick.AddListener(() => ShowHowToObtain(item));
                }
            }
        }
    }

    // Hàm thực hiện mặc đồ
    private void PerformEquip(int slotIndex, ItemSO item)
    {
        PlayerUnitData currentUnit = _unitList[_currentIndex];

        // --- 1. LẤY LỰC CHIẾN CŨ ---
        int oldCP = currentUnit.GetCombatPower(_currentStaticData);

        // --- Logic mặc đồ ---
        if (PlayerDataManager.Instance != null)
        {
            int player = 2;
            PlayerDataManager.Instance.EquipItemUnitsFromServer(player, currentUnit.UnitID, item.itemType.ToString(), item.itemID,  slotIndex,
                onSuccess: () =>
                {
                    PlayerDataManager.Instance.InventoryModel.RemoveItem(item, 1);
                    currentUnit.isEquipped[slotIndex] = true;

                    if (item.statModifiers != null)
                    {
                        foreach (var mod in item.statModifiers) currentUnit.AddStatModifier(mod, true);
                    }
                    // --- 2. LẤY LỰC CHIẾN MỚI SAU KHI MẶC ---
                    int newCP = currentUnit.GetCombatPower(_currentStaticData);
                    int cpDiff = newCP - oldCP;

                    // --- 3. GỌI NOTIFICATION (Màu Xanh lá cây) ---
                    NotificationManager.Instance.ShowCombatPowerChange(oldCP, newCP);
                    NotificationManager.Instance.ShowNotification($"You have equipped {item.itemName} for {currentUnit.Name}!");

                    // Cập nhật lại các ô giao diện
                    RefreshContent();
                    RefreshEquipmentSlots();
                    OnSlotClicked(slotIndex);

                    // --- 4. GỌI HIỆU ỨNG NHẢY SỐ ---
                    AnimateCombatPower(oldCP, newCP);
                    if (btnEquip != null) btnEquip.interactable = true;
                },
        onError: (err) =>
        {
            NotificationManager.Instance.ShowNotification($"Failed to equip {item.itemName} : {err}");
            Debug.Log("[Equip API] " + err);
            if (btnEquip != null) btnEquip.interactable = true;
        }
        );
        }
        else
        {
            // Fallback: no PlayerDataManager available -> do local equip (original behavior)
            PlayerDataManager.Instance.InventoryModel.RemoveItem(item, 1);
            currentUnit.isEquipped[slotIndex] = true;

            if (item.statModifiers != null)
            {
                foreach (var mod in item.statModifiers) currentUnit.AddStatModifier(mod, true);
            }

            int newCP = currentUnit.GetCombatPower(_currentStaticData);

            NotificationManager.Instance.ShowCombatPowerChange(oldCP, newCP);
            NotificationManager.Instance.ShowNotification($"You have equipped {item.itemName} for {currentUnit.Name}!");

            RefreshContent();
            RefreshEquipmentSlots();
            OnSlotClicked(slotIndex);

            AnimateCombatPower(oldCP, newCP);

            if (btnEquip != null) btnEquip.interactable = true;
        }
    }
    // --- HÀM XỬ LÝ THÁO ĐỒ ---
    private void PerformUnequip(int slotIndex, ItemSO item)
    {
        PlayerUnitData currentUnit = _unitList[_currentIndex];

        // Disable button to avoid double submit
        if (btnEquip != null) btnEquip.interactable = false;

        int oldCP = currentUnit.GetCombatPower(_currentStaticData);

        if (PlayerDataManager.Instance != null)
        {
            // TODO: replace playerId with your runtime player id
            int playerId = 2;
            string itemTypeStr = item.itemType.ToString();

            PlayerDataManager.Instance.UnEquipItemUnitsFromServer(
                playerId: playerId,
                unitID: currentUnit.UnitID,
                itemType: itemTypeStr,
                itemID: item.itemID,
                slotIndex: slotIndex,
                onSuccess: () =>
                {
                    // Apply local changes only after server confirms
                    PlayerDataManager.Instance.InventoryModel.AddItem(item, 1);
                    currentUnit.isEquipped[slotIndex] = false;

                    if (item.statModifiers != null)
                    {
                        foreach (var mod in item.statModifiers) currentUnit.AddStatModifier(mod, false);
                    }

                    int newCP = currentUnit.GetCombatPower(_currentStaticData);

                    NotificationManager.Instance.ShowNotification($"You have unequipped {item.itemName} from {currentUnit.Name}!");
                    NotificationManager.Instance.ShowCombatPowerChange(oldCP, newCP);

                    RefreshContent();
                    RefreshEquipmentSlots();
                    OnSlotClicked(slotIndex);

                    AnimateCombatPower(oldCP, newCP);

                    if (btnEquip != null) btnEquip.interactable = true;
                },
                onError: (err) =>
                {
                    NotificationManager.Instance.ShowNotification($"Failed to unequip {item.itemName}: {err}");
                    Debug.LogError("[Unequip API] " + err);
                    if (btnEquip != null) btnEquip.interactable = true;
                }
            );
        }
        else
        {
            // Fallback local unequip (if offline)
            PlayerDataManager.Instance.InventoryModel.AddItem(item, 1);
            currentUnit.isEquipped[slotIndex] = false;
            if (item.statModifiers != null)
            {
                foreach (var mod in item.statModifiers) currentUnit.AddStatModifier(mod, false);
            }
            int newCP = currentUnit.GetCombatPower(_currentStaticData);
            NotificationManager.Instance.ShowNotification($"You have unequipped {item.itemName} from {currentUnit.Name}!");
            NotificationManager.Instance.ShowCombatPowerChange(oldCP, newCP);
            RefreshContent();
            RefreshEquipmentSlots();
            OnSlotClicked(slotIndex);
            AnimateCombatPower(oldCP, newCP);
            if (btnEquip != null) btnEquip.interactable = true;
        }
    }
    private void ShowHowToObtain(ItemSO item)
    {
        Debug.Log("Hiện bảng map rơi đồ: " + item.itemName);
        
        if (howToObtainPopup != null)
        {
            howToObtainPopup.SetupAndShow(item);
        }
    }
    // Hàm dùng chung để cập nhật dữ liệu khi chuyển tướng
    private void RefreshContent()
    {
        if (_unitList == null || _unitList.Count == 0 || _currentIndex < 0) return;

        PlayerUnitData dynamicData = _unitList[_currentIndex];

        // Lấy lại dữ liệu nếu cần (hoặc dùng _currentStaticData đã cache)
        var result = _getPrefabFunc.Invoke(dynamicData);
        UnitSO staticData = result.Item1;
        GameObject prefabOverride = result.Item2;

        // Safety Check lại lần nữa cho chắc (vì hàm này được gọi khi bấm Next/Prev)
        if (staticData == null)
        {
            Debug.LogError($"Lỗi tại RefreshContent: Không tìm thấy SO cho {dynamicData.UnitID}");
            return;
        }

        _currentStaticData = staticData;

        // Cập nhật UI
        if (nameText) nameText.text = staticData.name; // Dùng unitName thay vì name (tên file)
        if (rankText) rankText.text = "RANK " + dynamicData.Rank;
        if (levelText) levelText.text = dynamicData.Level.ToString();

        // Kiểm tra null avatar trước khi gán
        if (avatarImage != null && staticData.avatar != null)
            avatarImage.sprite = staticData.avatar;
        else if (avatarImage != null)
            avatarImage.sprite = null; // Hoặc ảnh mặc định
        if (powerText != null)
        {
            // currentUnit là dữ liệu Tướng hiện tại (PlayerUnitData)
            // staticData là dữ liệu gốc của Tướng đó (UnitSO)
            int combatPower = dynamicData.GetCombatPower(staticData);
            powerText.text = combatPower.ToString();
        }

        // Spawn Model
        SpawnCharacterModel(prefabOverride);

        // Hiệu ứng
        if (nameText) nameText.transform.DOPunchScale(Vector3.one * 0.05f, 0.2f);

        RefreshEquipmentSlots();

        // 2. Ẩn bảng thông tin bên phải đi, hoặc tự động bấm vào ô đầu tiên để tránh bị dính thông tin đồ của tướng trước
        if (equipmentSlots.Length > 0 && equipmentSlots[0] != null)
        {
            OnSlotClicked(0);
        }
    }
    private void RefreshEquipmentSlots()
    {
        if (_unitList == null || _unitList.Count == 0 || _currentIndex < 0) return;

        PlayerUnitData currentUnit = _unitList[_currentIndex];

        for (int i = 0; i < 6; i++)
        {
            if (i >= equipmentSlots.Length || equipmentSlots[i] == null) continue;

            // 1. Lấy dữ liệu Item dựa trên Rank của tướng HIỆN TẠI
            ItemSO item = _currentStaticData.GetItemAtSlot(currentUnit.Rank, i);

            // 2. Kiểm tra xem tướng HIỆN TẠI đã mặc món này chưa
            bool isEquipped = currentUnit.isEquipped[i];

            // 3. Kiểm tra trong kho xem có đồ không
            bool hasItem = false;
            if (item != null && PlayerDataManager.Instance != null && PlayerDataManager.Instance.InventoryModel != null)
            {
                hasItem = PlayerDataManager.Instance.InventoryModel.HasItem(item, 1);
            }

            // 4. Lấy Icon mặc định (nếu có)
            Sprite defaultIcon = null;
            if (defaultItemIcons != null && i < defaultItemIcons.Length)
            {
                defaultIcon = defaultItemIcons[i];
            }

            // 5. Setup lại ô UI
            equipmentSlots[i].Setup(item, isEquipped, hasItem, i, OnSlotClicked, defaultIcon);
        }
    }

    // Sửa hàm này nhận thêm Button để làm hiệu ứng nhún nút
    private void SwitchTab(TabType type, Button clickedButton)
    {
        // [DOTWEEN] Hiệu ứng nhún nút (Punch) khi bấm
        if (clickedButton != null)
        {
            // Reset scale cũ đề phòng bấm liên tục
            clickedButton.transform.DOKill();
            clickedButton.transform.localScale = Vector3.one;
            // Nhún nhẹ: (vector sức mạnh, thời gian, độ rung, độ đàn hồi)
            clickedButton.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 0.2f, 10, 1);
        }

        // Tắt hết pages
        pageEquipment.SetActive(false);
        pageLevelUp.SetActive(false);
        pageSkills.SetActive(false);
        pageAscension.SetActive(false);

        SetButtonColor(btnTabEquipment, inactiveTabColor);
        SetButtonColor(btnTabLevelUp, inactiveTabColor);
        SetButtonColor(btnTabSkills, inactiveTabColor);
        SetButtonColor(btnTabAscension, inactiveTabColor);

        GameObject targetPage = null;
        Button targetBtn = null;

        switch (type)
        {
            case TabType.Equipment:
                targetPage = pageEquipment;
                targetBtn = btnTabEquipment;
                RefreshEquipmentPage();
                break;
            case TabType.LevelUp:
                targetPage = pageLevelUp;
                targetBtn = btnTabLevelUp;
                break;
            case TabType.Skills:
                targetPage = pageSkills;
                targetBtn = btnTabSkills;
                break;
            case TabType.Ascension:
                targetPage = pageAscension;
                targetBtn = btnTabAscension;
                break;
        }

        // Bật page được chọn và đổi màu
        if (targetPage != null)
        {
            targetPage.SetActive(true);

            // [DOTWEEN] Hiệu ứng nội dung trượt lên nhẹ nhàng
            // 1. Đặt vị trí thấp hơn 1 chút và trong suốt
            CanvasGroup pageCG = targetPage.GetComponent<CanvasGroup>();
            if (pageCG == null) pageCG = targetPage.AddComponent<CanvasGroup>();

            pageCG.alpha = 0;
            targetPage.transform.localPosition += new Vector3(0, -50, 0); // Dịch xuống

            // 2. Tween lên vị trí cũ và hiện rõ
            pageCG.DOFade(1, 0.3f);
            targetPage.transform.DOLocalMoveY(targetPage.transform.localPosition.y + 50, 0.3f).SetEase(Ease.OutQuad);
        }

        if (targetBtn != null) SetButtonColor(targetBtn, activeTabColor);
    }

    private void SetButtonColor(Button btn, Color color)
    {
        var img = btn.GetComponent<Image>();
        if (img != null) img.DOColor(color, 0.2f); // [DOTWEEN] Đổi màu mượt mà thay vì đổi cái rụp
    }

    private void RefreshEquipmentPage() { }

    private void SpawnCharacterModel(GameObject prefab)
    {
        if (_currentModelInstance != null) Destroy(_currentModelInstance);

        if (prefab != null && modelSpawnPoint != null)
        {
            _currentModelInstance = Instantiate(prefab, modelSpawnPoint);
            _currentModelInstance.transform.localPosition = modelOffSet;
            _currentModelInstance.transform.localRotation = Quaternion.identity;

            // [DOTWEEN] Hiệu ứng Model xuất hiện (Nảy từ 0 lên 1)
            _currentModelInstance.transform.localScale = Vector3.zero; // Bắt đầu từ 0
            _currentModelInstance.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack); // Phóng to + Nảy

            // Xử lý Scripts
            MonoBehaviour[] scripts = _currentModelInstance.GetComponentsInChildren<MonoBehaviour>();
            foreach (var script in scripts)
            {
                if (script is EntityRenderer renderer)
                {
                    renderer.ApplySpriterZOrder = true;
                    continue;
                }
                script.enabled = false;
            }
        }
    }
    private string TranslateStatName(StatType type)
    {
        switch (type)
        {
            case StatType.HP: return "Max HP";
            case StatType.Attack: return "Attack";
            case StatType.Armor: return "Armor";
            case StatType.AttackSpeed: return "Attack Speed";
            // Thêm các loại khác sau này...
            default: return type.ToString();
        }
    }
    // Hàm xử lý hiệu ứng nhảy số
    private void AnimateCombatPower(int oldCP, int newCP)
    {
        if (powerText == null) return;

        // 1. Dừng ngay hiệu ứng cũ nếu đang chạy dở
        if (_cpTween != null) _cpTween.Kill();
        powerText.transform.DOKill();

        // 2. Trả Text về kích thước gốc trước khi làm hiệu ứng
        powerText.transform.localScale = Vector3.one;

        // 3. Hiệu ứng Text phình to ra một chút để thu hút sự chú ý
        powerText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f).SetUpdate(true);

        // 4. HIỆU ỨNG NHẢY SỐ
        int tempCP = oldCP;
        _cpTween = DOTween.To(() => tempCP, x =>
        {
            tempCP = x;
            powerText.text = tempCP.ToString("N0"); // Cập nhật text liên tục
        }, newCP, 0.5f) // Nhảy số trong 0.5 giây
        .SetEase(Ease.OutQuad) // Chậm dần về cuối cho mượt
        .SetUpdate(true);
    }
    public void ShowNext()
    {
        if (_unitList == null || _unitList.Count <= 1) return;

        _currentIndex++;
        if (_currentIndex >= _unitList.Count) _currentIndex = 0; // Vòng lặp về đầu

        // Hiệu ứng nút bấm
        nextBtn.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.2f);

        RefreshContent();
    }

    public void ShowPrevious()
    {
        if (_unitList == null || _unitList.Count <= 1) return;

        _currentIndex--;
        if (_currentIndex < 0) _currentIndex = _unitList.Count - 1; // Vòng lặp tới cuối

        // Hiệu ứng nút bấm
        prevBtn.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.2f);

        RefreshContent();
    }

    public void ClosePanel()
    {
        if (_currentModelInstance != null) Destroy(_currentModelInstance);

        // [DOTWEEN] Hiệu ứng đóng Panel (Thu nhỏ + Mờ dần)
        // Khi chạy xong hiệu ứng (OnComplete) thì mới SetActive(false)
        transform.DOScale(0.8f, 0.2f).SetUpdate(true);
        _canvasGroup.DOFade(0f, 0.2f).SetUpdate(true).OnComplete(() =>
        {
            this.gameObject.SetActive(false);
        });
    }
}