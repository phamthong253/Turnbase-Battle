    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Unity.VisualScripting;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Random = UnityEngine.Random;

    public enum BattleState
    {
        START,
        PLAYERTURN,
        ENEMYTURN,
        NEXTWAVE,
        WON,
        LOST
    }
    public class BattleHandler : MonoBehaviour
    {
        public GameObject playerPrefab;
        public GameObject[] enemyPrefab;
        private List<UnitController> listEnemy = new List<UnitController>();
        private List<BattleHUD> listEnemyHUD = new List<BattleHUD>();

        GameObject player, enemy;

        public Transform playerBattleStation, enemyBattleStation;

        public BattleState state;
        public BattleHUD playerHUD;
        public BattleHUD enemyHUD;
        UnitController playerController, enemyController;

        private void Start()
        {
            state = BattleState.START;
            StartCoroutine(SetupBattle());
        }
        private IEnumerator SetupBattle()
        {
            player = Instantiate(playerPrefab, playerBattleStation.position, Quaternion.identity);
            playerController = player.GetComponent<UnitController>();
            SpawnEnemy();
            yield return new WaitForSeconds(0.1f);
            StartCoroutine(playerHUD.StartHUD(playerHUD, playerController));
            for (int i = 0; i < listEnemy.Count; i++)
            {
                StartCoroutine(listEnemyHUD[i].StartHUD(listEnemyHUD[i], listEnemy[i]));
            }

            state = BattleState.PLAYERTURN;
            PlayerTurn();
        }

        private void Update()
        {
            switch (state)
            {
                case BattleState.START:
                    break;
                case BattleState.PLAYERTURN:
                    break;
                case BattleState.ENEMYTURN:
                    break;
                case BattleState.NEXTWAVE:
                    break;
                case BattleState.WON:
                    break;
                case BattleState.LOST:
                    break;
            }
        }
    void SpawnEnemy()
    {
        listEnemyHUD.Clear();
        listEnemy.Clear();

        int enemyCount = WaveScene.Instance.GetEnemyCountForWave();
        WaveScene.Instance.enemySpawnedSoFar += enemyCount; // Cập nhật số lượng enemy đã spawn
        if (enemyCount <= 0) return;

        float spacingX = 6f, spacingY = 5f;
        int numRows = Mathf.CeilToInt(enemyCount / 2f);
        int enemySpawned = 0;

        for (int row = 0; row < numRows && enemySpawned < enemyCount; row++)
        {
            int enemiesInRow = Mathf.Min(2, enemyCount - enemySpawned);
            float startX = -((enemiesInRow - 1) * spacingX) / 2;

            for (int col = 0; col < enemiesInRow && enemySpawned < enemyCount; col++)
            {
                // Tính toán vị trí
                Vector3 enemyPos = enemyBattleStation.position + new Vector3(startX + col * spacingX, -row * spacingY, -1);

                // Tạo enemy và gán vào danh sách
                GameObject enemyGO = Instantiate(enemyPrefab[Random.Range(0, enemyPrefab.Length)], enemyPos, Quaternion.identity);
                UnitController enemyUnit = enemyGO.GetComponent<UnitController>();
                listEnemy.Add(enemyUnit);

                // Tạo HUD
                BattleHUD newEnemyHUD = Instantiate(enemyHUD);
                newEnemyHUD.isEnemyHUD = true;
                newEnemyHUD.transform.SetParent(GameObject.Find("Canvas").transform, false);
                newEnemyHUD.StartHUD(newEnemyHUD, enemyUnit);
                newEnemyHUD.SetTarget(enemyUnit.transform);
                listEnemyHUD.Add(newEnemyHUD);

                enemySpawned++;
            }
        }

        // Đặt enemyController mặc định (nếu có ít nhất 1 enemy)
        if (listEnemy.Count > 0)
        {
            enemyController = listEnemy[0];
        }
    }


    void PlayerTurn()
        {
            state = BattleState.PLAYERTURN;

            for (int i = 0; i < listEnemy.Count; i++)
            {
                listEnemyHUD[i].UpdateHUD(listEnemy[i]);
            }
            playerController.AttackTurn(enemyController, () =>
            {
                if (enemyController.currentHealth <= 0)
                {
                    int index = listEnemy.IndexOf(enemyController);
                    if (index >= 0)
                    {
                        Destroy(listEnemyHUD[index].gameObject); // Xóa enemyHUD tương ứng
                        listEnemyHUD.RemoveAt(index); // Xóa enemyHUD tương ứng
                        listEnemy.Remove(enemyController);  // Xóa enemy đã chết khỏi danh sách
                    }
                    enemyController.Delete();
                    WaveScene.Instance.enemyKilled++; // Cập nhật số lượng enemy đã bị tiêu diệt
                    Debug.Log("Enemy killed: " + WaveScene.Instance.enemyKilled);
                    if (listEnemy.Count == 0)
                    {
                        if (WaveScene.Instance.enemyKilled >= WaveScene.Instance.totalEnemyInPool)
                        {
                            state = BattleState.WON;
                            EndBattle();
                        }
                        else { 
                        state = BattleState.NEXTWAVE;
                        WaveScene.Instance.NextWave();
                        }
                        return;
                    }
                    else
                    {
                        enemyController = listEnemy[0]; // Chọn enemy tiếp theo làm mục tiêu
                    }
                }
                state = BattleState.ENEMYTURN;
                StartCoroutine(EnemyTurn());
            });
        }
        IEnumerator EnemyTurn()
        {
            Debug.Log("Enemy turn");
            foreach (UnitController enemy in listEnemy)
            {
                if (enemy.currentHealth > 0) // Kiểm tra nếu enemy còn sống mới tấn công
                {
                    bool isTurnFinished = false;
                    enemy.AttackTurn(playerController, () =>
                    {
                        isTurnFinished = true;
                        // Cập nhật HUD sau mỗi lượt tấn công
                        playerHUD.UpdateHUD(playerController);
                    });
                    yield return new WaitUntil(() => isTurnFinished);
                }
            }

            if (playerController.currentHealth <= 0)
            {
                state = BattleState.LOST;
                playerController.Delete();
                EndBattle();
            }
            else
            {
                // Cập nhật toàn bộ danh sách enemy HUD sau lượt tấn công
                for (int i = 0; i < listEnemy.Count; i++)
                {
                    listEnemyHUD[i].UpdateHUD(listEnemy[i]);
                }


                state = BattleState.PLAYERTURN;
                PlayerTurn();
            }
        }
            void EndBattle()
        {
            if (state == BattleState.WON)
            {
                Debug.Log("You won the battle!");
                WaveScene.Instance.DisplayWinnerUI();

        }
            else if (state == BattleState.LOST)
            {
                Debug.Log("You lost the battle!");
            }
        }
    }