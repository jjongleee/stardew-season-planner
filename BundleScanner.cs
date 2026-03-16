using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace SeasonPlanner;

public enum BundleCategory { Crop, Fish, Forage, Artisan, Construction, Other }

public sealed class BundleItem
{
    public int            ItemId       { get; init; }
    public string         ItemName     { get; init; } = string.Empty;
    public int            Quantity     { get; init; }
    public int            Quality      { get; init; }
    public string?        Season       { get; init; }
    public int            GrowDays     { get; init; }
    public string         BundleName   { get; init; } = string.Empty;
    public bool           RequiresRain { get; init; }
    public BundleCategory Category     { get; init; }
    public string?        ShopSource   { get; init; }
}

public sealed class BundleScanner
{
    // ── Hasat ID → büyüme günü (Data/Crops'tan türetilmiş, fallback) ─────
    private static readonly Dictionary<int, int> CropGrowDays = new()
    {
        { 24,  4 }, { 188, 4 }, { 190, 6 }, { 192, 4 }, { 254, 4 },
        { 256, 8 }, { 270, 4 }, { 272, 5 }, { 276, 9 }, { 280, 5 },
        { 281, 9 }, // Cranberry
        { 282, 5 }, // Beet
        { 283, 8 }, // Artichoke
        { 284, 6 }, { 300, 6 }, { 304, 6 }, { 398, 13 }, { 400, 6 },
        { 421, 8 }, // Sunflower
        { 433, 10}, // Coffee
        { 454, 8 }, // Ancient Fruit / Qi Fruit
        { 262, 14}, // Starfruit
        { 266, 14}, // Red Cabbage (summer)
        { 278, 14}, // Pumpkin
        { 745, 8 }, // Strawberry
    };

    // ── Yağmurda tutulan balıklar ─────────────────────────────────────────
    private static readonly HashSet<int> RainFish = new()
    { 137, 138, 142, 143, 145, 149, 150, 151 };
    // Catfish, Catfish(2), Eel, Octopus, Catfish(bait), Eel(2), Perch, Squid

    // ── İnşaat malzemeleri ────────────────────────────────────────────────
    private static readonly HashSet<int> ConstructionItems = new()
    { 388, 390, 709, 766, 767, 382, 378, 380, 384, 386 };

    // ── Toplama (forage) öğeleri ──────────────────────────────────────────
    private static readonly HashSet<int> ForageItems = new()
    {
        16, 18, 20, 22,          // Spring: Daffodil, Leek, Dandelion, Parsnip
        78, 281, 396, 398, 402,  // Summer: Cave Carrot, Cranberry, Spice Berry, Grape, Sweet Pea
        281, 404, 406, 408, 410, // Fall: Chanterelle, Common Mushroom, Red Mushroom, Purple Mushroom
        412, 414, 416, 418,      // Winter: Crystal Fruit, Holly, Crocus, Snow Yam
        88, 90, 78, 420,         // Misc forage
        257, 281, 724, 725, 726, // Beach forage
    };

    // ── Zanaat ürünleri ───────────────────────────────────────────────────
    private static readonly HashSet<int> ArtisanIds = new()
    {
        340, 342, 344, 346, 348, 350, // Honey, Mayo, Void Mayo, Cheese, Goat Cheese, Cloth
        424, 426, 428, 432,           // Jelly, Juice, Wine, Pale Ale
        614, 724, 725, 726,           // Roe, Caviar, Aged Roe, Mead
        306, 307, 308,                // Mayonnaise (quality), Cheese (quality)
        614, 812,                     // Roe, Smoked Fish
    };

    // ── Mağaza kaynakları ─────────────────────────────────────────────────
    private static readonly Dictionary<int, string> ShopSources = new()
    {
        // Pierre — Tohum dükkanı
        { 24,  "Pierre" }, { 188, "Pierre" }, { 190, "Pierre" }, { 192, "Pierre" },
        { 254, "Pierre" }, { 256, "Pierre" }, { 270, "Pierre" }, { 272, "Pierre" },
        { 276, "Pierre" }, { 280, "Pierre" }, { 284, "Pierre" }, { 300, "Pierre" },
        { 304, "Pierre" }, { 400, "Pierre" }, { 421, "Pierre" }, { 262, "Pierre" },
        { 266, "Pierre" }, { 278, "Pierre" }, { 281, "Pierre" }, { 282, "Pierre" },
        { 283, "Pierre" },
        // Marnie — Hayvan ürünleri
        { 176, "Marnie" }, { 174, "Marnie" }, // Egg, Large Egg
        { 186, "Marnie" }, { 438, "Marnie" }, // Large Milk, Large Goat Milk
        { 182, "Marnie" },                     // Milk
        // Willy — Balık dükkanı
        { 145, "Willy"  }, { 140, "Willy"  }, // Catfish, Walleye
        { 131, "Willy"  },                     // Sardine
        // Krobus — Kanalizasyon
        { 372, "Krobus" }, { 766, "Krobus" }, // Void Egg, Slime
        // Gus — Saloon
        { 346, "Gus"    }, { 348, "Gus"    }, // Beer, Pale Ale
        // Harvey — Klinik
        { 773, "Harvey" },                     // Life Elixir
        // Traveling Cart — Gezgin satıcı
        { 745, "Gezgin Satıcı" }, { 433, "Gezgin Satıcı" },
    };

