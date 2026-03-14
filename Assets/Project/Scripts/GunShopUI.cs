using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// GunShopUI - Tu tao toan bo Canvas + UI trong Awake().
/// Chi can them component nay vao 1 empty GameObject trong scene la xong.
/// Nhan G de mo / dong kho sung.
/// </summary>
public class GunShopUI : MonoBehaviour
{
    public static GunShopUI Instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreateIfMissing()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("GunShopUI_Auto");
        go.AddComponent<GunShopUI>();
        Debug.Log("[GunShopUI] Auto-created for this run.");
    }

    public Key toggleKey = Key.G;

    private GameObject     shopPanel;
    private TextMeshProUGUI coinText;
    private TextMeshProUGUI currentWeaponHudText;
    private List<GunCardUI> cards = new List<GunCardUI>();
    private List<int>       cardGunIndices = new List<int>();
    private Transform       cardArea;   // ref to HorizontalLayout area for lazy card build
    private GameManager     boundGameManager;
    private GunShop         boundGunShop;
    private WeaponManager   boundWeaponManager;
    private bool isOpen = false;

    // Palette
    static Color CD = new Color(0f, 0f, 0f, 0.75f);
    static Color CP = new Color(0.09f, 0.12f, 0.18f, 0.97f);
    static Color CH = new Color(0.15f, 0.20f, 0.30f, 1f);
    static Color CC = new Color(0.13f, 0.17f, 0.24f, 1f);
    static Color CB = new Color(0.13f, 0.60f, 0.27f, 1f);
    static Color CO = new Color(0.27f, 0.27f, 0.32f, 1f);
    static Color CG = new Color(1f, 0.82f, 0.18f, 1f);
    static Color CGR = new Color(0.86f, 0.86f, 0.86f, 1f);
    static Color CR = new Color(0.68f, 0.16f, 0.16f, 1f);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        BuildUI();
    }

    void Start()
    {
        // Build cards now - GunShop.Instance is guaranteed to exist (WeaponManager.Awake ran first)
        if (GunShop.Instance != null && cardArea != null)
            BuildCards(cardArea);
        else
            Debug.LogWarning("[GunShopUI] GunShop.Instance is NULL in Start! Make sure WeaponManager is in the scene.");

        RebindSceneReferences();

        Debug.Log($"[GunShopUI] Ready. GunShop={GunShop.Instance}, cards={cards.Count}");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (boundGameManager != null)
            boundGameManager.OnCoinsChanged -= OnCoinsChanged;
        if (boundGunShop != null)
            boundGunShop.OnGunUnlocked -= OnGunUnlocked;
        if (boundWeaponManager != null)
            boundWeaponManager.OnWeaponChanged -= OnWeaponChanged;

        if (Instance == this)
            Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindSceneReferences();

        if (cardArea != null && GunShop.Instance != null)
            BuildCards(cardArea);

        // Ensure state is clean after scene switch
        if (isOpen)
            CloseShop();
    }

    void RebindSceneReferences()
    {
        if (boundGameManager != null)
            boundGameManager.OnCoinsChanged -= OnCoinsChanged;
        if (boundGunShop != null)
            boundGunShop.OnGunUnlocked -= OnGunUnlocked;
        if (boundWeaponManager != null)
            boundWeaponManager.OnWeaponChanged -= OnWeaponChanged;

        boundGameManager = GameManager.Instance;
        boundGunShop = GunShop.Instance;
        boundWeaponManager = FindFirstObjectByType<WeaponManager>();

        if (boundGameManager != null)
        {
            boundGameManager.OnCoinsChanged += OnCoinsChanged;
            boundGameManager.gunShopUI = this;
        }

        if (boundGunShop != null)
            boundGunShop.OnGunUnlocked += OnGunUnlocked;

        if (boundWeaponManager != null)
        {
            boundWeaponManager.OnWeaponChanged += OnWeaponChanged;
            OnWeaponChanged(boundWeaponManager.CurrentIndex, boundWeaponManager.CurrentGunData);
        }
        else
        {
            UpdateCurrentWeaponHUD(null);
        }
    }

    void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.State == GameManager.GameState.GameOver) return;

        // Hardcode gKey to avoid Key enum reset issue in Inspector
        bool pressed = Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;
        if (pressed)
        {
            Debug.Log($"[GunShopUI] G pressed! isOpen={isOpen} shopPanel={shopPanel?.name}");
            if (isOpen) CloseShop(); else OpenShop();
        }
    }

    public void OpenShop()
    {
        if (shopPanel == null) { Debug.LogError("[GunShopUI] OpenShop: shopPanel is NULL!"); return; }

        // Ensure parent hierarchy/canvas is enabled, then bring overlay to the top.
        Transform t = shopPanel.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            t = t.parent;
        }

        Canvas parentCanvas = shopPanel.GetComponentInParent<Canvas>(true);
        if (parentCanvas != null)
        {
            parentCanvas.enabled = true;
            parentCanvas.sortingOrder = Mathf.Max(parentCanvas.sortingOrder, 500);
        }

        shopPanel.transform.SetAsLastSibling();
        Debug.Log($"[GunShopUI] OpenShop called. shopPanel={shopPanel.name} active={shopPanel.activeSelf} inHierarchy={shopPanel.activeInHierarchy}");
        shopPanel.SetActive(true);
        isOpen = true;
        Time.timeScale = 0f;
        RefreshAll();
    }

    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        isOpen = false;
        Time.timeScale = 1f;
        if (GunShop.Instance != null) GunShop.Instance.CloseShop();
    }

    public void BuyGun(int index)
    {
        if (GunShop.Instance == null)
        {
            Debug.LogWarning("[GunShopUI] BuyGun failed: GunShop.Instance is null");
            return;
        }

        bool ok = GunShop.Instance.TryUnlock(index);
        Debug.Log(ok
            ? $"[GunShopUI] BuyGun success: index={index}"
            : $"[GunShopUI] BuyGun failed: index={index}");
    }

    // ── Build UI ──────────────────────────────────────────────────────────
    void BuildUI()
    {
        // Doi GunShop co san (WeaponManager.Awake tao truoc)
        // Neu chua co thi skip - se update sau
        Canvas cv = GetOrCreateCanvas();
        EnsureEventSystem();

        // shopPanel = overlay toan man hinh (KHAC voi gameObject chua GunShopUI)
        shopPanel = NewUI("GunShopOverlay", cv.transform);
        Stretch(shopPanel); Img(shopPanel, CD);
        shopPanel.SetActive(false);  // <<< GunShopUI.gameObject van active, chi shopPanel bi an

        // HUD always visible: Current weapon text (outside shop panel)
        currentWeaponHudText = TMP("CurrentWeaponHUD", cv.transform, "Weapon: -", 20, FontStyles.Bold, Color.white);
        var wRT = currentWeaponHudText.GetComponent<RectTransform>();
        wRT.anchorMin = wRT.anchorMax = new Vector2(0, 1);
        wRT.pivot = new Vector2(0, 1);
        wRT.anchoredPosition = new Vector2(24, -24);
        wRT.sizeDelta = new Vector2(520, 40);
        currentWeaponHudText.alignment = TextAlignmentOptions.MidlineLeft;

        // Panel center
        int cnt = 4; // so luong slot toi da
        float cW = 190f, cH = 280f, gap = 14f, pad = 28f;
        float W = cnt * cW + (cnt - 1) * gap + pad * 2;
        float H = cH + 135f;
        GameObject panel = NewUI("Panel", shopPanel.transform);
        Center(panel, W, H); Img(panel, CP);

        // Header
        GameObject hdr = NewUI("Header", panel.transform);
        TopBar(hdr, 80f); Img(hdr, CH);
        var ttl = TMP("Title", hdr.transform, "Gun Storage", 28, FontStyles.Bold, Color.white);
        Stretch(ttl.gameObject); ttl.alignment = TextAlignmentOptions.MidlineLeft; ttl.margin = new Vector4(16,0,0,0);
        coinText = TMP("Coin", hdr.transform, "Coin: 0", 24, FontStyles.Bold, CG);
        Stretch(coinText.gameObject); coinText.alignment = TextAlignmentOptions.MidlineRight; coinText.margin = new Vector4(0,0,14,0);

        // Hint
        var hint = TMP("Hint", panel.transform, "[G] Close Shop", 14, FontStyles.Bold, Color.white);
        var hRT = hint.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0,1); hRT.anchorMax = new Vector2(1,1);
        hRT.pivot = new Vector2(0.5f,1);
        hRT.offsetMin = new Vector2(0,-112); hRT.offsetMax = new Vector2(0,-82);
        hint.alignment = TextAlignmentOptions.Center;

        // Card area
        GameObject area = NewUI("Cards", panel.transform);
        cardArea = area.transform;
        var aRT = area.GetComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0,0); aRT.anchorMax = new Vector2(1,0);
        aRT.pivot = new Vector2(0.5f,0);
        aRT.offsetMin = new Vector2(pad, pad); aRT.offsetMax = new Vector2(-pad, pad + cH);
        var hlg = area.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = gap; hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

        // Cards built in Start() once GunShop.Instance is ready

        // Nut X
        GameObject xGO = NewUI("X", panel.transform);
        var xRT = xGO.GetComponent<RectTransform>();
        xRT.anchorMin = xRT.anchorMax = new Vector2(1,1); xRT.pivot = new Vector2(1,1);
        xRT.offsetMin = new Vector2(-48,-44); xRT.offsetMax = new Vector2(-8,-8);
        Img(xGO, CR);
        var xBtn = xGO.AddComponent<Button>(); xBtn.targetGraphic = xGO.GetComponent<Image>();
        xBtn.onClick.AddListener(CloseShop);
        var xT = TMP("L", xGO.transform, "X", 22, FontStyles.Bold, Color.white);
        Stretch(xT.gameObject); xT.alignment = TextAlignmentOptions.Center;
    }

    void BuildCards(Transform area)
    {
        if (GunShop.Instance == null) { Debug.LogWarning("[GunShopUI] BuildCards: GunShop.Instance is null"); return; }

        // Recover from incomplete GunShop data (common after scene switch / inspector mismatch).
        WeaponManager wm = FindFirstObjectByType<WeaponManager>();
        if (wm != null && wm.gunDataList != null && wm.gunDataList.Count > 0)
        {
            bool needsSync = GunShop.Instance.GunCount != wm.gunDataList.Count;
            if (!needsSync)
            {
                for (int i = 0; i < wm.gunDataList.Count; i++)
                {
                    if (wm.gunDataList[i] != null && GunShop.Instance.GetGunData(i) == null)
                    {
                        needsSync = true;
                        break;
                    }
                }
            }

            if (needsSync)
            {
                GunShop.Instance.allGuns = new List<GunData>(wm.gunDataList);
                GunShop.Instance.ReinitUnlockState();
                Debug.Log($"[GunShopUI] Synced GunShop data from WeaponManager. Count={wm.gunDataList.Count}");
            }
        }

        cards.Clear();
        cardGunIndices.Clear();
        // Destroy any leftover cards
        foreach (Transform child in area) Destroy(child.gameObject);
        int count = GunShop.Instance.GunCount;
        if (wm != null && wm.gunDataList != null)
            count = Mathf.Max(count, wm.gunDataList.Count);

        for (int i = 0; i < count; i++)
        {
            GunData d = GunShop.Instance.GetGunData(i);
            if (d == null && wm != null && i < wm.gunDataList.Count)
                d = wm.gunDataList[i];
            if (d == null) continue;
            int idx = i;
            var card = BuildCard(area, d, idx);
            cards.Add(card);
            cardGunIndices.Add(idx);
        }
        Debug.Log($"[GunShopUI] Built {cards.Count} gun cards.");
    }

    GunCardUI BuildCard(Transform parent, GunData d, int idx)
    {
        GameObject go = NewUI("Card_" + d.gunName, parent);
        Img(go, CC);
        go.AddComponent<Outline>().effectColor = new Color(0.25f,0.35f,0.5f,1f);
        var card = go.AddComponent<GunCardUI>();

        // Icon
        GameObject iGO = NewUI("Icon", go.transform);
        var iRT = iGO.GetComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0.08f,0.52f); iRT.anchorMax = new Vector2(0.92f,0.97f);
        iRT.offsetMin = iRT.offsetMax = Vector2.zero;
        var iImg = iGO.AddComponent<Image>();
        if (d.gunSprite != null) { iImg.sprite = d.gunSprite; iImg.preserveAspect = true; }
        else iImg.color = new Color(1,1,1,0.07f);
        card.gunIcon = iImg;

        // Name
        card.gunNameText = Row("N", go.transform, d.gunName, 18, FontStyles.Bold, Color.white, 0.44f, 0.52f);

        // Desc
        string desc = string.IsNullOrEmpty(d.description)
            ? "Damage: " + d.damage + " | Fire Rate: " + d.fireRate.ToString("F1") + "s" : d.description;
        var dTmp = Row("D", go.transform, desc, 12, FontStyles.Bold, Color.white, 0.34f, 0.44f);
        dTmp.textWrappingMode = TextWrappingModes.Normal;
        card.descText = dTmp;

        // Cost
        card.costText = Row("C", go.transform,
            d.unlockedByDefault ? "Free" : d.unlockCost + " Coins",
            16, FontStyles.Bold, CG, 0.25f, 0.34f);

        // Button
        GameObject bGO = NewUI("Btn", go.transform);
        var bRT = bGO.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.07f,0); bRT.anchorMax = new Vector2(0.93f,0);
        bRT.pivot = new Vector2(0.5f,0);
        bRT.offsetMin = new Vector2(0,6); bRT.offsetMax = new Vector2(0,48);
        Img(bGO, d.unlockedByDefault ? CO : CB);
        var btn = bGO.AddComponent<Button>(); btn.targetGraphic = bGO.GetComponent<Image>();
        btn.onClick.AddListener(() => BuyGun(idx));
        var bT = TMP("L", bGO.transform,
            d.unlockedByDefault ? "OWNED" : "BUY " + d.unlockCost,
            14, FontStyles.Bold, Color.white);
        Stretch(bT.gameObject); bT.alignment = TextAlignmentOptions.Center; bT.margin = new Vector4(0, 2, 0, -2);
        card.buyButton = btn; card.buyButtonLabel = bT;

        // Owned overlay
        GameObject oGO = NewUI("Own", go.transform);
        Stretch(oGO); Img(oGO, new Color(0.1f,0.6f,0.2f,0.15f));
        oGO.SetActive(false); card.ownedOverlay = oGO;

        card.Setup(d, () => BuyGun(idx));
        return card;
    }

    // ── Refresh ───────────────────────────────────────────────────────────
    void RefreshAll()
    {
        // If cards not yet built (shouldn't happen), try now
        if (cards.Count == 0 && GunShop.Instance != null && cardArea != null)
            BuildCards(cardArea);
        int coins = GameManager.Instance != null ? GameManager.Instance.Coins : 0;
        if (coinText != null) coinText.text = "Coin: " + coins;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null) continue;
            int gunIndex = (i < cardGunIndices.Count) ? cardGunIndices[i] : i;
            bool u = GunShop.Instance != null && GunShop.Instance.IsUnlocked(gunIndex);
            cards[i].Refresh(u, coins);
        }
    }

    void OnCoinsChanged(int c) { if (isOpen) RefreshAll(); }
    void OnGunUnlocked(int _) { if (isOpen) RefreshAll(); }
    void OnWeaponChanged(int selectedIndex, GunData data)
    {
        UpdateCurrentWeaponHUD(data);
    }

    void UpdateCurrentWeaponHUD(GunData data)
    {
        if (currentWeaponHudText == null) return;
        currentWeaponHudText.text = data != null
            ? $"Weapon: {data.gunName}  |  Keys: 1-4"
            : "Weapon: None  |  Keys: 1-4";
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    Canvas GetOrCreateCanvas()
    {
        // Always create a dedicated canvas so we're guaranteed on top of all other UI
        var go = new GameObject("Canvas_GunShop");
        DontDestroyOnLoad(go);
        var cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 500;   // above everything
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();
        Debug.Log("[GunShopUI] Created dedicated Canvas_GunShop (sortOrder=500)");
        return cv;
    }

    static void EnsureEventSystem()
    {
        EventSystem es = FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(go);
            Debug.Log("[GunShopUI] Created EventSystem + InputSystemUIInputModule");
            return;
        }

        if (es.GetComponent<InputSystemUIInputModule>() == null)
        {
            es.gameObject.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[GunShopUI] Added InputSystemUIInputModule to existing EventSystem");
        }
    }

    static GameObject NewUI(string n, Transform p)
    { var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(p, false); return g; }

    static void Stretch(GameObject g)
    { var r = g.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

    static void Center(GameObject g, float w, float h)
    { var r = g.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = new Vector2(0.5f,0.5f); r.pivot = new Vector2(0.5f,0.5f); r.sizeDelta = new Vector2(w,h); }

    static void TopBar(GameObject g, float h)
    { var r = g.GetComponent<RectTransform>(); r.anchorMin = new Vector2(0,1); r.anchorMax = new Vector2(1,1); r.pivot = new Vector2(0.5f,1); r.offsetMin = new Vector2(0,-h); r.offsetMax = Vector2.zero; }

    static void Img(GameObject g, Color c) { g.AddComponent<Image>().color = c; }

    static TextMeshProUGUI TMP(string n, Transform p, string txt, float sz, FontStyles fs, Color c)
    {
        var g = NewUI(n, p);
        var t = g.AddComponent<TextMeshProUGUI>();
        t.text = txt;
        t.fontSize = sz;
        t.fontStyle = fs;
        t.color = c.a <= 0f ? new Color(c.r, c.g, c.b, 1f) : c;
        t.raycastTarget = false;

        if (TMP_Settings.defaultFontAsset != null)
            t.font = TMP_Settings.defaultFontAsset;

        // Improve readability over busy UI backgrounds
        var outline = g.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(1.8f, -1.8f);

        var shadow = g.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(0.8f, -0.8f);

        return t;
    }

    static TextMeshProUGUI Row(string n, Transform p, string txt, float sz, FontStyles fs, Color c, float y0, float y1)
    {
        var t = TMP(n, p, txt, sz, fs, c);
        var r = t.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.04f, y0);
        r.anchorMax = new Vector2(0.96f, y1);
        r.offsetMin = r.offsetMax = Vector2.zero;
        t.alignment = TextAlignmentOptions.Center;

        // Add a subtle dark backing strip for stronger text contrast.
        var bgGO = NewUI(n + "_BG", p);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.04f, y0);
        bgRT.anchorMax = new Vector2(0.96f, y1);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);
        bg.raycastTarget = false;
        bgGO.transform.SetSiblingIndex(t.transform.GetSiblingIndex());
        t.transform.SetAsLastSibling();

        return t;
    }
}
