using DG.Tweening.Core.Easing;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapUIManager : MonoBehaviour
{
    public static MapUIManager Instance;

    [Header("Popup Info")]
    //public TextMeshProUGUI popupStaminaCost;

    public FormationPopupManager formationPopup;
    public StageInfoUI stageInfoUI;
    private void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);

        formationPopup.gameObject.SetActive(false);
    }
    public void ShowStageInfo(StageSO data)
    {
        if(stageInfoUI != null)
        {
            stageInfoUI.SetupAndShow(data);
        }
        else
        {
            Debug.LogError("StageInfoUI is not assigned in MapUIManager.");
        }
    }
    // Hàm được gọi từ StageNodeUI
    public void ShowStagePopup(StageSO data)
    {
        Debug.Log($"[MapUIManager] Đã nhận lệnh mở Popup cho ải: {(data != null ? data.name : "NULL")}");
        if (formationPopup != null)
        {
            formationPopup.OpenStagePopup(data);
        }
        else
        {
            Debug.LogError("FormationPopupManager is not assigned in MapUIManager.");
        }
    }

    public void OnClosePopup()
    {
        if(formationPopup != null)
            formationPopup.CloseStagePopup();
        else
            Debug.LogWarning("FormationPopupManager is not assigned in MapUIManager.");
    }
    public void RefreshMapNodes()
    {
        // Tìm tất cả StageNodeUI (bao gồm inactive) và gọi RefreshVisual
        StageNodeUI[] nodes = FindObjectsOfType<StageNodeUI>(true);
        if (nodes == null || nodes.Length == 0) return;

        for (int i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            if (node == null) continue;
            node.RefreshVisual();
        }
    }
}