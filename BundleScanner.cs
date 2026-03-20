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
    public string? Season         { get; init; }   // ilk mevsim (geriye dÃ¶nÃ¼k uyumluluk)
    public IReadOnlyList<string> Seasons { get; init; } = Array.Empty<string>(); // tÃ¼m mevsimler
    public int    GrowDays        { get; init; }
    public bool   IsGreenhouse    { get; init; }   // true = no season restriction
    public string BundleName      { get; init; } = string.Empty;
    public bool   RequiresRain    { get; init; }
    public BundleCategory Category { get; init; }
    public string? ShopSource     { get; init; }
    public IReadOnlySet<string> ContextTags { get; init; } = new HashSet<string>();
    public IReadOnlyList<string> FishLocations { get; init; } = Array.Empty<string>();
    public string? FishTimeRange  { get; init; }
    public string? FishWeather    { get; init; }
    public bool    IsMuseumItem   { get; init; }
    public bool    IsMuseumDonated { get; init; }

    public bool MatchesItem(Item item)
    {
        if (item is null) return false;

        if (!string.IsNullOrWhiteSpace(QualifiedItemId)
            && string.Equals(QualifiedItemId, item.QualifiedItemId, StringComparison.OrdinalIgnoreCase))
            return true;

        if (ItemId > 0
            && item is StardewValley.Object obj
            && obj.ParentSheetIndex == ItemId)
            return true;

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

        if (!string.IsNullOrWhiteSpace(QualifiedItemId))
        {
            string rawId = QualifiedItemId.StartsWith("(", StringComparison.Ordinal)
                ? QualifiedItemId[(QualifiedItemId.IndexOf(')') + 1)..].ToLower()
                : QualifiedItemId.ToLower();
            string expectedTag = $"item_id_object_{rawId}";
            try
            {
                var tags = item.GetContextTags();
                if (tags is not null && tags.Contains(expectedTag, StringComparer.OrdinalIgnoreCase))
                    return true;
            }
            catch { }
        }

        return false;
    }
}

