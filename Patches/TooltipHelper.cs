using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace SeasonPlanner.Patches;

internal static class TooltipHelper
{
    private static readonly Color ColorCrop         = new(34, 139, 34);
    private static readonly Color ColorFish         = new(30, 100, 200);
    private static readonly Color ColorArtisan      = new(160, 60, 160);
    private static readonly Color ColorConstruction = new(139, 90, 43);
    private static readonly Color ColorOther        = new(180, 120, 0);

    private const string Separator = "- - - - - - - - - - - - - -";

    internal static Color GetCategoryColor(BundleCategory cat) => cat switch
    {
        BundleCategory.Crop         => ColorCrop,
        BundleCategory.Fish         => ColorFish,
        BundleCategory.Artisan      => ColorArtisan,
        BundleCategory.Construction => ColorConstruction,
        _                           => ColorOther,
    };

    internal static string GetCategoryLabel(BundleCategory cat) => I18n.CategoryLabel(cat);

    internal static void DrawBundleTooltip(
        SpriteBatch b,
        Item? hovered,
        IReadOnlyList<BundleItem> missing,
        ModConfig config,
        int vanillaTooltipWidth = 0)
    {
        if (!config.ShowInventoryTooltips) return;
        if (hovered is null) return;

        string qualifiedId = hovered.QualifiedItemId ?? string.Empty;
        float bundleScale = Math.Clamp(config.BundleTooltipScale / 100f, 0.50f, 2.00f);
        float seedScale   = Math.Clamp(config.SeedTooltipScale   / 100f, 0.50f, 2.00f);

        var scanner = ModEntry.Instance?.Scanner;

        bool isFTypeSapling = qualifiedId.StartsWith("(F)", System.StringComparison.OrdinalIgnoreCase);
        if (isFTypeSapling || (scanner is not null && scanner.IsFruitTreeSapling(qualifiedId)))
        {
            string treeProductId   = string.Empty;
            string treeProductName = string.Empty;
            var    treeSeasons     = new List<string>();
            if (scanner is not null)
            {
                var treeInfo = scanner.GetFruitTreeInfo(qualifiedId);
                treeProductId   = treeInfo.productQualifiedId;
                treeProductName = treeInfo.productName;
                treeSeasons     = treeInfo.seasons;
            }
            var productMatches = !string.IsNullOrWhiteSpace(treeProductId)
                ? missing.Where(bi => string.Equals(bi.QualifiedItemId, treeProductId,
                                    System.StringComparison.OrdinalIgnoreCase)).ToList()
                : new List<BundleItem>();
            DrawFruitTreeTooltip(b, treeProductName, treeSeasons,
                BuildBundleLines(productMatches, config), vanillaTooltipWidth, seedScale);
            return;
        }

        if (scanner is not null && scanner.IsCustomBushSapling(qualifiedId))
        {
            var bushInfo = scanner.GetCustomBushInfo(qualifiedId);
            var productMatches = !string.IsNullOrWhiteSpace(bushInfo.productQualifiedId)
                ? missing.Where(bi => string.Equals(bi.QualifiedItemId, bushInfo.productQualifiedId,
                                    System.StringComparison.OrdinalIgnoreCase)).ToList()
                : new List<BundleItem>();
            DrawFruitTreeTooltip(b, bushInfo.productName, bushInfo.seasons,
                BuildBundleLines(productMatches, config), vanillaTooltipWidth, seedScale);
            return;
        }

        if (hovered is not StardewValley.Object obj) return;

        int itemId = obj.ParentSheetIndex;

        bool isSeed = obj.Category == StardewValley.Object.SeedsCategory
                   || BundleScanner.SeedToHarvest.ContainsKey(itemId)
                   || IsCropSeed(qualifiedId, scanner);

        if (isSeed)
        {
            List<string> seasons;
            string harvestQualified;
            int growDays;
            string? season;

            if (scanner is not null)
            {
                var info = scanner.GetSeedInfo(qualifiedId);
                growDays        = info.growDays;
                season          = info.season;
                harvestQualified = info.harvestQualifiedId;
                seasons         = info.seasons;
            }
            else
            {
                var info = BundleScanner.GetCropInfoFromSeedQualified(qualifiedId);
                growDays        = info.growDays;
                season          = info.season;
                harvestQualified = info.harvestQualifiedId;
                seasons         = season is not null ? new List<string> { season } : new List<string>();
            }

            var harvestMatches = !string.IsNullOrWhiteSpace(harvestQualified)
                ? missing.Where(bi => string.Equals(bi.QualifiedItemId, harvestQualified,
                                    System.StringComparison.OrdinalIgnoreCase)).ToList()
                : new List<BundleItem>();

            if (harvestMatches.Count == 0 && itemId > 0)
            {
                var (_, _, legacyHarvestId) = BundleScanner.GetCropInfoFromSeed(itemId);
                if (legacyHarvestId > 0)
                    harvestMatches = missing.Where(bi => bi.ItemId == legacyHarvestId).ToList();
            }

            DrawSeedTooltip(b, qualifiedId, growDays, season, seasons,
                BuildBundleLines(harvestMatches, config), vanillaTooltipWidth, seedScale, bundleScale);
            return;
        }

        var matches = missing.Where(bi => bi.MatchesItem(hovered)).ToList();

        var completedMatches = new List<BundleItem>();
        var allItems = scanner?.GetAllBundleItems();
        if (allItems is not null)
        {
            var missingKeys = new HashSet<string>(
                missing.Select(bi => $"{bi.QualifiedItemId}:{bi.BundleName}"),
                StringComparer.OrdinalIgnoreCase);
            completedMatches = allItems
                .Where(bi => bi.MatchesItem(hovered)
                          && !missingKeys.Contains($"{bi.QualifiedItemId}:{bi.BundleName}"))
                .ToList();
        }

        if (matches.Count == 0 && completedMatches.Count == 0) return;

        var lines = BuildBundleLines(matches, config);

        if (completedMatches.Count > 0)
        {
            if (lines.Count > 0)
                lines.Add((Separator, new Color(150, 150, 150)));
            foreach (var comp in completedMatches)
                lines.Add((I18n.TooltipCompletedFor(comp.BundleName), new Color(34, 139, 34)));
        }

        DrawBox(b, lines, vanillaTooltipWidth, bundleScale);
    }

