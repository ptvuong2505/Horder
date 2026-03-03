using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    public Player player;
    public Image healthBarFill;

    private Color originalColor;

    void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
        }

        if (healthBarFill == null)
        {
            healthBarFill = transform.Find("hp_player")?.GetComponent<Image>();
            
            if (healthBarFill == null)
            {
                healthBarFill = GetComponentInChildren<Image>(true);
            }
        }

        if (healthBarFill != null)
        {
            originalColor = healthBarFill.color;
        }

        if (player != null)
        {
            player.OnHealthChanged += UpdateHealthBar;
            UpdateHealthBar(player.currentHealth, player.maxHealth);
        }
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.OnHealthChanged -= UpdateHealthBar;
        }
    }

    void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthBarFill != null)
        {
            float fillAmount = (float)currentHealth / maxHealth;
            healthBarFill.fillAmount = Mathf.Clamp01(fillAmount);

            if (fillAmount <= 0.25f)
            {
                healthBarFill.color = Color.red;
            }
            else if (fillAmount <= 0.5f)
            {
                healthBarFill.color = Color.yellow;
            }
            else
            {
                healthBarFill.color = originalColor;
            }
        }
    }
}
