using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening; // Cần thiết cho DOTween
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class UnitPrefabMapping
{
    [Header("Prefab References")]
    [Tooltip("Kéo Prefab của BattleHUD dành cho Enemy vào đây")]
    public UnitSO unitData;
    [Tooltip("Kéo BattleHUD của Player đã có sẵn trong Scene vào đây")]
    public GameObject unitPrefab;
}

[System.Serializable]
public class StageSetup
{
    [Header("Stage Settings")]
    [Tooltip("Tên của Stage, sẽ hiển thị trong UI nếu cần")]
    public string stageName;
    public StageTemplate stageTemplate;
    public List<UnitSO> enemiesInThisStage;
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

    [Header("Camera Settings")]
    public CinemachineCamera cinemachineCamera;
    public Transform[] teamFollowTarget;

    [Header("Component References")]
    [SerializeField] private BattleHandler battleHandler;

    [Header("Stage & Movement Settings")]
    public List<StageSetup> stageSetups;

    [Header("Unit Prefab Mapping")]
    public List<UnitPrefabMapping> unitPrefabMapping;

    [Header("Party & Enemy Management")]
    public List<UnitController> playerUnits;
    public List<UnitController> activeEnemies;
    public List<GameObject> enemyPrefabs;

    [Header("UI & Game State")]
    public GameObject WinnerUI;
    private bool isGameFinished = false;

    [Header("Winner Display")]
    public Transform winnerDisplayAnchor;
    [SerializeField] public float winnerHorizontalSpacing;
    [SerializeField] public WinnerCanvas winnerCanvas;
    public float moveDuration = 1.0f;
    public static event Action OnGameFinish;
    public RewardManager rewardManager;

    // Biến trạng thái nội bộ
    private int currentStageIndex = -1;
    private bool isAdvancing = false;

    [Header("Battle Start UI")]
    public BattleStartUI battleStartUI; // Script quản lý hiệu ứng chữ START

    [Header("Cinematic Settings")]
    [Tooltip("Khoảng cách unit xuất hiện so với vị trí đứng (Unit sẽ chạy từ khoảng cách này vào)")]
    public float entryOffsetDistance = 40f;
    [Tooltip("Thời gian chạy vào sân đấu")]
    public float entryDuration = 3.5f;

    void Start()
    {
        Debug.Log("WaveScene: Khởi tạo màn chơi.");
        AudioManager.Instance.PlayBattleMusic("BattleTheme1");
        RewardManager.Instance.StartNewReward();

        // THAY ĐỔI: Thay vì gọi StartNextStage ngay, ta gọi Intro Cinematic cho Wave đầu tiên
        StartCoroutine(StartIntroCinematic());
    }

    void Update()
    {
        if (isGameFinished || isAdvancing) return;

        activeEnemies.RemoveAll(enemy => enemy == null || enemy.isDestroyed);

        if (activeEnemies.Count == 0)
        {
            if (currentStageIndex >= stageSetups.Count - 1)
            {
                isGameFinished = true;
                DisplayWinnerUI();
            }
            else
            {
                StartNextStage();
            }
        }
    }

    // ======================================================================================
    // LOGIC CINEMATIC (MỚI THÊM VÀO)
    // ======================================================================================

