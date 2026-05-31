using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;
using UnityEngine.Playables;
using static UnityEngine.EventSystems.EventTrigger;

public class BattleHandler : MonoBehaviour
{
    public Transform teamFollowTarget;
    // Singleton instance
    [Header("UI Prefabs & References")]
    [Tooltip("Kéo Prefab của BattleHUD dành cho Enemy vào đây")]
    public GameObject enemyHUDPrefab;
    [Tooltip("Kéo Prefab của BattleHUD dành cho Player vào đây")]
    public GameObject playerHUDPrefab; // Đổi thành Prefab để có thể tạo cho nhiều player
    public GameObject summonHUDPrefab; // Prefab dành cho SummonUnit, nếu cần thiết
    public Transform playerHUDContainer;
    public GameObject damagePopup;
    public Dictionary<UnitController, BattleHUD> hudMap = new Dictionary<UnitController, BattleHUD>();

    // Danh sách các unit đang tham chiến
    private List<UnitController> playerTeam;
    private List<UnitController> enemyTeam;
    private List<BattleHUD> activePlayerHUDs = new List<BattleHUD>();
    private List<BattleHUD> activeEnemyHUDs = new List<BattleHUD>();
    private bool isBattleActive = false;
    private bool isPlayerHudInitialized = false;

    // Giữ lại danh sách này để có thể quản lý và dọn dẹp HUD khi hết wave
    public Transform playerTeamAnchor;
    public float teamSpeed = 15f; // Tốc độ di chuyển của team player
    public float engageDistance = 5f; // Khoảng cách để các unit của player có thể tham gia trận chiến
    public bool isTeamAdvance = false;
    /// <summary>
    /// Được gọi bởi WaveScene để bắt đầu một trận chiến mới.
    /// </summary>
    /// <param name="players">Danh sách unit của người chơi.</param>
    /// <param name="enemies">Danh sách unit của kẻ địch vừa được spawn.</param>
    /// 
    // --- CÁC HÀM ĐIỀU KHIỂN CHÍNH ---

    /// <summary>
    /// Được gọi MỘT LẦN bởi WaveScene khi bắt đầu màn chơi.
    /// </summary>
    public void InitializeBattle(List<UnitController> players)
    {
        if(isPlayerHudInitialized) return; // Tránh khởi tạo lại nếu đã có
        this.playerTeam = players;
        foreach(var player in playerTeam)
        {
            // --- SỬA LỖI TẠI ĐÂY ---
            // 1. Tạm thời load UnitSO có sẵn trên nhân vật (hoặc load từ Resources)
            UnitSO baseSO = player.configSO; // (nếu bạn chưa xóa biến này) 
                                                                          // Hoặc: UnitSO baseSO = Resources.Load<UnitSO>("Path/To/Your/UnitSO");

            // 2. Giả lập dữ liệu động từ DB (Node.js)
            PlayerUnitData mockDBData = new PlayerUnitData(baseSO.unitID)
            {
                Name = baseSO.name,
                Level = 10,
                Rank = 2
            };

            // 3. Khởi tạo Lõi dữ liệu
            RuntimeUnit runtimeData = new RuntimeUnit(baseSO, mockDBData);

            // 4. Bơm vào Controller
            player.SetupUnit(runtimeData);
        }
        SetupPlayerHUDs(); // Chỉ tạo HUD cho player ở đây
        isPlayerHudInitialized = true; // Đánh dấu là đã khởi tạo HUD cho player
    }
    /// <summary>
    /// Được gọi MỖI KHI có một wave mới bắt đầu.
    /// </summary>
    public void StartWave(List<UnitController> enemies)
    {
        this.enemyTeam = new List<UnitController>(enemies);

        ClearEnemyHUDs();   // Dọn dẹp HUD của enemy từ wave trước
        SetupEnemyHUDs();   // Tạo HUD mới cho enemy của wave này

        // --- SỬA LỖI TẠI ĐÂY ---
        // 1. Kiểm tra playerTeam có tồn tại không
        if (this.playerTeam == null)
        {
            Debug.LogError("StartWave: playerTeam chưa được khởi tạo! Hãy gọi InitializeBattle trước.");
            return;
        }

        // 2. Kiểm tra playerTeamAnchor
        if (this.playerTeamAnchor == null)
        {
            Debug.LogWarning("StartWave: playerTeamAnchor bị null. Unit sẽ không có điểm neo để di chuyển.");
        }

        // Duyệt danh sách an toàn
        for (int i = playerTeam.Count - 1; i >= 0; i--)
        {
            var player = playerTeam[i];

            // 3. Kiểm tra từng unit player
            if (player == null)
            {
                // Nếu unit bị null, xóa khỏi danh sách để tránh lỗi
                playerTeam.RemoveAt(i);
                continue;
            }

            // Gọi hàm an toàn
            if (this.playerTeamAnchor != null)
            {
                player.UpdateTeamAnchor(this.playerTeamAnchor);
            }
        }
        isBattleActive = true;

    }