    // ── Tohum ID → hasat ID (fallback tablosu) ────────────────────────────
    public static readonly Dictionary<int, int> SeedToHarvest = new()
    {
        { 472, 24  }, { 473, 188 }, { 474, 190 }, { 475, 192 },
        { 476, 254 }, { 477, 256 }, { 478, 270 }, { 479, 272 },
        { 480, 276 }, { 481, 280 }, { 482, 284 }, { 483, 300 },
        { 484, 304 }, { 485, 398 }, { 486, 400 }, { 487, 454 },
        { 488, 24  }, { 489, 270 }, { 490, 300 },
        { 495, 282 }, { 496, 281 }, { 497, 283 },
        { 499, 433 }, { 745, 262 }, { 802, 454 },
        { 431, 421 }, // Sunflower Seeds → Sunflower
        { 347, 262 }, // Rare Seed → Sweet Gem Berry
        { 251, 433 }, // Coffee Bean → Coffee
    };

    private readonly IMonitor _monitor;
    private string            _cacheKey = string.Empty;
    private List<BundleItem>  _cache    = new();

    // Dinamik olarak yüklenen balık ID seti (ilk kullanımda doldurulur)
    private static HashSet<int>? _dynamicFishIds;
    private static HashSet<int> GetFishIds()
    {
        if (_dynamicFishIds != null) return _dynamicFishIds;
        _dynamicFishIds = new HashSet<int>
        {
            128,129,130,131,132,136,137,138,139,140,141,142,143,144,145,146,147,148,
            149,150,151,152,153,154,155,156,157,158,159,160,161,162,163,164,165,
            698,699,700,701,702,703,704,705,706,707,708,715,716,717,718,719,720,
            721,722,723,734,775,795,796,798,799,800,801
        };
        try
        {
            var data = Game1.content.Load<Dictionary<string, string>>("Data/Fish");
            foreach (var k in data.Keys)
                if (int.TryParse(k, out int id)) _dynamicFishIds.Add(id);
        }
        catch { }
        return _dynamicFishIds;
    }

