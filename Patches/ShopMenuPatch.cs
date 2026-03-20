using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace SeasonPlanner.Patches;

[HarmonyPatch(typeof(ShopMenu), nameof(ShopMenu.draw), new[] { typeof(SpriteBatch) })]
internal static class ShopMenuPatch
{
    [HarmonyPriority(Priority.Low)]
    private static void Postfix(ShopMenu __instance, SpriteBatch b)
    {
        if (!ModEntry.TryGetSharedState(out var missingItems, out var config)) return;
        if (config is null || !config.ShowInventoryTooltips) return;

        Item? hovered = __instance.hoveredItem as Item;
        if (hovered is null) return;

        TooltipHelper.DrawBundleTooltip(b, hovered, missingItems, config,
            vanillaTooltipWidth: 1);
    }
}

