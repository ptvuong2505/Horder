using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BgPauseManager – Gắn vào Canvas trong scene bg_pause.
/// Tự tìm và bind 2 nút Resume và Restart trong Start().
/// </summary>
public class BgPauseManager : MonoBehaviour
{
    void Start()
    {
        // Tím buttons theo tên object trong scene bg_pause
        GameObject resumeObj  = GameObject.Find("ResumeButton");
        GameObject restartObj = GameObject.Find("RestartButton");

        if (resumeObj != null)
        {
            Button btn = resumeObj.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnResume);
        }

        if (restartObj != null)
        {
            Button btn = restartObj.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnRestart);
        }
    }

    public void OnResume()
    {
        PauseManager pm = FindFirstObjectByType<PauseManager>();
        if (pm != null)
            pm.ResumeGame();
    }

    public void OnRestart()
    {
        PlayerPrefs.DeleteKey("FinalScore");
        PlayerPrefs.DeleteKey("FinalCoins");
        PlayerPrefs.DeleteKey("FinalScrap");
        GameManager.ClearSavedCoins();
        GunShop.ClearSavedUnlocks();
        WeaponManager.ClearSavedEquippedGun();

        Time.timeScale = 1f;
        string lastLevel = PlayerPrefs.GetString("LastLevel", "Level1");
        UnityEngine.SceneManagement.SceneManager.LoadScene(lastLevel);
    }
}
