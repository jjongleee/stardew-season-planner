using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SeasonPlanner.Patches;

[HarmonyPatch(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.draw), new[] { typeof(SpriteBatch) })]
internal static class JunimoNoteMenuPatch
{
    private static readonly FieldInfo? BundlesField =
        typeof(JunimoNoteMenu).GetField("bundles",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo? CurrentPageBundleField =
        typeof(JunimoNoteMenu).GetField("currentPageBundle",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static FieldInfo? _ingredientsField;
    private static FieldInfo? _ingredientItemField;

    [HarmonyPriority(Priority.Low)]
    private static void Postfix(JunimoNoteMenu __instance, SpriteBatch b)
    {
        if (!ModEntry.TryGetSharedState(out var missingItems, out var config)) return;
        if (config is null || !config.ShowInventoryTooltips) return;

        Item? hovered = FindHoveredIngredient(__instance);
        if (hovered is null) return;

        TooltipHelper.DrawCommunityTooltip(b, hovered, missingItems, config);
    }

    private static void ResolveBundleFields(object bundle)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var bundleType = bundle.GetType();

        if (_ingredientsField is null)
            _ingredientsField = bundleType.GetField("ingredients", flags);

        if (_ingredientsField is null) return;

        if (_ingredientItemField is null)
        {
            var list = _ingredientsField.GetValue(bundle) as IList;
            if (list is null || list.Count == 0) return;
            var first = list[0];
            if (first is not null)
                _ingredientItemField = first.GetType().GetField("item", flags);
        }
    }

    private static Item? FindHoveredIngredient(JunimoNoteMenu menu)
    {
        int mx = Game1.getMouseX();
        int my = Game1.getMouseY();

        var currentBundle = CurrentPageBundleField?.GetValue(menu);
        if (currentBundle is not null)
        {
            var item = SearchBundleIngredients(currentBundle, mx, my);
            if (item is not null) return item;
        }

        var bundles = BundlesField?.GetValue(menu) as IList;
        if (bundles is null) return null;

        foreach (var bundle in bundles)
        {
            if (bundle is null) continue;
            var item = SearchBundleIngredients(bundle, mx, my);
            if (item is not null) return item;
        }

        return null;
    }

    private static Item? SearchBundleIngredients(object bundle, int mx, int my)
    {
        ResolveBundleFields(bundle);
        if (_ingredientsField is null) return null;

        var ingredients = _ingredientsField.GetValue(bundle) as IList;
        if (ingredients is null) return null;

        foreach (var ingredient in ingredients)
        {
            if (ingredient is not ClickableTextureComponent comp) continue;
            if (!comp.bounds.Contains(mx, my)) continue;

            if (_ingredientItemField is not null)
            {
                var item = _ingredientItemField.GetValue(ingredient) as Item;
                if (item is not null) return item;
            }

            var fallback = TryCreateItem(comp.name ?? string.Empty);
            if (fallback is not null) return fallback;
        }

        return null;
    }

    private static Item? TryCreateItem(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        string itemId = name.Contains(' ') ? name.Split(' ')[0] : name;
        if (string.IsNullOrWhiteSpace(itemId) || itemId == "ingredient_list_slot") return null;

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

        if (int.TryParse(itemId, out int legacyId) && legacyId > 0)
        {
            try { return new StardewValley.Object(itemId, 1); }
            catch { }
        }

        return null;
    }
}
