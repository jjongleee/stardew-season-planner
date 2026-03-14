using StardewModdingAPI;

namespace SeasonPlanner;

/// <summary>
/// SMAPI Translation helper. ModEntry.Entry'de Initialize çağrılmalı.
/// </summary>
internal static class I18n
{
    private static ITranslationHelper _t = null!;

    internal static void Initialize(ITranslationHelper translation) => _t = translation;

    // ── Panel ─────────────────────────────────────────────────────────────
    public static string PanelTitle()         => _t.Get("panel.title");
    public static string PanelAllComplete()   => _t.Get("panel.all_complete");
    public static string PanelCategoryEmpty() => _t.Get("panel.category_empty");
    public static string PanelFooterHint()    => _t.Get("panel.footer_hint");
    public static string PanelItemsCount(int count) =>
        _t.Get("panel.items_count", new { count });

    // ── Sekmeler ─────────────────────────────────────────────────────────
    public static string TabAll()          => _t.Get("tab.all");
    public static string TabCrop()         => _t.Get("tab.crop");
    public static string TabFish()         => _t.Get("tab.fish");
    public static string TabArtisan()      => _t.Get("tab.artisan");
    public static string TabConstruction() => _t.Get("tab.construction");
    public static string TabOther()        => _t.Get("tab.other");

    // ── Badge ─────────────────────────────────────────────────────────────
    public static string BadgeToday()           => _t.Get("badge.today");
    public static string BadgeDaysLeft(int days) => _t.Get("badge.days_left", new { days });
    public static string BadgeRain()            => _t.Get("badge.rain");

    // ── Kalite ───────────────────────────────────────────────────────────
    public static string QualityLabel(int q) => q switch
    {
        1 => _t.Get("quality.silver"),
        2 => _t.Get("quality.gold"),
        4 => _t.Get("quality.iridium"),
        _ => _t.Get("quality.other", new { q }),
    };

    // ── Mevsim ───────────────────────────────────────────────────────────
    public static string SeasonLabel(string s) => s switch
    {
        "spring" => _t.Get("season.spring"),
        "summer" => _t.Get("season.summer"),
        "fall"   => _t.Get("season.fall"),
        "winter" => _t.Get("season.winter"),
        _        => s,
    };

    // ── Tooltip ──────────────────────────────────────────────────────────
    public static string TooltipRequiredFor(string bundle) =>
        _t.Get("tooltip.required_for", new { bundle });
    public static string TooltipCategoryAmount(string category, int qty) =>
        _t.Get("tooltip.category_amount", new { category, qty });
    public static string TooltipQualitySuffix(string quality) =>
        _t.Get("tooltip.quality_suffix", new { quality });
    public static string TooltipSeason(string season) =>
        _t.Get("tooltip.season", new { season });
    public static string TooltipGrowDeadline(int days, int day) =>
        _t.Get("tooltip.grow_deadline", new { days, day });
    public static string TooltipRainFish() => _t.Get("tooltip.rain_fish");
    public static string TooltipShopSource(string shop) =>
        _t.Get("tooltip.shop_source", new { shop });

    // ── Kategori ─────────────────────────────────────────────────────────
    public static string CategoryLabel(BundleCategory cat) => cat switch
    {
        BundleCategory.Crop         => _t.Get("category.crop"),
        BundleCategory.Fish         => _t.Get("category.fish"),
        BundleCategory.Artisan      => _t.Get("category.artisan"),
        BundleCategory.Construction => _t.Get("category.construction"),
        _                           => _t.Get("category.other"),
    };

    // ── HUD ──────────────────────────────────────────────────────────────
    public static string HudPlantingToday(string item, string bundle) =>
        _t.Get("hud.planting_today", new { item, bundle });
    public static string HudPlantingWarning(string item, int day, int days, string bundle) =>
        _t.Get("hud.planting_warning", new { item, day, days, bundle });
    public static string HudRainFish(string item, string bundle) =>
        _t.Get("hud.rain_fish", new { item, bundle });

    // ── GMCM ─────────────────────────────────────────────────────────────
    public static string GmcmCalendarMarkers()           => _t.Get("gmcm.calendar_markers");
    public static string GmcmCalendarMarkersTooltip()    => _t.Get("gmcm.calendar_markers.tooltip");
    public static string GmcmHudNotifications()          => _t.Get("gmcm.hud_notifications");
    public static string GmcmHudNotificationsTooltip()   => _t.Get("gmcm.hud_notifications.tooltip");
    public static string GmcmInventoryTooltip()          => _t.Get("gmcm.inventory_tooltip");
    public static string GmcmInventoryTooltipTooltip()   => _t.Get("gmcm.inventory_tooltip.tooltip");
    public static string GmcmChestTooltip()              => _t.Get("gmcm.chest_tooltip");
    public static string GmcmChestTooltipTooltip()       => _t.Get("gmcm.chest_tooltip.tooltip");
    public static string GmcmFilterConstruction()        => _t.Get("gmcm.filter_construction");
    public static string GmcmFilterConstructionTooltip() => _t.Get("gmcm.filter_construction.tooltip");
    public static string GmcmShopSource()                => _t.Get("gmcm.shop_source");
    public static string GmcmShopSourceTooltip()         => _t.Get("gmcm.shop_source.tooltip");
    public static string GmcmWarningDays()               => _t.Get("gmcm.warning_days");
    public static string GmcmWarningDaysTooltip()        => _t.Get("gmcm.warning_days.tooltip");
    public static string GmcmPanelHotkey()               => _t.Get("gmcm.panel_hotkey");
    public static string GmcmPanelHotkeyTooltip()        => _t.Get("gmcm.panel_hotkey.tooltip");
}
