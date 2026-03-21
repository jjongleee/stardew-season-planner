using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System.Collections;
using System.Reflection;

namespace SeasonPlanner.Patches;

[HarmonyPatch(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.draw), new[] { typeof(SpriteBatch) })]
internal static class JunimoNoteMenuPatch
{
    private static readonly FieldInfo? _currentPageBundleField =
        typeof(JunimoNoteMenu).GetField("currentPageBundle",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo? _ingredientListField =
        typeof(JunimoNoteMenu).GetField("ingredientList",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static FieldInfo? _bundleIngredientsField;
    private static FieldInfo? _bundleIngredientCompletedField;
    private static FieldInfo? _bundleIngredientIdField;

    [HarmonyPriority(Priority.Low)]
    private static void Postfix(JunimoNoteMenu __instance, SpriteBatch b)
    {
        if (!ModEntry.TryGetSharedState(out var missingItems, out var config)) return;
        if (config is null || !config.ShowInventoryTooltips) return;

        var currentBundle = _currentPageBundleField?.GetValue(__instance);
        if (currentBundle is null) return;

        EnsureBundleFields(currentBundle);

        var ingredientList = _ingredientListField?.GetValue(__instance) as IList;
        var ingredients    = _bundleIngredientsField?.GetValue(currentBundle) as IList;

        if (ingredientList is null || ingredientList.Count == 0) return;
        if (ingredients is null || ingredients.Count == 0) return;

        Item? hovered = FindHoveredIngredient(ingredientList, ingredients);
        if (hovered is null) return;

        TooltipHelper.DrawCommunityTooltip(b, hovered, missingItems, config);
    }

    private static Item? FindHoveredIngredient(IList ingredientList, IList ingredients)
    {
        int mx = Game1.getMouseX(true);
        int my = Game1.getMouseY(true);

        for (int i = 0; i < ingredients.Count && i < ingredientList.Count; i++)
        {
            var ingredient = ingredients[i];
            if (ingredient is null) continue;

            bool completed = _bundleIngredientCompletedField?.GetValue(ingredient) is true;
            if (completed) continue;

            if (ingredientList[i] is not ClickableTextureComponent ctc) continue;
            if (!ctc.bounds.Contains(mx, my)) continue;

            var rawId = _bundleIngredientIdField?.GetValue(ingredient)?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawId)) continue;

            return TryCreateItem(rawId);
        }

        return null;
    }

    private static void EnsureBundleFields(object bundle)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        if (_bundleIngredientsField is null)
            _bundleIngredientsField = bundle.GetType().GetField("ingredients", flags);

        if (_bundleIngredientsField is null) return;

        if (_bundleIngredientCompletedField is null || _bundleIngredientIdField is null)
        {
            var list = _bundleIngredientsField.GetValue(bundle) as IList;
            if (list is null || list.Count == 0) return;
            var first = list[0];
            if (first is null) return;
            var t = first.GetType();
            _bundleIngredientCompletedField ??= t.GetField("completed", flags);
            _bundleIngredientIdField        ??= t.GetField("id", flags);
        }
    }

    private static Item? TryCreateItem(string rawId)
    {
        if (rawId.StartsWith("(", System.StringComparison.Ordinal))
        {
            try { return ItemRegistry.Create(rawId, 1, 0, allowNull: true); } catch { }
        }

        foreach (string prefix in new[] { "(O)", "(F)", "(BC)", "(W)", "(H)", "(S)" })
        {
            try
            {
                var item = ItemRegistry.Create($"{prefix}{rawId}", 1, 0, allowNull: true);
                if (item is not null) return item;
            }
            catch { }
        }

        if (int.TryParse(rawId, out _))
        {
            try { return new StardewValley.Object(rawId, 1); } catch { }
        }

        return null;
    }
}
