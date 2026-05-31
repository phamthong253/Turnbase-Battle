using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;

[System.Serializable]
public class WaveData
{
    [Header("Danh sách quái trong Wave này")]
    public List<UnitSO> enemies;
}

[CreateAssetMenu(fileName = "New Stage", menuName = "Scriptable Objects/StageSO")]
public class StageSO : ScriptableObject
{
    [Header("Thông tin ải")]
    public int stageID;         // VD: 101 (cho ải 1-1)
    public string stageName;    // VD: "Juno Plains 1-1"
    public int nextStageID;     // ID của ải tiếp theo (VD: 102). Nếu = 0 là hết map.

    //[Header("Cấu hình trận đấu")]
    //public GameObject mapPrefab; // Prefab môi trường (Cây cối, đường đất...)

    // Priconne thường có 3 Wave mỗi màn
    public List<WaveData> waves;

    [Header("Phần thưởng mỗi ngày")]
    public bool hasExpReward;
    public bool hasCrystalReward;

    [Header("Phần thưởng giới hạn")]
    public List<ItemSO> rewardItems;
}