    private static bool IsCropSeed(string qualifiedId, BundleScanner? scanner)
    {
        if (string.IsNullOrWhiteSpace(qualifiedId)) return false;

        if (scanner is not null)
        {
            var info = scanner.GetSeedInfo(qualifiedId);
            if (info.growDays > 0) return true;
        }

        try
        {
            var crops = Game1.content.Load<Dictionary<string,
                StardewValley.GameData.Crops.CropData>>("Data/Crops");
            string rawKey = qualifiedId.StartsWith("(", StringComparison.Ordinal)
                ? qualifiedId[(qualifiedId.IndexOf(')') + 1)..]
                : qualifiedId;
            return crops.ContainsKey(rawKey);
        }
        catch { return false; }
    }

    private static List<(string text, Color color)> BuildBundleLines(
        List<BundleItem> matches, ModConfig config)
    {
        var lines = new List<(string text, Color color)>();
        bool first = true;

        foreach (var match in matches)
        {
            Color headerColor = GetCategoryColor(match.Category);
            if (!first) lines.Add((Separator, new Color(150, 150, 150)));
            first = false;

            lines.Add((I18n.TooltipRequiredFor(match.BundleName), headerColor));

            string qSuffix = match.Quality > 0
                ? I18n.TooltipQualitySuffix(I18n.QualityLabel(match.Quality))
                : string.Empty;
            lines.Add((I18n.TooltipCategoryAmount(GetCategoryLabel(match.Category), match.Quantity) + qSuffix,
                        Game1.textColor));

            if (match.Category == BundleCategory.Fish)
            {
                if (match.FishLocations.Count > 0)
                    lines.Add((I18n.TooltipFishLocation(string.Join(", ", match.FishLocations)), new Color(30, 100, 200)));

                if (match.Season is not null)
                {
                    string seasonText = match.Seasons.Count > 1
                        ? string.Join(", ", match.Seasons.Select(s => I18n.SeasonLabel(s)))
                        : I18n.SeasonLabel(match.Season);
                    lines.Add((I18n.TooltipSeason(seasonText), Game1.textColor));
                }

                if (match.FishTimeRange is not null)
                    lines.Add((I18n.TooltipFishTime(match.FishTimeRange), Game1.textColor));

                if (match.RequiresRain)
                    lines.Add((I18n.TooltipRainFish(), new Color(80, 140, 220)));
                else if (match.FishWeather == "sunny")
                    lines.Add((I18n.TooltipSunnyFish(), new Color(220, 180, 0)));
            }
            else
            {
                if (match.Season is not null)
                {
                    string seasonText = match.Seasons.Count > 1
                        ? string.Join(", ", match.Seasons.Select(s => I18n.SeasonLabel(s)))
                        : I18n.SeasonLabel(match.Season);
                    lines.Add((I18n.TooltipSeason(seasonText), Game1.textColor));
                }
                else if (match.IsGreenhouse && match.GrowDays > 0)
                    lines.Add((I18n.SeedTooltipGreenhouseAvailable(), new Color(34, 139, 34)));

                if (match.GrowDays > 0)
                {
                    int lastPlant = 28 - match.GrowDays;
                    int daysLeft  = lastPlant - Game1.dayOfMonth;
                    Color dlColor = daysLeft <= 0 ? Color.Red
                                  : daysLeft <= 3 ? new Color(220, 80, 0)
                                  : daysLeft <= 7 ? new Color(200, 160, 0)
                                  : Game1.textColor;
                    lines.Add((I18n.TooltipGrowDeadline(match.GrowDays, lastPlant), dlColor));
                }

                if (match.Category == BundleCategory.Forage)
                    lines.Add((I18n.TooltipForageHint(), new Color(34, 139, 34)));
            }

            if (config.ShowShopSource && match.ShopSource is not null)
                lines.Add((I18n.TooltipShopSource(match.ShopSource), new Color(0, 150, 100)));
        }

        return lines;
    }

