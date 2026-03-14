using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Auto-assign sprites for GunData assets based on folder naming convention.
/// Menu: Tools/Gun Shop/Auto Assign Gun Sprites
/// </summary>
public static class GunSpriteAutoAssign
{
    private const string GunDataFolder = "Assets/Project/Data/Gun";
    private const string SpriteRootFolder = "Assets/Project/Sprites/Gun";

    [MenuItem("Tools/Gun Shop/Auto Assign Gun Sprites")]
    public static void AutoAssign()
    {
        string[] gunGuids = AssetDatabase.FindAssets("t:GunData", new[] { GunDataFolder });
        if (gunGuids.Length == 0)
        {
            Debug.LogWarning("[GunSpriteAutoAssign] No GunData assets found.");
            return;
        }

        int assigned = 0;
        int skipped = 0;

        foreach (string guid in gunGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GunData data = AssetDatabase.LoadAssetAtPath<GunData>(path);
            if (data == null)
            {
                skipped++;
                continue;
            }

            string group = GuessGroup(data);
            if (string.IsNullOrEmpty(group))
            {
                Debug.LogWarning($"[GunSpriteAutoAssign] Cannot guess group for {data.name} ({data.gunName}).");
                skipped++;
                continue;
            }

            Sprite sprite = PickBestSprite(group, data);
            if (sprite == null)
            {
                Debug.LogWarning($"[GunSpriteAutoAssign] No sprite found for {data.name} in group {group}.");
                skipped++;
                continue;
            }

            data.gunSprite = sprite;
            EditorUtility.SetDirty(data);
            assigned++;

            Debug.Log($"[GunSpriteAutoAssign] Assigned {sprite.name} -> {data.name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GunSpriteAutoAssign] Done. Assigned={assigned}, Skipped={skipped}");
    }

    private static string GuessGroup(GunData data)
    {
        string key = (data.gunName + " " + data.name).ToLowerInvariant();

        if (key.Contains("pistol")) return "Pistol";
        if (key.Contains("rifle")) return "Rifle";
        if (key.Contains("shotgun")) return "Shotgun";
        if (key.Contains("sniper")) return "Sniper";

        return null;
    }

    private static Sprite PickBestSprite(string group, GunData data)
    {
        string folder = $"{SpriteRootFolder}/{group}";
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        if (textureGuids.Length == 0) return null;

        string gunNameKey = data.gunName.ToLowerInvariant().Replace(" ", "_");

        // Expand each texture path to sprite sub-assets (works for Single and Multiple mode).
        List<(Sprite sprite, string path)> candidates = new List<(Sprite sprite, string path)>();
        foreach (string guid in textureGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (UnityEngine.Object obj in subAssets)
            {
                if (obj is Sprite sp)
                    candidates.Add((sp, path));
            }
        }

        if (candidates.Count == 0) return null;

        var ordered = candidates
            .OrderBy(c => ScorePath(c.path, c.sprite.name, gunNameKey))
            .ThenBy(c => c.path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.sprite.name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ordered[0].sprite;
    }

    private static int ScorePath(string path, string spriteName, string gunNameKey)
    {
        string file = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        string sprite = (spriteName ?? string.Empty).ToLowerInvariant();
        string key = gunNameKey ?? string.Empty;

        if (!string.IsNullOrEmpty(key) && (file.Contains(key) || sprite.Contains(key))) return 0;
        if (file.Contains("starter") || sprite.Contains("starter")) return 1;
        if (file.Contains("sprite_") || sprite.Contains("sprite_")) return 2;
        return 3;
    }
}
