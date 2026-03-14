using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace SeasonPlanner.Patches;

/// <summary>
/// InventoryPage.draw postfix — envanterde bundle tooltip'i gösterir.
/// </summary>
[HarmonyPatch(typeof(InventoryPage), nameof(InventoryPage.draw), new[] { typeof(SpriteBatch) })]
internal static class InventoryPagePatch
{
    internal static IReadOnlyList<BundleItem>? MissingItems;
    internal static ModConfig?                 Config;

    private static void Postfix(InventoryPage __instance, SpriteBatch b)
    {
        if (Config is null || MissingItems is null) return;

        Item? hovered = __instance.inventory?.hover(
            Game1.getMouseX(), Game1.getMouseY(), null);

        TooltipHelper.DrawBundleTooltip(b, hovered, MissingItems, Config);
    }
}