    private static void DrawFruitTreeTooltip(SpriteBatch b, string productName,
        List<string> seasons, List<(string text, Color color)> bundleLines,
        int vanillaTooltipWidth, float scale = 1f)
    {
        string currentSeason = Game1.currentSeason?.ToLower() ?? "";
        bool inSeason = seasons.Count == 0 || seasons.Contains(currentSeason);

        var lines = new List<(string text, Color color)>();
        lines.Add((I18n.FruitTreeTooltipTitle(), new Color(80, 60, 20)));

        if (!string.IsNullOrWhiteSpace(productName))
            lines.Add((I18n.FruitTreeProduct(productName), Game1.textColor));

        if (seasons.Count == 0)
            lines.Add((I18n.FruitTreeAllSeason(), new Color(34, 139, 34)));
        else
        {
            string seasonText = string.Join(", ", seasons.Select(s => I18n.SeasonLabel(s)));
            lines.Add((I18n.FruitTreeSeasons(seasonText), inSeason ? Game1.textColor : new Color(160, 80, 0)));
        }

        lines.Add((I18n.FruitTreeGrowth(), Game1.textColor));

        if (bundleLines.Count > 0)
        {
            lines.Add((Separator, new Color(150, 150, 150)));
            lines.AddRange(bundleLines);
        }

        DrawBox(b, lines, vanillaTooltipWidth, scale);
    }

