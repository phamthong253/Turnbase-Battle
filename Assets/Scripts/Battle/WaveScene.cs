using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

// Class mới: Chỉ chứa thông tin VỊ TRÍ TRÊN SCENE (Không chứa dữ liệu quái)
[System.Serializable]
public class SceneStageLocation
{
    [Header("Physical Locations")]
    [Tooltip("Kéo StageTemplate có sẵn trên Scene vào đây")]
    public StageTemplate stageTemplate;

    [Tooltip("Camera cho vị trí này (Optional)")]
    public CinemachineCamera stageCamera;
}

[System.Serializable]
public class UnitPrefabMapping
{
    [Header("Prefab References")]
    public UnitSO unitData;
    public GameObject unitPrefab;
}

public class WaveScene : MonoBehaviour
{
    #region Singleton
    public static WaveScene Instance;
    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }
    #endregion

    [Header("DEBUG & DATA")]
    [Tooltip("Nếu tích, game sẽ dùng stageSOTest thay vì lấy từ PlayerDataManager")]
    public bool useDebugStage = false;
    public StageSO stageSOTest;

    // Dữ liệu màn chơi hiện tại (Load từ Manager hoặc Debug)
    private StageSO currentStageData;

    [Header("Scene Locations")]
    [Tooltip("Danh sách các điểm đánh nhau trên bản đồ. Wave 1 dùng Element 0, Wave 2 dùng Element 1...")]
    public List<SceneStageLocation> sceneLocations;

    [Header("Camera Settings")]
    public CinemachineCamera cinemachineCamera;
    public Transform[] teamFollowTarget; // Giữ lại để tương thích logic cũ nếu cần

    [Header("Component References")]
    [SerializeField] private BattleHandler battleHandler;

    [Header("Unit Prefab Mapping")]
    public List<UnitPrefabMapping> unitPrefabMapping;

    [Header("Party & Enemy Management")]
    public List<UnitController> playerUnits;
    public List<UnitController> activeEnemies;

    [Header("UI & Game State")]
    public GameObject WinnerUI;
    private bool isGameFinished = false;
    private bool isWaitingForNextStage = false;

    [Header("Winner Display")]
    public Transform winnerDisplayAnchor;
    [SerializeField] public float winnerHorizontalSpacing;
    [SerializeField] public WinnerCanvas winnerCanvas;
    public float moveDuration = 1.0f;
    public static event Action OnGameFinish;
    public RewardManager rewardManager;

    // Biến trạng thái nội bộ
    private int currentWaveIndex = -1; // Đổi tên từ StageIndex -> WaveIndex cho đúng ngữ nghĩa
    private bool isAdvancing = false;

    [Header("Battle Start UI")]
    public BattleStartUI battleStartUI;
    [Header("Scene Transition Settings")]
    public SceneTransitionManager transitionManager;

    [Header("Cinematic Settings")]
    public float entryOffsetDistance = 40f;
    public float entryDuration = 3.5f;

    void Start()
    {
        Debug.Log("WaveScene: Khởi tạo màn chơi.");

        // 1. LOAD DỮ LIỆU
        LoadStageData();

        if (currentStageData == null)
        {
            Debug.LogError("LỖI CRITICAL: Không có Stage Data để chạy! Vui lòng kiểm tra PlayerDataManager hoặc chế độ Debug.");
            return;
        }

        AudioManager.Instance.PlayBattleMusic("BattleTheme1");
        RewardManager.Instance.StartNewReward();

        // 2. Chạy Intro cho Wave đầu tiên
        StartCoroutine(StartIntroCinematic());
    }

    private void LoadStageData()
    {
        if (useDebugStage && stageSOTest != null)
        {
            currentStageData = stageSOTest;
            Debug.LogWarning($"[DEBUG MODE] Đang sử dụng Stage: {currentStageData.stageName}");
        }
        else if (PlayerDataManager.Instance != null)
        {
            currentStageData = PlayerDataManager.Instance.currentStageSO;
        }

        // Cập nhật text hiển thị số Wave
        if (currentStageData != null && WaveText.Instance != null)
        {
            WaveText.Instance.UpdateWaveText(1, currentStageData.waves.Count);
        }
    }

    void Update()
    {
        if (isGameFinished || isAdvancing || isWaitingForNextStage) return;

        activeEnemies.RemoveAll(enemy => enemy == null || enemy.isDestroyed);

        if (activeEnemies.Count == 0)
        {
            StartCoroutine(WaitAndNextWave());
        }
    }

    // ======================================================================================
    // LOGIC CINEMATIC (WAVE 1)
    // ======================================================================================

    private IEnumerator StartIntroCinematic()
    {
        isAdvancing = true;
        currentWaveIndex = 0;

        // 1. Lấy vị trí Scene cho Wave 1 (Element 0)
        SceneStageLocation location = GetLocationForWave(0);
        if (location == null) yield break;

        // Setup Camera
        if (location.stageCamera != null)
        {
            // Logic chuyển cam nếu dùng CinemachineCamera riêng
            // location.stageCamera.Priority = 100; 
        }
        else if (teamFollowTarget.Length > 0 && cinemachineCamera != null)
        {
            cinemachineCamera.Follow = teamFollowTarget[0];
        }

        // 2. Spawn Player và Enemy (Dựa trên StageSO)
        SpawnPlayerAtOffset(location.stageTemplate.playerTeamAnchor);
        SpawnEnemiesAtOffset(location.stageTemplate.enemyTeamAnchor, currentStageData.waves[0]);

        yield return new WaitForSeconds(0.5f);

        // 3. Cinematic chạy vào
        yield return StartCoroutine(RunIntoBattleCinematic(location));

        // 4. Hiển thị chữ START
        if (battleStartUI != null)
        {
            yield return battleStartUI.PlayStartSequence().WaitForCompletion();
        }

        // 5. START BATTLE
        isAdvancing = false;

        if (battleHandler != null)
        {
            battleHandler.playerTeamAnchor = location.stageTemplate.playerTeamAnchor;
            battleHandler.InitializeBattle(this.playerUnits);
            battleHandler.StartWave(this.activeEnemies);
        }

        Debug.Log("<color=green>INTRO HOÀN TẤT - BẮT ĐẦU WAVE 1</color>");
    }

    private IEnumerator RunIntoBattleCinematic(SceneStageLocation location)
    {
        List<Tween> runTweens = new List<Tween>();

        // Player Run
        foreach (UnitController unit in playerUnits)
        {
            if (unit == null) continue;
            if (unit.unitBase != null) unit.unitBase.PlayAnimation("move");

            Transform dest = location.stageTemplate.playerTeamAnchor.Find($"PlayerPos{unit.formationSlotIndex + 1}");
            if (dest != null)
            {
                Tween t = unit.transform.DOMove(dest.position, entryDuration).SetEase(Ease.OutQuad)
                    .OnComplete(() => {
                        if (unit.unitBase != null)
                        {
                            unit.unitBase.PlayAnimation("passive");
                            unit.PlayPassiveVisuals();
                        }
                        unit.unitBase.PlayAnimation("idle");
                        unit.transform.rotation = Quaternion.Euler(0, 0, 0);
                    });
                runTweens.Add(t);
            }
        }

        // Enemy Run
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            UnitController unit = activeEnemies[i];
            if (unit == null) continue;
            if (unit.unitBase != null) unit.unitBase.PlayAnimation("move");

            Transform dest = location.stageTemplate.enemyTeamAnchor.Find($"EnemyPos{i + 1}");
            if (dest != null)
            {
                Tween t = unit.transform.DOMove(dest.position, entryDuration).SetEase(Ease.OutQuad)
                    .OnComplete(() => {
                        if (unit.unitBase != null) unit.unitBase.PlayAnimation("idle");
                        unit.transform.rotation = Quaternion.Euler(0, 180, 0);
                    });
                runTweens.Add(t);
            }
        }

        if (runTweens.Count > 0) yield return runTweens[0].WaitForCompletion();
    }


    // ======================================================================================
    // SPAWN LOGIC (Hệ thống Spawn mới)
    // ======================================================================================

    private void SpawnPlayerAtOffset(Transform anchor)
    {
        Debug.Log("--- SPAWN PLAYER ---");
        if (playerUnits == null) playerUnits = new List<UnitController>();
        playerUnits.Clear();

        UnitSO[] formation = PlayerDataManager.Instance.battleTeamData;
        if (formation == null) return;

        for (int i = 0; i < formation.Length; i++)
        {
            if (formation[i] == null) continue;
            UnitSO data = formation[i];

            // Sử dụng hàm GetMapping để fix lỗi Clone vs Asset
            UnitPrefabMapping mapping = GetMapping(data);

            if (mapping != null && mapping.unitPrefab != null)
            {
                Transform destPoint = anchor.Find($"PlayerPos{i + 1}");
                if (destPoint != null)
                {
                    Vector3 spawnPos = destPoint.position - new Vector3(entryOffsetDistance, 0, 0);
                    GameObject p = Instantiate(mapping.unitPrefab, spawnPos, Quaternion.identity);

                    SetupUnitController(p, data, i, anchor);
                    playerUnits.Add(p.GetComponent<UnitController>());
                }
            }
        }
    }

    private void SpawnEnemiesAtOffset(Transform anchor, WaveData waveData)
    {
        activeEnemies.Clear();
        List<UnitSO> enemies = waveData.enemies; // Lấy dữ liệu từ StageSO

        for (int i = 0; i < enemies.Count; i++)
        {
            UnitSO data = enemies[i];
            if (data == null) continue;

            UnitPrefabMapping mapping = GetMapping(data);
            Transform destPoint = anchor.Find($"EnemyPos{i + 1}");

            if (mapping != null && destPoint != null && mapping.unitPrefab != null)
            {
                Vector3 spawnPos = destPoint.position + new Vector3(entryOffsetDistance, 0, 0);
                GameObject e = Instantiate(mapping.unitPrefab, spawnPos, Quaternion.Euler(0, 180, 0));

                UnitController ctrl = e.GetComponent<UnitController>();
                PlayerUnitData enemyDynamicData = new PlayerUnitData(data.unitID)
                {
                    Level = 1, // Quái wave 1 cấp 1
                    Rank = 1
                };
                RuntimeUnit enemyRuntimeData = new RuntimeUnit(data, enemyDynamicData);

                ctrl.SetupUnit(enemyRuntimeData); // Bơm dữ liệu
                activeEnemies.Add(ctrl);
            }
        }
    }

    // ======================================================================================
    // LOGIC CHUYỂN WAVE
    // ======================================================================================

    private IEnumerator WaitAndNextWave()
    {
        isWaitingForNextStage = true;
        yield return new WaitForSeconds(0.5f);

        // Kiểm tra xem đã hết Wave chưa dựa trên DATA
        if (currentWaveIndex >= currentStageData.waves.Count - 1)
        {
            isGameFinished = true;
            DisplayWinnerUI();
        }
        else
        {
            StartNextWave();
        }
        isWaitingForNextStage = false;
    }

    private void StartNextWave()
    {
        isAdvancing = true;
        currentWaveIndex++;

        if (WaveText.Instance != null)
            WaveText.Instance.UpdateWaveText(currentWaveIndex + 1, currentStageData.waves.Count);

        Debug.Log($"<color=yellow>Chuyển sang Wave {currentWaveIndex + 1}</color>");
        StartCoroutine(AdvanceToNextWaveCoroutine());
    }

    private IEnumerator AdvanceToNextWaveCoroutine()
    {
        yield return StartCoroutine(MoveAndSetupBattleForWave());
        if (battleHandler != null)
        {
            battleHandler.StartWave(this.activeEnemies);
        }

        isAdvancing = false;
        Debug.Log($"<color=green>Bắt đầu Wave {currentWaveIndex + 1}</color>");
    }

    private IEnumerator MoveAndSetupBattleForWave()
    {
        // -----------------------------------------------------------------------
        // BƯỚC 1: CHẠY ĐẾN ĐIỂM KẾT THÚC CỦA WAVE TRƯỚC (NẾU CÓ)
        // -----------------------------------------------------------------------

        // Vì currentWaveIndex đã được cộng thêm 1 ở hàm StartNextWave, 
        // nên vị trí hiện tại đang đứng là (index - 1)
        int previousWaveIndex = currentWaveIndex - 1;

        if (previousWaveIndex >= 0)
        {
            SceneStageLocation oldLoc = GetLocationForWave(previousWaveIndex);

            // Kiểm tra xem StageTemplate cũ có điểm EndPoint không
            if (oldLoc != null && oldLoc.stageTemplate != null && oldLoc.stageTemplate.stageEndPoint != null)
            {
                Debug.Log("Unit đang di chuyển đến StageEndPoint...");

                Vector3 exitPos = oldLoc.stageTemplate.stageEndPoint.position;
                int unitsArrived = 0;
                int totalLivingUnits = 0;

                foreach (UnitController unit in playerUnits)
                {
                    if (unit != null && !unit.isDestroyed)
                    {
                        totalLivingUnits++;

                        // Chạy animation Move
                        if (unit.unitBase != null) unit.unitBase.PlayAnimation("move");

                        // Tính toán vị trí đến: EndPoint + một chút ngẫu nhiên để unit không đứng chồng lên nhau
                        Vector3 target = exitPos + new Vector3(UnityEngine.Random.Range(-1.5f, 0.5f), UnityEngine.Random.Range(-1f, 1f), 0);

                        // Ra lệnh di chuyển
                        StartCoroutine(unit.MoveCoroutineAndCallback(target, () =>
                        {
                            unitsArrived++;
                            // Đến nơi thì đứng nghỉ
                            if (unit.unitBase != null) unit.unitBase.PlayAnimation("idle");
                        }));
                    }
                }

                // Chờ cho đến khi tất cả unit còn sống chạy đến nơi
                if (totalLivingUnits > 0)
                {
                    yield return new WaitUntil(() => unitsArrived >= totalLivingUnits);
                }
            }
        }

        // -----------------------------------------------------------------------
        // BƯỚC 2: HIỆU ỨNG CHUYỂN CẢNH (MÀN HÌNH TỐI DẦN)
        // -----------------------------------------------------------------------
        if (transitionManager != null)
        {
            yield return transitionManager.FadeIn().WaitForCompletion();
        }
        if (currentWaveIndex < teamFollowTarget.Length && cinemachineCamera != null)
        {
            cinemachineCamera.Follow = teamFollowTarget[currentWaveIndex];
        }

        // -----------------------------------------------------------------------
        // BƯỚC 3: SETUP VỊ TRÍ MỚI TRONG BÓNG TỐI (TELEPORT)
        // -----------------------------------------------------------------------

        // Xác định vị trí mới
        SceneStageLocation newLoc = GetLocationForWave(currentWaveIndex);
        if (newLoc == null) yield break;

        Transform playerTeamAnchor = newLoc.stageTemplate.playerTeamAnchor;

        // Cập nhật Anchor cho BattleHandler để nó biết trận chiến mới diễn ra ở đâu
        if (battleHandler != null) battleHandler.playerTeamAnchor = playerTeamAnchor;

        // Dịch chuyển tức thời (Teleport) Unit sang vị trí đội hình mới
        foreach (UnitController unit in playerUnits)
        {
            if (unit != null && !unit.isDestroyed)
            {
                Transform newPos = playerTeamAnchor.Find($"PlayerPos{unit.formationSlotIndex + 1}");
                if (newPos != null)
                {
                    // Teleport ngay lập tức (không chạy bộ nữa)
                    unit.transform.position = newPos.position;

                    // Reset hướng quay mặt sang phải
                    unit.transform.rotation = Quaternion.Euler(0, 0, 0);

                    // Cập nhật lại anchor bên trong unit
                    unit.SetFormationAnchor(playerTeamAnchor, newPos.position);

                    // Đảm bảo unit đang ở trạng thái Idle
                    if (unit.unitBase != null) unit.unitBase.PlayAnimation("idle");
                }
            }
        }

        // Spawn Enemy tại chỗ mới
        SpawnEnemiesForWaveSimple(newLoc.stageTemplate.enemyTeamAnchor, currentStageData.waves[currentWaveIndex]);

        // Chờ 1 chút cho hệ thống ổn định (tránh giật khi Fade Out)
        yield return new WaitForSeconds(0.2f);

        // -----------------------------------------------------------------------
        // BƯỚC 4: KẾT THÚC HIỆU ỨNG (MÀN HÌNH SÁNG DẦN)
        // -----------------------------------------------------------------------
        if (transitionManager != null)
        {
            yield return transitionManager.FadeOut().WaitForCompletion();
        }
    }

    private void SpawnEnemiesForWaveSimple(Transform anchor, WaveData waveData)
    {
        activeEnemies.Clear();
        List<UnitSO> enemies = waveData.enemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            UnitSO data = enemies[i];
            UnitPrefabMapping mapping = GetMapping(data);
            Transform spawnPoint = anchor.Find($"EnemyPos{i + 1}");

            if (mapping != null && spawnPoint != null && mapping.unitPrefab != null)
            {
                // Spawn ngay tại chỗ
                GameObject e = Instantiate(mapping.unitPrefab, spawnPoint.position, Quaternion.identity);
                UnitController ctrl = e.GetComponent<UnitController>();
                int enemyLevel = 1 + currentWaveIndex; // VD: Wave 2 -> Level 2, Wave 3 -> Level 3

                PlayerUnitData enemyDynamicData = new PlayerUnitData(data.unitID)
                {
                    Level = enemyLevel, // Quái mạnh dần theo Wave
                    Rank = 1
                };
                RuntimeUnit enemyRuntimeData = new RuntimeUnit(data, enemyDynamicData);

                ctrl.SetupUnit(enemyRuntimeData);
                activeEnemies.Add(ctrl);
            }
        }
    }

    // ======================================================================================
    // CÁC HÀM TIỆN ÍCH (HELPER)
    // ======================================================================================

    /// <summary>
    /// Tìm Prefab dựa trên Tên để tránh lỗi Clone vs Asset
    /// </summary>
    private UnitPrefabMapping GetMapping(UnitSO data)
    {
        if (data == null) return null;
        return unitPrefabMapping.Find(m =>
            m.unitData != null &&
            (m.unitData.name == data.name || data.name.StartsWith(m.unitData.name))
        );
    }

    /// <summary>
    /// Lấy vị trí Scene dựa trên Wave Index.
    /// Nếu số lượng vị trí ít hơn số Wave, sẽ dùng lại vị trí cuối cùng.
    /// </summary>
    private SceneStageLocation GetLocationForWave(int index)
    {
        if (sceneLocations == null || sceneLocations.Count == 0) return null;
        // Clamp index: Ví dụ có 3 vị trí nhưng 5 wave -> Wave 4, 5 sẽ đánh ở vị trí 3.
        int locIndex = Mathf.Clamp(index, 0, sceneLocations.Count - 1);
        return sceneLocations[locIndex];
    }

    private void SetupUnitController(GameObject obj, UnitSO data, int index, Transform anchor)
    {
        UnitController ctrl = obj.GetComponent<UnitController>();
        if (ctrl != null)
        {
            PlayerUnitData dynamicData = new PlayerUnitData(data.unitID) {
                Level = 1,
                Rank = 1
            };
            RuntimeUnit runtimeUnit = new RuntimeUnit(data, dynamicData);
            ctrl.SetupUnit(runtimeUnit);
            ctrl.formationSlotIndex = index;
            // SetFormationAnchor quan trọng để tính toán khi unit quay về chỗ cũ
            Transform slotTransform = anchor.Find($"PlayerPos{index + 1}");
            if (slotTransform != null)
                ctrl.SetFormationAnchor(anchor, slotTransform.position);
        }
    }

    // ... (Giữ nguyên phần DisplayWinnerUI và FinishMatch) ...
    // ======================================================================================
    // LOGIC CHIẾN THẮNG & DI CHUYỂN ĐỘI HÌNH (Đã chỉnh sửa)
    // ======================================================================================

    private void DisplayWinnerUI()
    {
        isAdvancing = true;

        // 1. Tính toán số sao NGAY LẬP TỨC (để lưu trữ)
        int deathUnitsCount = 0;
        foreach (UnitController unit in playerUnits)
        {
            if (unit == null || unit.isDestroyed) deathUnitsCount++;
        }

        int finalStars = 3;
        if (deathUnitsCount > 2) finalStars = 1;
        else if (deathUnitsCount == 2) finalStars = 2;

        // 2. Xóa HUD máu trên đầu nhân vật cho đẹp đội hình
        battleHandler?.RemovePlayerHUDs();

        // 3. Bắt đầu di chuyển nhân vật ra giữa màn hình
        StartCoroutine(MoveUnitsToWinnerScreenCoroutine(() =>
        {
            // 4. Callback: Sau khi di chuyển xong thì mới hiện UI và Lưu game
            FinishMatch(finalStars);
        }));
    }

    private IEnumerator MoveUnitsToWinnerScreenCoroutine(Action onComplete)
    {
        List<UnitController> livingUnits = playerUnits.FindAll(u => u != null && !u.isDestroyed);
        int unitCount = livingUnits.Count;

        // Nếu không còn ai sống (thua cuộc?) thì gọi finish luôn
        if (unitCount == 0) { onComplete?.Invoke(); yield break; }

        // Tính toán vị trí dàn hàng ngang (Căn giữa)
        float totalWidth = (unitCount - 1) * winnerHorizontalSpacing;
        float startX = -totalWidth / 2f;
        int unitsFinishedMoving = 0;

        for (int i = 0; i < unitCount; i++)
        {
            UnitController unit = livingUnits[i];

            // Tính vị trí đích cho từng unit dựa trên Anchor
            float xOffset = startX + i * winnerHorizontalSpacing;
            Vector3 finalPosition = winnerDisplayAnchor.position + winnerDisplayAnchor.right * xOffset;

            // Gọi lệnh di chuyển
            StartCoroutine(unit.MoveCoroutineAndCallback(finalPosition, moveDuration, () =>
            {
                unitsFinishedMoving++;

                // QUAN TRỌNG: Quay mặt về phía Camera (hoặc theo hướng Anchor)
                unit.transform.rotation = winnerDisplayAnchor.rotation; // Đảm bảo Anchor quay mặt về Camera (Rotation Y = 180 thường dùng)

                // Chơi Animation chiến thắng (hoặc Idle)
                if (unit.unitBase != null) unit.unitBase.PlayAnimation("idle"); // Nếu có anim victory thì thay vào đây
            }));
        }

        // Chờ tất cả chạy đến nơi
        yield return new WaitUntil(() => unitsFinishedMoving >= unitCount);

        // Chờ thêm 1 chút xíu cho unit tạo dáng (0.5s)
        yield return new WaitForSeconds(0.5f);

        onComplete?.Invoke();
    }

    // Sửa lại hàm này để nhận tham số Star
    public void FinishMatch(int stars)
    {
        // --- 1. LƯU TIẾN ĐỘ ---
        if (currentStageData != null && PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.MapProgressModel.StageCompleted(currentStageData.stageID, stars, currentStageData.nextStageID);

            // Lưu Vàng/XP nếu cần thiết tại đây
            PlayerDataManager.Instance.CompleteStageAndSave(currentStageData.stageID, stars, currentStageData.nextStageID); // Đừng quên gọi Save
        }

        // --- 2. HIỂN THỊ UI ---
        OnGameFinish?.Invoke();

        if (WinnerCanvas.Instance != null)
        {
            if (WinnerUI != null) WinnerUI.gameObject.SetActive(true); // Bật GameObject cha nếu cần

            // Truyền số sao vào UI để hiển thị
            WinnerCanvas.Instance.SetupWinScreen(stars);

            WinnerCanvas.Instance.nextButton.gameObject.SetActive(true);
            WinnerCanvas.Instance.ActivateWinScreen();

            // Gọi trao thưởng cuối cùng
            if (RewardManager.Instance != null) RewardManager.Instance.FinalizeReward();
        }
    }
}
