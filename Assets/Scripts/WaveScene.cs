using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Cinemachine; // Cần thiết cho Action

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
/// <summary>
/// Quản lý cấp cao nhất của màn chơi.
/// Chịu trách nhiệm về tiến trình qua các "Stage" (đợt),
/// vị trí và sự di chuyển của các đơn vị giữa các stage,
/// và ra lệnh cho BattleHandler bắt đầu trận chiến.
/// </summary>
public class WaveScene : MonoBehaviour
{
    #region Singleton
    public static WaveScene Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
    [Header("Camera Settings")]
    [Tooltip("Tốc độ di chuyển của camera giữa các stage, có thể điều chỉnh trong Inspector")]
    public CinemachineCamera cinemachineCamera;
    public Transform[] teamFollowTarget; // Vị trí mục tiêu để camera di chuyển đến
    [Header("Component References")]
    [Tooltip("Kéo GameObject chứa script BattleHandler vào đây")]
    [SerializeField] private BattleHandler battleHandler;

    [Header("Stage & Movement Settings")]
    [Tooltip("Kéo các GameObject cha của từng Stage (Stage1, Stage2,...) vào đây theo đúng thứ tự")]
    public List<StageSetup> stageSetups;

    [Header("Unit Prefab Mapping")]
    [Tooltip("Kéo Prefab của Unit người chơi vào đây")]
    public List<UnitPrefabMapping> unitPrefabMapping;

    [Header("Party & Enemy Management")]
    [Tooltip("Kéo các GameObject Unit của người chơi vào đây từ Hierarchy")]
    public List<UnitController> playerUnits;
    [Tooltip("Danh sách kẻ địch đang active, sẽ được cập nhật tự động")]
    public List<UnitController> activeEnemies;
    [Tooltip("Kéo Prefab của các loại kẻ địch vào đây")]
    public List<GameObject> enemyPrefabs; // Cho phép có nhiều loại kẻ địch

    [Header("UI & Game State")]
    [Tooltip("Kéo Panel UI chiến thắng vào đây")]
    public GameObject WinnerUI;
    private bool isGameFinished = false;

    // Biến trạng thái nội bộ
    private int currentStageIndex = -1;
    private bool isAdvancing = false; // Cờ trạng thái để đảm bảo không gọi wave mới khi đang di chuyển

    void Start()
    {
        Debug.Log("WaveScene: Ra lệnh cho BattleHandler bắt đầu trận đấu."); // Đảm bảo dòng này có
        Transform playerTeamAnchor = stageSetups[0].stageTemplate.playerTeamAnchor; // Lấy vị trí của Stage đầu tiên để camera theo dõi
        SpawnPlayer(); // Spawn các tướng người chơi vào vị trí đã chỉ định
        if (battleHandler != null)
        {
            battleHandler.InitializeBattle(this.playerUnits);
        }
        // Bắt đầu màn chơi bằng cách vào Stage đầu tiên
        StartNextStage();
    }

    void Update()
    {
        // Nếu game đã kết thúc hoặc đang trong quá trình chuyển stage, không làm gì cả
        if (isGameFinished || isAdvancing) return;

        // Dọn dẹp những kẻ địch đã bị phá hủy (null) ra khỏi danh sách
        activeEnemies.RemoveAll(enemy => enemy == null || enemy.isDestroyed);

        // Nếu không còn kẻ địch nào trên màn
        if (activeEnemies.Count == 0)
        {
            // Kiểm tra xem đây có phải là stage cuối cùng không
            if (currentStageIndex >= stageSetups.Count - 1)
            {
                // Đã hoàn thành stage cuối cùng -> CHIẾN THẮNG
                isGameFinished = true;
                DisplayWinnerUI();
            }
            else
            {
                // Còn stage tiếp theo, bắt đầu quá trình chuyển stage
                StartNextStage();
            }
        }
    }

    /// <summary>
    /// Bắt đầu quá trình chuyển sang stage tiếp theo.
    /// </summary>
    private void StartNextStage()
    {
        isAdvancing = true;
        currentStageIndex++;
        cinemachineCamera.Follow = teamFollowTarget[currentStageIndex]; // Cập nhật camera theo dõi vị trí kết thúc stage
        Debug.Log($"<color=yellow>Bắt đầu quá trình chuyển sang Stage {currentStageIndex + 1}</color>");
        StartCoroutine(AdvanceToNextStageCoroutine());
        WaveText.Instance.UpdateWaveText(currentStageIndex + 1, stageSetups.Count);
    }

