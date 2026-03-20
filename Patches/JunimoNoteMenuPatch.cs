using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System.Reflection;

namespace SeasonPlanner.Patches;

[HarmonyPatch(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.draw), new[] { typeof(SpriteBatch) })]
internal static class JunimoNoteMenuPatch
{
    private static readonly FieldInfo? IngredientListField =
        typeof(JunimoNoteMenu).GetField("ingredientList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    [HarmonyPriority(Priority.Low)]
    private static void Postfix(JunimoNoteMenu __instance, SpriteBatch b)
    {
        if (!ModEntry.TryGetSharedState(out var missingItems, out var config)) return;
        if (config is null || !config.ShowInventoryTooltips) return;

        Item? hovered = FindHoveredIngredient(__instance);
        if (hovered is null) return;

        TooltipHelper.DrawCommunityTooltip(b, hovered, missingItems, config);
    }

    private static Item? FindHoveredIngredient(JunimoNoteMenu menu)
    {
        int mx = Game1.getMouseX();
        int my = Game1.getMouseY();

        var ingredientList = IngredientListField?.GetValue(menu) as System.Collections.Generic.List<ClickableTextureComponent>;
        if (ingredientList is null) return null;

        foreach (var component in ingredientList)
        {
            if (!component.bounds.Contains(mx, my)) continue;

            string name = component.name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) continue;

            string itemId = name.Contains(' ') ? name.Split(' ')[0] : name;

            var item = TryCreateItem(itemId);
            if (item is not null) return item;
        }

        return null;
    }

    private static Item? TryCreateItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return null;

        if (itemId.StartsWith("(", System.StringComparison.Ordinal))
        {
            try
            {
                var item = ItemRegistry.Create(itemId, 1, 0, allowNull: true);
                if (item is not null) return item;
            }
            catch { }
        }

        foreach (string prefix in new[] { "(O)", "(F)", "(BC)", "(W)", "(H)", "(S)" })
        {
            try
            {
                var item = ItemRegistry.Create($"{prefix}{itemId}", 1, 0, allowNull: true);
                if (item is not null) return item;
            }
            catch { }
        }

        try
        {
            var data = ItemRegistry.GetData(itemId);
            if (data is not null)
                return ItemRegistry.Create(data.QualifiedItemId, 1, 0, allowNull: true);
        }
        catch { }

        if (int.TryParse(itemId, out int legacyId) && legacyId > 0)
        {
            try { return new StardewValley.Object(itemId, 1); }
            catch { }
        }

        return null;
    }
}
