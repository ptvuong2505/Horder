using TMPro;
using UnityEngine;

/// <summary>
/// HealthTextUI – Hiển thị HP dạng text "❤ 100 / 100" trên HUD.
/// Gắn vào Canvas > HealthTextUI object.
/// </summary>
public class HealthTextUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI healthText;

    private Player player;

    void Start()
    {
        player = FindFirstObjectByType<Player>();

        if (player != null)
        {
            player.OnHealthChanged += UpdateText;
            UpdateText(player.currentHealth, player.maxHealth);
        }
    }

    void OnDestroy()
    {
        if (player != null)
            player.OnHealthChanged -= UpdateText;
    }

    void UpdateText(int current, int max)
    {
        if (healthText != null)
            healthText.text = $"❤ {current} / {max}";
    }
}
