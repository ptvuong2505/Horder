using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tạo hiệu ứng shockwave: nhiều vòng tròn đỏ tỏa ra từ trung tâm.
/// Gắn vào cùng GameObject với BossWarningUI.
/// </summary>
public class BossWarningEffect : MonoBehaviour
{
    [Header("Ring Prefab")]
    [Tooltip("Prefab là UI Image (vòng tròn). Nếu để trống sẽ tự tạo.")]
    public GameObject ringPrefab;

    [Tooltip("Transform cha dùng để chứa các ring (để center)")]
    public RectTransform ringParent;

    [Header("Pool Settings")]
    public int ringPoolSize = 6;

    [Header("Animation Settings")]
    public float pulseInterval = 0.3f;
    public float ringDuration   = 0.85f;
    public float startScale     = 0.9f;  // ring bắt đầu từ rìa ảnh warning (400*0.9≈360)
    public float endScale       = 4.0f;

    private GameObject[] _pool;
    private bool _running;

    // ──────────────────────────────────────────────────────────────
    void Awake()
    {
        BuildPool();
    }

    void BuildPool()
    {
        // Đảm bảo có ringParent
        if (ringParent == null)
        {
            GameObject rp = new GameObject("RingParent");
            rp.transform.SetParent(transform, false);
            ringParent = rp.AddComponent<RectTransform>();
            ringParent.anchorMin = ringParent.anchorMax = new Vector2(0.5f, 0.5f);
            ringParent.sizeDelta = Vector2.zero;
        }

        _pool = new GameObject[ringPoolSize];
        for (int i = 0; i < ringPoolSize; i++)
        {
            GameObject ring;
            if (ringPrefab != null)
            {
                ring = Instantiate(ringPrefab, ringParent);
            }
            else
            {
                ring = new GameObject("Ring_" + i);
                ring.transform.SetParent(ringParent, false);
                RectTransform rt = ring.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(400f, 400f);
                Image img = ring.AddComponent<Image>();
                img.sprite = GetDefaultCircleSprite();
                img.color = new Color(1f, 0.1f, 0.05f, 0.9f);
                img.raycastTarget = false;
            }
            ring.SetActive(false);
            _pool[i] = ring;
        }
    }

    // ──────────────────────────────────────────────────────────────
    /// <summary>Bắt đầu hiệu ứng shockwave trong <paramref name="totalDuration"/> giây.</summary>
    public void PlayWarning(float totalDuration = 2f)
    {
        if (_running) return;
        StartCoroutine(PulseLoop(totalDuration));
    }

    public void StopWarning()
    {
        _running = false;
        StopAllCoroutines();
        foreach (var r in _pool) if (r) r.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────────
    IEnumerator PulseLoop(float totalDuration)
    {
        _running = true;
        float elapsed = 0f;
        int poolIndex = 0;

        while (elapsed < totalDuration)
        {
            GameObject ring = _pool[poolIndex % ringPoolSize];
            poolIndex++;
            StartCoroutine(AnimateRing(ring));

            yield return new WaitForSecondsRealtime(pulseInterval);
            elapsed += pulseInterval;
        }

        _running = false;
    }

    IEnumerator AnimateRing(GameObject ring)
    {
        ring.SetActive(true);
        RectTransform rt = ring.GetComponent<RectTransform>();
        Image img = ring.GetComponent<Image>();

        float t = 0f;
        Color startColor = new Color(1f, 0.1f, 0.05f, 1f);
        Color endColor   = new Color(1f, 0.1f, 0.05f, 0f);

        while (t < ringDuration)
        {
            float norm = t / ringDuration;
            float eased = 1f - (1f - norm) * (1f - norm); // EaseOut quad

            float scale = Mathf.Lerp(startScale, endScale, eased);
            rt.localScale = new Vector3(scale, scale, 1f);

            if (img != null)
                img.color = Color.Lerp(startColor, endColor, norm);

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        ring.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────────
    /// Tạo sprite hình vành khuyên (rỗng ở giữa) bằng code.
    static Sprite GetDefaultCircleSprite()
    {
        const int size = 128;
        const float outerR = size / 2f;
        const float innerR = outerR * 0.65f; // độ dày vành: 35% bán kính

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                // Anti-alias nhẹ ở mép ngoài và mép trong
                float alpha = Mathf.Clamp01(outerR - d) * Mathf.Clamp01(d - innerR);
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha * 8f));
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f));
    }
}
