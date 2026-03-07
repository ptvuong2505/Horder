#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tao toan bo UI Kho Sung vao scene bang 1 click.
/// Menu: Tools > Setup Gun Shop UI
/// </summary>
public static class GunShopSetup
{
    // Palette
    static Color C_DIM   = new Color(0f,    0f,    0f,    0.75f);
    static Color C_PANEL = new Color(0.09f, 0.12f, 0.18f, 0.97f);
    static Color C_HEAD  = new Color(0.15f, 0.20f, 0.30f, 1f);
    static Color C_CARD  = new Color(0.13f, 0.17f, 0.24f, 1f);
    static Color C_BUY   = new Color(0.13f, 0.60f, 0.27f, 1f);
    static Color C_OWN   = new Color(0.27f, 0.27f, 0.32f, 1f);
    static Color C_GOLD  = new Color(1f,    0.82f, 0.18f, 1f);
    static Color C_GRAY  = new Color(0.60f, 0.60f, 0.60f, 1f);
    static Color C_RED   = new Color(0.68f, 0.16f, 0.16f, 1f);

    [MenuItem("Tools/Setup Gun Shop UI")]
    static void Run()
    {
        // --- Lay WeaponManager de co danh sach GunData ---
        WeaponManager wm = Object.FindFirstObjectByType<WeaponManager>();
        if (wm == null || wm.gunDataList.Count == 0)
        {
            EditorUtility.DisplayDialog("Gun Shop Setup",
                "Khong tim thay WeaponManager hoac gunDataList rong!\nHay mo scene game truoc.", "OK");
            return;
        }

        // --- Xoa cai cu neu co ---
        var old = GameObject.Find("GunShopController");
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Gun Shop Setup",
                "Da co GunShopController trong scene. Xoa va tao lai?", "Co", "Khong"))
                return;
            Object.DestroyImmediate(old);
        }

        // --- Tim hoac tao Canvas ---
        Canvas canvas = FindOrCreateCanvas();

        // ================================================================
        // GunShopController: luon Active, chua GunShopUI (nhan phim G)
        // ================================================================
        GameObject ctrl = NewUI("GunShopController", canvas.transform);
        Stretch(ctrl);
        ctrl.AddComponent<CanvasGroup>(); // khong block raycast khi an
        var ctrlCG = ctrl.GetComponent<CanvasGroup>();
        ctrlCG.blocksRaycasts = false;
        ctrlCG.interactable   = false;
        GunShopUI shopUI = ctrl.AddComponent<GunShopUI>();

        // ================================================================
        // Overlay: toan man hinh, tam diem, chi hien khi mo shop
        // ================================================================
        GameObject overlay = NewUI("Overlay", ctrl.transform);
        Stretch(overlay);
        AddImg(overlay, C_DIM);
        overlay.SetActive(false);           // <<< tat tu dau

        // ================================================================
        // Center Panel
        // ================================================================
        int count   = wm.gunDataList.Count;
        float cardW = 195f, cardH = 285f, gap = 14f, pad = 30f;
        float W = count * cardW + (count - 1) * gap + pad * 2;
        float H = cardH + 140f;

        GameObject panel = NewUI("Panel", overlay.transform);
        Center(panel, W, H);
        AddImg(panel, C_PANEL);

        // Header strip
        GameObject header = NewUI("Header", panel.transform);
        TopBar(header, 82f);
        AddImg(header, C_HEAD);

        // Title text
        var titleGO = NewUI("Title", header.transform);
        Stretch(titleGO);
        var title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text      = "KHO SUNG";
        title.fontSize  = 26;
        title.fontStyle = FontStyles.Bold;
        title.color     = Color.white;
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.margin    = new Vector4(18, 0, W * 0.4f, 0);

        // Coin display
        var coinGO = NewUI("CoinText", header.transform);
        Stretch(coinGO);
        var coinTmp = coinGO.AddComponent<TextMeshProUGUI>();
        coinTmp.text      = "Coin: 0";
        coinTmp.fontSize  = 21;
        coinTmp.fontStyle = FontStyles.Bold;
        coinTmp.color     = C_GOLD;
        coinTmp.alignment = TextAlignmentOptions.MidlineRight;
        coinTmp.margin    = new Vector4(0, 0, 18, 0);

        // Hint
        var hintGO = NewUI("Hint", panel.transform);
        var hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0, 1); hintRT.anchorMax = new Vector2(1, 1);
        hintRT.pivot     = new Vector2(0.5f, 1);
        hintRT.offsetMin = new Vector2(0, -115); hintRT.offsetMax = new Vector2(0, -85);
        var hintTmp = hintGO.AddComponent<TextMeshProUGUI>();
        hintTmp.text      = "[ G ] de dong";
        hintTmp.fontSize  = 13;
        hintTmp.color     = C_GRAY;
        hintTmp.alignment = TextAlignmentOptions.Center;

        // ================================================================
        // Card area (HorizontalLayout)
        // ================================================================
        GameObject area = NewUI("CardArea", panel.transform);
        var areaRT = area.GetComponent<RectTransform>();
        areaRT.anchorMin = new Vector2(0, 0); areaRT.anchorMax = new Vector2(1, 0);
        areaRT.pivot     = new Vector2(0.5f, 0);
        areaRT.offsetMin = new Vector2(pad, pad);
        areaRT.offsetMax = new Vector2(-pad, pad + cardH);
        var hlg = area.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = gap;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

        // ================================================================
        // Cards
        // ================================================================
        for (int i = 0; i < wm.gunDataList.Count; i++)
        {
            GunData data = wm.gunDataList[i];
            if (data == null) continue;
            int idx = i;
            BuildCard(area.transform, data, idx, shopUI);
        }

        // ================================================================
        // Close button (X)
        // ================================================================
        GameObject xGO = NewUI("CloseBtn", panel.transform);
        var xRT = xGO.GetComponent<RectTransform>();
        xRT.anchorMin = xRT.anchorMax = new Vector2(1, 1);
        xRT.pivot = new Vector2(1, 1);
        xRT.offsetMin = new Vector2(-50, -46);
        xRT.offsetMax = new Vector2(-8, -8);
        AddImg(xGO, C_RED);
        var xBtn = xGO.AddComponent<Button>();
        xBtn.targetGraphic = xGO.GetComponent<Image>();

        // Wire Close button -> CloseShop via PersistentListener
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
            xBtn.onClick, shopUI.CloseShop);

        var xTxtGO = NewUI("X", xGO.transform);
        Stretch(xTxtGO);
        var xTxt = xTxtGO.AddComponent<TextMeshProUGUI>();
        xTxt.text      = "X";
        xTxt.fontSize  = 24;
        xTxt.fontStyle = FontStyles.Bold;
        xTxt.color     = Color.white;
        xTxt.alignment = TextAlignmentOptions.Center;

        // --- Danh dau dirty de Unity luu scene ---
        EditorUtility.SetDirty(ctrl);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Selection.activeGameObject = ctrl;

        EditorUtility.DisplayDialog("Gun Shop Setup",
            "Tao UI thanh cong!\n\nNhan G khi Play de mo / dong Kho Sung.\nNho Save Scene (Ctrl+S).", "OK");

        Debug.Log("[GunShopSetup] Da tao GunShopController trong scene.");
    }

    // ────────────────────────────────────────────────────────────────────────
    static GunCardUI BuildCard(Transform parent, GunData data, int index, GunShopUI shopUI)
    {
        GameObject go = NewUI("Card_" + data.gunName, parent);
        AddImg(go, C_CARD);
        go.AddComponent<Outline>().effectColor = new Color(0.25f, 0.35f, 0.5f, 1f);
        var card = go.AddComponent<GunCardUI>();

        // Icon
        GameObject iconGO = NewUI("Icon", go.transform);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.08f, 0.52f);
        iconRT.anchorMax = new Vector2(0.92f, 0.97f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        var iconImg = iconGO.AddComponent<Image>();
        if (data.gunSprite != null) { iconImg.sprite = data.gunSprite; iconImg.preserveAspect = true; }
        else iconImg.color = new Color(1, 1, 1, 0.07f);
        card.gunIcon = iconImg;

        // Name
        card.gunNameText = MakeTMP("Name", go.transform, data.gunName, 15, FontStyles.Bold, Color.white, 0.44f, 0.52f);

        // Desc
        string desc = string.IsNullOrEmpty(data.description)
            ? "DMG:" + data.damage + "  Rate:" + data.fireRate.ToString("F1") + "s"
            : data.description;
        var descTmp = MakeTMP("Desc", go.transform, desc, 10, FontStyles.Normal, C_GRAY, 0.34f, 0.44f);
        descTmp.enableWordWrapping = true;
        card.descText = descTmp;

        // Cost
        card.costText = MakeTMP("Cost", go.transform,
            data.unlockedByDefault ? "Mien phi" : data.unlockCost + " Coin",
            14, FontStyles.Bold, C_GOLD, 0.25f, 0.34f);

        // Buy button
        GameObject btnGO = NewUI("BuyBtn", go.transform);
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.07f, 0);
        btnRT.anchorMax = new Vector2(0.93f, 0);
        btnRT.pivot     = new Vector2(0.5f, 0);
        btnRT.offsetMin = new Vector2(0, 10);
        btnRT.offsetMax = new Vector2(0, 50);
        AddImg(btnGO, data.unlockedByDefault ? C_OWN : C_BUY);
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnGO.GetComponent<Image>();

        // Wire Buy button -> BuyGun(index)
        UnityEditor.Events.UnityEventTools.AddIntPersistentListener(
            btn.onClick, shopUI.BuyGun, index);

        var btnLblGO = NewUI("Label", btnGO.transform);
        Stretch(btnLblGO);
        var btnLbl = btnLblGO.AddComponent<TextMeshProUGUI>();
        btnLbl.text      = data.unlockedByDefault ? "DA SO HUU" : "MUA " + data.unlockCost;
        btnLbl.fontSize  = 12;
        btnLbl.fontStyle = FontStyles.Bold;
        btnLbl.color     = Color.white;
        btnLbl.alignment = TextAlignmentOptions.Center;
        card.buyButton      = btn;
        card.buyButtonLabel = btnLbl;

        // Owned overlay
        GameObject ownGO = NewUI("OwnedOverlay", go.transform);
        Stretch(ownGO);
        AddImg(ownGO, new Color(0.1f, 0.6f, 0.2f, 0.15f));
        ownGO.SetActive(false);
        card.ownedOverlay = ownGO;

        return card;
    }

    // ────────────────────────────────────────────────────────────────────────
    static Canvas FindOrCreateCanvas()
    {
        Canvas[] all = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in all)
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) return c;

        GameObject cGO = new GameObject("Canvas");
        var cv = cGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 100;
        var cs = cGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cGO.AddComponent<GraphicRaycaster>();
        return cv;
    }

    static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void Center(GameObject go, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
    }

    static void TopBar(GameObject go, float height)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(0, -height); rt.offsetMax = Vector2.zero;
    }

    static Image AddImg(GameObject go, Color c)
    {
        var img = go.AddComponent<Image>();
        img.color = c;
        return img;
    }

    static TextMeshProUGUI MakeTMP(string name, Transform parent, string text,
        float size, FontStyles style, Color color, float yMin, float yMax)
    {
        var go = NewUI(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.04f, yMin);
        rt.anchorMax = new Vector2(0.96f, yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size;
        tmp.fontStyle = style; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }
}
#endif
