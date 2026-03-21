using HarmonyLib;
using SeasonPlanner.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;

namespace SeasonPlanner;

public sealed class ModEntry : Mod
{
    private const int TestedSmapiMajor = 4;
    private const string OwnershipSignature = "Jawen | SeasonPlanner | JAWEN-SP-2026-001";

    private const string PipeName = "SeasonPlannerDebug";
    private NamedPipeServerStream? _pipe;
    private StreamWriter?          _pipeWriter;

    internal static ModEntry? Instance { get; private set; }

    private ModConfig _config = null!;
    private BundleScanner _scanner = null!;
    internal BundleScanner? Scanner => _scanner;
    private bool _safeToRun = true;
    private bool _eventsRegistered;

    private readonly object _stateLock = new();
    private IReadOnlyList<BundleItem> _latestMissing = Array.Empty<BundleItem>();

    private readonly object _logLock = new();
    private readonly List<(string message, int type)> _notificationLog = new();
    private const int MaxLogEntries = 50;

    internal static IReadOnlyList<(string message, int type)> GetNotificationLog()
    {
        var instance = Instance;
        if (instance is null) return Array.Empty<(string, int)>();
        lock (instance._logLock)
            return instance._notificationLog.ToList();
    }

    private void AddToLog(string message, int type)
    {
        lock (_logLock)
        {
            _notificationLog.Insert(0, (message, type));
            if (_notificationLog.Count > MaxLogEntries)
                _notificationLog.RemoveAt(_notificationLog.Count - 1);
        }
    }

    public override void Entry(IModHelper helper)
    {
        Instance = this;

        _config = helper.ReadConfig<ModConfig>();
        _scanner = new BundleScanner(Monitor, helper);
        I18n.Initialize(helper.Translation);

        if (_config.DebugMode)
        {
            _pipe = new NamedPipeServerStream(PipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.None);

            string scriptPath = Path.Combine(Helper.DirectoryPath, "debug_console.ps1");
            Process.Start(new ProcessStartInfo
            {
                FileName        = "powershell.exe",
                Arguments       = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                UseShellExecute = true,
            });

            Task.Run(() =>
            {
                try
                {
                    _pipe.WaitForConnection();
                    _pipeWriter = new StreamWriter(_pipe) { AutoFlush = true };
                    _pipeWriter.WriteLine("=== SeasonPlanner Debug Console ===");
                    _pipeWriter.WriteLine("DebugMode aktif. Kayit yuklenince loglar burada gorunecek.");
                    _pipeWriter.WriteLine();
                }
                catch { }
            });
        }

        var smapi = helper.ModRegistry
            .Get("SMAPI")?.Manifest.Version
            ?? Constants.ApiVersion;

        if (smapi.MajorVersion != TestedSmapiMajor)
        {
            Monitor.Log(
                $"[SeasonPlanner] SMAPI major surum uyumsuzlugu! Test edilen: {TestedSmapiMajor}.x, mevcut: {smapi}. Harmony patch'leri devre disi birakildi. Modu guncelleyin. / SMAPI major version mismatch! Tested with {TestedSmapiMajor}.x, running on {smapi}. Harmony patches are disabled to prevent crashes. Please update the mod.",
                LogLevel.Warn);
            _safeToRun = false;
        }
        else if (smapi.MinorVersion > 9)
        {
            Monitor.Log(
                $"[SeasonPlanner] SMAPI {smapi} uzerinde calisiyor (test edilen: {TestedSmapiMajor}.x). Sorun yasarsaniz lutfen bildirin. / Running on SMAPI {smapi} (tested up to {TestedSmapiMajor}.x). If you encounter issues, please report them.",
                LogLevel.Debug);
        }

        if (_safeToRun)
        {
            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll();
        }

        RegisterEvents(helper.Events);

        Monitor.Log($"Season Planner & Demet Hatirlatici yuklendi / loaded (SMAPI {smapi}).", LogLevel.Info);
        Monitor.Log($"Ownership signature: {OwnershipSignature}", LogLevel.Trace);
    }

