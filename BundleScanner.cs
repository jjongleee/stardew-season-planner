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
    public string?        ShopSource   { get; init; } // null = mağazada yok
}

public sealed class BundleScanner
{
    // harvest item ID → büyüme günü
    private static readonly Dictionary<int, int> CropGrowDays = new()
    {
        { 24,  4 }, { 188, 4 }, { 190, 6 }, { 192, 4 }, { 254, 4 },
        { 256, 8 }, { 270, 4 }, { 272, 5 }, { 276, 9 }, { 280, 5 },
        { 284, 6 }, { 300, 6 }, { 304, 6 }, { 398, 13 }, { 400, 6 }, { 454, 8 },
    };

    // Yağmurda tutulan balıklar
    private static readonly HashSet<int> RainFish = new() { 145, 137, 142 };

    // İnşaat malzemeleri (Construction kategorisi)
    private static readonly HashSet<int> ConstructionItems = new()
    { 388, 390, 709, 766, 767, 382, 378, 380, 384, 386 }; // Wood, Stone, Hardwood, Slime, Bat Wing, Coal, Copper, Iron, Gold, Iridium

    // Mağaza kaynakları: item ID → satıcı adı
    private static readonly Dictionary<int, string> ShopSources = new()
    {
        { 24,  "Pierre" }, { 188, "Pierre" }, { 190, "Pierre" }, { 192, "Pierre" },
        { 254, "Pierre" }, { 256, "Pierre" }, { 270, "Pierre" }, { 272, "Pierre" },
        { 276, "Pierre" }, { 280, "Pierre" }, { 284, "Pierre" }, { 300, "Pierre" },
        { 304, "Pierre" }, { 400, "Pierre" },
        { 176, "Marnie" }, { 174, "Marnie" }, // Egg, Large Egg
        { 186, "Marnie" }, { 438, "Marnie" }, // Large Milk, Large Goat Milk
        { 340, "Willy"  }, // Honey
        { 145, "Willy"  }, // Catfish (bait)
    };

    // Balık item ID'leri
    private static readonly HashSet<int> FishIds = new()
    { 128,129,130,131,132,136,137,138,139,140,141,142,143,144,145,146,147,148,
      149,150,151,152,153,154,155,156,157,158,159,160,161,162,163,164,165,
      698,699,700,701,702,703,704,705,706,707,708,715,716,717,718,719,720,
      721,722,723,734,775,795,796,798,799,800,801 };

    // Zanaat ürünleri
    private static readonly HashSet<int> ArtisanIds = new()
    { 340, 344, 346, 348, 350, 432, 614, 724, 725, 726, 432, 428, 426, 424 };

    private readonly IMonitor _monitor;
    private string            _cacheKey = string.Empty;
    private List<BundleItem>  _cache    = new();

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

    public void Invalidate() => _cacheKey = string.Empty;

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

            string bundleName = parts[0];
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

                result.Add(new BundleItem
                {
                    ItemId       = itemId,
                    ItemName     = GetItemName(itemId),
                    Quantity     = qty,
                    Quality      = quality,
                    Season       = GetItemSeason(itemId),
                    GrowDays     = CropGrowDays.GetValueOrDefault(itemId, 0),
                    BundleName   = bundleName,
                    RequiresRain = RainFish.Contains(itemId),
                    Category     = ClassifyItem(itemId),
                    ShopSource   = ShopSources.GetValueOrDefault(itemId),
                });
            }
        }

        _monitor.Log($"[BundleScanner] {result.Count} eksik eşya.", LogLevel.Debug);
        return result;
    }

    private static BundleCategory ClassifyItem(int id)
    {
        if (ConstructionItems.Contains(id)) return BundleCategory.Construction;
        if (FishIds.Contains(id))           return BundleCategory.Fish;
        if (ArtisanIds.Contains(id))        return BundleCategory.Artisan;
        if (CropGrowDays.ContainsKey(id))   return BundleCategory.Crop;
        return BundleCategory.Other;
    }

    private static string GetItemName(int id)
    {
        try   { return new StardewValley.Object(id.ToString(), 1).DisplayName; }
        catch { return $"Item#{id}"; }
    }

    private static string? GetItemSeason(int itemId)
    {
        try
        {
            var crops = Game1.content.Load<Dictionary<string, string>>("Data/Crops");
            foreach (var (_, cropRaw) in crops)
            {
                string[] p = cropRaw.Split('/');
                if (p.Length < 4) continue;
                if (!int.TryParse(p[3], out int harvestId) || harvestId != itemId) continue;
                return p[0].Split(' ')[0].ToLower();
            }
        }
        catch { }
        return null;
    }
}
