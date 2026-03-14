using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace SeasonPlanner.Patches;

/// <summary>
/// ItemGrabMenu.draw postfix — sandık/mini-fridge/çanta menüsünde bundle tooltip'i gösterir.
/// ItemGrabMenu; sandık, mini-fridge, balıkçı sandığı ve benzeri tüm "grab" menülerini kapsar.
/// </summary>
[HarmonyPatch(typeof(ItemGrabMenu), nameof(ItemGrabMenu.draw), new[] { typeof(SpriteBatch) })]
internal static class ChestPatch
{
    internal static IReadOnlyList<BundleItem>? MissingItems;
    internal static ModConfig?                 Config;

    private static void Postfix(ItemGrabMenu __instance, SpriteBatch b)
    {
        if (Config is null || MissingItems is null) return;
        if (!Config.ShowChestTooltips) return;

        // Sandığın içindeki hover
        Item? hoveredChest = __instance.ItemsToGrabMenu?.hover(
            Game1.getMouseX(), Game1.getMouseY(), null);

        // Oyuncunun envanterindeki hover
        Item? hoveredPlayer = __instance.inventory?.hover(
            Game1.getMouseX(), Game1.getMouseY(), null);

        Item? hovered = hoveredChest ?? hoveredPlayer;

        TooltipHelper.DrawBundleTooltip(b, hovered, MissingItems, Config);
    }
}
