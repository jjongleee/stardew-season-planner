using HarmonyLib;
using SeasonPlanner.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SeasonPlanner;

public sealed class ModEntry : Mod
{
    // Mod bu SMAPI major versiyonu ile yazıldı.
    // Farklı bir major gelirse Harmony patch'leri tehlikeli olabilir.
    private const int TestedSmapiMajor = 4;

    private ModConfig     _config  = null!;
    private BundleScanner _scanner = null!;
    private bool          _safeToRun = true; // uyumsuz SMAPI'de patch'leri atla

    public override void Entry(IModHelper helper)
    {
        _config  = helper.ReadConfig<ModConfig>();
        _scanner = new BundleScanner(Monitor);
        I18n.Initialize(helper.Translation);

        // ── SMAPI sürüm kontrolü ─────────────────────────────────────────
        var smapi = helper.ModRegistry
            .Get("SMAPI")?.Manifest.Version
            ?? Constants.ApiVersion;

        if (smapi.MajorVersion != TestedSmapiMajor)
        {
            Monitor.Log(
                $"[SeasonPlanner] SMAPI major version mismatch! " +
                $"Tested with {TestedSmapiMajor}.x, running on {smapi}. " +
                $"Harmony patches are disabled to prevent crashes. " +
                $"Please update the mod.",
                LogLevel.Warn);
            _safeToRun = false;
        }
        else if (smapi.MinorVersion > 9)
        {
            // Minor versiyon çok ilerlemişse uyarı ver ama çalışmaya devam et
            Monitor.Log(
                $"[SeasonPlanner] Running on SMAPI {smapi} (tested up to {TestedSmapiMajor}.x). " +
                $"If you encounter issues, please report them.",
                LogLevel.Debug);
        }

        if (_safeToRun)
        {
            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll();
        }

        // Patch bağımlılıkları (safeToRun false olsa bile null kalmasın)
        CalendarPagePatch.Config  = _config;
        InventoryPagePatch.Config = _config;
        ChestPatch.Config         = _config;

        helper.Events.GameLoop.SaveLoaded   += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted   += OnDayStarted;
        helper.Events.Display.MenuChanged   += OnMenuChanged;
        helper.Events.Input.ButtonPressed   += OnButtonPressed;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;

        Monitor.Log($"Season Planner & Bundle Reminder loaded (SMAPI {smapi}).", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _scanner.Invalidate();
        PushDataToPatches();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        _scanner.Invalidate();
        var missing = _scanner.GetMissingItems(_config.FilterConstructionItems);
        PushDataToPatches(missing);

        if (!_config.ShowHudNotifications) return;
        CheckPlantingDeadlines(missing);
        CheckRainFishOpportunity(missing);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsPlayerFree && Game1.activeClickableMenu is not BundlePanelMenu) return;
        if (e.Button != _config.PanelHotkey) return;

        if (Game1.activeClickableMenu is BundlePanelMenu)
        {
            Game1.activeClickableMenu.exitThisMenu();
            return;
        }

        if (!Context.IsWorldReady) return;
        var missing = _scanner.GetMissingItems(_config.FilterConstructionItems);
        var panel   = new BundlePanelMenu(missing, _config);
        panel.exitFunction = () => { panel.SavePositionPublic(); Helper.WriteConfig(_config); };
        Game1.activeClickableMenu = panel;
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu is StardewValley.Menus.InventoryPage
         || e.NewMenu is StardewValley.Menus.GameMenu
         || e.NewMenu is StardewValley.Menus.Billboard
         || e.NewMenu is StardewValley.Menus.ItemGrabMenu)
        {
            PushDataToPatches();
        }
    }

    private void PushDataToPatches(
        System.Collections.Generic.IReadOnlyList<BundleItem>? items = null)
    {
        items ??= _scanner.GetMissingItems(_config.FilterConstructionItems);
        CalendarPagePatch.MissingItems  = items;
        InventoryPagePatch.MissingItems = items;
        ChestPatch.MissingItems         = items;
    }

    private void CheckPlantingDeadlines(
        System.Collections.Generic.IReadOnlyList<BundleItem> missing)
    {
        string season    = Game1.currentSeason.ToLower();
        int    today     = Game1.dayOfMonth;
        int    threshold = _config.CalendarWarningDaysLeft;

        foreach (var item in missing)
        {
            if (item.Season != season || item.GrowDays <= 0) continue;
            int lastPlantDay = 28 - item.GrowDays;
            int daysLeft     = lastPlantDay - today;
            if (daysLeft < 0 || daysLeft > threshold) continue;

            string msg = daysLeft == 0
                ? I18n.HudPlantingToday(item.ItemName, item.BundleName)
                : I18n.HudPlantingWarning(item.ItemName, lastPlantDay, daysLeft, item.BundleName);

            Game1.addHUDMessage(new HUDMessage(msg, HUDMessage.error_type));
        }
    }

    private void CheckRainFishOpportunity(
        System.Collections.Generic.IReadOnlyList<BundleItem> missing)
    {
        bool rainTomorrow = Game1.weatherForTomorrow == Game1.weather_rain
                         || Game1.weatherForTomorrow == Game1.weather_lightning;
        if (!rainTomorrow) return;

        foreach (var item in missing)
        {
            if (!item.RequiresRain) continue;
            Game1.addHUDMessage(new HUDMessage(
                I18n.HudRainFish(item.ItemName, item.BundleName),
                HUDMessage.newQuest_type));
            break;
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
            "spacechase0.GenericModConfigMenu");
        if (gmcm is null) return;

        gmcm.Register(
            mod:   ModManifest,
            reset: () => _config = new ModConfig(),
            save:  () => Helper.WriteConfig(_config));

        gmcm.AddBoolOption(ModManifest,
            () => _config.ShowCalendarMarkers,   v => _config.ShowCalendarMarkers = v,
            () => I18n.GmcmCalendarMarkers(),    () => I18n.GmcmCalendarMarkersTooltip());

        gmcm.AddBoolOption(ModManifest,
            () => _config.ShowHudNotifications,  v => _config.ShowHudNotifications = v,
            () => I18n.GmcmHudNotifications(),   () => I18n.GmcmHudNotificationsTooltip());

        gmcm.AddBoolOption(ModManifest,
            () => _config.ShowInventoryTooltips, v => _config.ShowInventoryTooltips = v,
            () => I18n.GmcmInventoryTooltip(),   () => I18n.GmcmInventoryTooltipTooltip());

        gmcm.AddBoolOption(ModManifest,
            () => _config.ShowChestTooltips,     v => _config.ShowChestTooltips = v,
            () => I18n.GmcmChestTooltip(),       () => I18n.GmcmChestTooltipTooltip());

        gmcm.AddBoolOption(ModManifest,
            () => _config.FilterConstructionItems, v => _config.FilterConstructionItems = v,
            () => I18n.GmcmFilterConstruction(), () => I18n.GmcmFilterConstructionTooltip());

        gmcm.AddBoolOption(ModManifest,
            () => _config.ShowShopSource,        v => _config.ShowShopSource = v,
            () => I18n.GmcmShopSource(),         () => I18n.GmcmShopSourceTooltip());

        gmcm.AddNumberOption(ModManifest,
            () => _config.CalendarWarningDaysLeft, v => _config.CalendarWarningDaysLeft = v,
            () => I18n.GmcmWarningDays(),        () => I18n.GmcmWarningDaysTooltip(),
            min: 1, max: 14);

        gmcm.AddKeybind(ModManifest,
            () => _config.PanelHotkey, v => _config.PanelHotkey = v,
            () => I18n.GmcmPanelHotkey(), () => I18n.GmcmPanelHotkeyTooltip());

        gmcm.AddBoolOption(ModManifest,
            () => _config.RememberPanelPosition, v => _config.RememberPanelPosition = v,
            () => I18n.GmcmRememberPanelPosition(), () => I18n.GmcmRememberPanelPositionTooltip());
    }
}