    /// <summary>
    /// Coroutine đặc biệt chỉ chạy 1 lần lúc bắt đầu game để tạo hiệu ứng chạy vào sân
    /// </summary>
    private IEnumerator StartIntroCinematic()
    {
        isAdvancing = true;
        currentStageIndex = 0;

        // 1. Setup Camera ở vị trí stage đầu tiên
        if (cinemachineCamera != null && teamFollowTarget.Length > 0)
        {
            cinemachineCamera.Follow = teamFollowTarget[0];
        }


        // 2. Spawn Player và Enemy ở vị trí LỆCH (Off-screen)
        SpawnPlayerAtOffset();
        SpawnEnemiesAtOffset();

        yield return new WaitForSeconds(0.5f); // Chờ một chút cho hệ thống ổn định

        // 3. Ra lệnh cho TẤT CẢ chạy vào vị trí chính thức
        List<Tween> runTweens = new List<Tween>();

        // --- Xử lý Player chạy vào ---
        Transform playerAnchor = stageSetups[0].stageTemplate.playerTeamAnchor;
        foreach (UnitController unit in playerUnits)
        {
            if (unit != null)
            {
                // Animation chạy
                if (unit.unitBase != null) unit.unitBase.PlayAnimation("move");

                // Tìm vị trí đích thực sự
                Transform dest = playerAnchor.Find($"PlayerPos{unit.formationSlotIndex + 1}");
                if (dest != null)
                {
                    // DOTween di chuyển
                    Tween t = unit.transform.DOMove(dest.position, entryDuration).SetEase(Ease.OutQuad)
                        .OnComplete(() => {
                            // Đến nơi thì đứng nghỉ
                            if (unit.unitBase != null) unit.unitBase.PlayAnimation("idle");
                            unit.transform.rotation = Quaternion.Euler(0, 0, 0); // Quay mặt sang phải
                        });
                    runTweens.Add(t);
                }
            }
        }

        // --- Xử lý Enemy chạy vào ---
        Transform enemyAnchor = stageSetups[0].stageTemplate.enemyTeamAnchor;
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            UnitController unit = activeEnemies[i];
            if (unit != null)
            {
                if (unit.unitBase != null) unit.unitBase.PlayAnimation("move");

                // Tìm vị trí đích thực sự
                Transform dest = enemyAnchor.Find($"EnemyPos{i + 1}");
                if (dest != null)
                {
                    Tween t = unit.transform.DOMove(dest.position, entryDuration).SetEase(Ease.OutQuad)
                        .OnComplete(() => {
                            if (unit.unitBase != null) unit.unitBase.PlayAnimation("idle");
                            unit.transform.rotation = Quaternion.Euler(0, 180, 0); // Quay mặt sang trái
                        });
                    runTweens.Add(t);
                }
            }
        }

        // 4. Chờ tất cả chạy xong
        if (runTweens.Count > 0)
        {
            yield return runTweens[0].WaitForCompletion(); // Chỉ cần chờ 1 cái vì thời gian như nhau
        }

        // 5. Hiển thị hiệu ứng START
        if (battleStartUI != null)
        {
            Debug.Log("Hiển thị chữ START...");
            yield return battleStartUI.PlayStartSequence().WaitForCompletion();
        }

        // 6. BẮT ĐẦU TRẬN ĐẤU
        isAdvancing = false;
        WaveText.Instance.UpdateWaveText(1, stageSetups.Count);

        if (battleHandler != null)
        {
            battleHandler.playerTeamAnchor = playerAnchor;
            battleHandler.InitializeBattle(this.playerUnits);
            battleHandler.StartWave(this.activeEnemies);
        }

