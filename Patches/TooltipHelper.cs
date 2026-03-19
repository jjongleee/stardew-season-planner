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

    // Safe separator — ASCII hyphens, no unicode box-drawing chars
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
        if (hovered is not StardewValley.Object obj) return;

        int itemId = obj.ParentSheetIndex;
        float bundleScale = Math.Clamp(config.BundleTooltipScale / 100f, 0.50f, 2.00f);
        float seedScale   = Math.Clamp(config.SeedTooltipScale   / 100f, 0.50f, 2.00f);

        bool isSeed = obj.Category == StardewValley.Object.SeedsCategory
                   || BundleScanner.SeedToHarvest.ContainsKey(itemId)
                   || IsCropSeed(itemId);

        if (isSeed)
        {
            var (_, _, harvestId) = BundleScanner.GetCropInfoFromSeed(itemId);
            string harvestQualified = harvestId > 0 ? $"(O){harvestId}" : string.Empty;
            var harvestMatches = harvestId > 0
                ? missing.Where(bi => string.Equals(bi.QualifiedItemId, harvestQualified,
                                    System.StringComparison.OrdinalIgnoreCase)
                                   || bi.ItemId == harvestId).ToList()
                : new List<BundleItem>();
            DrawSeedTooltip(b, itemId, BuildBundleLines(harvestMatches, config), vanillaTooltipWidth, seedScale, bundleScale);
            return;
        }

        var matches = missing.Where(bi => bi.MatchesItem(hovered)).ToList();
        if (matches.Count == 0) return;
        DrawBox(b, BuildBundleLines(matches, config), vanillaTooltipWidth, bundleScale);
    }

    private static bool IsCropSeed(int itemId)
    {
        try
        {
            var crops = Game1.content.Load<Dictionary<string,
                StardewValley.GameData.Crops.CropData>>("Data/Crops");
            return crops.ContainsKey(itemId.ToString());
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

            if (match.Season is not null)
                lines.Add((I18n.TooltipSeason(I18n.SeasonLabel(match.Season)), Game1.textColor));
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

            if (match.RequiresRain)
                lines.Add((I18n.TooltipRainFish(), new Color(80, 140, 220)));

            if (config.ShowShopSource && match.ShopSource is not null)
                lines.Add((I18n.TooltipShopSource(match.ShopSource), new Color(0, 150, 100)));
        }

        return lines;
    }

    private static void DrawSeedTooltip(SpriteBatch b, int seedId,
        List<(string text, Color color)> bundleLines, int vanillaTooltipWidth,
        float seedScale = 1f, float bundleScale = 1f)
    {
        var (growDays, season, _) = BundleScanner.GetCropInfoFromSeed(seedId);

        string currentSeason = Game1.currentSeason?.ToLower() ?? "";
        int today        = Game1.dayOfMonth;
        int harvestDay   = today + growDays;
        int lastPlantDay = 28 - growDays;
        int daysLeft     = lastPlantDay - today;
        bool wrongSeason = season != null && season != currentSeason;

        var lines = new List<(string text, Color color)>();

        if (growDays > 0)
        {
            lines.Add((I18n.SeedTooltipTitle(), new Color(80, 60, 20)));
            lines.Add((I18n.SeedTooltipGrowDays(growDays), Game1.textColor));

            if (season != null)
                lines.Add((I18n.SeedTooltipSeason(I18n.SeasonLabel(season)), Game1.textColor));

            if (wrongSeason)
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

    internal static void DrawBox(SpriteBatch b, List<(string text, Color color)> lines,
        int vanillaTooltipWidth = 0, float scale = 1f)
    {
        const int Pad    = 12;
        const int Margin = 8;
        const int Gap    = 16;

        // scale'e göre font yüksekliği
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

        if (vanillaTooltipWidth > 0)
        {
            int leftX = mx - width - Gap;
            x = leftX >= marginS ? leftX : mx + Gap;
            x = Math.Clamp(x, marginS, sw - width - marginS);
            y = Math.Clamp(my - height - Gap, marginS, sh - height - marginS);
        }
        else
        {
            x = marginS;
            y = sh - height - marginS;
        }

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