    private static void DrawSeedTooltip(SpriteBatch b, string qualifiedSeedId,
        int growDays, string? season, List<string> seasons,
        List<(string text, Color color)> bundleLines, int vanillaTooltipWidth,
        float seedScale = 1f, float bundleScale = 1f)
    {
        string currentSeason = Game1.currentSeason?.ToLower() ?? "";
        int today        = Game1.dayOfMonth;
        int lastPlantDay = 28 - growDays;
        int daysLeft     = lastPlantDay - today;
        bool wrongSeason = season != null && !seasons.Contains(currentSeason);

        var lines = new List<(string text, Color color)>();

        if (growDays > 0)
        {
            lines.Add((I18n.SeedTooltipTitle(), new Color(80, 60, 20)));
            lines.Add((I18n.SeedTooltipGrowDays(growDays), Game1.textColor));

            if (season != null)
            {
                string seasonText = seasons.Count > 1
                    ? string.Join(", ", seasons.Select(s => I18n.SeasonLabel(s)))
                    : I18n.SeasonLabel(season);
                lines.Add((I18n.SeedTooltipSeason(seasonText), Game1.textColor));
            }
            else
                lines.Add((I18n.SeedTooltipGreenhouseAvailable(), new Color(34, 139, 34)));

            if (season == null)
            {
                int harvestDay = Game1.dayOfMonth + growDays;
                lines.Add(harvestDay <= 28
                    ? (I18n.SeedTooltipHarvestDay(harvestDay), new Color(34, 139, 34))
                    : (I18n.SeedTooltipWontFit(), Color.Red));
            }
            else if (wrongSeason)
            {
                bool hasGreenhouse = Game1.player?.hasOrWillReceiveMail("ccPantry") == true
                                  || Game1.player?.hasOrWillReceiveMail("jojaGreenhouse") == true;
                if (!hasGreenhouse)
                {
                    var gh = Game1.getLocationFromName("Greenhouse");
                    hasGreenhouse = gh != null && gh.isFarm.Value;
                }
                lines.Add(hasGreenhouse
                    ? (I18n.SeedTooltipGreenhouseAvailable(), new Color(34, 139, 34))
                    : (I18n.SeedTooltipGreenhouseLocked(), new Color(160, 80, 0)));
            }
            else
            {
                int harvestDay = today + growDays;
                lines.Add(harvestDay <= 28
                    ? (I18n.SeedTooltipHarvestDay(harvestDay), new Color(34, 139, 34))
                    : (I18n.SeedTooltipWontFit(), Color.Red));

                if (lastPlantDay >= 1)
                {
                    Color dlColor = daysLeft <= 0 ? Color.Red
                                  : daysLeft <= 3 ? new Color(220, 80, 0)
                                  : daysLeft <= 7 ? new Color(200, 160, 0)
                                  : Game1.textColor;
                    lines.Add((daysLeft <= 0
                        ? I18n.SeedTooltipLastDayPassed()
                        : I18n.SeedTooltipLastPlant(lastPlantDay, daysLeft), dlColor));
                }
            }
        }

        if (bundleLines.Count > 0)
        {
            if (lines.Count > 0)
                lines.Add((Separator, new Color(150, 150, 150)));
            lines.AddRange(bundleLines);
        }

        if (lines.Count == 0) return;
        DrawBox(b, lines, vanillaTooltipWidth, seedScale);
    }