    void SpawnPlayer()
    {
        playerUnits.Clear(); // Xóa danh sách cũ
        // Lấy danh sách tướng từ FormationManager
        UnitSO[] formation = FormationManager.Instance.selectedFormation;
        if(formation == null)
        {
            Debug.LogError("FormationManager chưa có đội hình nào được chọn!");
            return;
        }
        Transform playerAnchor = stageSetups[0].stageTemplate.playerTeamAnchor;
        if (playerAnchor == null)
        {
            Debug.LogError("Không tìm thấy 'PlayerTeamAnchor' trong Stage đầu tiên!");
            return;
        }
        for(int i = 0;  i < formation.Length; i++)
        {
            if (formation[i] != null)
            {
                UnitSO unitToSpawn = formation[i];
                UnitPrefabMapping mapping = this.unitPrefabMapping.Find(mapping => mapping.unitData == unitToSpawn);
                Transform spawnPoint = playerAnchor.Find($"PlayerPos{i+1}"); // Lấy vị trí của Stage đầu tiên để camera theo dõi
                if(mapping != null && spawnPoint != null)
                {
                    GameObject playerPrefab = mapping.unitPrefab;
                    if (playerPrefab != null)
                    {
                        // Nếu có prefab tương ứng, spawn tại vị trí đã chỉ định
                        playerPrefab = Instantiate(playerPrefab, spawnPoint.position,Quaternion.identity);
                        UnitController playerController = playerPrefab.GetComponent<UnitController>();

                        
                        playerController.unitSO = unitToSpawn; // Gán dữ liệu tướng từ UnitSO
                        playerController.InitializeStatsFromSO(); // Giả sử có hàm Initialize để thiết lập ban đầu
                        playerController.formationSlotIndex = i;
                        playerController.SetFormationAnchor(playerAnchor, spawnPoint.position);
                        playerUnits.Add(playerController); // Thêm vào danh sách quản lý
                    }
                    else
                    {
                        Debug.LogError($"Không tìm thấy prefab cho tướng {unitToSpawn.name} tại vị trí {i + 1} trong PlayerPrefabMapping!");
                        continue; // Bỏ qua nếu không có prefab
                    }
                    Debug.Log($"<color=lightblue>Đã spawn {playerUnits.Count} tướng vào đội hình.</color>");
                }
            }
        }
    }

    /// <summary>
    /// Coroutine xử lý tuần tự các hành động: Di chuyển team -> Chờ -> Spawn kẻ địch -> Bắt đầu trận đấu.
    /// </summary>
    private IEnumerator MoveAndSetupBattle()
    {
        Debug.Log("Bắt đầu logic di chuyển và cài đặt stage mới...");

        // Chỉ thực hiện di chuyển từ stage thứ 2 trở đi
        if (currentStageIndex > 0)
        {
            // --- LẤY THÔNG TIN VỊ TRÍ STAGE MỚI ---
            StageSetup currentStageSetup = stageSetups[currentStageIndex];
            Transform playerTeamAnchor = currentStageSetup.stageTemplate.playerTeamAnchor;

            // Xử lý các stage không có kẻ địch (chỉ di chuyển)
            if (currentStageSetup.stageTemplate != null && currentStageSetup.stageTemplate.stageEndPoint != null)
            {
                if (currentStageSetup.enemiesInThisStage.Count == 0)
                {
                    // Chờ cho team di chuyển đến điểm cuối xong
                    yield return StartCoroutine(MoveTeamToPosition(currentStageSetup.stageTemplate.stageEndPoint.position));
                }
            }

            // Cập nhật vị trí anchor cho BattleHandler
            if (battleHandler != null)
            {
                battleHandler.playerTeamAnchor = playerTeamAnchor;
            }

            // --- DI CHUYỂN PLAYER ĐẾN VỊ TRÍ MỚI ---
            int playersFinishedMoving = 0;
            if (playerUnits.Count > 0 && playerTeamAnchor != null)
            {
                foreach (UnitController unit in playerUnits)
                {
                    if (unit != null)
                    {
                        int slotIndex = unit.formationSlotIndex;
                        Transform newPos = playerTeamAnchor.Find($"PlayerPos{slotIndex + 1}");
                        if (newPos != null)
                        {
                            StartCoroutine(unit.MoveCoroutineAndCallback(newPos.position, () => {
                                playersFinishedMoving++;
                            }));
                        }
                        else
                        {
                            playersFinishedMoving++;
                        }
                    }
                }
                // Chờ tất cả player di chuyển xong
                yield return new WaitUntil(() => playersFinishedMoving >= playerUnits.Count);
                Debug.Log("Tất cả người chơi đã đến vị trí mới.");
            }
        }

        // --- SPAWN KẺ ĐỊCH CỦA STAGE MỚI ---
        SpawnEnemiesForStage();
        Debug.Log("Logic di chuyển và cài đặt stage đã hoàn tất.");
    }
    private IEnumerator AdvanceToNextStageCoroutine()
    {
        // Gọi Transition Manager và truyền vào coroutine logic của chúng ta
        if (StageTransitionManager.Instance != null)
        {
            // Chờ cho cả hiệu ứng VÀ logic di chuyển/setup hoàn thành
            yield return StartCoroutine(StageTransitionManager.Instance.Transition(MoveAndSetupBattle()));
        }
        else
        {
            // Nếu không có hiệu ứng, chỉ chạy logic di chuyển/setup
            yield return StartCoroutine(MoveAndSetupBattle());
        }

        // --- SAU KHI MÀN HÌNH ĐÃ SÁNG LẠI ---
        // Bắt đầu wave chiến đấu mới
        if (battleHandler != null)
        {
            battleHandler.StartWave(this.activeEnemies);
        }

        isAdvancing = false;
        Debug.Log($"<color=green>Sẵn sàng chiến đấu tại Stage {currentStageIndex + 1}</color>");
    }


