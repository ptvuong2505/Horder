using UnityEngine;
using UnityEngine.UI;
public class BossHealthBar : MonoBehaviour
{
    [Header("UI References")]
    public Image fillImage;

    private BossEnemy currentBoss;

    void Awake()
    {
        // Ẩn ở runtime; object vẫn active trong prefab để chỉnh UI trong Editor
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Gọi từ BossEnemy.Start() khi boss vừa spawn.
    /// </summary>
    public void RegisterBoss(BossEnemy boss)
    {
        if (boss == null) return;

        if (currentBoss != null)
        {
            currentBoss.OnHealthChanged -= UpdateFill;
            currentBoss.OnBossDied -= OnBossDied;
        }

        currentBoss = boss;
        currentBoss.OnHealthChanged += UpdateFill;
        currentBoss.OnBossDied += OnBossDied;

        gameObject.SetActive(true);
        UpdateFill(boss.maxHealth, boss.maxHealth);
    }

    void UpdateFill(int current, int max)
    {
        if (fillImage == null) return;

        float pct = (float)current / Mathf.Max(1, max);
        fillImage.fillAmount = Mathf.Clamp01(pct);

        if (pct > 0.5f)
            fillImage.color = Color.green;
        else if (pct > 0.25f)
            fillImage.color = Color.yellow;
        else
            fillImage.color = Color.red;
    }

    void OnBossDied()
    {
        if (currentBoss != null)
        {
            currentBoss.OnHealthChanged -= UpdateFill;
            currentBoss.OnBossDied -= OnBossDied;
            currentBoss = null;
        }
        Invoke(nameof(HideBar), 2f);
    }

    void HideBar()
    {
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (currentBoss != null)
        {
            currentBoss.OnHealthChanged -= UpdateFill;
            currentBoss.OnBossDied -= OnBossDied;
        }
    }
}