    internal static void DrawMuseumTooltip(
        SpriteBatch b,
        Item hovered,
        IReadOnlyList<BundleItem> missing,
        ModConfig config)
    {
        float scale = Math.Clamp(config.BundleTooltipScale / 100f, 0.50f, 2.00f);

        var lib = Game1.getLocationFromName("ArchaeologyHouse")
                  as StardewValley.Locations.LibraryMuseum;
        if (lib is null) return;

        if (!lib.isItemSuitableForDonation(hovered)) return;

        bool alreadyDonated = lib.museumPieces.Values.Any(
            v => string.Equals((string)v, hovered.ItemId, StringComparison.OrdinalIgnoreCase));

        var lines = new List<(string text, Color color)>();
        lines.Add((I18n.MuseumTooltipTitle(), new Color(120, 80, 20)));

        if (alreadyDonated)
            lines.Add((I18n.MuseumDonated(), new Color(34, 139, 34)));
        else
            lines.Add((I18n.MuseumNeeded(), new Color(220, 80, 0)));

        int category = hovered is StardewValley.Object o ? o.Category : 0;
        bool isMineral = category == StardewValley.Object.GemCategory
                      || category == StardewValley.Object.mineralsCategory
                      || IsMuseumMineral(hovered.ItemId);

        if (isMineral)
            lines.Add((I18n.MuseumCategoryMineral(), new Color(160, 80, 200)));
        else
            lines.Add((I18n.MuseumCategoryArtifact(), new Color(160, 120, 40)));

        var sources = GetMuseumItemSources(hovered);
        foreach (var src in sources)
            lines.Add((src, new Color(100, 140, 100)));

        int totalPieces = GetMuseumTotalCount();
        int donatedCount = GetMuseumDonatedCount();
        if (totalPieces > 0)
            lines.Add((I18n.MuseumProgress(donatedCount, totalPieces), new Color(150, 150, 150)));

        var bundleMatches = missing.Where(bi => bi.MatchesItem(hovered)).ToList();
        if (bundleMatches.Count > 0)
        {
            lines.Add((Separator, new Color(150, 150, 150)));
            lines.AddRange(BuildBundleLines(bundleMatches, config));
        }

        DrawBox(b, lines, 0, scale);
    }

    private static bool IsMuseumMineral(string itemId)
    {
        try
        {
            var minerals = Game1.content.Load<Dictionary<string,
                StardewValley.GameData.Objects.ObjectData>>("Data/Objects");
            if (minerals.TryGetValue(itemId, out var data))
                return data.Category == StardewValley.Object.GemCategory
                    || data.Category == StardewValley.Object.mineralsCategory;
        }
        catch { }
        return false;
    }

    private static List<string> GetMuseumItemSources(Item item)
    {
        var sources = new List<string>();
        try
        {
            var objects = Game1.content.Load<Dictionary<string,
                StardewValley.GameData.Objects.ObjectData>>("Data/Objects");
            string rawId = item.ItemId;
            if (!objects.TryGetValue(rawId, out var data)) return sources;

            string desc = data.Description?.ToLower() ?? string.Empty;
            string ctx  = string.Join(" ", data.ContextTags ?? new List<string>()).ToLower();
            string combined = desc + " " + ctx;

            if (combined.Contains("mine") || combined.Contains("maden") || combined.Contains("geode"))
                sources.Add(I18n.MuseumSourceMine());
            if (combined.Contains("fish") || combined.Contains("balik") || combined.Contains("fishing"))
                sources.Add(I18n.MuseumSourceFishing());
            if (combined.Contains("artifact") || combined.Contains("artefakt") || combined.Contains("digging") || combined.Contains("hoe"))
                sources.Add(I18n.MuseumSourceArtifactSpot());
            if (combined.Contains("pan") || combined.Contains("panning"))
                sources.Add(I18n.MuseumSourcePanning());
            if (combined.Contains("monster") || combined.Contains("drop") || combined.Contains("enemy"))
                sources.Add(I18n.MuseumSourceMonster());
        }
        catch { }
        return sources;
    }

