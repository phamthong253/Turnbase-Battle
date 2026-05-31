using TMPro;
using UnityEngine;

public class MainSceneUIManager : MonoBehaviour
{
    public TextMeshProUGUI playerLevel;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI playerIdText;
    public TextMeshProUGUI expText;
    public TextMeshProUGUI crystalsText;
    public TextMeshProUGUI staminaText;
    public int playerId = 2;

    private void Start()
    {
        LoadPlayerData();
    }

    public void LoadPlayerData()
    {
        playerLevel.text = "Loading...";
        playerName.text = "Loading...";
        playerIdText.text = "Loading...";

        APIManager.Instance.GetPlayer(playerId, (playerData =>
        {
            PlayerDataManager.Instance.ApplyPlayerProfile(playerData);

            playerLevel.text = "Player Level: " + playerData.level;
            playerName.text = "Name: " + playerData.displayName;
            playerIdText.text = "ID: " + playerData.id;
            expText.text = "EXP: " + playerData.exp;
            crystalsText.text = " " + playerData.crystals;
            staminaText.text = "Stamina: " + playerData.stamina;

            PlayerDataManager.Instance.FetchAndMatchUnitsFromServer(playerData.id);
            PlayerDataManager.Instance.FetchAndMatchItemsFromServer(playerData.id);
            PlayerDataManager.Instance.LoadMapProgressFromServer();
        }), (error =>
        {
            playerLevel.text = "Error";
            playerName.text = error;
            playerIdText.text = "error";
            expText.text = "";
            crystalsText.text = "";
            staminaText.text = "";
        }));
    }
}
