using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    // Gắn vào Button Play Again
    public void OnPlayAgain()
    {
        SceneManager.LoadScene("Level1");
    }

    // Gắn vào Button Back to Menu
    public void OnBackToMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}
