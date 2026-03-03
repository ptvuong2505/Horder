using UnityEngine;

/// <summary>
/// AudioManager – Singleton quản lý toàn bộ audio trong game.
/// Gắn vào 1 GameObject trong scene đầu tiên (Menu), DontDestroyOnLoad.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource bgmSource;

    [Header("SFX Clips — Gán trong Inspector")]
    public AudioClip shootSFX;
    public AudioClip enemyHitSFX;
    public AudioClip enemyDieSFX;
    public AudioClip playerHitSFX;
    public AudioClip waveStartSFX;
    public AudioClip waveClearSFX;
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
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
        }

        sfxSource.volume = sfxVolume;
        bgmSource.volume = bgmVolume;
    }

    // ──────────────────────────────────────────────
    //  SFX
    // ──────────────────────────────────────────────
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayShoot(AudioClip overrideClip = null)
    {
        PlaySFX(overrideClip != null ? overrideClip : shootSFX);
    }

    public void PlayEnemyHit() => PlaySFX(enemyHitSFX);
    public void PlayEnemyDie() => PlaySFX(enemyDieSFX);
    public void PlayPlayerHit() => PlaySFX(playerHitSFX);
    public void PlayWaveStart() => PlaySFX(waveStartSFX);
    public void PlayWaveClear() => PlaySFX(waveClearSFX);
    public void PlayPickup() => PlaySFX(pickupSFX);
    public void PlayUpgrade() => PlaySFX(upgradeSFX);
    public void PlayExplosion() => PlaySFX(explosionSFX);
    public void PlayGameOver() => PlaySFX(gameOverSFX);

    // ──────────────────────────────────────────────
    //  BGM
    // ──────────────────────────────────────────────
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

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
            sfxSource.volume = sfxVolume;
    }

    public void SetBGMVolume(float vol)
    {
        bgmVolume = Mathf.Clamp01(vol);
        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }
}