    private static int GetMuseumTotalCount()
    {
        try
        {
            var lib = Game1.getLocationFromName("ArchaeologyHouse");
            if (lib is null) return 0;

            foreach (var methodName in new[] { "numberOfMuseumItemsTotal", "getNumberOfMuseumItemsTotal", "GetNumberOfMuseumItemsTotal" })
            {
                var m = lib.GetType().GetMethod(methodName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (m is not null && m.Invoke(lib, null) is int v)
                    return v;
            }

            var field = lib.GetType().GetField("museumPieces",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(lib) is System.Collections.IDictionary dict)
                return dict.Count > 0 ? 95 : 0;

            return 95;
        }
        catch { return 95; }
    }

    private static int GetMuseumDonatedCount()
    {
        try
        {
            var lib = Game1.getLocationFromName("ArchaeologyHouse");
            if (lib is null) return 0;

            foreach (var methodName in new[] { "numberOfMuseumItemsFound", "getNumberOfMuseumItemsFound", "GetNumberOfMuseumItemsFound" })
            {
                var m = lib.GetType().GetMethod(methodName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (m is not null && m.Invoke(lib, null) is int v)
                    return v;
            }

            var field = lib.GetType().GetField("museumPieces",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(lib) is System.Collections.IDictionary dict)
                return dict.Count;

            return 0;
        }
        catch { return 0; }
    }

    internal static void DrawCommunityTooltip(
        SpriteBatch b,
        Item? hovered,
        IReadOnlyList<BundleItem> missing,
        ModConfig config)
    {
        if (hovered is null) return;

        var scanner = ModEntry.Instance?.Scanner;
        float scale = Math.Clamp(config.BundleTooltipScale / 100f, 0.50f, 2.00f);

        var matches = missing.Where(bi => bi.MatchesItem(hovered)).ToList();

        var completedMatches = new List<BundleItem>();
        var allItems = scanner?.GetAllBundleItems();
        if (allItems is not null)
        {
            var missingKeys = new HashSet<string>(
                missing.Select(bi => $"{bi.QualifiedItemId}:{bi.BundleName}"),
                StringComparer.OrdinalIgnoreCase);
            completedMatches = allItems
                .Where(bi => bi.MatchesItem(hovered)
                          && !missingKeys.Contains($"{bi.QualifiedItemId}:{bi.BundleName}"))
                .ToList();
        }

        if (matches.Count == 0 && completedMatches.Count == 0) return;

        var lines = BuildBundleLines(matches, config);

        foreach (var comp in completedMatches)
        {
            if (lines.Count > 0) lines.Add((Separator, new Color(150, 150, 150)));
            lines.Add((I18n.TooltipCompletedFor(comp.BundleName), new Color(34, 139, 34)));
        }

        if (lines.Count == 0) return;
        DrawBox(b, lines, 0, scale);
    }

    internal static void DrawBox(SpriteBatch b, List<(string text, Color color)> lines,
        int vanillaTooltipWidth = 0, float scale = 1f)
    {
        const int Pad    = 12;
        const int Margin = 8;

        float fontH = Game1.smallFont.MeasureString("A").Y * scale;
        int lineH   = (int)fontH + (int)(6 * scale);
        int sw      = Game1.uiViewport.Width;
        int sh      = Game1.uiViewport.Height;
        int mx      = Game1.getMouseX();
        int my      = Game1.getMouseY();

        int padS    = (int)(Pad * scale);
        int marginS = Margin;

        int maxLines = Math.Max(1, ((int)(sh * 0.80f) - marginS * 2 - padS * 2) / Math.Max(1, lineH));

        List<(string text, Color color)> visible;
        if (lines.Count <= maxLines)
        {
            visible = lines;
        }
        else
        {
            visible = new List<(string text, Color color)>(lines.Take(maxLines - 1))
            {
                ("v ...", new Color(120, 120, 120))
            };
        }

        int textW  = visible.Max(l => (int)(Game1.smallFont.MeasureString(l.text).X * scale));
        int width  = Math.Min(textW + padS * 2, sw - marginS * 2);
        int height = visible.Count * lineH + padS * 2;

        int x, y;

        x = marginS;
        y = sh - height - marginS;

        b.Draw(Game1.staminaRect,
            new Rectangle(x + 4, y + 4, width, height),
            Color.Black * 0.28f);

        IClickableMenu.drawTextureBox(
            b, Game1.menuTexture,
            new Rectangle(0, 256, 60, 60),
            x, y, width, height,
            Color.White, drawShadow: false);

        for (int i = 0; i < visible.Count; i++)
            b.DrawString(Game1.smallFont, visible[i].text,
                new Vector2(x + padS, y + padS + i * lineH),
                visible[i].color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