    private void RegisterEvents(IModEvents events)
    {
        if (_eventsRegistered)
            return;

        events.GameLoop.SaveLoaded += OnSaveLoaded;
        events.GameLoop.DayStarted += OnDayStarted;
        events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        events.Display.MenuChanged += OnMenuChanged;
        events.Input.ButtonPressed += OnButtonPressed;
        events.GameLoop.GameLaunched += OnGameLaunched;
        events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
        events.Display.Rendered += OnRendered;
        _eventsRegistered = true;
    }

    private void UpdateSharedState(IReadOnlyList<BundleItem>? items = null)
    {
        var resolved = items ?? _scanner.GetMissingItems(_config.FilterConstructionItems);
        lock (_stateLock)
            _latestMissing = resolved;
    }

    internal static bool TryGetSharedState(out IReadOnlyList<BundleItem> missingItems, out ModConfig? config)
    {
        var instance = Instance;
        if (instance is null)
        {
            missingItems = Array.Empty<BundleItem>();
            config = null;
            return false;
        }

        lock (instance._stateLock)
            missingItems = instance._latestMissing;
        config = instance._config;
        return true;
    }

    internal void DebugLog(string msg) 
    {
        if (!_config.DebugMode) return;
        try { _pipeWriter?.WriteLine($"[SeasonPlanner] {msg}"); } catch { }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _scanner.Invalidate();
        UpdateSharedState();

        if (!_config.DebugMode) return;

        for (int i = 0; i < 20 && _pipeWriter is null; i++)
            System.Threading.Thread.Sleep(200);

        DebugLog("=== SeasonPlanner Debug Mode ===");

        var missing = _scanner.GetMissingItems(_config.FilterConstructionItems);
        DebugLog($"Eksik bundle item sayisi: {missing.Count}");

        var museum = _scanner.GetMuseumItems();
        int donated = museum.Count(i => i.IsMuseumDonated);
        DebugLog($"Muze: {donated}/{museum.Count} bagislandi");

        var fishItems = missing.Where(i => i.Category == BundleCategory.Fish).ToList();
        DebugLog($"Balik bundle item sayisi: {fishItems.Count}");
        foreach (var f in fishItems)
        {
            string locs = f.FishLocations.Count > 0 ? string.Join(", ", f.FishLocations) : "(lokasyon yok)";
            DebugLog($"  Balik: {f.ItemName} | Lokasyon: {locs} | Hava: {f.FishWeather ?? "herhangi"} | Saat: {f.FishTimeRange ?? "herhangi"}");
        }

        var shopItems = missing
            .Where(i => i.ShopSource is not null)
            .GroupBy(i => $"{i.QualifiedItemId}|{i.ShopSource}")
            .Select(g => g.First())
            .OrderBy(i => i.ShopSource)
            .ThenBy(i => i.ItemName)
            .ToList();
        DebugLog($"Magazadan alinan item sayisi (tekrarsiz): {shopItems.Count}");
        foreach (var s in shopItems)
            DebugLog($"  Magaza: {s.ItemName} ({s.QualifiedItemId}) -> {s.ShopSource}");

        var noShopItems = missing
            .Where(i => i.ShopSource is null && i.Category != BundleCategory.Fish && i.Category != BundleCategory.Construction)
            .GroupBy(i => i.QualifiedItemId)
            .Select(g => g.First())
            .OrderBy(i => i.ItemName)
            .ToList();
        DebugLog($"Magaza kaynagi bulunamayan item sayisi: {noShopItems.Count}");
        foreach (var s in noShopItems)
            DebugLog($"  Kaynak yok: {s.ItemName} ({s.QualifiedItemId}) | Kategori: {s.Category}");

        var seasonal = _scanner.GetAllSeasonalItems();
        DebugLog($"Mevsimsel item sayisi: {seasonal.Count}");

        DebugLog("=== Debug bitti ===");
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        _scanner.Invalidate();
        var missing = _scanner.GetMissingItems(_config.FilterConstructionItems);
        UpdateSharedState(missing);

        if (!_config.ShowHudNotifications)
            return;

        CheckCompletedPlanned(missing);
        CheckPlantingDeadlines(missing);
        CheckRainFishOpportunity(missing);
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        _scanner.Invalidate();
        lock (_stateLock)
            _latestMissing = Array.Empty<BundleItem>();
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsPlayerFree && Game1.activeClickableMenu is not BundlePanelMenu)
            return;

