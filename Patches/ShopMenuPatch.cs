using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace SeasonPlanner.Patches;

[HarmonyPatch(typeof(ShopMenu), nameof(ShopMenu.draw), new[] { typeof(SpriteBatch) })]
internal static class ShopMenuPatch
{
    internal static IReadOnlyList<BundleItem>? MissingItems;
    internal static ModConfig?                 Config;

    private static void Postfix(ShopMenu __instance, SpriteBatch b)
    {
        if (Config is null || !Config.ShowInventoryTooltips) return;
        if (MissingItems is null) return;

        Item? hovered = __instance.hoveredItem as Item;
        if (hovered is null) return;

        TooltipHelper.DrawBundleTooltip(b, hovered, MissingItems, Config,
            vanillaTooltipWidth: 1);
    }
}