    // <summary>
    /// Chỉ tạo và thiết lập HUD cho đội người chơi.
    /// </summary>
    private void SetupPlayerHUDs()
    {
        if (playerHUDPrefab == null || playerHUDContainer == null) return;
        activePlayerHUDs.Clear(); // Dọn dẹp container trước khi thêm HUD mới
        foreach (var player in playerTeam)
        {
            GameObject hudObj = Instantiate(playerHUDPrefab, playerHUDContainer);
            BattleHUD newPlayerHUD = hudObj.GetComponent<BattleHUD>();
            SkillButtonHandler skillButtonHandler = hudObj.GetComponent<SkillButtonHandler>();
            if (newPlayerHUD != null)
            {
                newPlayerHUD.isEnemyHUD = false; // Đảm bảo HUD này biết nó là của Player
                newPlayerHUD.LinkToUnit(player, this.damagePopup);
                skillButtonHandler.LinkToSkill(player); // Liên kết SkillButtonHandler với UnitController
                activePlayerHUDs.Add(newPlayerHUD);
            }
            else
            {
                Debug.Log("PlayerHUD không tìm thấy BattleHUD component trong prefab!");
            }
        }
    }

    /// <summary>
    /// Chỉ tạo và thiết lập HUD cho đội kẻ địch.
    /// </summary>
    private void SetupEnemyHUDs()
    {
        foreach (var enemy in enemyTeam)
        {
            GameObject hudObj = Instantiate(enemyHUDPrefab);
            BattleHUD newEnemyHUD = hudObj.GetComponent<BattleHUD>();
            newEnemyHUD.transform.SetParent(GameObject.Find("Canvas").transform, false); // Đặt HUD vào Canvas chính
            if (newEnemyHUD != null)
            {
                newEnemyHUD.isEnemyHUD = true; // Đảm bảo HUD này biết nó là của Enemy
                newEnemyHUD.LinkToUnit(enemy, this.damagePopup);
                newEnemyHUD.SetTargetToFollow(enemy.transform);
                activeEnemyHUDs.Add(newEnemyHUD);
            }
        }
    }
    public void RemovePlayerHUDs() { 
        foreach (var hud in activePlayerHUDs)
        {
            if (hud != null) Destroy(hud.gameObject);
        }
        activePlayerHUDs.Clear();
        isPlayerHudInitialized = false; // Cho phép khởi tạo lại HUD cho player trong tương lai
    }

    private void Update()
    {
        if (!isBattleActive)
        {
            return; // Không làm gì nếu trận đấu không hoạt động
        }
        RemoveDeadUnits(playerTeam);
        RemoveDeadUnits(enemyTeam); // Loại bỏ các unit đã chết khỏi danh sách
        // Kiểm tra điều kiện kết thúc trận đấu
        if (enemyTeam.Count == 0 || playerTeam.Count == 0)
        {
            isBattleActive = false; // Kết thúc trận đấu nếu không còn kẻ địch hoặc người chơi
            return;
        }
        AssignTarget(playerTeam, enemyTeam);
        AssignTarget(enemyTeam, playerTeam);
    }

    private void OnEnable()
    {
        UnitController.OnUnitDestroyed += HandleUnitDestroyed;
    }
    private void OnDisable()
    {
        UnitController.OnUnitDestroyed -= HandleUnitDestroyed;
    }

    public void HandleUnitDestroyed(UnitController unit)
    {
        if(!unit.isPlayerUnit && unit.UnitData.BaseData.attackType == UnitSO.AttackType.Melee)
        {
            StartCoroutine(CheckAdvanceFrontLine());
        }
    }
    /// Coroutine này sẽ kiểm tra và cập nhật vị trí tiến lên của đội hình player
    private IEnumerator CheckAdvanceFrontLine()
    {
        if(isTeamAdvance) yield break; // Nếu đã đang tiến lên, không làm gì cả
        isTeamAdvance = true; // Đánh dấu là đang tiến lên
        yield return null;
        // Tính toán khoảng cách từ vị trí hiện tại của đội hình đến vị trí của kẻ địch gần nhất
        UnitController newTargetClosest = FindClosestOpponent(playerTeamAnchor, enemyTeam);

        if (newTargetClosest == null)
        {
            isTeamAdvance = false; // Kết thúc quá trình tiến lên
            yield break; // Kết thúc Coroutine nếu không có kẻ địch
        }
        // Tính toán khoảng cách cần di chuyển
        Vector3 enemyClosestPos = newTargetClosest.transform.position;
        Vector3 desiredPlayerPos = enemyClosestPos - new Vector3(engageDistance, 0, 0); // Giả sử đội hình player sẽ đứng ngang hàng với kẻ địch
        // Di chuyển đội hình player về vị trí mới
        while (Vector3.Distance(playerTeamAnchor.position, desiredPlayerPos) > 0.1f)
        {
            
            playerTeamAnchor.position = Vector3.MoveTowards(playerTeamAnchor.position, desiredPlayerPos, teamSpeed * Time.deltaTime);
            yield return null; // Chờ frame tiếp theo
        }
        isTeamAdvance = false; // Kết thúc quá trình tiến lên
    }
    private void AssignTarget(List<UnitController> allies, List<UnitController> opponent) {
        foreach (var unit in allies)
        {
            if(unit.GetCurrentTarget() == null || unit.GetCurrentTarget().UnitData.IsDead)
            {
                // Tìm kiếm kẻ địch còn sống để gán làm mục tiêu
                UnitController newTarget = FindClosestOpponent(unit.transform, opponent);
                unit.SetTarget(newTarget);
            }
        }
    }
    UnitController FindClosestOpponent(Transform attacker,List<UnitController> opponents) {
        UnitController closest = null;
        float minDistance = float.MaxValue;
        foreach (var opponent in opponents)
        {
            // Chỉ xem xét những đối thủ còn sống và tồn tại
            if (opponent != null && !opponent.isDestroyed)
            {
                // Tính khoảng cách từ vị trí "fromTransform" được truyền vào
                float distance = Vector3.Distance(attacker.position, opponent.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = opponent;
                }
            }
        }
        return closest; // Trả về kẻ địch gần nhất còn sống
    }
    /// <summary>
    /// Được gọi khi trận đấu kết thúc (ví dụ bởi WaveScene) để ẩn và dọn dẹp UI.
    /// </summary>
    

