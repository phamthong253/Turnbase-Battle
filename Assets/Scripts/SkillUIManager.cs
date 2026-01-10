using UnityEngine;
using System.Collections.Generic;

public enum CharacterID
{
    None,
    Harime,
    Necromancer,
    FallenAngel,
    Valkyrie,
    DarkOracle
}

[System.Serializable]
public class SkillUISet
{
    public CharacterID characterID;
    public GameObject skillUI_In;
    public GameObject skillUI_Out;
}

public class SkillUIManager : MonoBehaviour
{
    public static SkillUIManager Instance { get; private set; }
    public List<SkillUISet> skillUISets;
    private Dictionary<CharacterID, SkillUISet> uiDictionary = new Dictionary<CharacterID, SkillUISet>(); // Không gán new ở đây

    void Awake()
    {
        // Bắt đầu phần code của Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(this.gameObject); // Bỏ comment dòng này nếu bạn muốn Manager tồn tại qua các Scene
        }
        // Xây dựng dictionary để tra cứu nhanh
        foreach (var set in skillUISets)
        {
            if (!uiDictionary.ContainsKey(set.characterID))
            {
                uiDictionary.Add(set.characterID, set);
            }
        }
    }

    public SkillUISet GetUISet(CharacterID id)
    {
        if (uiDictionary.TryGetValue(id, out SkillUISet foundSet))
        {
            // --- LOG TÊN Ở ĐÂY ---
            string inName = foundSet.skillUI_In != null ? foundSet.skillUI_In.name : "NULL";
            string outName = foundSet.skillUI_Out != null ? foundSet.skillUI_Out.name : "NULL";

            Debug.Log($"[SkillUIManager] Đã tìm thấy Set của: {foundSet.characterID}. " +
                      $"UI_In: {inName} | UI_Out: {outName}");
            // ---------------------

            return foundSet;
        }

        Debug.LogError($"SkillUIManager: Không tìm thấy UI cho tướng có ID: {id}.");
        return null;
    }
}