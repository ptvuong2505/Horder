using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PlayerSelectUI – G?n vào root canvas c?a màn hình ch?n nhân v?t.
/// T? ??ng t?o PlayerCardUI t? prefab cho t?ng PlayerData trong registry.
/// </summary>
public class PlayerSelectUI : MonoBehaviour
{
    [Header("References")]
    public PlayerSelectManager playerSelectManager;

    [Header("Card Layout")]
    public Transform cardContainer;     // HorizontalLayoutGroup / GridLayoutGroup
    public PlayerCardUI cardPrefab;

    [Header("Gold Display")]
    public TextMeshProUGUI goldText;

    private PlayerCardUI[] cards;

    void Start()
    {
        if (playerSelectManager == null)
            playerSelectManager = PlayerSelectManager.Instance;

        SpawnCards();
        RefreshGold();

        playerSelectManager.OnSaveChanged += RefreshAll;
    }

    void OnDestroy()
    {
        if (playerSelectManager != null)
            playerSelectManager.OnSaveChanged -= RefreshAll;
    }

    void SpawnCards()
    {
        var players = playerSelectManager.playerRegistry.allPlayers;
        cards = new PlayerCardUI[players.Count];

        for (int i = 0; i < players.Count; i++)
        {
            PlayerCardUI card = Instantiate(cardPrefab, cardContainer);
            card.Setup(players[i]);
            cards[i] = card;
        }
    }

    void RefreshAll()
    {
        if (cards == null) return;
        foreach (var card in cards)
            card.Refresh();
        RefreshGold();
    }

    void RefreshGold()
    {
        if (goldText != null && playerSelectManager != null)
            goldText.text = $"Gold: {playerSelectManager.GetGold()}";
    }
}