public sealed class BundleScanner
{
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
        { 745, "Gezgin SatÄ±cÄ±" }, { 433, "Gezgin SatÄ±cÄ±" },
    };

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
    private Dictionary<string, (int growDays, string? season, bool isGreenhouse, List<string> seasons)>? _cropInfoCache;
    private Dictionary<string, (int growDays, string? season, bool isGreenhouse, List<string> seasons, string harvestQualifiedId)>? _seedCache;
    private HashSet<string>?                                     _rainFishCache;
    private Dictionary<string, (string productQualifiedId, string productName, List<string> seasons)>? _fruitTreeCache;
    private Dictionary<string, (string productQualifiedId, string productName, List<string> seasons, int ageToProduce)>? _customBushCache;
    private Dictionary<string, (List<string> locations, string? timeRange, string? weather)>? _fishInfoCache;

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
        string key = $"{Game1.currentSeason}_{Game1.dayOfMonth}_{GetBundleCompletionHash()}";
        if (_cacheKey != key)
        {
            _cache    = BuildMissingList();
            _cacheKey = key;
        }

        return filterConstruction
            ? _cache.Where(i => i.Category != BundleCategory.Construction).ToList()
            : _cache;
    }

    public IReadOnlyList<BundleItem> GetAllBundleItems()
    {
        GetMissingItems();
        return BuildAllBundleList();
    }

    private static string GetBundleCompletionHash()
    {
        if (Game1.getLocationFromName("CommunityCenter") is not CommunityCenter cc)
            return string.Empty;

        int completed = 0;
        foreach (var slots in cc.bundles.Values)
            foreach (bool slot in slots)
                if (slot) completed++;

        return completed.ToString();
    }

    public IReadOnlyList<BundleItem> GetAllSeasonalItems()
    {
        EnsureCropInfoCache();
        EnsureFruitTreeCache();
        EnsureCustomBushCache();

        string season  = Game1.currentSeason?.ToLower() ?? "";
        var missing    = GetMissingItems();
        var result     = new List<BundleItem>();
        var seen       = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (_seedCache is not null)
        {
            foreach (var (seedKey, info) in _seedCache)
            {
                if (!seedKey.StartsWith("(", StringComparison.Ordinal)) continue;
                if (!info.isGreenhouse && !info.seasons.Contains(season)) continue;
                if (!seen.Add(seedKey)) continue;

                string itemName = GetItemName(info.harvestQualifiedId, TryGetLegacyObjectId(info.harvestQualifiedId), 0);
                var bundleMatch = missing.FirstOrDefault(b =>
                    string.Equals(b.QualifiedItemId, info.harvestQualifiedId, StringComparison.OrdinalIgnoreCase));

                result.Add(new BundleItem
                {
                    QualifiedItemId = info.harvestQualifiedId,
                    ItemId          = TryGetLegacyObjectId(info.harvestQualifiedId),
                    ItemName        = itemName,
                    Quantity        = 1,
                    Quality         = 0,
                    Season          = info.season,
                    Seasons         = info.seasons,
                    GrowDays        = info.growDays,
                    IsGreenhouse    = info.isGreenhouse,
                    BundleName      = bundleMatch?.BundleName ?? string.Empty,
                    Category        = BundleCategory.Crop,
                    ShopSource      = ResolveShopSource(seedKey, TryGetLegacyObjectId(seedKey)),
                });
            }
        }

        if (_fruitTreeCache is not null)
        {
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (saplingKey, info) in _fruitTreeCache)
            {
                if (!saplingKey.StartsWith("(", StringComparison.Ordinal)) continue;
                if (info.seasons.Count > 0 && !info.seasons.Contains(season)) continue;
                if (!processed.Add(info.productQualifiedId)) continue;
                if (string.IsNullOrWhiteSpace(info.productQualifiedId)) continue;

                var bundleMatch = missing.FirstOrDefault(b =>
                    string.Equals(b.QualifiedItemId, info.productQualifiedId, StringComparison.OrdinalIgnoreCase));

                result.Add(new BundleItem
                {
                    QualifiedItemId = info.productQualifiedId,
                    ItemId          = TryGetLegacyObjectId(info.productQualifiedId),
                    ItemName        = info.productName,
                    Quantity        = 1,
                    Quality         = 0,
                    Season          = info.seasons.Count > 0 ? info.seasons[0] : null,
                    Seasons         = info.seasons,
                    GrowDays        = 28,
                    IsGreenhouse    = info.seasons.Count == 0,
                    BundleName      = bundleMatch?.BundleName ?? string.Empty,
                    Category        = BundleCategory.Crop,
                });
            }
        }

        if (_customBushCache is not null)
        {
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (saplingKey, info) in _customBushCache)
            {
                if (!saplingKey.StartsWith("(", StringComparison.Ordinal)) continue;
                if (info.seasons.Count > 0 && !info.seasons.Contains(season)) continue;
                if (!processed.Add(info.productQualifiedId)) continue;
                if (string.IsNullOrWhiteSpace(info.productQualifiedId)) continue;

                var bundleMatch = missing.FirstOrDefault(b =>
                    string.Equals(b.QualifiedItemId, info.productQualifiedId, StringComparison.OrdinalIgnoreCase));

                result.Add(new BundleItem
                {
                    QualifiedItemId = info.productQualifiedId,
                    ItemId          = TryGetLegacyObjectId(info.productQualifiedId),
                    ItemName        = info.productName,
                    Quantity        = 1,
                    Quality         = 0,
                    Season          = info.seasons.Count > 0 ? info.seasons[0] : null,
                    Seasons         = info.seasons,
                    GrowDays        = info.ageToProduce,
                    IsGreenhouse    = info.seasons.Count == 0,
                    BundleName      = bundleMatch?.BundleName ?? string.Empty,
                    Category        = BundleCategory.Crop,
                });
            }
        }

        return result.OrderBy(i => string.IsNullOrEmpty(i.BundleName) ? 1 : 0)
                     .ThenBy(i => i.ItemName)
                     .ToList();
    }

    public IReadOnlyList<BundleItem> GetMuseumItems()
    {
        var lib = Game1.getLocationFromName("ArchaeologyHouse")
                  as StardewValley.Locations.LibraryMuseum;
        if (lib is null) return Array.Empty<BundleItem>();

        var result = new List<BundleItem>();

        Dictionary<string, StardewValley.GameData.Objects.ObjectData>? objects = null;
        try { objects = _gameContent.Load<Dictionary<string, StardewValley.GameData.Objects.ObjectData>>("Data/Objects"); }
        catch { return result; }

        var donatedIds = new HashSet<string>(
            lib.museumPieces.Values.Select(v => (string)v),
            StringComparer.OrdinalIgnoreCase);

        foreach (var (itemId, data) in objects)
        {
            if (data is null) continue;

            Item? testItem = null;
            try { testItem = ItemRegistry.Create($"(O){itemId}", 1, 0, allowNull: true); }
            catch { }
            if (testItem is null) continue;

            bool suitable = false;
            try { suitable = lib.isItemSuitableForDonation(testItem); }
            catch { }
            if (!suitable) continue;

            bool donated = donatedIds.Contains(itemId);
            bool isMineral = data.Category == StardewValley.Object.GemCategory
                          || data.Category == StardewValley.Object.mineralsCategory;

            string displayName = testItem.DisplayName ?? itemId;

            result.Add(new BundleItem
            {
                ItemId          = int.TryParse(itemId, out int lid) ? lid : -1,
                QualifiedItemId = $"(O){itemId}",
                ItemName        = displayName,
                Quantity        = 1,
                Quality         = 0,
                BundleName      = isMineral ? I18n.MuseumCategoryMineral().Trim() : I18n.MuseumCategoryArtifact().Trim(),
                Category        = BundleCategory.Other,
                IsMuseumItem    = true,
                IsMuseumDonated = donated,
            });
        }

        return result.OrderBy(i => i.IsMuseumDonated).ThenBy(i => i.ItemName).ToList();
    }

    public void Invalidate()
    {
        _cacheKey        = string.Empty;
        _shopSourceCache = null;
        _cropInfoCache   = null;
        _seedCache       = null;
        _rainFishCache   = null;
        _fruitTreeCache  = null;
        _customBushCache = null;
        _fishInfoCache   = null;
    }


    private List<BundleItem> BuildAllBundleList()
    {
        var result = new List<BundleItem>();
        if (Game1.getLocationFromName("CommunityCenter") is not CommunityCenter cc) return result;

        EnsureShopSourceCache();
        EnsureCropInfoCache();
        EnsureRainFishCache();

        Dictionary<string, string> bundleData;
        try   { bundleData = _gameContent.Load<Dictionary<string, string>>("Data/Bundles"); }
        catch { return result; }

        foreach (var (_, raw) in bundleData)
        {
            string[] parts = raw.Split('/');
            if (parts.Length < 3) continue;
            string bundleName = LocalizeBundleName(parts[0]);
            string[] tokens   = parts[2].Split(' ');
            for (int i = 0; i + 2 < tokens.Length; i += 3)
            {
                string rawToken = tokens[i];
                if (!int.TryParse(tokens[i + 1], out int qty))     continue;
                if (!int.TryParse(tokens[i + 2], out int quality)) continue;
                string qualifiedId = ResolveItemId(rawToken);
                if (string.IsNullOrWhiteSpace(qualifiedId)) continue;
                int legacyId = TryGetLegacyObjectId(qualifiedId);
                var cropInfo = GetCropInfo(qualifiedId, legacyId);
                var tags2    = GetItemContextTags(qualifiedId, quality);
                var fishInfo2 = GetFishInfo(qualifiedId);
                result.Add(new BundleItem
                {
                    ItemId          = legacyId,
                    QualifiedItemId = qualifiedId,
                    ItemName        = GetItemName(qualifiedId, legacyId, quality),
                    Quantity        = qty,
                    Quality         = quality,
                    Season          = cropInfo.season,
                    Seasons         = cropInfo.seasons,
                    GrowDays        = cropInfo.growDays,
                    IsGreenhouse    = cropInfo.isGreenhouse,
                    BundleName      = bundleName,
                    RequiresRain    = IsRainItem(qualifiedId, tags2),
                    Category        = ClassifyItem(qualifiedId, legacyId, tags2, cropInfo.growDays),
                    ShopSource      = ResolveShopSource(qualifiedId, legacyId),
                    ContextTags     = tags2,
                    FishLocations   = fishInfo2.locations,
                    FishTimeRange   = fishInfo2.timeRange,
                    FishWeather     = fishInfo2.weather,
                });
            }
        }
        return result;
    }

    private List<BundleItem> BuildMissingList()
    {
        var result = new List<BundleItem>();

        if (Game1.getLocationFromName("CommunityCenter") is not CommunityCenter cc)
            return result;

        EnsureShopSourceCache();
        EnsureCropInfoCache();
        EnsureRainFishCache();
        EnsureFruitTreeCache();
        EnsureCustomBushCache();

        Dictionary<string, string> bundleData;
        try   { bundleData = _gameContent.Load<Dictionary<string, string>>("Data/Bundles"); }
        catch { return result; }

        var bundleKeys = bundleData.Keys.ToList();

        foreach (var (key, raw) in bundleData)
        {
            string[] parts = raw.Split('/');
            if (parts.Length < 3) continue;

            string bundleName = LocalizeBundleName(parts[0]);
            string itemsRaw   = parts[2];

            bool[] completion;
            if (int.TryParse(key.Split('/').Last(), out int bundleIndex)
                && cc.bundles.TryGetValue(bundleIndex, out completion!))
            {
            }
            else
            {
                int ordinal = bundleKeys.IndexOf(key);
                if (ordinal < 0 || !cc.bundles.TryGetValue(ordinal, out completion!))
                    continue; // can't map â†’ skip safely
            }

            string[] tokens = itemsRaw.Split(' ');
            for (int i = 0; i + 2 < tokens.Length; i += 3)
            {
                string rawToken    = tokens[i];
                if (!int.TryParse(tokens[i + 1], out int qty))     continue;
                if (!int.TryParse(tokens[i + 2], out int quality)) continue;

                int slot = i / 3;
                if (slot < completion.Length && completion[slot]) continue;

                string qualifiedId = ResolveItemId(rawToken);
                if (string.IsNullOrWhiteSpace(qualifiedId)) continue;

                int    legacyId  = TryGetLegacyObjectId(qualifiedId);
                var    tags      = GetItemContextTags(qualifiedId, quality);
                var    cropInfo  = GetCropInfo(qualifiedId, legacyId);
                string? shopSrc  = ResolveShopSource(qualifiedId, legacyId);
                var    fishInfo  = GetFishInfo(qualifiedId);

                result.Add(new BundleItem
                {
                    ItemId          = legacyId,
                    QualifiedItemId = qualifiedId,
                    ItemName        = GetItemName(qualifiedId, legacyId, quality),
                    Quantity        = qty,
                    Quality         = quality,
                    Season          = cropInfo.season,
                    Seasons         = cropInfo.seasons,
                    GrowDays        = cropInfo.growDays,
                    IsGreenhouse    = cropInfo.isGreenhouse,
                    BundleName      = bundleName,
                    RequiresRain    = IsRainItem(qualifiedId, tags),
                    Category        = ClassifyItem(qualifiedId, legacyId, tags, cropInfo.growDays),
                    ShopSource      = shopSrc,
                    ContextTags     = tags,
                    FishLocations   = fishInfo.locations,
                    FishTimeRange   = fishInfo.timeRange,
                    FishWeather     = fishInfo.weather,
                });
            }
        }

        _monitor.Log($"[BundleScanner] {result.Count} eksik item tarandı. / {result.Count} missing items scanned.", LogLevel.Debug);
        return result;
    }


    private string ResolveItemId(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return string.Empty;

        if (rawToken.StartsWith("(", StringComparison.Ordinal))
            return rawToken;

        if (int.TryParse(rawToken, out int id) && id > 0)
            return $"(O){id}";

        if (int.TryParse(rawToken, out int negId) && negId <= 0)
        {
            _monitor.Log($"[BundleScanner] '{rawToken}' token atlandı (placeholder). / Skipping placeholder token '{rawToken}'", LogLevel.Trace);
            return string.Empty;
        }

        try
        {
            var data = ItemRegistry.GetData($"(O){rawToken}");
            if (data is not null) return $"(O){rawToken}";
        }
        catch { }

        try
        {
            var data = ItemRegistry.GetData(rawToken);
            if (data is not null) return data.QualifiedItemId;
        }
        catch { }

        foreach (string prefix in new[] { "(O)", "(BC)", "(F)", "(W)", "(H)", "(S)", "(P)" })
        {
            try
            {
                var data = ItemRegistry.GetData($"{prefix}{rawToken}");
                if (data is not null) return $"{prefix}{rawToken}";
            }
            catch { }
        }

        _monitor.Log($"[BundleScanner] '{rawToken}' item token çözümlenemedi. / Could not resolve item token '{rawToken}' via ItemRegistry", LogLevel.Trace);
        return string.Empty;
    }


    private void EnsureCropInfoCache()
    {
        if (_cropInfoCache is not null) return;
        _cropInfoCache = new Dictionary<string, (int, string?, bool, List<string>)>(StringComparer.OrdinalIgnoreCase);
        _seedCache     = new Dictionary<string, (int, string?, bool, List<string>, string)>(StringComparer.OrdinalIgnoreCase);

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

            bool isGreenhouse = data.Seasons is null || data.Seasons.Count == 0;
            var seasonsList   = isGreenhouse
                ? new List<string>()
                : data.Seasons!.Select(s => s.ToString().ToLower()).ToList();
            string? season    = seasonsList.Count > 0 ? seasonsList[0] : null;

            int harvestIntId = int.TryParse(data.HarvestItemId, out int hid) ? hid : -1;
            string harvestQualified = harvestIntId > 0
                ? $"(O){harvestIntId}"
                : $"(O){data.HarvestItemId}";

            if (harvestIntId < 0)
            {
                try
                {
                    var itemData = ItemRegistry.GetData($"(O){data.HarvestItemId}");
                    if (itemData is not null) harvestQualified = itemData.QualifiedItemId;
                }
                catch { }
            }

            var cropTuple = (growDays, season, isGreenhouse, seasonsList);
            var seedTuple = (growDays, season, isGreenhouse, seasonsList, harvestQualified);

            string normalizedHarvest = harvestIntId > 0 ? $"(O){harvestIntId}" : harvestQualified;
            _cropInfoCache.TryAdd(normalizedHarvest, cropTuple);
            _cropInfoCache.TryAdd(data.HarvestItemId, cropTuple);

            _seedCache.TryAdd(seedKey, seedTuple);
            if (int.TryParse(seedKey, out int seedInt))
                _seedCache.TryAdd($"(O){seedInt}", seedTuple);
            else
                _seedCache.TryAdd($"(O){seedKey}", seedTuple);
        }
    }

    private (int growDays, string? season, bool isGreenhouse, List<string> seasons) GetCropInfo(string qualifiedId, int legacyId)
    {
        if (_cropInfoCache is null) return (0, null, false, new List<string>());

        if (_cropInfoCache.TryGetValue(qualifiedId, out var info)) return info;

        if (legacyId > 0 && _cropInfoCache.TryGetValue(legacyId.ToString(), out info)) return info;

        return (0, null, false, new List<string>());
    }


    private void EnsureRainFishCache()
    {
        if (_rainFishCache is not null) return;
        _rainFishCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _fishInfoCache = new Dictionary<string, (List<string>, string?, string?)>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var fishData = _gameContent.Load<Dictionary<string, string>>("Data/Fish");
            foreach (var (fishId, raw) in fishData)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string[] fields = raw.Split('/');
                if (fields.Length <= 7) continue;

                string weather = fields[7].Trim().ToLower();

                string? timeRange = null;
                if (fields.Length > 5)
                {
                    string[] times = fields[5].Trim().Split(' ');
                    if (times.Length >= 2
                        && int.TryParse(times[0], out int tStart)
                        && int.TryParse(times[1], out int tEnd))
                    {
                        timeRange = $"{FormatGameTime(tStart)}-{FormatGameTime(tEnd)}";
                    }
                }

                var locations = new List<string>();
                if (fields.Length > 4)
                {
                    foreach (var loc in fields[4].Trim().Split(' '))
                    {
                        string mapped = MapFishLocation(loc.Trim());
                        if (!string.IsNullOrWhiteSpace(mapped) && !locations.Contains(mapped))
                            locations.Add(mapped);
                    }
                }

                string? weatherLabel = weather switch
                {
                    "rainy" => "rain",
                    "sunny" => "sunny",
                    _       => null,
                };

                var entry = (locations, timeRange, weatherLabel);

                var keys = ResolveAllFishKeys(fishId);
                foreach (var k in keys)
                {
                    _fishInfoCache.TryAdd(k, entry);
                    if (weather is "rainy" or "both")
                        _rainFishCache.Add(k);
                }
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"[BundleScanner] Data/Fish yüklenemedi / could not load: {ex.Message}", LogLevel.Trace);
        }

        EnrichFishLocationsFromLocationData();
    }

    private void EnrichFishLocationsFromLocationData()
    {
        try
        {
            var locationData = _gameContent.Load<Dictionary<string, StardewValley.GameData.Locations.LocationData>>("Data/Locations");
            foreach (var (locationName, data) in locationData)
            {
                if (data?.Fish is null) continue;
                string mappedLocation = MapLocationName(locationName);
                if (string.IsNullOrWhiteSpace(mappedLocation)) continue;

                foreach (var fishEntry in data.Fish)
                {
                    var itemIds = new List<string>();
                    if (!string.IsNullOrWhiteSpace(fishEntry.ItemId))
                        itemIds.Add(fishEntry.ItemId);
                    if (fishEntry.RandomItemId is not null)
                        itemIds.AddRange(fishEntry.RandomItemId);

                    foreach (var rawId in itemIds)
                    {
                        if (string.IsNullOrWhiteSpace(rawId)) continue;
                        var keys = ResolveAllFishKeys(rawId);
                        foreach (var k in keys)
                        {
                            if (_fishInfoCache!.TryGetValue(k, out var existing))
                            {
                                if (!existing.Item1.Contains(mappedLocation))
                                {
                                    var newLocs = new List<string>(existing.Item1) { mappedLocation };
                                    _fishInfoCache[k] = (newLocs, existing.Item2, existing.Item3);
                                }
                            }
                            else
                            {
                                _fishInfoCache!.TryAdd(k, (new List<string> { mappedLocation }, null, null));
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"[BundleScanner] Data/Locations balık zenginleştirme hatası / fish enrichment error: {ex.Message}", LogLevel.Trace);
        }
    }

    private static string MapLocationName(string locationName) => locationName.ToLower() switch
    {
        "beach"             => "Ocean",
        "forest"            => "Forest",
        "mountain"          => "Mountain Lake",
        "town"              => "Town",
        "mine"              => "Mines",
        "desert"            => "Desert",
        "islandwest"        => "Island",
        "islandsouth"       => "Island Ocean",
        "islandnorth"       => "Island",
        "islandeast"        => "Island",
        "sewer"             => "Sewer",
        "witchswamp"        => "Witch's Swamp",
        "volcanodungeon"    => "Volcano",
        "submarine"         => "Submarine",
        "bathhouse_pool"    => "Spa",
        _                   => string.Empty,
    };

    private static List<string> ResolveAllFishKeys(string fishId)
    {
        var keys = new List<string>();

        if (string.IsNullOrWhiteSpace(fishId)) return keys;

        if (fishId.StartsWith("(", StringComparison.Ordinal))
        {
            keys.Add(fishId);
            string inner = fishId[(fishId.IndexOf(')') + 1)..];
            keys.Add(inner);
            keys.Add($"(O){inner}");
            return keys;
        }

        if (int.TryParse(fishId, out _))
        {
            keys.Add(fishId);
            keys.Add($"(O){fishId}");
        }
        else
        {
            keys.Add(fishId);
            keys.Add($"(O){fishId}");

            try
            {
                var data = ItemRegistry.GetData($"(O){fishId}");
                if (data is not null && !keys.Contains(data.QualifiedItemId))
                    keys.Add(data.QualifiedItemId);
            }
            catch { }

            try
            {
                var data = ItemRegistry.GetData(fishId);
                if (data is not null && !keys.Contains(data.QualifiedItemId))
                    keys.Add(data.QualifiedItemId);
            }
            catch { }
        }

        return keys;
    }

    private static string FormatGameTime(int t)
    {
        int h = t / 100;
        int m = t % 100;
        return m == 0 ? $"{h}:00" : $"{h}:{m:D2}";
    }

    private static string MapFishLocation(string loc) => loc.ToLower() switch
    {
        "ocean"        => "Ocean",
        "river"        => "River",
        "lake"         => "Lake",
        "forest"       => "Forest",
        "mountain"     => "Mountain Lake",
        "mines"        => "Mines",
        "desert"       => "Desert",
        "town"         => "Town",
        "woodskip"     => "Secret Woods",
        "submarine"    => "Submarine",
        "witch"        => "Witch's Swamp",
        "volcano"      => "Volcano",
        "islandfreshwater" => "Island",
        "islandocean"  => "Island Ocean",
        _              => string.Empty,
    };

    public (List<string> locations, string? timeRange, string? weather) GetFishInfo(string qualifiedId)
    {
        if (_fishInfoCache is null) EnsureRainFishCache();
        if (_fishInfoCache!.TryGetValue(qualifiedId, out var info)) return info;

        string rawId = qualifiedId.StartsWith("(", StringComparison.OrdinalIgnoreCase)
            ? qualifiedId[(qualifiedId.IndexOf(')') + 1)..] : qualifiedId;

        if (_fishInfoCache.TryGetValue(rawId, out info)) return info;
        if (_fishInfoCache.TryGetValue($"(O){rawId}", out info)) return info;

        foreach (var key in _fishInfoCache.Keys)
        {
            string keyRaw = key.StartsWith("(", StringComparison.Ordinal)
                ? key[(key.IndexOf(')') + 1)..] : key;
            if (string.Equals(keyRaw, rawId, StringComparison.OrdinalIgnoreCase))
                return _fishInfoCache[key];
        }

        return (new List<string>(), null, null);
    }


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
            _monitor.Log($"[BundleScanner] Data/Shops yüklenemedi / could not load: {ex.Message}", LogLevel.Trace);
        }
    }

    private static string ResolveShopName(string shopId) => shopId switch
    {
        "SeedShop"              => "Pierre",
        "AnimalShop"            => "Marnie",
        "FishShop"              => "Willy",
        "ScienceHouse"          => "Robin",
        "Blacksmith"            => "Clint",
        "Hospital"              => "Harvey",
        "AdventureShop"         => "Marlon",
        "Saloon"                => "Gus",
        "Sandy"                 => "Sandy",
        "IceCreamStand"         => "Alex",
        "Krobus"                => "Krobus",
        "DesertShop"            => "Sandy",
        "HatMouse"              => "Hat Mouse",
        "QiGemShop"             => "Qi",
        "ResortBar"             => "Gus",
        "VolcanoShop"           => "Volcano Shop",
        "DesertFestival_Vincent" => "Vincent",
        "DesertFestival_Sophia" => "Sophia",
        "DesertFestival"        => "Desert Festival",
        "WizardShop"            => "Wizard",
        "TravelingMerchant"     => "Traveling Merchant",
        "Dwarf"                 => "Dwarf",
        "Marlon"                => "Marlon",
        "Pierre"                => "Pierre",
        _                       => PrettifyShopId(shopId),
    };

    private static string PrettifyShopId(string shopId)
    {
        if (string.IsNullOrWhiteSpace(shopId)) return shopId;

        int dotIdx = shopId.LastIndexOf('.');
        string name = dotIdx >= 0 ? shopId[(dotIdx + 1)..] : shopId;

        name = name.Replace('_', ' ');

        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]) && name[i - 1] != ' ')
                result.Append(' ');
            result.Append(c);
        }

        return result.ToString().Trim();
    }

    private string? ResolveShopSource(string qualifiedId, int legacyId)
    {
        if (_shopSourceCache is null) return null;
        if (_shopSourceCache.TryGetValue(qualifiedId, out string? shop)) return shop;
        if (legacyId > 0 && _shopSourceCache.TryGetValue($"(O){legacyId}", out shop)) return shop;
        return null;
    }


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
        try
        {
            Item? item = ItemRegistry.Create(qualifiedId, 1, quality, allowNull: true);
            if (item is not null && !string.IsNullOrWhiteSpace(item.DisplayName))
                return item.DisplayName;
        }
        catch { }

        if (legacyId > 0)
        {
            try
            {
                var obj = new StardewValley.Object(legacyId.ToString(), 1);
                if (!string.IsNullOrWhiteSpace(obj.DisplayName)) return obj.DisplayName;
            }
            catch { }
        }

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

        return qualifiedId.StartsWith("(O)", StringComparison.OrdinalIgnoreCase)
            ? qualifiedId[3..] : qualifiedId;
    }

    private bool IsRainItem(string qualifiedId, IReadOnlyCollection<string> tags)
    {
        if (_rainFishCache is not null && _rainFishCache.Contains(qualifiedId))
            return true;

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


    public (int growDays, string? season, string harvestQualifiedId, List<string> seasons) GetSeedInfo(string qualifiedSeedId)
    {
        if (_seedCache is null) EnsureCropInfoCache();

        string rawKey = qualifiedSeedId.StartsWith("(", StringComparison.Ordinal)
            ? qualifiedSeedId[(qualifiedSeedId.IndexOf(')') + 1)..]
            : qualifiedSeedId;

        if (_seedCache!.TryGetValue(rawKey, out var cached))
            return (cached.growDays, cached.season, cached.harvestQualifiedId, cached.seasons);
        if (_seedCache.TryGetValue(qualifiedSeedId, out cached))
            return (cached.growDays, cached.season, cached.harvestQualifiedId, cached.seasons);

        if (int.TryParse(rawKey, out int seedInt))
        {
            if (SeedToHarvest.TryGetValue(seedInt, out int fallbackHarvest))
            {
                string harvestQ = $"(O){fallbackHarvest}";
                if (_cropInfoCache?.TryGetValue(harvestQ, out var ci) == true)
                    return (ci.growDays, ci.season, harvestQ, ci.seasons);
                return (0, null, harvestQ, new List<string>());
            }
        }

        return (0, null, string.Empty, new List<string>());
    }

    public static (int growDays, string? season, int harvestId) GetCropInfoFromSeed(int seedId)
    {
        var result = GetCropInfoFromSeedKey(seedId.ToString());
        if (result.growDays > 0) return (result.growDays, result.season, result.harvestId);

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

    public static (int growDays, string? season, string harvestQualifiedId) GetCropInfoFromSeedQualified(string qualifiedSeedId)
    {
        string rawKey = qualifiedSeedId.StartsWith("(", StringComparison.Ordinal)
            ? qualifiedSeedId[(qualifiedSeedId.IndexOf(')') + 1)..]
            : qualifiedSeedId;

        if (int.TryParse(rawKey, out int intId))
        {
            var (gd, s, hid) = GetCropInfoFromSeed(intId);
            return (gd, s, hid > 0 ? $"(O){hid}" : string.Empty);
        }

        var info = GetCropInfoFromSeedKey(rawKey);
        return (info.growDays, info.season, info.harvestId > 0 ? $"(O){info.harvestId}" : info.harvestQualifiedId);
    }

    private static (int growDays, string? season, int harvestId, string harvestQualifiedId) GetCropInfoFromSeedKey(string key)
    {
        try
        {
            var crops = Game1.content.Load<Dictionary<string, CropData>>("Data/Crops");
            if (crops.TryGetValue(key, out var data))
            {
                string? season = data.Seasons?.Count > 0 ? data.Seasons[0].ToString().ToLower() : null;
                int harvestIntId = int.TryParse(data.HarvestItemId, out int hid) ? hid : -1;
                string harvestQualified = harvestIntId > 0
                    ? $"(O){harvestIntId}"
                    : !string.IsNullOrWhiteSpace(data.HarvestItemId)
                        ? $"(O){data.HarvestItemId}"
                        : string.Empty;
                int growDays = 0;
                if (data.DaysInPhase != null)
                    foreach (var d in data.DaysInPhase)
                        if (d > 0) growDays += d;
                return (growDays, season, harvestIntId, harvestQualified);
            }
        }
        catch { }

        try
        {
            var crops = Game1.content.Load<Dictionary<string, string>>("Data/Crops");
            if (crops.TryGetValue(key, out string? raw))
            {
                string[] p = raw.Split('/');
                if (p.Length >= 4)
                {
                    string season  = p[0].Split(' ')[0].ToLower();
                    int harvestId  = int.TryParse(p[3], out int hid) ? hid : -1;
                    int growDays   = 0;
                    foreach (var part in p[1].Split(' '))
                        if (int.TryParse(part, out int d) && d > 0) growDays += d;
                    return (growDays, season, harvestId, harvestId > 0 ? $"(O){harvestId}" : string.Empty);
                }
            }
        }
        catch { }

        return (0, null, -1, string.Empty);
    }


    private void EnsureFruitTreeCache()
    {
        if (_fruitTreeCache is not null) return;
        _fruitTreeCache = new Dictionary<string, (string, string, List<string>)>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var trees = Game1.content.Load<Dictionary<string, StardewValley.GameData.FruitTrees.FruitTreeData>>("Data/FruitTrees");
            _monitor.Log($"[FruitTree] Data/FruitTrees yüklendi / loaded: {trees.Count} kayıt / records", LogLevel.Debug);
            foreach (var (saplingKey, data) in trees)
            {
                if (data is null) continue;

                string productQualifiedId = string.Empty;
                string productName        = string.Empty;
                if (data.Fruit?.Count > 0)
                {
                    var firstFruit = data.Fruit[0];
                    string rawId   = firstFruit?.ItemId ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(rawId))
                    {
                        productQualifiedId = rawId.StartsWith("(", StringComparison.Ordinal)
                            ? rawId : $"(O){rawId}";
                        try
                        {
                            var item = ItemRegistry.Create(productQualifiedId, 1, 0, allowNull: true);
                            productName = item?.DisplayName ?? productQualifiedId;
                        }
                        catch { productName = productQualifiedId; }
                    }
                }

                var seasons = data.Seasons?.Select(s => s.ToString().ToLower()).ToList()
                           ?? new List<string>();

                bool isAlreadyQualified = saplingKey.StartsWith("(", StringComparison.Ordinal);
                bool isInt = int.TryParse(saplingKey, out int saplingInt);

                string saplingQualifiedO = isAlreadyQualified ? saplingKey
                    : isInt ? $"(O){saplingInt}" : $"(O){saplingKey}";
                string saplingQualifiedF = isAlreadyQualified
                    ? System.Text.RegularExpressions.Regex.Replace(saplingKey, @"^\([^)]+\)", "(F)")
                    : isInt ? $"(F){saplingInt}" : $"(F){saplingKey}";

                var entry = (productQualifiedId, productName, seasons);
                _fruitTreeCache.TryAdd(saplingKey, entry);
                _fruitTreeCache.TryAdd(saplingQualifiedO, entry);
                _fruitTreeCache.TryAdd(saplingQualifiedF, entry);

                _monitor.Log($"[FruitTree] Cache: {saplingKey} / {saplingQualifiedF} -> {productName} [{string.Join(",", seasons)}]", LogLevel.Trace);
            }
            _monitor.Log($"[FruitTree] Cache dolduruldu / filled: {_fruitTreeCache.Count} giris / entries ({trees.Count} agac / trees)", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            _monitor.Log($"[BundleScanner] Data/FruitTrees yuklenemedi / could not load: {ex.Message}", LogLevel.Warn);
        }
    }

    public (string productQualifiedId, string productName, List<string> seasons) GetFruitTreeInfo(string qualifiedSaplingId)
    {
        EnsureFruitTreeCache();
        if (_fruitTreeCache is null) return (string.Empty, string.Empty, new List<string>());

        if (_fruitTreeCache.TryGetValue(qualifiedSaplingId, out var info)) return info;

        string rawKey = qualifiedSaplingId.StartsWith("(", StringComparison.Ordinal)
            ? qualifiedSaplingId[(qualifiedSaplingId.IndexOf(')') + 1)..]
            : qualifiedSaplingId;
        if (_fruitTreeCache.TryGetValue(rawKey, out info)) return info;

        try
        {
            var trees = Game1.content.Load<Dictionary<string, StardewValley.GameData.FruitTrees.FruitTreeData>>("Data/FruitTrees");
            if (trees.TryGetValue(rawKey, out var data) && data is not null)
            {
                string productQualifiedId = string.Empty;
                string productName        = string.Empty;
                if (data.Fruit?.Count > 0)
                {
                    string rawId = data.Fruit[0]?.ItemId ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(rawId))
                    {
                        productQualifiedId = rawId.StartsWith("(", StringComparison.Ordinal) ? rawId : $"(O){rawId}";
                        try
                        {
                            var item = ItemRegistry.Create(productQualifiedId, 1, 0, allowNull: true);
                            productName = item?.DisplayName ?? productQualifiedId;
                        }
                        catch { productName = productQualifiedId; }
                    }
                }
                var seasons = data.Seasons?.Select(s => s.ToString().ToLower()).ToList() ?? new List<string>();
                var entry = (productQualifiedId, productName, seasons);
                _fruitTreeCache.TryAdd(qualifiedSaplingId, entry);
                _fruitTreeCache.TryAdd(rawKey, entry);
                return entry;
            }
        }
        catch { }

        return (string.Empty, string.Empty, new List<string>());
    }

    public bool IsFruitTreeSapling(string qualifiedId)
    {
        if (string.IsNullOrWhiteSpace(qualifiedId)) return false;
        EnsureFruitTreeCache();
        if (_fruitTreeCache is null) return false;

        if (_fruitTreeCache.ContainsKey(qualifiedId)) return true;

        string rawKey = qualifiedId.StartsWith("(", StringComparison.Ordinal)
            ? qualifiedId[(qualifiedId.IndexOf(')') + 1)..]
            : qualifiedId;

        return _fruitTreeCache.ContainsKey(rawKey);
    }


    private void EnsureCustomBushCache()
    {
        if (_customBushCache is not null) return;
        _customBushCache = new Dictionary<string, (string, string, List<string>, int)>(StringComparer.OrdinalIgnoreCase);

        if (!_modRegistry.IsLoaded("furyx639.CustomBush")) return;

        try
        {
            var customBushAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "CustomBush");
            if (customBushAssembly is null)
            {
                _monitor.Log("[CustomBush] Assembly bulunamadi / Assembly not found", LogLevel.Trace);
                return;
            }

            var customBushDataType = customBushAssembly.GetType("LeFauxMods.CustomBush.Models.CustomBushData");
            if (customBushDataType is null)
            {
                _monitor.Log("[CustomBush] CustomBushData tipi bulunamadi / CustomBushData type not found", LogLevel.Trace);
                return;
            }

            var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), customBushDataType);

            var loadMethod = _gameContent.GetType()
                .GetMethods()
                .FirstOrDefault(m => m.Name == "Load" && m.IsGenericMethod && m.GetParameters().Length == 1)
                ?.MakeGenericMethod(dictType);

            var rawData = loadMethod?.Invoke(_gameContent, new object[] { "furyx639.CustomBush/Data" }) as System.Collections.IDictionary;
            if (rawData is null) return;

            var ageField     = customBushDataType.GetProperty("AgeToProduce") ?? (System.Reflection.MemberInfo?)customBushDataType.GetField("AgeToProduce");
            var seasonsField  = customBushDataType.GetProperty("Seasons")      ?? (System.Reflection.MemberInfo?)customBushDataType.GetField("Seasons");
            var producedField = customBushDataType.GetProperty("ItemsProduced") ?? (System.Reflection.MemberInfo?)customBushDataType.GetField("ItemsProduced");

            foreach (System.Collections.DictionaryEntry kv in rawData)
            {
                string saplingQualifiedId = kv.Key?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(saplingQualifiedId) || kv.Value is null) continue;

                object entry = kv.Value;

                int ageToProduce = GetReflectionInt(entry, ageField);

                var seasons = new List<string>();
                object? seasonsObj = GetReflectionValue(entry, seasonsField);
                if (seasonsObj is System.Collections.IEnumerable seasonsEnum)
                    foreach (var s in seasonsEnum)
                        if (s?.ToString() is string sv && sv.Length > 0)
                            seasons.Add(sv.ToLower());

                string productQualifiedId = string.Empty;
                string productName        = string.Empty;
                object? producedObj = GetReflectionValue(entry, producedField);
                if (producedObj is System.Collections.IList producedList && producedList.Count > 0)
                {
                    var firstItem = producedList[0];
                    if (firstItem is not null)
                    {
                        var itemIdProp = firstItem.GetType().GetProperty("ItemId") ?? (System.Reflection.MemberInfo?)firstItem.GetType().GetField("ItemId");
                        string rawId = GetReflectionValue(firstItem, itemIdProp)?.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(rawId))
                        {
                            productQualifiedId = rawId.StartsWith("(", StringComparison.Ordinal) ? rawId : $"(O){rawId}";
                            try
                            {
                                var item = ItemRegistry.Create(productQualifiedId, 1, 0, allowNull: true);
                                productName = item?.DisplayName ?? productQualifiedId;
                            }
                            catch { productName = productQualifiedId; }
                        }
                    }
                }

                var bushEntry = (productQualifiedId, productName, seasons, ageToProduce);
                _customBushCache.TryAdd(saplingQualifiedId, bushEntry);
                string rawKey = saplingQualifiedId.StartsWith("(", StringComparison.Ordinal)
                    ? saplingQualifiedId[(saplingQualifiedId.IndexOf(')') + 1)..] : saplingQualifiedId;
                _customBushCache.TryAdd(rawKey, bushEntry);
            }
            _monitor.Log($"[CustomBush] Cache dolduruldu / filled: {_customBushCache.Count / 2} bush", LogLevel.Debug);
        }
        catch (Exception ex)
        {
            _monitor.Log($"[BundleScanner] CustomBush cache hatasi / error: {ex.Message}", LogLevel.Trace);
        }
    }

    private static object? GetReflectionValue(object obj, System.Reflection.MemberInfo? member) => member switch
    {
        System.Reflection.PropertyInfo p => p.GetValue(obj),
        System.Reflection.FieldInfo f    => f.GetValue(obj),
        _                                => null,
    };

    private static int GetReflectionInt(object obj, System.Reflection.MemberInfo? member)
    {
        var val = GetReflectionValue(obj, member);
        return val is int i ? i : val is long l ? (int)l : 0;
    }

    public bool IsCustomBushSapling(string qualifiedId)
    {
        if (string.IsNullOrWhiteSpace(qualifiedId)) return false;
        EnsureCustomBushCache();
        if (_customBushCache is null) return false;
        if (_customBushCache.ContainsKey(qualifiedId)) return true;
        string rawKey = qualifiedId.StartsWith("(", StringComparison.Ordinal)
            ? qualifiedId[(qualifiedId.IndexOf(')') + 1)..] : qualifiedId;
        return _customBushCache.ContainsKey(rawKey);
    }

    public (string productQualifiedId, string productName, List<string> seasons, int ageToProduce) GetCustomBushInfo(string qualifiedId)
    {
        EnsureCustomBushCache();
        if (_customBushCache is null) return (string.Empty, string.Empty, new List<string>(), 0);
        if (_customBushCache.TryGetValue(qualifiedId, out var info)) return info;
        string rawKey = qualifiedId.StartsWith("(", StringComparison.Ordinal)
            ? qualifiedId[(qualifiedId.IndexOf(')') + 1)..] : qualifiedId;
        if (_customBushCache.TryGetValue(rawKey, out info)) return info;
        return (string.Empty, string.Empty, new List<string>(), 0);
    }

    private void LogFrameworkCompatibility()    {
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
