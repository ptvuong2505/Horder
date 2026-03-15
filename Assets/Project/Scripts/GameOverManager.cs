using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI finalCoinsText;  // Gán Text hiển thị coins
    public TextMeshProUGUI finalScrapText;  // Gán Text hiển thị scrap (tiền trong run)

    void Start()
    {
        int finalCoins = PlayerPrefs.GetInt("FinalCoins", 0);
        int finalScrap = PlayerPrefs.GetInt("FinalScrap", 0);

        if (finalCoinsText != null)
            finalCoinsText.text = $"💰 {finalCoins}";

        if (finalScrapText != null)
            finalScrapText.text = $"🧩 {finalScrap}";
    }

    // Gắn vào Button "Play Again"
    public void OnPlayAgain()
    {
        PlayerPrefs.DeleteKey("FinalScore");
        PlayerPrefs.DeleteKey("FinalCoins");
        GameManager.ClearSavedCoins();
        GunShop.ClearSavedUnlocks();
        WeaponManager.ClearSavedEquippedGun();
        SceneManager.LoadScene("Level1");
    }

    // Gắn vào Button "Back to Menu"
    public void OnBackToMenu()
    {
        PlayerPrefs.DeleteKey("FinalCoins");
        PlayerPrefs.DeleteKey("FinalScrap");
        SceneManager.LoadScene("Menu");
    }
}
