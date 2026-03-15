using Assets.Project.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PlayerCardUI – G?n vào prefab card trong màn hình ch?n nhân v?t.
/// Prefab c?n có: Image portrait, TMP tên, TMP HP/Speed, TMP cost,
/// Button btnSelect/btnUnlock, GameObject lockOverlay.
/// </summary>
public class PlayerCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image portrait;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI costText;

    public Button cardButton;
    public Button btnUnlock;

    public GameObject lockOverlay;      // panel m? ph? lên khi ch?a m? khóa

    private PlayerData data;
    private Outline selectedOutline;

    public void Setup(PlayerData playerData)
    {
        data = playerData;

        if (portrait != null && data.portrait != null)
            portrait.sprite = data.portrait;

        if (nameText != null)
            nameText.text = data.playerName;

        if (statsText != null)
            statsText.text = $"Health: {data.maxHealth} - speed: {data.speed}";

        if (costText != null)
            costText.text = data.unlockCost > 0 ? $"Cost: {data.unlockCost}" : "Free";

        cardButton = GetComponent<Button>();
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(OnCardClicked);

        selectedOutline = GetComponent<Outline>();

        btnUnlock.onClick.RemoveAllListeners();
        btnUnlock.onClick.AddListener(OnUnlockClicked);

        Refresh();
    }

    public void Refresh()
    {
        if (data == null || PlayerSelectManager.Instance == null) return;

        bool unlocked = PlayerSelectManager.Instance.IsUnlocked(data);
        bool selected = PlayerSelectManager.Instance.IsSelected(data);

        if (lockOverlay != null) lockOverlay.SetActive(!unlocked);
        if (selectedOutline != null) selectedOutline.enabled = selected;

        btnUnlock.gameObject.SetActive(!unlocked);
    }

    void OnCardClicked()
    {
        if (!PlayerSelectManager.Instance.IsUnlocked(data))
            return;
        PlayerSelectManager.Instance.SelectPlayer(data);
        GetComponent<Outline>().enabled = true;
    }

    void OnUnlockClicked()
    {
        bool success = PlayerSelectManager.Instance.TryUnlock(data);
        if (!success)
            Debug.Log("[PlayerCardUI] Không ?? vàng!");
        // Refresh s? ???c g?i qua event OnSaveChanged trong PlayerSelectUI
    }
}
