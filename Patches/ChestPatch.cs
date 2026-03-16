using System.Collections.Generic;
using StardewValley;

namespace SeasonPlanner.Patches;

/// <summary>
/// Tooltip çizimi ModEntry.OnRenderedActiveMenu'ye taşındı.
/// Bu sınıf artık sadece config referansı tutar.
/// </summary>
internal static class ChestPatch
{
    internal static IReadOnlyList<BundleItem>? MissingItems;
    internal static ModConfig?                 Config;
}