    public BundleScanner(IMonitor monitor) => _monitor = monitor;

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
        _cacheKey      = string.Empty;
        _dynamicFishIds = null; // Yeni save yüklenince balık listesini de sıfırla
    }

    private List<BundleItem> BuildMissingList()
    {
        var result = new List<BundleItem>();

        if (Game1.getLocationFromName("CommunityCenter") is not CommunityCenter cc)
            return result;

        Dictionary<string, string> bundleData;
        try { bundleData = Game1.content.Load<Dictionary<string, string>>("Data/Bundles"); }
        catch { return result; }

        foreach (var (key, raw) in bundleData)
        {
            string[] parts = raw.Split('/');
            if (parts.Length < 3) continue;

            string bundleName = LocalizeBundleName(parts[0]);
            string itemsRaw   = parts[2];

            if (!int.TryParse(key.Split('/').Last(), out int bundleIndex)) continue;
            if (!cc.bundles.TryGetValue(bundleIndex, out bool[] completion))  continue;

            string[] tokens = itemsRaw.Split(' ');
            for (int i = 0; i + 2 < tokens.Length; i += 3)
            {
                if (!int.TryParse(tokens[i],     out int itemId))  continue;
                if (!int.TryParse(tokens[i + 1], out int qty))     continue;
                if (!int.TryParse(tokens[i + 2], out int quality)) continue;
                if (itemId < 0) continue;

                int slot = i / 3;
                if (slot < completion.Length && completion[slot]) continue;

                int growDays = GetGrowDays(itemId);

                result.Add(new BundleItem
                {
                    ItemId       = itemId,
                    ItemName     = GetItemName(itemId),
                    Quantity     = qty,
                    Quality      = quality,
                    Season       = GetItemSeason(itemId),
                    GrowDays     = growDays,
                    BundleName   = bundleName,
                    RequiresRain = RainFish.Contains(itemId),
                    Category     = ClassifyItem(itemId),
                    ShopSource   = ShopSources.GetValueOrDefault(itemId),
                });
            }
        }

        _monitor.Log($"[BundleScanner] {result.Count} eksik eşya tarandı.", LogLevel.Debug);
        return result;
    }

    // Data/Crops'tan dinamik büyüme günü okuma, fallback CropGrowDays
    private static int GetGrowDays(int harvestId)
    {
        if (CropGrowDays.TryGetValue(harvestId, out int days)) return days;
        try
        {
            var crops = Game1.content.Load<Dictionary<string, StardewValley.GameData.Crops.CropData>>("Data/Crops");
            foreach (var (_, data) in crops)
            {
                if (!int.TryParse(data.HarvestItemId, out int hid) || hid != harvestId) continue;
                int total = 0;
                if (data.DaysInPhase != null)
                    foreach (var d in data.DaysInPhase)
                        if (d > 0) total += d;
                return total;
            }
        }
        catch { }
        return 0;
    }

    private static string LocalizeBundleName(string englishName)
    {
        string key = "bundle." + englishName.ToLower().Replace(' ', '_').Replace("'", "");
        string localized = I18n.BundleName(key);
        return localized == key ? englishName : localized;
    }

    private static BundleCategory ClassifyItem(int id)
    {
        if (ConstructionItems.Contains(id)) return BundleCategory.Construction;
        if (GetFishIds().Contains(id))      return BundleCategory.Fish;
        if (ArtisanIds.Contains(id))        return BundleCategory.Artisan;
        if (ForageItems.Contains(id))       return BundleCategory.Forage;
        if (CropGrowDays.ContainsKey(id))   return BundleCategory.Crop;
        // Data/Crops'ta hasat ID olarak geçiyorsa Crop
        if (GetGrowDays(id) > 0)            return BundleCategory.Crop;
        return BundleCategory.Other;
    }

    private static string GetItemName(int id)
    {
        try   { return new StardewValley.Object(id.ToString(), 1).DisplayName; }
        catch { return $"#{id}"; }
    }

    private static string? GetItemSeason(int itemId)
    {
        try
        {
            var crops = Game1.content.Load<Dictionary<string, StardewValley.GameData.Crops.CropData>>("Data/Crops");
            foreach (var (_, data) in crops)
            {
                if (!int.TryParse(data.HarvestItemId, out int hid) || hid != itemId) continue;
                return data.Seasons?.Count > 0 ? data.Seasons[0].ToString().ToLower() : null;
            }
        }
        catch { }
        return null;
    }

    /// <summary>Tohum ID'sinden büyüme günü, mevsim ve hasat ID'si döndürür.</summary>
    public static (int growDays, string? season, int harvestId) GetCropInfoFromSeed(int seedId)
    {
        try
        {
            var crops = Game1.content.Load<Dictionary<string, StardewValley.GameData.Crops.CropData>>("Data/Crops");
            if (crops.TryGetValue(seedId.ToString(), out var data))
            {
                string? season   = data.Seasons?.Count > 0 ? data.Seasons[0].ToString().ToLower() : null;
                int     harvestId = int.TryParse(data.HarvestItemId, out int hid) ? hid : -1;
                int     growDays  = 0;
                if (data.DaysInPhase != null)
                    foreach (var d in data.DaysInPhase)
                        if (d > 0) growDays += d;
                return (growDays, season, harvestId);
            }
        }
        catch { }
        // Fallback: eski string format dene
        try
        {
            var crops = Game1.content.Load<Dictionary<string, string>>("Data/Crops");
            if (crops.TryGetValue(seedId.ToString(), out string? raw))
            {
                string[] p = raw.Split('/');
                if (p.Length >= 4)
                {
                    string season    = p[0].Split(' ')[0].ToLower();
                    int    harvestId = int.TryParse(p[3], out int hid) ? hid : -1;
                    int    growDays  = 0;
                    foreach (var part in p[1].Split(' '))
                        if (int.TryParse(part, out int d) && d > 0) growDays += d;
                    return (growDays, season, harvestId);
                }
            }
        }
        catch { }
        // Son fallback: statik tablo
        if (SeedToHarvest.TryGetValue(seedId, out int fallbackHarvest))
        {
            int fallbackGrow = CropGrowDays.GetValueOrDefault(fallbackHarvest, 0);
            return (fallbackGrow, null, fallbackHarvest);
        }
        return (0, null, -1);
    }
}
