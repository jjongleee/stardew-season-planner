using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.Locations;

namespace SeasonPlanner;

public enum BundleCategory { Crop, Fish, Forage, Artisan, Construction, Other }

public sealed class BundleItem
{
    public int    ItemId          { get; init; }
    public string QualifiedItemId { get; init; } = string.Empty;
    public string ItemName        { get; init; } = string.Empty;
    public int    Quantity        { get; init; }
    public int    Quality         { get; init; }
    public string? Season         { get; init; }   // null = greenhouse / all-season
    public int    GrowDays        { get; init; }
    public bool   IsGreenhouse    { get; init; }   // true = no season restriction
    public string BundleName      { get; init; } = string.Empty;
    public bool   RequiresRain    { get; init; }
    public BundleCategory Category { get; init; }
    public string? ShopSource     { get; init; }
    public IReadOnlySet<string> ContextTags { get; init; } = new HashSet<string>();

    public bool MatchesItem(Item item)
    {
        if (item is null) return false;

        // Primary: qualified ID match (covers all 1.6 mod items)
        if (!string.IsNullOrWhiteSpace(QualifiedItemId)
            && string.Equals(QualifiedItemId, item.QualifiedItemId, StringComparison.OrdinalIgnoreCase))
            return true;

        // Legacy int ID match (vanilla fallback)
        if (ItemId > 0
            && item is StardewValley.Object obj
            && obj.ParentSheetIndex == ItemId)
            return true;

        // Mod item fallback: unqualified ID match
        // e.g. QualifiedItemId = "(O)FlashShifter.SVE_Starfish", item.ItemId = "FlashShifter.SVE_Starfish"
        if (!string.IsNullOrWhiteSpace(QualifiedItemId) && QualifiedItemId.Length > 3
            && QualifiedItemId.StartsWith("(", StringComparison.Ordinal))
        {
            int closeIdx = QualifiedItemId.IndexOf(')');
            if (closeIdx > 0)
            {
                string unqualified = QualifiedItemId[(closeIdx + 1)..];
                if (string.Equals(unqualified, item.ItemId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}

public sealed class BundleScanner
{
    // Vanilla hardcoded shop sources (legacy int ID → shop name)
    private static readonly Dictionary<int, string> VanillaShopSources = new()
    {
        { 24,  "Pierre" }, { 188, "Pierre" }, { 190, "Pierre" }, { 192, "Pierre" },
        { 254, "Pierre" }, { 256, "Pierre" }, { 270, "Pierre" }, { 272, "Pierre" },
        { 276, "Pierre" }, { 280, "Pierre" }, { 284, "Pierre" }, { 300, "Pierre" },
        { 304, "Pierre" }, { 400, "Pierre" }, { 421, "Pierre" }, { 262, "Pierre" },
        { 266, "Pierre" }, { 278, "Pierre" }, { 281, "Pierre" }, { 282, "Pierre" },
        { 283, "Pierre" },
        { 176, "Marnie" }, { 174, "Marnie" }, { 186, "Marnie" }, { 438, "Marnie" },
        { 182, "Marnie" },
        { 145, "Willy"  }, { 140, "Willy"  }, { 131, "Willy"  },
        { 372, "Krobus" }, { 766, "Krobus" },
        { 346, "Gus"    }, { 348, "Gus"    },
        { 773, "Harvey" },
        { 745, "Gezgin Satıcı" }, { 433, "Gezgin Satıcı" },
    };

    // Vanilla seed → harvest fallback (for seeds not in Data/Crops)
    public static readonly Dictionary<int, int> SeedToHarvest = new()
    {
        { 472, 24  }, { 473, 188 }, { 474, 190 }, { 475, 192 },
        { 476, 254 }, { 477, 256 }, { 478, 270 }, { 479, 272 },
        { 480, 276 }, { 481, 280 }, { 482, 284 }, { 483, 300 },
        { 484, 304 }, { 485, 398 }, { 486, 400 }, { 487, 454 },
        { 488, 24  }, { 489, 270 }, { 490, 300 },
        { 495, 282 }, { 496, 281 }, { 497, 283 },
        { 499, 433 }, { 745, 262 }, { 802, 454 },
        { 431, 421 },
        { 347, 262 },
        { 251, 433 },
    };

    private readonly IMonitor            _monitor;
    private readonly IModRegistry        _modRegistry;
    private readonly IGameContentHelper  _gameContent;
    private readonly IModContentHelper   _modContent;

    private string            _cacheKey = string.Empty;
    private List<BundleItem>  _cache    = new();

    private Dictionary<string, string>?                          _shopSourceCache;
    private Dictionary<string, (int growDays, string? season, bool isGreenhouse)>? _cropInfoCache;
    // qualifiedId → true if it's a rain fish (built from Data/Fish)
    private HashSet<string>?                                     _rainFishCache;

    public BundleScanner(IMonitor monitor, IModHelper helper)
    {
        _monitor     = monitor;
        _modRegistry = helper.ModRegistry;
        _gameContent = helper.GameContent;
        _modContent  = helper.ModContent;
        LogFrameworkCompatibility();
    }

    public IReadOnlyList<BundleItem> GetMissingItems(bool filterConstruction = false)
    {
        string key = $"{Game1.currentSeason}_{Game1.dayOfMonth}";
        if (_cacheKey != key)
        {
            _cache    = BuildMissingList();
            _cacheKey = key;
        }

        return filterConstruction
            ? _cache.Where(i => i.Category != BundleCategory.Construction).ToList()
            : _cache;
    }

    public void Invalidate()
    {
        _cacheKey        = string.Empty;
        _shopSourceCache = null;
        _cropInfoCache   = null;
        _rainFishCache   = null;
    }

    // ─── Main scan ────────────────────────────────────────────────────────────

    private List<BundleItem> BuildMissingList()
    {
        var result = new List<BundleItem>();

        if (Game1.getLocationFromName("CommunityCenter") is not CommunityCenter cc)
            return result;

        EnsureShopSourceCache();
        EnsureCropInfoCache();
        EnsureRainFishCache();

        Dictionary<string, string> bundleData;
        try   { bundleData = _gameContent.Load<Dictionary<string, string>>("Data/Bundles"); }
        catch { return result; }

        // Build an ordered list of bundle keys for randomized-bundle index fallback
        var bundleKeys = bundleData.Keys.ToList();

        foreach (var (key, raw) in bundleData)
        {
            string[] parts = raw.Split('/');
            if (parts.Length < 3) continue;

            string bundleName = LocalizeBundleName(parts[0]);
            string itemsRaw   = parts[2];

            // --- FIX 2: Randomized bundles ---
            // Primary: parse the bundle index from the key (e.g. "Pantry/0" → 0)
            // Fallback: use the ordinal position in the dictionary
            bool[] completion;
            if (int.TryParse(key.Split('/').Last(), out int bundleIndex)
                && cc.bundles.TryGetValue(bundleIndex, out completion!))
            {
                // found by explicit index — normal path
            }
            else
            {
                // Randomized bundles shift indices; try matching by ordinal position
                int ordinal = bundleKeys.IndexOf(key);
                if (ordinal < 0 || !cc.bundles.TryGetValue(ordinal, out completion!))
                    continue; // can't map → skip safely
            }

            string[] tokens = itemsRaw.Split(' ');
            for (int i = 0; i + 2 < tokens.Length; i += 3)
            {
                string rawToken    = tokens[i];
                if (!int.TryParse(tokens[i + 1], out int qty))     continue;
                if (!int.TryParse(tokens[i + 2], out int quality)) continue;

                int slot = i / 3;
                if (slot < completion.Length && completion[slot]) continue;

                // --- FIX 1: JA/DGA placeholder IDs (-1, negative) ---
                string qualifiedId = ResolveItemId(rawToken);
                if (string.IsNullOrWhiteSpace(qualifiedId)) continue;

                int    legacyId  = TryGetLegacyObjectId(qualifiedId);
                var    tags      = GetItemContextTags(qualifiedId, quality);
                var    cropInfo  = GetCropInfo(qualifiedId, legacyId);
                string? shopSrc  = ResolveShopSource(qualifiedId, legacyId);

                result.Add(new BundleItem
                {
                    ItemId          = legacyId,
                    QualifiedItemId = qualifiedId,
                    ItemName        = GetItemName(qualifiedId, legacyId, quality),
                    Quantity        = qty,
                    Quality         = quality,
                    Season          = cropInfo.season,
                    GrowDays        = cropInfo.growDays,
                    IsGreenhouse    = cropInfo.isGreenhouse,
                    BundleName      = bundleName,
                    RequiresRain    = IsRainItem(qualifiedId, tags),
                    Category        = ClassifyItem(qualifiedId, legacyId, tags, cropInfo.growDays),
                    ShopSource      = shopSrc,
                    ContextTags     = tags,
                });
            }
        }

        _monitor.Log($"[BundleScanner] {result.Count} eksik item tarandı.", LogLevel.Debug);
        return result;
    }

    // ─── FIX 1: Resolve item IDs including JA/DGA placeholders ──────────────

    private string ResolveItemId(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return string.Empty;

        // Already qualified (e.g. "(O)24", "(BC)130")
        if (rawToken.StartsWith("(", StringComparison.Ordinal))
            return rawToken;

        // Positive int → vanilla object
        if (int.TryParse(rawToken, out int id) && id > 0)
            return $"(O){id}";

        // Negative or zero int → unresolvable placeholder, skip
        if (int.TryParse(rawToken, out int negId) && negId <= 0)
        {
            _monitor.Log($"[BundleScanner] Skipping placeholder token '{rawToken}'", LogLevel.Trace);
            return string.Empty;
        }

        // Non-numeric string key — mod item (e.g. "FlashShifter.StardewValleyExpanded_Starfish")
        // Try ItemRegistry with (O) prefix first (most mod items are objects)
        try
        {
            var data = ItemRegistry.GetData($"(O){rawToken}");
            if (data is not null) return $"(O){rawToken}";
        }
        catch { }

        // Try without prefix (ItemRegistry may resolve it directly)
        try
        {
            var data = ItemRegistry.GetData(rawToken);
            if (data is not null) return data.QualifiedItemId;
        }
        catch { }

        // Try common type prefixes for mod items
        foreach (string prefix in new[] { "(O)", "(BC)", "(F)", "(W)", "(H)", "(S)", "(P)" })
        {
            try
            {
                var data = ItemRegistry.GetData($"{prefix}{rawToken}");
                if (data is not null) return $"{prefix}{rawToken}";
            }
            catch { }
        }

        _monitor.Log($"[BundleScanner] Could not resolve item token '{rawToken}' via ItemRegistry", LogLevel.Trace);
        return string.Empty;
    }

    // ─── Crop info cache ─────────────────────────────────────────────────────

    private void EnsureCropInfoCache()
    {
        if (_cropInfoCache is not null) return;
        _cropInfoCache = new Dictionary<string, (int, string?, bool)>(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, CropData>? crops = null;
        try { crops = _gameContent.Load<Dictionary<string, CropData>>("Data/Crops"); }
        catch { return; }

        foreach (var (seedKey, data) in crops)
        {
            if (string.IsNullOrWhiteSpace(data.HarvestItemId)) continue;

            int growDays = 0;
            if (data.DaysInPhase != null)
                foreach (var d in data.DaysInPhase)
                    if (d > 0) growDays += d;

            // FIX 4: empty Seasons = greenhouse / all-season crop
            bool isGreenhouse = data.Seasons is null || data.Seasons.Count == 0;
            string? season    = isGreenhouse ? null : data.Seasons![0].ToString().ToLower();

            // Index by qualified harvest ID — "(O)24" or "(O)MizuJakkaru.Cornucopia_Cabbage"
            string harvestQualified = NormalizeQualifiedItemId(data.HarvestItemId);
            if (!string.IsNullOrWhiteSpace(harvestQualified))
                _cropInfoCache.TryAdd(harvestQualified, (growDays, season, isGreenhouse));

            // Also index by raw harvest ID string (legacy int or mod string key)
            _cropInfoCache.TryAdd(data.HarvestItemId, (growDays, season, isGreenhouse));

            // For mod crops: also try resolving via ItemRegistry to get the true qualified ID
            if (!int.TryParse(data.HarvestItemId, out _))
            {
                try
                {
                    var itemData = ItemRegistry.GetData($"(O){data.HarvestItemId}");
                    if (itemData is not null)
                        _cropInfoCache.TryAdd(itemData.QualifiedItemId, (growDays, season, isGreenhouse));
                }
                catch { }
            }
        }
    }

    private (int growDays, string? season, bool isGreenhouse) GetCropInfo(string qualifiedId, int legacyId)
    {
        if (_cropInfoCache is null) return (0, null, false);

        if (_cropInfoCache.TryGetValue(qualifiedId, out var info)) return info;

        if (legacyId > 0 && _cropInfoCache.TryGetValue(legacyId.ToString(), out info)) return info;

        return (0, null, false);
    }

    // ─── FIX 3: Rain fish cache (reads Data/Fish) ────────────────────────────

    private void EnsureRainFishCache()
    {
        if (_rainFishCache is not null) return;
        _rainFishCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Data/Fish format (1.6): string dictionary, each value is slash-separated
        // Field index 7 = weather ("rainy", "sunny", "both")
        try
        {
            var fishData = _gameContent.Load<Dictionary<string, string>>("Data/Fish");
            foreach (var (fishId, raw) in fishData)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string[] fields = raw.Split('/');
                if (fields.Length <= 7) continue;
                string weather = fields[7].Trim().ToLower();
                if (weather is "rainy" or "both")
                    _rainFishCache.Add(NormalizeQualifiedItemId(fishId));
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"[BundleScanner] Data/Fish yüklenemedi: {ex.Message}", LogLevel.Trace);
        }
    }

    // ─── Shop source cache ───────────────────────────────────────────────────

    private void EnsureShopSourceCache()
    {
        if (_shopSourceCache is not null) return;
        _shopSourceCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (id, shop) in VanillaShopSources)
            _shopSourceCache[$"(O){id}"] = shop;

        try
        {
            var shops = _gameContent.Load<Dictionary<string, StardewValley.GameData.Shops.ShopData>>("Data/Shops");
            foreach (var (shopId, shopData) in shops)
            {
                if (shopData?.Items is null) continue;
                string shopName = ResolveShopName(shopId);

                foreach (var entry in shopData.Items)
                {
                    string? itemId = entry?.ItemId;
                    if (string.IsNullOrWhiteSpace(itemId)) continue;

                    string qualified = NormalizeQualifiedItemId(itemId);
                    if (!string.IsNullOrWhiteSpace(qualified))
                        _shopSourceCache.TryAdd(qualified, shopName);
                }
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"[BundleScanner] Data/Shops yüklenemedi: {ex.Message}", LogLevel.Trace);
        }
    }

    private static string ResolveShopName(string shopId) => shopId switch
    {
        "SeedShop"       => "Pierre",
        "AnimalShop"     => "Marnie",
        "FishShop"       => "Willy",
        "ScienceHouse"   => "Robin",
        "Blacksmith"     => "Clint",
        "Hospital"       => "Harvey",
        "AdventureShop"  => "Marlon",
        "Saloon"         => "Gus",
        "Sandy"          => "Sandy",
        "IceCreamStand"  => "Alex",
        "Krobus"         => "Krobus",
        "DesertShop"     => "Sandy",
        "HatMouse"       => "Hat Mouse",
        "QiGemShop"      => "Qi",
        "ResortBar"      => "Gus",
        "VolcanoShop"    => "Volcano Shop",
        _                => shopId,
    };

    private string? ResolveShopSource(string qualifiedId, int legacyId)
    {
        if (_shopSourceCache is null) return null;
        if (_shopSourceCache.TryGetValue(qualifiedId, out string? shop)) return shop;
        if (legacyId > 0 && _shopSourceCache.TryGetValue($"(O){legacyId}", out shop)) return shop;
        return null;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static string NormalizeQualifiedItemId(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return string.Empty;
        if (rawToken.StartsWith("(", StringComparison.Ordinal)) return rawToken;
        return int.TryParse(rawToken, out int id) ? $"(O){id}" : rawToken;
    }

    private static int TryGetLegacyObjectId(string qualifiedItemId)
    {
        if (string.IsNullOrWhiteSpace(qualifiedItemId)) return -1;
        if (!qualifiedItemId.StartsWith("(O)", StringComparison.OrdinalIgnoreCase)) return -1;
        return int.TryParse(qualifiedItemId[3..], out int parsed) ? parsed : -1;
    }

    private static HashSet<string> GetItemContextTags(string qualifiedId, int quality)
    {
        try
        {
            Item? item = ItemRegistry.Create(qualifiedId, 1, quality, allowNull: true);
            if (item is not null)
            {
                var tags = new HashSet<string>(item.GetContextTags(), StringComparer.OrdinalIgnoreCase);
                if (tags.Count > 0) return tags;
            }
        }
        catch { }

        // Fallback: read context tags directly from Data/Objects for mod items
        try
        {
            int legacyId = TryGetLegacyObjectId(qualifiedId);
            if (legacyId > 0)
            {
                var objects = Game1.content.Load<Dictionary<string, string>>("Data/Objects");
                string key  = legacyId.ToString();
                if (objects.TryGetValue(key, out string? raw))
                {
                    string[] fields = raw.Split('/');
                    // Field 13 in 1.6 format = context tags (space-separated)
                    if (fields.Length > 13 && !string.IsNullOrWhiteSpace(fields[13]))
                    {
                        return new HashSet<string>(
                            fields[13].Split(' ', StringSplitOptions.RemoveEmptyEntries),
                            StringComparer.OrdinalIgnoreCase);
                    }
                }
            }
        }
        catch { }

        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private static string GetItemName(string qualifiedId, int legacyId, int quality)
    {
        // Primary: ItemRegistry (covers all 1.6 mod items via CP/JA/DGA)
        try
        {
            Item? item = ItemRegistry.Create(qualifiedId, 1, quality, allowNull: true);
            if (item is not null && !string.IsNullOrWhiteSpace(item.DisplayName))
                return item.DisplayName;
        }
        catch { }

        // Fallback 1: legacy Object constructor
        if (legacyId > 0)
        {
            try
            {
                var obj = new StardewValley.Object(legacyId.ToString(), 1);
                if (!string.IsNullOrWhiteSpace(obj.DisplayName)) return obj.DisplayName;
            }
            catch { }
        }

        // Fallback 2: Data/Objects direct read
        try
        {
            string lookupKey = legacyId > 0 ? legacyId.ToString()
                             : qualifiedId.StartsWith("(O)", StringComparison.OrdinalIgnoreCase)
                               ? qualifiedId[3..] : qualifiedId;
            var objects = Game1.content.Load<Dictionary<string, string>>("Data/Objects");
            if (objects.TryGetValue(lookupKey, out string? raw))
            {
                string[] fields = raw.Split('/');
                if (fields.Length > 4 && !string.IsNullOrWhiteSpace(fields[4]))
                    return fields[4]; // Display name field
            }
        }
        catch { }

        // Last resort: strip prefix from qualified ID
        return qualifiedId.StartsWith("(O)", StringComparison.OrdinalIgnoreCase)
            ? qualifiedId[3..] : qualifiedId;
    }

    private bool IsRainItem(string qualifiedId, IReadOnlyCollection<string> tags)
    {
        // FIX 3: Check Data/Fish cache first (covers mod-added rain fish)
        if (_rainFishCache is not null && _rainFishCache.Contains(qualifiedId))
            return true;

        // Context tag fallback (for fish that set tags but aren't in Data/Fish)
        if (tags.Any(t => t.StartsWith("fish", StringComparison.OrdinalIgnoreCase)))
            return tags.Any(t => t.Contains("rain", StringComparison.OrdinalIgnoreCase));

        return false;
    }

    private static BundleCategory ClassifyItem(
        string qualifiedId, int legacyId,
        IReadOnlyCollection<string> tags, int growDays)
    {
        if (tags.Any(t => t.StartsWith("fish", StringComparison.OrdinalIgnoreCase)))
            return BundleCategory.Fish;

        if (tags.Any(t => t.Contains("artisan", StringComparison.OrdinalIgnoreCase)))
            return BundleCategory.Artisan;

        if (tags.Any(t => t.Contains("forage", StringComparison.OrdinalIgnoreCase)))
            return BundleCategory.Forage;

        if (tags.Any(t => t.Contains("crop", StringComparison.OrdinalIgnoreCase)) || growDays > 0)
            return BundleCategory.Crop;

        if (tags.Any(t => t.Contains("building_resource", StringComparison.OrdinalIgnoreCase)
                       || t.Contains("construction", StringComparison.OrdinalIgnoreCase)))
            return BundleCategory.Construction;

        if (qualifiedId.StartsWith("(O)", StringComparison.OrdinalIgnoreCase)
            && legacyId is 388 or 390 or 709 or 766 or 767 or 382 or 378 or 380 or 384 or 386)
            return BundleCategory.Construction;

        return BundleCategory.Other;
    }

    private static string LocalizeBundleName(string englishName)
    {
        string key       = "bundle." + englishName.ToLower().Replace(' ', '_').Replace("'", "");
        string localized = I18n.BundleName(key);
        return localized == key ? englishName : localized;
    }

    // ─── Public helper used by TooltipHelper ─────────────────────────────────

    public static (int growDays, string? season, int harvestId) GetCropInfoFromSeed(int seedId)
    {
        try
        {
            var crops = Game1.content.Load<Dictionary<string, CropData>>("Data/Crops");
            if (crops.TryGetValue(seedId.ToString(), out var data))
            {
                // FIX 4: empty Seasons = greenhouse crop
                string? season = data.Seasons?.Count > 0 ? data.Seasons[0].ToString().ToLower() : null;
                int harvestId  = int.TryParse(data.HarvestItemId, out int hid) ? hid : -1;
                int growDays   = 0;
                if (data.DaysInPhase != null)
                    foreach (var d in data.DaysInPhase)
                        if (d > 0) growDays += d;
                return (growDays, season, harvestId);
            }
        }
        catch { }

        // Legacy string format fallback
        try
        {
            var crops = Game1.content.Load<Dictionary<string, string>>("Data/Crops");
            if (crops.TryGetValue(seedId.ToString(), out string? raw))
            {
                string[] p = raw.Split('/');
                if (p.Length >= 4)
                {
                    string season  = p[0].Split(' ')[0].ToLower();
                    int harvestId  = int.TryParse(p[3], out int hid) ? hid : -1;
                    int growDays   = 0;
                    foreach (var part in p[1].Split(' '))
                        if (int.TryParse(part, out int d) && d > 0) growDays += d;
                    return (growDays, season, harvestId);
                }
            }
        }
        catch { }

        if (SeedToHarvest.TryGetValue(seedId, out int fallbackHarvest))
        {
            try
            {
                var crops = Game1.content.Load<Dictionary<string, CropData>>("Data/Crops");
                foreach (var (_, data) in crops)
                {
                    if (!int.TryParse(data.HarvestItemId, out int hid) || hid != fallbackHarvest) continue;
                    int growDays = 0;
                    if (data.DaysInPhase != null)
                        foreach (var d in data.DaysInPhase)
                            if (d > 0) growDays += d;
                    return (growDays, null, fallbackHarvest);
                }
            }
            catch { }
        }

        return (0, null, -1);
    }

    private void LogFrameworkCompatibility()
    {
        string[] knownFrameworks =
        {
            "spacechase0.SpaceCore",
            "spacechase0.JsonAssets",
            "spacechase0.DynamicGameAssets",
            "FlashShifter.StardewValleyExpanded",
            "MizuJakkaru.CornucopiaMoreCrops",
            "MizuJakkaru.CornucopiaCookingRecipes",
            "Bonster.BonsterCrops",
            "Bonster.CulinaryDelight",
            "Bonster.BetterThings",
        };

        foreach (string fw in knownFrameworks)
            if (_modRegistry.IsLoaded(fw))
                _monitor.Log($"[BundleScanner] Detected mod: {fw}", LogLevel.Debug);
    }
}
