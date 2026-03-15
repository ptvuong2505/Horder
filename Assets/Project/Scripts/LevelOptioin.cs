using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelOptioin : MonoBehaviour
{
    const string MenuSceneName = "Menu";
    const string MenuPanelName = "Menu";
    const string PlayButtonName = "Play_Button";
    const string ContinueButtonName = "Continue_Button";
    const float ContinueButtonOffsetY = -96f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SetupMenuButtonsOnLoad()
    {
        if (SceneManager.GetActiveScene().name != MenuSceneName)
            return;

        LevelOptioin levelOption = FindSceneComponent<LevelOptioin>();
        if (levelOption == null)
            return;

        levelOption.SetupMenuButtons();
    }

    public void OpenLevel(int levelId)
    {
        string levelName = "Level" + levelId;
        SceneManager.LoadScene(levelName);
    }

    public void ResetGameAndOpenLevel(int levelId)
    {
        PlayerPrefs.DeleteKey("FinalScore");
        PlayerPrefs.DeleteKey("FinalCoins");
        GameManager.ClearSavedCoins();
        GunShop.ClearSavedUnlocks();
        WeaponManager.ClearSavedEquippedGun();
        OpenLevel(levelId);
    }

    public void ResetGameOnly()
    {
        PlayerPrefs.DeleteKey("FinalScore");
        PlayerPrefs.DeleteKey("FinalCoins");
        GameManager.ClearSavedCoins();
        GunShop.ClearSavedUnlocks();
        WeaponManager.ClearSavedEquippedGun();
    }

    void SetupMenuButtons()
    {
        GameObject menuPanel = FindSceneObject(MenuPanelName);
        if (menuPanel == null)
            return;

        Button newGameButton = FindButton(menuPanel.transform, PlayButtonName);
        if (newGameButton == null)
            return;

        Button continueButton = FindButton(menuPanel.transform, ContinueButtonName);
        if (continueButton == null)
        {
            GameObject continueGO = Instantiate(newGameButton.gameObject, newGameButton.transform.parent);
            continueGO.name = ContinueButtonName;
            continueButton = continueGO.GetComponent<Button>();

            RectTransform continueRect = continueGO.GetComponent<RectTransform>();
            continueRect.anchoredPosition += new Vector2(0f, ContinueButtonOffsetY);
            continueGO.transform.SetSiblingIndex(newGameButton.transform.GetSiblingIndex() + 1);
        }

        SetButtonLabel(newGameButton, "New Game");
        SetButtonLabel(continueButton, "Continue");

        newGameButton.onClick.AddListener(ResetGameOnly);
    }

    static void SetButtonLabel(Button button, string label)
    {
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text == null)
            return;

        text.text = label;
        text.fontSize = 28f;
        text.enableAutoSizing = false;
    }

    static Button FindButton(Transform root, string buttonName)
    {
        Transform found = FindChildRecursive(root, buttonName);
        return found != null ? found.GetComponent<Button>() : null;
    }

    static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    static T FindSceneComponent<T>() where T : Component
    {
        foreach (T component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (component == null)
                continue;

            if (component.gameObject.scene == SceneManager.GetActiveScene())
                return component;
        }

        return null;
    }

    static GameObject FindSceneObject(string objectName)
    {
        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject == null)
                continue;

            if (gameObject.name == objectName && gameObject.scene == SceneManager.GetActiveScene())
                return gameObject;
        }

        return null;
    }
}