        if (e.Button != _config.PanelHotkey)
            return;

        if (Game1.activeClickableMenu is BundlePanelMenu)
        {
            Game1.activeClickableMenu.exitThisMenu();
            return;
        }

        if (!Context.IsWorldReady)
            return;

        var missing = _scanner.GetMissingItems(_config.FilterConstructionItems);
        var panel = new BundlePanelMenu(missing, _config);
        panel.exitFunction = () =>
        {
            panel.SavePositionPublic();
            Helper.WriteConfig(_config);
        };
        Game1.activeClickableMenu = panel;
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.OldMenu is StardewValley.Menus.JunimoNoteMenu)
        {
            _scanner.Invalidate();
        }

        if (e.NewMenu is StardewValley.Menus.InventoryPage
         || e.NewMenu is StardewValley.Menus.GameMenu
         || e.NewMenu is StardewValley.Menus.Billboard
         || e.NewMenu is StardewValley.Menus.ItemGrabMenu
         || e.NewMenu is StardewValley.Menus.ShopMenu
         || e.NewMenu is StardewValley.Menus.MuseumMenu)
        {
            UpdateSharedState();
        }
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        DrawTooltipIfNeeded(e.SpriteBatch);
    }

    private void OnRendered(object? sender, RenderedEventArgs e)
    {

    }

    private void DrawTooltipIfNeeded(Microsoft.Xna.Framework.Graphics.SpriteBatch b)
    {
        if (!_config.ShowInventoryTooltips && !_config.ShowChestTooltips)
            return;

        var menu = Game1.activeClickableMenu;
        if (menu is null)
            return;

        if (menu is StardewValley.Menus.ShopMenu)
            return;

        if (menu is StardewValley.Menus.GameMenu gm2
            && gm2.pages.Any(p => p is StardewValley.Menus.ShopMenu))
            return;

        if (!TryGetSharedState(out var missing, out _))
            return;

        if (_config.DebugMode && (menu is StardewValley.Menus.MuseumMenu || menu.GetType().Name.Contains("Museum")))
            Monitor.Log($"[DBG] active menu = {menu.GetType().FullName}", LogLevel.Debug);

        Item? hovered = null;
        if (_config.ShowInventoryTooltips
            && menu is StardewValley.Menus.GameMenu gm
            && gm.currentTab == StardewValley.Menus.GameMenu.inventoryTab
            && gm.pages[StardewValley.Menus.GameMenu.inventoryTab] is StardewValley.Menus.InventoryPage invPage)
        {
            hovered = invPage.hoveredItem
                ?? invPage.inventory?.hover(Game1.getMouseX(), Game1.getMouseY(), null);
        }
        else if (_config.ShowChestTooltips
            && menu is StardewValley.Menus.ItemGrabMenu igm)
        {
            hovered = igm.ItemsToGrabMenu?.hover(Game1.getMouseX(), Game1.getMouseY(), null)
                ?? igm.inventory?.hover(Game1.getMouseX(), Game1.getMouseY(), null);
        }
        else if (_config.ShowInventoryTooltips && menu is StardewValley.Menus.MuseumMenu museumMenu)
        {
            int mx = Game1.getMouseX(true);
            int my = Game1.getMouseY(true);

            Item? museumHovered = museumMenu.hoveredItem
                ?? museumMenu.inventory?.getItemAt(mx, my)
                ?? museumMenu.inventory?.getItemAt(Game1.getMouseX(), Game1.getMouseY());

            if (museumHovered is not null)
            {
                TooltipHelper.DrawMuseumTooltip(b, museumHovered, missing, _config);
                return;
            }
        }

        if (hovered is null)
            return;

        if (!TooltipHelper.DrawMuseumTooltip(b, hovered, missing, _config))
            TooltipHelper.DrawBundleTooltip(b, hovered, missing, _config);
    }

    private void CheckCompletedPlanned(IReadOnlyList<BundleItem> missing)
    {
        if (_config.PlannedItems.Count == 0) return;

        var stillMissing = new HashSet<string>(
            missing.Select(i => $"{i.QualifiedItemId}:{i.BundleName}:{i.Quality}:{i.Quantity}"));

        var completed = new List<string>();
        foreach (var key in _config.PlannedItems)
        {
            if (!stillMissing.Contains(key))
                completed.Add(key);
        }

        foreach (var key in completed)
        {
            _config.PlannedItems.Remove(key);
            var parts = key.Split(':');
            string itemName   = parts.Length > 0 ? parts[0] : key;
            string bundleName = parts.Length > 1 ? parts[1] : string.Empty;

            var found = missing.FirstOrDefault(i =>
                $"{i.QualifiedItemId}:{i.BundleName}:{i.Quality}:{i.Quantity}" == key);
            if (found is not null) { itemName = found.ItemName; bundleName = found.BundleName; }

            Game1.addHUDMessage(new HUDMessage(
                I18n.HudPlannedCompleted(itemName, bundleName),
                HUDMessage.newQuest_type));
            AddToLog(I18n.HudPlannedCompleted(itemName, bundleName), HUDMessage.newQuest_type);
        }

        if (completed.Count > 0)
            Helper.WriteConfig(_config);
    }

    private void CheckPlantingDeadlines(IReadOnlyList<BundleItem> missing)
    {
        string season = Game1.currentSeason.ToLower();
        int today = Game1.dayOfMonth;
        int threshold = _config.CalendarWarningDaysLeft;

        foreach (var item in missing)
        {
            if (item.Season != season || item.GrowDays <= 0)
                continue;

            int lastPlantDay = 28 - item.GrowDays;
            int daysLeft = lastPlantDay - today;
            if (daysLeft < 0 || daysLeft > threshold)
                continue;

            string msg = daysLeft == 0
                ? I18n.HudPlantingToday(item.ItemName, item.BundleName)
                : I18n.HudPlantingWarning(item.ItemName, lastPlantDay, daysLeft, item.BundleName);

            Game1.addHUDMessage(new HUDMessage(msg, HUDMessage.error_type));
            AddToLog(msg, HUDMessage.error_type);
        }
    }

    private void CheckRainFishOpportunity(IReadOnlyList<BundleItem> missing)
    {
        bool rainTomorrow = Game1.weatherForTomorrow == Game1.weather_rain
                         || Game1.weatherForTomorrow == Game1.weather_lightning;
        if (!rainTomorrow)
            return;

        foreach (var item in missing)
        {
            if (!item.RequiresRain)
                continue;

            Game1.addHUDMessage(new HUDMessage(
                I18n.HudRainFish(item.ItemName, item.BundleName),
                HUDMessage.newQuest_type));
            AddToLog(I18n.HudRainFish(item.ItemName, item.BundleName), HUDMessage.newQuest_type);
            break;
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
            "spacechase0.GenericModConfigMenu");
        if (gmcm is null)
            return;

        gmcm.Register(
            mod: ModManifest,
            reset: () => _config = new ModConfig(),
            save: () => Helper.WriteConfig(_config));

        gmcm.AddBoolOption(ModManifest,
            () => _config.ShowCalendarMarkers, v => _config.ShowCalendarMarkers = v,
            () => I18n.GmcmCalendarMarkers(), () => I18n.GmcmCalendarMarkersTooltip());

        gmcm.AddBoolOption(ModManifest,
            () => _config.ShowHudNotifications, v => _config.ShowHudNotifications = v,
            () => I18n.GmcmHudNotifications(), () => I18n.GmcmHudNotificationsTooltip());

        gmcm.AddBoolOption(ModManifest,
            () => _config.ShowInventoryTooltips, v => _config.ShowInventoryTooltips = v,
            () => I18n.GmcmInventoryTooltip(), () => I18n.GmcmInventoryTooltipTooltip());

        gmcm.AddBoolOption(ModManifest,
            () => _config.ShowChestTooltips, v => _config.ShowChestTooltips = v,
            () => I18n.GmcmChestTooltip(), () => I18n.GmcmChestTooltipTooltip());

        gmcm.AddBoolOption(ModManifest,
            () => _config.FilterConstructionItems, v => _config.FilterConstructionItems = v,
            () => I18n.GmcmFilterConstruction(), () => I18n.GmcmFilterConstructionTooltip());

        gmcm.AddBoolOption(ModManifest,
            () => _config.ShowShopSource, v => _config.ShowShopSource = v,
            () => I18n.GmcmShopSource(), () => I18n.GmcmShopSourceTooltip());

        gmcm.AddNumberOption(ModManifest,
            () => _config.PanelScale, v => _config.PanelScale = v,
            () => I18n.GmcmPanelScale(), () => I18n.GmcmPanelScaleTooltip(),
            min: 50, max: 150, interval: 10, formatValue: v => $"{v}%");

        gmcm.AddNumberOption(ModManifest,
            () => _config.BundleTooltipScale, v => _config.BundleTooltipScale = v,
            () => I18n.GmcmBundleTooltipScale(), () => I18n.GmcmBundleTooltipScaleTooltip(),
            min: 50, max: 200, interval: 10, formatValue: v => $"{v}%");

        gmcm.AddNumberOption(ModManifest,
            () => _config.SeedTooltipScale, v => _config.SeedTooltipScale = v,
            () => I18n.GmcmSeedTooltipScale(), () => I18n.GmcmSeedTooltipScaleTooltip(),
            min: 50, max: 200, interval: 10, formatValue: v => $"{v}%");

        gmcm.AddNumberOption(ModManifest,
            () => _config.CalendarWarningDaysLeft, v => _config.CalendarWarningDaysLeft = v,
            () => I18n.GmcmWarningDays(), () => I18n.GmcmWarningDaysTooltip(),
            min: 1, max: 14, interval: null, formatValue: null);

        gmcm.AddKeybind(ModManifest,
            () => _config.PanelHotkey, v => _config.PanelHotkey = v,
            () => I18n.GmcmPanelHotkey(), () => I18n.GmcmPanelHotkeyTooltip());

        gmcm.AddBoolOption(ModManifest,
            () => _config.RememberPanelPosition, v => _config.RememberPanelPosition = v,
            () => I18n.GmcmRememberPanelPosition(), () => I18n.GmcmRememberPanelPositionTooltip());

        gmcm.AddTextOption(ModManifest,
            getValue: () => _config.PanelAnchor.ToString(),
            setValue: v =>
            {
                if (Enum.TryParse<PanelAnchor>(v, out var anchor))
                {
                    _config.PanelAnchor = anchor;
                    if (anchor != PanelAnchor.Custom)
                    {
                        _config.PanelX = -1;
                        _config.PanelY = -1;
                    }
                }
            },
            name: () => I18n.GmcmPanelAnchor(),
            tooltip: () => I18n.GmcmPanelAnchorTooltip(),
            allowedValues: new[]
            {
                "TopLeft","TopCenter","TopRight",
                "MiddleLeft","Center","MiddleRight",
                "BottomLeft","BottomCenter","BottomRight",
                "Custom",
            },
            formatAllowedValue: v => I18n.GmcmAnchorLabel(v));

        gmcm.AddBoolOption(ModManifest,
            getValue: () => false,
            setValue: v =>
            {
                if (!v)
                    return;

                _config.PanelX = -1;
                _config.PanelY = -1;
                _config.PanelAnchor = PanelAnchor.Center;
            },
            name: () => I18n.GmcmResetPosition(),
            tooltip: () => I18n.GmcmResetPositionTooltip());

        Helper.Events.GameLoop.GameLaunched -= OnGameLaunched;
    }
}

