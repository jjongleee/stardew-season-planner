using StardewModdingAPI;

namespace SeasonPlanner;



internal static class I18n
{
    private static ITranslationHelper _t = null!;

    internal static void Initialize(ITranslationHelper translation) => _t = translation;

    public static string PanelTitle()         => _t.Get("panel.title");
    public static string PanelAllComplete()   => _t.Get("panel.all_complete");
    public static string PanelCategoryEmpty() => _t.Get("panel.category_empty");
    public static string PanelFooterHint()    => _t.Get("panel.footer_hint");
    public static string PanelItemsCount(int count) =>
        _t.Get("panel.items_count", new { count });
    public static string PanelShowPlanned()   => _t.Get("panel.show_planned");
    public static string PanelClearPlanned()  => _t.Get("panel.clear_planned");
    public static string PanelLogBtn()        => _t.Get("panel.log_btn");
    public static string PanelLogTitle()      => _t.Get("panel.log_title");
    public static string PanelLogEmpty()      => _t.Get("panel.log_empty");
    public static string PanelLogClose()      => _t.Get("panel.log_close");

    public static string TabAll()          => _t.Get("tab.all");
    public static string TabCrop()         => _t.Get("tab.crop");
    public static string TabFish()         => _t.Get("tab.fish");
    public static string TabArtisan()      => _t.Get("tab.artisan");
    public static string TabForage()       => _t.Get("tab.forage");
    public static string TabConstruction() => _t.Get("tab.construction");
    public static string TabOther()        => _t.Get("tab.other");
    public static string TabSeasonal()     => _t.Get("tab.seasonal");

    public static string BadgeToday()           => _t.Get("badge.today");
    public static string BadgeDaysLeft(int days) => _t.Get("badge.days_left", new { days });
    public static string BadgeRain()            => _t.Get("badge.rain");

    public static string QualityLabel(int q) => q switch
    {
        1 => _t.Get("quality.silver"),
        2 => _t.Get("quality.gold"),
        4 => _t.Get("quality.iridium"),
        _ => _t.Get("quality.other", new { q }),
    };

    public static string SeasonLabel(string s) => s switch
    {
        "spring" => _t.Get("season.spring"),
        "summer" => _t.Get("season.summer"),
        "fall"   => _t.Get("season.fall"),
        "winter" => _t.Get("season.winter"),
        _        => s,
    };

    public static string TooltipRequiredFor(string bundle) =>
        _t.Get("tooltip.required_for", new { bundle });
    public static string TooltipCompletedFor(string bundle) =>
        _t.Get("tooltip.completed_for", new { bundle });
    public static string TooltipFishLocation(string location) =>
        _t.Get("tooltip.fish_location", new { location });
    public static string TooltipFishTime(string time) =>
        _t.Get("tooltip.fish_time", new { time });
    public static string TooltipSunnyFish() => _t.Get("tooltip.sunny_fish");
    public static string TooltipForageHint() => _t.Get("tooltip.forage_hint");
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

    public static string CategoryLabel(BundleCategory cat) => cat switch
    {
        BundleCategory.Crop         => _t.Get("category.crop"),
        BundleCategory.Fish         => _t.Get("category.fish"),
        BundleCategory.Artisan      => _t.Get("category.artisan"),
        BundleCategory.Construction => _t.Get("category.construction"),
        BundleCategory.Forage       => _t.Get("category.forage"),
        _                           => _t.Get("category.other"),
    };

    public static string HudPlantingToday(string item, string bundle) =>
        _t.Get("hud.planting_today", new { item, bundle });
    public static string HudPlantingWarning(string item, int day, int days, string bundle) =>
        _t.Get("hud.planting_warning", new { item, day, days, bundle });
    public static string HudRainFish(string item, string bundle) =>
        _t.Get("hud.rain_fish", new { item, bundle });
    public static string HudPlannedCompleted(string item, string bundle) =>
        _t.Get("hud.planned_completed", new { item, bundle });

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
    public static string GmcmPanelScale()             => _t.Get("gmcm.panel_scale");
    public static string GmcmPanelScaleTooltip()      => _t.Get("gmcm.panel_scale.tooltip");
    public static string GmcmBundleTooltipScale()     => _t.Get("gmcm.bundle_tooltip_scale");
    public static string GmcmBundleTooltipScaleTooltip() => _t.Get("gmcm.bundle_tooltip_scale.tooltip");
    public static string GmcmSeedTooltipScale()       => _t.Get("gmcm.seed_tooltip_scale");
    public static string GmcmSeedTooltipScaleTooltip()   => _t.Get("gmcm.seed_tooltip_scale.tooltip");
    public static string GmcmRememberPanelPosition()     => _t.Get("gmcm.remember_panel_position");
    public static string GmcmRememberPanelPositionTooltip() => _t.Get("gmcm.remember_panel_position.tooltip");
    public static string GmcmPanelAnchor()               => _t.Get("gmcm.panel_anchor");
    public static string GmcmPanelAnchorTooltip()        => _t.Get("gmcm.panel_anchor.tooltip");
    public static string GmcmAnchorLabel(string key)     => _t.Get($"gmcm.anchor.{key.ToLower()}");
    public static string GmcmResetPosition()             => _t.Get("gmcm.reset_position");
    public static string GmcmResetPositionTooltip()      => _t.Get("gmcm.reset_position.tooltip");

