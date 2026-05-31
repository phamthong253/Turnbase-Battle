using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class StageDataEntry
{
    public int stageID;
    public int starsEarned;
    public bool isCompleted;

    // Thêm các trường server có thể trả
    public int attempts;
    public double progress; // 0.0 - 1.0
    public DateTime? completedAt;
}

[System.Serializable]
public class MapProgressModel
{
    public List<StageDataEntry> stageList = new List<StageDataEntry>();
    public int currentMaxCompleteStageID = 101; // Mặc định bắt đầu từ stage 101

    public bool IsStageUnlock(int stageID)
    {
        if (stageID <= currentMaxCompleteStageID) return true;
        return stageList.Exists(s => s.stageID == stageID);
    }
    public bool IsStageCompleted(int stageID)
    {
        return stageList.Exists(s => s.stageID == stageID && s.isCompleted);
    }

    // số sao đã đạt được trong một stage cụ thể
    public int GetStarsForStage(int stageID)
    {
        StageDataEntry entry = stageList.Find(s => s.stageID == stageID);
        return entry != null ? entry.starsEarned : 0;
    }

    // Cập nhật trạng thái hoàn thành và số sao của một stage
    // Cập nhật trạng thái hoàn thành và số sao của một stage
    public void StageCompleted(int stageID, int stars, int nextStageID)
    {
        var currentStage = stageList.FirstOrDefault(s => s.stageID == stageID);
        if (currentStage == null)
        {
            currentStage = new StageDataEntry
            {
                stageID = stageID,
                isCompleted = true,
                starsEarned = stars,
                attempts = 1,
                progress = 1.0,
                completedAt = DateTime.UtcNow
            };
            stageList.Add(currentStage);
        }
        else
        {
            currentStage.isCompleted = true;
            currentStage.attempts += 1;
            if (stars > currentStage.starsEarned) currentStage.starsEarned = stars;
            currentStage.progress = 1.0;
            currentStage.completedAt = DateTime.UtcNow;
        }

        if (nextStageID != 0)
        {
            if (nextStageID > currentMaxCompleteStageID)
            {
                currentMaxCompleteStageID = nextStageID;
            }

            var nextStage = stageList.FirstOrDefault(s => s.stageID == nextStageID);
            if (nextStage == null)
            {
                stageList.Add(new StageDataEntry
                {
                    stageID = nextStageID,
                    isCompleted = false,
                    starsEarned = 0,
                    attempts = 0,
                    progress = 0.0,
                    completedAt = null
                });
            }
        }
    }
    public void FromServerStageList(IEnumerable<StageDataEntry> serverList)
    {
        stageList = serverList?.Select(s => new StageDataEntry
        {
            stageID = s.stageID,
            starsEarned = s.starsEarned,
            isCompleted = s.isCompleted,
            attempts = s.attempts,
            progress = s.progress,
            completedAt = s.completedAt
        }).ToList() ?? new List<StageDataEntry>();
    }
}