    /// <summary>
    /// Coroutine phụ trợ để di chuyển cả team đến một vị trí.
    /// </summary>
    private IEnumerator MoveTeamToPosition(Vector3 destination)
    {
        // Di chuyển mục tiêu của camera trước

        // Ra lệnh cho tất cả player di chuyển
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

        // Chờ cho đến khi tất cả player đến nơi
        yield return new WaitUntil(() => playersFinishedMoving >= playerUnits.Count);
    }
    /// <summary>
    /// Tạo ra kẻ địch tại các điểm spawn của stage hiện tại.
    /// </summary>
    private void SpawnEnemiesForStage()
    {
        activeEnemies.Clear(); // Xóa sạch danh sách kẻ địch cũ
        StageSetup currentStageSetup = stageSetups[currentStageIndex];
        Transform enemyTeamAnchor = currentStageSetup.stageTemplate.enemyTeamAnchor;
        if (enemyTeamAnchor == null)
        {
            Debug.LogError($"Không tìm thấy 'EnemyTeamAnchor' trong Stage {currentStageIndex + 1}");
            return;
        }

        List<UnitSO> enemiesToSpawn = currentStageSetup.enemiesInThisStage;
        // Vòng lặp qua các điểm spawn (EnemyPos1, EnemyPos2,...) và tạo kẻ địch
        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            UnitSO enemyData = enemiesToSpawn[i];
            if (enemyData == null) continue;

            // Tìm prefab tương ứng trong danh sách mapping chung
            UnitPrefabMapping mapping = unitPrefabMapping.Find(m => m.unitData == enemyData);
            // Tìm vị trí spawn tương ứng với thứ tự trong danh sách
            Transform spawnPoint = enemyTeamAnchor.Find($"EnemyPos{i + 1}");

            if (mapping != null && spawnPoint != null && mapping.unitPrefab != null)
            {
                GameObject enemyInstance = Instantiate(mapping.unitPrefab, spawnPoint.position, Quaternion.identity);
                UnitController enemyController = enemyInstance.GetComponent<UnitController>();

                enemyController.unitSO = enemyData;
                enemyController.InitializeStatsFromSO();
                activeEnemies.Add(enemyController);
            }
            else
            {
                Debug.LogWarning($"Không thể spawn kẻ địch {enemyData.name} tại vị trí {i + 1}. Vui lòng kiểm tra Mapping hoặc tên điểm Spawn.");
            }
        }
        Debug.Log($"Đã spawn {activeEnemies.Count} kẻ địch cho Stage {currentStageIndex + 1} theo thiết kế.");
    }

    /// <summary>
    /// Hiển thị UI chiến thắng khi hoàn thành tất cả các stage.
    /// </summary>
    private void DisplayWinnerUI()
    {
        if (WinnerUI != null)
        {
            WinnerUI.SetActive(true);
        }
        Debug.Log("<color=cyan>CHIẾN THẮNG! Đã hoàn thành tất cả các stage!</color>");
    }

    
}