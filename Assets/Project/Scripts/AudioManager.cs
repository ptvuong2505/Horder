using UnityEngine;

/// <summary>
/// AudioManager (Singleton)
/// Trách nhiệm:
/// - Cung cấp 1 nơi duy nhất để play SFX/BGM.
/// - Giữ audio xuyên scene (DontDestroyOnLoad).
/// 
/// Quy ước dùng trong code:
/// - Gameplay gọi các method dạng PlayXxx() (PlayEnemyHit, PlayWaveClear...).
/// - Với súng: AutoGun gọi PlayShoot(overrideClip) để mỗi loại súng có SFX riêng.
/// 
/// Gợi ý mở rộng:
/// - Pooling/Limit số SFX đồng thời.
/// - Fade in/out BGM khi đổi scene.
/// - Lưu volume vào PlayerPrefs.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource announceSource;
    public AudioSource bgmSource;

    [Header("SFX Clips — Gán trong Inspector")]
    public AudioClip shootSFX;
    public AudioClip enemyHitSFX;
    public AudioClip enemyDieSFX;
    public AudioClip playerHitSFX;
    public AudioClip waveStartSFX;
    public AudioClip waveClearSFX;
    public AudioClip levelClearSFX;
    public AudioClip pickupSFX;
    public AudioClip upgradeSFX;
    public AudioClip explosionSFX;
    public AudioClip gameOverSFX;

    [Header("BGM Clips")]
    public AudioClip menuBGM;
    public AudioClip gameplayBGM;

    [Header("Settings")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    [Header("Wave SFX Mix")]
    [Range(0f, 2f)] public float waveStartVolumeMultiplier = 1.25f;
    [Range(0f, 2f)] public float waveClearVolumeMultiplier = 1.25f;

    [Header("Announce Priority")]
    public bool duckGameplaySfxDuringAnnounce = true;
    [Range(0f, 1f)] public float gameplayDuckMultiplier = 0.35f;
    [Range(0f, 2f)] public float announceDuckDuration = 0.8f;

    private float duckGameplayUntil = -1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Tự tạo AudioSource nếu chưa gán
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
        if (announceSource == null)
        {
            announceSource = gameObject.AddComponent<AudioSource>();
            announceSource.playOnAwake = false;
            announceSource.priority = 0;
        }
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
        }

        // Giữ source ở 1.0 để không bị nhân volume 2 lần khi PlayOneShot.
        sfxSource.volume = 1f;
        announceSource.volume = 1f;
        bgmSource.volume = bgmVolume;
    }

    // ──────────────────────────────────────────────
    //  SFX
    // ──────────────────────────────────────────────
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, GetEffectiveGameplaySfxVolume());
    }

    public void PlaySFX(AudioClip clip, float volumeMultiplier)
    {
        if (clip == null || sfxSource == null) return;
        float volumeScale = Mathf.Max(0f, GetEffectiveGameplaySfxVolume() * volumeMultiplier);
        sfxSource.PlayOneShot(clip, volumeScale);
    }

    public void PlayAnnounceSFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null || announceSource == null) return;
        float volumeScale = Mathf.Max(0f, sfxVolume * volumeMultiplier);
        announceSource.PlayOneShot(clip, volumeScale);

        if (duckGameplaySfxDuringAnnounce)
            duckGameplayUntil = Time.unscaledTime + announceDuckDuration;
    }

    private float GetEffectiveGameplaySfxVolume()
    {
        if (duckGameplaySfxDuringAnnounce && Time.unscaledTime < duckGameplayUntil)
            return sfxVolume * gameplayDuckMultiplier;

        return sfxVolume;
    }

    public void PlayShoot(AudioClip overrideClip = null)
    {
        PlaySFX(overrideClip != null ? overrideClip : shootSFX);
    }

    public void PlayEnemyHit() => PlaySFX(enemyHitSFX);
    public void PlayEnemyDie() => PlaySFX(enemyDieSFX);
    public void PlayPlayerHit()
    {
        Debug.Log($"[AudioManager] PlayPlayerHit | clip={playerHitSFX} | sfxVol={sfxVolume} | sfxSource={sfxSource}");
        PlaySFX(playerHitSFX);
    }
    public void PlayWaveStart() => PlayAnnounceSFX(waveStartSFX, waveStartVolumeMultiplier);
    public void PlayWaveClear() => PlayAnnounceSFX(waveClearSFX, waveClearVolumeMultiplier);
    public void PlayLevelClear() => PlayAnnounceSFX(levelClearSFX, waveClearVolumeMultiplier);
    public void PlayPickup() => PlaySFX(pickupSFX);
    public void PlayUpgrade() => PlaySFX(upgradeSFX);
    public void PlayExplosion() => PlaySFX(explosionSFX);
    public void PlayGameOver()
    {
        Debug.Log($"[AudioManager] PlayGameOver | clip={gameOverSFX} | sfxVol={sfxVolume} | sfxSource={sfxSource}");
        PlayAnnounceSFX(gameOverSFX);
    }

    // Trả về độ dài clip để GameManager chờ đúng thời gian
    public float GetGameOverClipLength() => gameOverSFX != null ? gameOverSFX.length : 2f;
    public float GetLevelClearClipLength() => levelClearSFX != null ? levelClearSFX.length : 2f;

    // ──────────────────────────────────────────────
    //  BGM
    // ──────────────────────────────────────────────
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;

        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    public void SetSFXVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        if (sfxSource != null)
            sfxSource.volume = 1f;
    }

    public void SetBGMVolume(float vol)
    {
        bgmVolume = Mathf.Clamp01(vol);
        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }
}