        Debug.Log("<color=green>INTRO HOÀN TẤT - BẮT ĐẦU WAVE 1</color>");
    }

    /// <summary>
    /// Spawn Player lùi về bên TRÁI
    /// </summary>
    private void SpawnPlayerAtOffset()
    {
        playerUnits.Clear();
        UnitSO[] formation = FormationManager.Instance.selectedFormation;
        if (formation == null) return;

        Transform playerAnchor = stageSetups[0].stageTemplate.playerTeamAnchor;

        for (int i = 0; i < formation.Length; i++)
        {
            if (formation[i] != null)
            {
                UnitPrefabMapping mapping = this.unitPrefabMapping.Find(m => m.unitData == formation[i]);
                Transform destPoint = playerAnchor.Find($"PlayerPos{i + 1}");

                if (mapping != null && destPoint != null && mapping.unitPrefab != null)
                {
                    // VỊ TRÍ SPAWN = Đích - Offset (Lùi về trái)
                    Vector3 spawnPos = destPoint.position - new Vector3(entryOffsetDistance, 0, 0);

                    GameObject p = Instantiate(mapping.unitPrefab, spawnPos, Quaternion.identity);
                    UnitController ctrl = p.GetComponent<UnitController>();

                    ctrl.unitSO = formation[i];
                    ctrl.InitializeStatsFromSO();
                    ctrl.formationSlotIndex = i;
                    ctrl.SetFormationAnchor(playerAnchor, destPoint.position);

                    playerUnits.Add(ctrl);
                }
            }
        }
    }

    /// <summary>
    /// Spawn Enemy lùi về bên PHẢI
    /// </summary>
    private void SpawnEnemiesAtOffset()
    {
        activeEnemies.Clear();
        StageSetup setup = stageSetups[0];
        Transform enemyAnchor = setup.stageTemplate.enemyTeamAnchor;

        for (int i = 0; i < setup.enemiesInThisStage.Count; i++)
        {
            UnitSO data = setup.enemiesInThisStage[i];
            UnitPrefabMapping mapping = unitPrefabMapping.Find(m => m.unitData == data);
            Transform destPoint = enemyAnchor.Find($"EnemyPos{i + 1}");

            if (mapping != null && destPoint != null && mapping.unitPrefab != null)
            {
                // VỊ TRÍ SPAWN = Đích + Offset (Lùi về phải)
                Vector3 spawnPos = destPoint.position + new Vector3(entryOffsetDistance, 0, 0);

                // Enemy quay mặt sang trái ngay từ đầu
                GameObject e = Instantiate(mapping.unitPrefab, spawnPos, Quaternion.Euler(0, 180, 0));
                UnitController ctrl = e.GetComponent<UnitController>();

                ctrl.unitSO = data;
                ctrl.InitializeStatsFromSO();
                activeEnemies.Add(ctrl);
            }
        }
    }

    // ======================================================================================
    // LOGIC CŨ (Dùng cho Wave 2, 3...)
    // ======================================================================================

    private void StartNextStage()
    {
        isAdvancing = true;
        currentStageIndex++;
        if (currentStageIndex < teamFollowTarget.Length)
            cinemachineCamera.Follow = teamFollowTarget[currentStageIndex];

        Debug.Log($"<color=yellow>Chuyển sang Stage {currentStageIndex + 1}</color>");
        StartCoroutine(AdvanceToNextStageCoroutine());
        WaveText.Instance.UpdateWaveText(currentStageIndex + 1, stageSetups.Count);
    }

    // Giữ nguyên logic cũ để dùng cho các màn sau (Wave 2, Wave 3)
    private IEnumerator AdvanceToNextStageCoroutine()
    {
        if (StageTransitionManager.Instance != null)
        {
            yield return StartCoroutine(StageTransitionManager.Instance.Transition(MoveAndSetupBattle()));
        }
        else
        {
            yield return StartCoroutine(MoveAndSetupBattle());
        }

        

        if (battleHandler != null)
        {
            battleHandler.StartWave(this.activeEnemies);
        }

        isAdvancing = false;
        Debug.Log($"<color=green>Sẵn sàng chiến đấu tại Stage {currentStageIndex + 1}</color>");
    }

    private IEnumerator MoveAndSetupBattle()
    {
        // ... (Giữ nguyên logic di chuyển giữa các stage của bạn ở đây) ...
        // Logic này không thay đổi vì nó dùng cho việc đi từ Wave 1 -> Wave 2

        if (currentStageIndex > 0)
        {
            StageSetup currentStageSetup = stageSetups[currentStageIndex];
            Transform playerTeamAnchor = currentStageSetup.stageTemplate.playerTeamAnchor;

            // Logic di chuyển camera và unit đến điểm tiếp theo
            // (Code cũ của bạn đã xử lý tốt phần này)
            if (currentStageSetup.stageTemplate?.stageEndPoint != null && currentStageSetup.enemiesInThisStage.Count == 0)
            {
                yield return StartCoroutine(MoveTeamToPosition(currentStageSetup.stageTemplate.stageEndPoint.position));
            }

            if (battleHandler != null) battleHandler.playerTeamAnchor = playerTeamAnchor;

            // Move Units
            int playersFinishedMoving = 0;
            foreach (UnitController unit in playerUnits)
            {
                if (unit != null)
                {
                    int slotIndex = unit.formationSlotIndex;
                    Transform newPos = playerTeamAnchor.Find($"PlayerPos{slotIndex + 1}");
                    if (newPos != null)
                    {
                        StartCoroutine(unit.MoveCoroutineAndCallback(newPos.position, () => { playersFinishedMoving++; }));
                    }
                    else playersFinishedMoving++;
                }
            }
            yield return new WaitUntil(() => playersFinishedMoving >= playerUnits.Count);
        }

        SpawnEnemiesForStage(); // Spawn bình thường cho các wave sau
    }

    private IEnumerator MoveTeamToPosition(Vector3 destination)
    {
        int playersFinishedMoving = 0;
        foreach (UnitController unit in playerUnits)
        {
            if (unit != null)
            {
                StartCoroutine(unit.MoveCoroutineAndCallback(destination, () => {
                    playersFinishedMoving++;
                }));
            }
        }
        yield return new WaitUntil(() => playersFinishedMoving >= playerUnits.Count);
    }

    // Spawn Enemy cho các Stage sau (Wave 2, 3...) - Spawn tại chỗ, không chạy từ xa
    private void SpawnEnemiesForStage()
    {
        activeEnemies.Clear();
        if (currentStageIndex >= stageSetups.Count) return;

        StageSetup currentStageSetup = stageSetups[currentStageIndex];
        Transform enemyTeamAnchor = currentStageSetup.stageTemplate.enemyTeamAnchor;

        List<UnitSO> enemiesToSpawn = currentStageSetup.enemiesInThisStage;
        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            UnitSO enemyData = enemiesToSpawn[i];
            if (enemyData == null) continue;

            UnitPrefabMapping mapping = unitPrefabMapping.Find(m => m.unitData == enemyData);
            Transform spawnPoint = enemyTeamAnchor.Find($"EnemyPos{i + 1}");

            if (mapping != null && spawnPoint != null && mapping.unitPrefab != null)
            {
                GameObject enemyInstance = Instantiate(mapping.unitPrefab, spawnPoint.position, Quaternion.identity);
                UnitController enemyController = enemyInstance.GetComponent<UnitController>();

                enemyController.unitSO = enemyData;
                enemyController.InitializeStatsFromSO();
                activeEnemies.Add(enemyController);
            }
        }
    }

    // ... (Giữ nguyên phần DisplayWinnerUI và MoveUnitsToWinnerScreenCoroutine) ...
    private void DisplayWinnerUI()
    {
        isAdvancing = true;
        StartCoroutine(MoveUnitsToWinnerScreenCoroutine(() =>
        {
            if (WinnerUI != null)
            {
                RewardManager.Instance.FinalizeReward();
            }
            WinnerUI.gameObject.SetActive(true);
            FinishMatch();
        }));
    }

    private IEnumerator MoveUnitsToWinnerScreenCoroutine(Action onComplete)
    {
        List<UnitController> livingUnits = playerUnits.FindAll(u => u != null && !u.isDestroyed);
        int unitCount = livingUnits.Count;
        if (unitCount == 0) { onComplete?.Invoke(); yield break; }

        float totalWidth = (unitCount - 1) * winnerHorizontalSpacing;
        float startX = -totalWidth / 2f;
        int unitsFinishedMoving = 0;

        for (int i = 0; i < unitCount; i++)
        {
            UnitController unit = livingUnits[i];
            float xOffset = startX + i * winnerHorizontalSpacing;
            Vector3 finalPosition = winnerDisplayAnchor.position + winnerDisplayAnchor.right * xOffset;

            StartCoroutine(unit.MoveCoroutineAndCallback(finalPosition, moveDuration, () =>
            {
                unitsFinishedMoving++;
                unit.transform.rotation = winnerDisplayAnchor.rotation;
                if (unit.unitBase != null) unit.unitBase.PlayAnimation("idle");
            }));
        }
        yield return new WaitUntil(() => unitsFinishedMoving >= unitCount);
        onComplete?.Invoke();
    }

    public void FinishMatch()
    {
        OnGameFinish?.Invoke();
        battleHandler?.RemovePlayerHUDs();
        if (WinnerCanvas.Instance != null)
        {
            WinnerCanvas.Instance.nextButton.gameObject.SetActive(true);
            WinnerCanvas.Instance.ActivateWinScreen();
        }
    }
}