    public static string InfoBundle()       => _t.Get("infotip.bundle");
    public static string InfoCategory()     => _t.Get("infotip.category");
    public static string InfoQuality()      => _t.Get("infotip.quality");
    public static string InfoSeason()       => _t.Get("infotip.season");
    public static string InfoGrow()         => _t.Get("infotip.grow");
    public static string InfoLastPlant()    => _t.Get("infotip.last_plant");
    public static string InfoDaysLeft()     => _t.Get("infotip.days_left");
    public static string InfoLastDay()      => _t.Get("infotip.last_day");
    public static string InfoRain()         => _t.Get("infotip.rain");
    public static string InfoShop()         => _t.Get("infotip.shop");
    public static string InfoPlanned()      => _t.Get("infotip.planned");
    public static string SortUrgency()      => _t.Get("infotip.sort_urgency");
    public static string SortName()         => _t.Get("infotip.sort_name");
    public static string SortBundle()       => _t.Get("infotip.sort_bundle");
    public static string SortLabel()        => _t.Get("infotip.sort_label");
    public static string ChipMissing()      => _t.Get("infotip.missing");
    public static string ChipUrgent()       => _t.Get("infotip.urgent");
    public static string ChipSeasonal()     => _t.Get("infotip.seasonal");
    public static string ChipPlanned()      => _t.Get("infotip.planned_count");
    public static string PlanBtn()          => _t.Get("infotip.plan_btn");
    public static string PlannedBtn()       => _t.Get("infotip.planned_btn");
    public static string CompletedBtn()     => _t.Get("infotip.completed_btn");

    public static string SeedTooltipTitle()                      => _t.Get("seed.tooltip_title");
    public static string SeedTooltipGrowDays(int days)           => _t.Get("seed.grow_days", new { days });
    public static string SeedTooltipSeason(string season)        => _t.Get("seed.season", new { season });
    public static string SeedTooltipHarvestDay(int day)          => _t.Get("seed.harvest_day", new { day });
    public static string SeedTooltipWontFit()                    => _t.Get("seed.wont_fit");
    public static string SeedTooltipLastPlant(int day, int left) => _t.Get("seed.last_plant", new { day, left });
    public static string SeedTooltipLastDayPassed()              => _t.Get("seed.last_day_passed");
    public static string SeedTooltipWrongSeason()                => _t.Get("seed.wrong_season");
    public static string SeedTooltipGreenhouseAvailable()        => _t.Get("seed.greenhouse_available");
    public static string SeedTooltipGreenhouseLocked()           => _t.Get("seed.greenhouse_locked");

    public static string FruitTreeTooltipTitle()                 => _t.Get("fruittree.tooltip_title");
    public static string FruitTreeProduct(string item)           => _t.Get("fruittree.product", new { item });
    public static string FruitTreeSeasons(string seasons)        => _t.Get("fruittree.seasons", new { seasons });
    public static string FruitTreeGrowth()                       => _t.Get("fruittree.growth");
    public static string FruitTreeAllSeason()                    => _t.Get("fruittree.all_season");

    public static string CalLegendUrgent() => _t.Get("cal.legend_urgent");
    public static string CalLegendSoon()   => _t.Get("cal.legend_soon");
    public static string CalLegendLater()  => _t.Get("cal.legend_later");

    public static string CalDay(int day)       => _t.Get("cal.day", new { day });
    public static string CalToday()            => _t.Get("cal.today");
    public static string CalDaysAgo()          => _t.Get("cal.days_ago");
    public static string CalDaysLater()        => _t.Get("cal.days_later");
    public static string CalLastPlantDay()     => _t.Get("cal.last_plant_day");
    public static string CalWeekMon()          => _t.Get("cal.week.mon");
    public static string CalWeekTue()          => _t.Get("cal.week.tue");
    public static string CalWeekWed()          => _t.Get("cal.week.wed");
    public static string CalWeekThu()          => _t.Get("cal.week.thu");
    public static string CalWeekFri()          => _t.Get("cal.week.fri");
    public static string CalWeekSat()          => _t.Get("cal.week.sat");
    public static string CalWeekSun()          => _t.Get("cal.week.sun");

    public static string SearchPlaceholder() => _t.Get("search.placeholder");

    public static string MuseumTooltipTitle()      => _t.Get("museum.tooltip_title");
    public static string MuseumNeeded()             => _t.Get("museum.needed");
    public static string MuseumDonated()            => _t.Get("museum.donated");
    public static string MuseumCategoryMineral()    => _t.Get("museum.category_mineral");
    public static string MuseumCategoryArtifact()   => _t.Get("museum.category_artifact");
    public static string MuseumProgress(int donated, int total) =>
        _t.Get("museum.progress", new { donated, total });
    public static string MuseumSourceMine()         => _t.Get("museum.source_mine");
    public static string MuseumSourceFishing()      => _t.Get("museum.source_fishing");
    public static string MuseumSourceArtifactSpot() => _t.Get("museum.source_artifact_spot");
    public static string MuseumSourcePanning()      => _t.Get("museum.source_panning");
    public static string MuseumSourceMonster()      => _t.Get("museum.source_monster");


    public static string BundleName(string key)
    {
        var t = _t.Get(key);
        return t.HasValue() ? t.ToString() : key;
    }
}

