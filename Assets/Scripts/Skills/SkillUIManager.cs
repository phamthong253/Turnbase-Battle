using UnityEngine;
using System.Collections.Generic;

// Giữ nguyên Enum của bạn
public enum CharacterID
{
    None,
    Harime,
    Necromancer,
    FallenAngel,
    Valkyrie,
    DarkOracle
}

// Thay đổi SkillUISet: Không chứa GameObject nữa, mà chứa Sprite và thông tin
[System.Serializable]
public class SkillCutsceneData
{
    public CharacterID characterID;

    [Header("Assets")]
    public Sprite cutInSprite;      // Ảnh hiển thị lúc Cutscene (quan trọng nhất)

    [Header("Optional Settings")]
    public Color themeColor; // Màu nền (nếu muốn tùy chỉnh theo hệ)
    // public AudioClip skillVoiceLine;    // Nếu muốn thêm âm thanh nói câu thoại
}

public class SkillUIManager : MonoBehaviour
{
    public static SkillUIManager Instance { get; private set; }

    [Header("Configuration")]
    public List<SkillCutsceneData> skillDataList; // Đổi tên cho rõ nghĩa

    // Dictionary để tra cứu nhanh từ ID ra Dữ liệu
    private Dictionary<CharacterID, SkillCutsceneData> dataMap = new Dictionary<CharacterID, SkillCutsceneData>();

    void Awake()
    {
        // Singleton pattern chuẩn
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad(this.gameObject); // Tùy chọn

        // Xây dựng Dictionary
        foreach (var data in skillDataList)
        {
            if (!dataMap.ContainsKey(data.characterID))
            {
                dataMap.Add(data.characterID, data);
            }
        }
    }

    /// <summary>
    /// Hàm chính: Nhận ID nhân vật -> Tìm dữ liệu -> Gọi SkillCutsceneManager chạy hiệu ứng
    /// </summary>
    public void TriggerSkillCutscene(CharacterID id)
    {
        // 1. Tìm dữ liệu dựa trên ID
        if (dataMap.TryGetValue(id, out SkillCutsceneData data))
        {
            // Kiểm tra xem Manager hiển thị có tồn tại không
            if (SkillCutsceneManager.Instance != null)
            {
                SkillCutsceneManager.Instance.PlayCutscene(data.cutInSprite);
                // Nếu bạn muốn đổi màu nền theo hệ:
                 SkillCutsceneManager.Instance.SetThemeColor(data.themeColor);
            }
            else
            {
                Debug.LogError("SkillCutsceneManager chưa được khởi tạo trong Scene!");
            }
        }
        else
        {
            Debug.LogWarning($"[SkillUIManager] Không tìm thấy dữ liệu Cutscene cho ID: {id}");
        }
    }

    /// <summary>
    /// Hàm lấy dữ liệu thô (nếu cần dùng cho việc khác ngoài cutscene)
    /// </summary>
    public SkillCutsceneData GetData(CharacterID id)
    {
        if (dataMap.TryGetValue(id, out SkillCutsceneData data))
            return data;
        return null;
    }
}