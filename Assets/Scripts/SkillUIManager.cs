using UnityEngine;
using System.Collections.Generic;

public enum CharacterID
{
    None,
    Harime,
    Necromancer,
    FallenAngel,
    Valkyrie
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
        // Kiểm tra xem dictionary có được khởi tạo chưa
        if (uiDictionary.TryGetValue(id, out SkillUISet foundSet))
        {
            return foundSet;
        }

        Debug.LogError($"SkillUIManager: Không tìm thấy UI cho tướng có ID: {id}. Hãy kiểm tra lại Inspector và ID của tướng!");
        return null;
    }
}