    /// <summary>
    /// Tạo và thiết lập HUD cho tất cả kẻ địch trong wave.
    /// </summary>

    /// <summary>
    /// Phá hủy tất cả HUD của kẻ địch từ wave trước.
    /// </summary>
    private void ClearEnemyHUDs()
    {
        foreach (var hud in activeEnemyHUDs)
        {
            if (hud != null) Destroy(hud.gameObject);
        }
        activeEnemyHUDs.Clear();
    }

    private void RemoveDeadUnits(List<UnitController> unitList)
    {
        // Tạo danh sách các unit đã chết hoặc null
        var deadUnit = unitList.Where(u => u == null || u.isDestroyed).ToList();
        //Lặp qua các unit đã chết và loại bỏ chúng khỏi danh sách
        foreach (var unit in deadUnit)
        {
            if (hudMap.ContainsKey(unit))
            {
                if(hudMap[unit] != null)
                {
                    Destroy(hudMap[unit].gameObject); // Phá hủy HUD tương ứng
                }
                hudMap.Remove(unit); // Loại bỏ khỏi bản đồ HUD
            }
        }
        unitList.RemoveAll(u => u == null || u.isDestroyed); // Loại bỏ các unit đã chết khỏi danh sách
    }

    public List<UnitController> GetOpponentListFor(UnitController unit)
    {
        return unit.isPlayerUnit ? enemyTeam : playerTeam;
    }
    public List<UnitController> GetAlliedListFor(UnitController unit)
    {
        return unit.isPlayerUnit ? playerTeam : enemyTeam;
    }
    public void RegisterNewUnit(UnitController newUnit)
    {
        // Có thể thêm logic để xử lý khi có unit mới tham gia trận đấu
        if (newUnit == null) return;
        if (newUnit.isPlayerUnit)
        {
            playerTeam.Add(newUnit);
        }
        else
        {
            enemyTeam.Add(newUnit);
        }
        Debug.Log("BattleHandler: Đăng ký một unit mới tham gia trận đấu.");
        // ---- THAY ĐỔI LOGIC Ở ĐÂY ----
        GameObject hudPrefabToUse = null;

        if (newUnit.GetComponent<SummonUnit>() != null)
        {
            // Nếu là unit được triệu hồi, luôn dùng HUD đi theo
            // Bạn có thể tạo summonHUDPrefab riêng hoặc tái sử dụng enemyHUDPrefab
            hudPrefabToUse = summonHUDPrefab != null ? summonHUDPrefab : enemyHUDPrefab;
        }
        else
        {
            // Xử lý cho các trường hợp khác nếu cần
        }

        if (hudPrefabToUse == null)
        {
            Debug.LogError("Không có Prefab HUD phù hợp cho unit mới!");
            return;
        }
        // Tạo HUD cho unit mới
        GameObject hudObj = Instantiate(summonHUDPrefab);
        BattleHUD newSummonUnitHUD = hudObj.GetComponent<BattleHUD>();
        newSummonUnitHUD.transform.SetParent(GameObject.Find("Canvas").transform, false); // Đặt HUD vào Canvas chính
        if (newSummonUnitHUD != null)
        {
            newSummonUnitHUD.LinkToUnit(newUnit, this.damagePopup);
            newSummonUnitHUD.SetTargetToFollow(newUnit.transform); // Đặt mục tiêu theo dõi là unit mới
            SummonUnit summonUnit = newUnit.GetComponent<SummonUnit>();
            if(summonUnit != null)
            {
                summonUnit.summonUnitHUD = newSummonUnitHUD; // Liên kết HUD với SummonUnit nếu có
                Debug.Log("Đã liên kết với summonUnit HUD ok");
            }
        }
    }
    public void EndBattle()
    {
        Debug.Log("BattleHandler: Dọn dẹp UI khi kết thúc trận đấu.");
    }

}
