using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{    [Header("UI")]
    public TextMeshProUGUI finalScoreText;  // Gán Text hiển thị điểm
    public TextMeshProUGUI finalCoinsText;  // Gán Text hiển thị coins

    void Start()
    {
        // Đọc điểm & coins đã lưu từ GameManager
        int finalScore = PlayerPrefs.GetInt("FinalScore", 0);
        int finalCoins = PlayerPrefs.GetInt("FinalCoins", 0);

        if (finalScoreText != null)
            finalScoreText.text = $"Score: {finalScore}";

        if (finalCoinsText != null)
            finalCoinsText.text = $"💰 {finalCoins}";
    }

    // Gắn vào Button "Play Again"
    public void OnPlayAgain()
    {
        PlayerPrefs.DeleteKey("FinalScore");
        PlayerPrefs.DeleteKey("FinalCoins");
        SceneManager.LoadScene("Level1");
    }

    // Gắn vào Button "Back to Menu"
    public void OnBackToMenu()
    {
        PlayerPrefs.DeleteKey("FinalScore");
        PlayerPrefs.DeleteKey("FinalCoins");
        SceneManager.LoadScene("Menu");
    }
}
