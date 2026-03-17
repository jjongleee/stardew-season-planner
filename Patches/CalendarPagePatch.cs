using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace SeasonPlanner.Patches;

/// <summary>
/// Billboard.draw postfix — son ekim günlerini takvimde renkli ünlem işaretiyle işaretler.
/// Renk kodlaması: kırmızı = acil (≤3 gün), turuncu = yakın (≤7 gün), altın = normal.
/// </summary>
[HarmonyPatch(typeof(Billboard), nameof(Billboard.draw), new[] { typeof(SpriteBatch) })]
internal static class CalendarPagePatch
{
    internal static IReadOnlyList<BundleItem>? MissingItems;
    internal static ModConfig?                 Config;

    private static void Postfix(Billboard __instance, SpriteBatch b)
    {
        if (Config is null || !Config.ShowCalendarMarkers) return;
        if (MissingItems is null || MissingItems.Count == 0) return;

        string season = Game1.currentSeason.ToLower();
        int    today  = Game1.dayOfMonth;

        // Gün → renk (en acil olanı kazanır)
        var dayColors = new Dictionary<int, Color>();

        foreach (var item in MissingItems)
        {
            if (item.Season != season || item.GrowDays <= 0) continue;
            int lastPlantDay = 28 - item.GrowDays;
            if (lastPlantDay < 1 || lastPlantDay > 28) continue;

            int daysLeft = lastPlantDay - today;
            Color color  = daysLeft <= 0 ? Color.Red
                         : daysLeft <= 3 ? new Color(220, 60, 0)   // kırmızı-turuncu
                         : daysLeft <= 7 ? new Color(230, 140, 0)  // turuncu
                         : new Color(255, 210, 0);                  // altın

            // Aynı güne birden fazla eşya düşebilir; en acil rengi tut
            if (!dayColors.TryGetValue(lastPlantDay, out Color existing)
                || IsMoreUrgent(color, existing))
            {
                dayColors[lastPlantDay] = color;
            }
        }

        if (dayColors.Count == 0) return;

        var calendarDays = Traverse.Create(__instance)
            .Field<List<ClickableTextureComponent>>("calendarDays")
            .Value;

        if (calendarDays is null || calendarDays.Count < 28) return;

        int mx = Game1.getMouseX();
        int my = Game1.getMouseY();
        int hoveredDay = -1;

        foreach (var (day, color) in dayColors)
        {
            int idx = day - 1;
            if (idx >= calendarDays.Count) continue;

            var tile = calendarDays[idx];

            // Sağ üst köşeye renkli dolgu bant
            int bw = 18, bh = 18;
            int bx = tile.bounds.Right - bw - 2;
            int by = tile.bounds.Top + 2;

            // Fare bu günün üzerinde mi?
            if (tile.bounds.Contains(mx, my))
                hoveredDay = day;

            // Gölge
            b.Draw(Game1.staminaRect,
                new Rectangle(bx + 2, by + 2, bw, bh),
                Color.Black * 0.30f);

            // Dolgu
            b.Draw(Game1.staminaRect,
                new Rectangle(bx, by, bw, bh),
                color * 0.90f);

            // Kenarlık (1px)
            b.Draw(Game1.staminaRect, new Rectangle(bx,      by,      bw, 1),  Color.White * 0.60f);
            b.Draw(Game1.staminaRect, new Rectangle(bx,      by+bh-1, bw, 1),  Color.White * 0.60f);
            b.Draw(Game1.staminaRect, new Rectangle(bx,      by,      1,  bh), Color.White * 0.60f);
            b.Draw(Game1.staminaRect, new Rectangle(bx+bw-1, by,      1,  bh), Color.White * 0.60f);

            // Ünlem işareti (küçük, ortada)
            b.Draw(
                Game1.mouseCursors,
                new Rectangle(bx + bw/2 - 3, by + 2, 6, 14),
                new Rectangle(403, 496, 5, 14),
                Color.White * 0.95f,
                0f, Vector2.Zero, SpriteEffects.None, 0.99f);
        }

        // Hover tooltip
        if (hoveredDay < 0 || MissingItems is null) return;

        string season2 = Game1.currentSeason.ToLower();
        var lines = new System.Collections.Generic.List<(string text, Color color)>();

        foreach (var item in MissingItems)
        {
            if (item.Season != season2 || item.GrowDays <= 0) continue;
            int lastPlantDay = 28 - item.GrowDays;
            if (lastPlantDay != hoveredDay) continue;

            int daysLeft = lastPlantDay - today;
            Color col = daysLeft <= 0 ? Color.Red
                      : daysLeft <= 3 ? new Color(220, 60, 0)
                      : daysLeft <= 7 ? new Color(230, 140, 0)
                      : new Color(180, 140, 0);

            lines.Add((item.ItemName, col));
            string deadline = daysLeft <= 0
                ? I18n.SeedTooltipLastDayPassed()
                : I18n.SeedTooltipLastPlant(lastPlantDay, daysLeft);
            lines.Add(($"  {deadline}", Game1.textColor));
            lines.Add(($"  {I18n.TooltipRequiredFor(item.BundleName)}", new Color(100, 100, 100)));
        }

        if (lines.Count > 0)
            TooltipHelper.DrawBox(b, lines);
    }

    // Kırmızı > turuncu > altın öncelik sırası
    private static bool IsMoreUrgent(Color a, Color b)
    {
        static int Urgency(Color c)
        {
            if (c == Color.Red)                  return 3;
            if (c.R > 200 && c.G < 100)          return 2; // kırmızı-turuncu
            if (c.R > 200 && c.G < 160)          return 1; // turuncu
            return 0;
        }
        return Urgency(a) > Urgency(b);
    }
}
