using StardewModdingAPI;

namespace SeasonPlanner;

public enum PanelAnchor
{
    TopLeft, TopCenter, TopRight,
    MiddleLeft, Center, MiddleRight,
    BottomLeft, BottomCenter, BottomRight,
    Custom,
}

public sealed class ModConfig
{
    public bool    ShowCalendarMarkers     { get; set; } = true;
    public bool    ShowHudNotifications    { get; set; } = true;
    public bool    ShowInventoryTooltips   { get; set; } = true;
    public bool    ShowChestTooltips       { get; set; } = true;
    public bool    FilterConstructionItems { get; set; } = true;
    public bool    ShowShopSource          { get; set; } = true;
    public int     CalendarWarningDaysLeft { get; set; } = 7;
    public SButton PanelHotkey            { get; set; } = SButton.F5;

    public bool        RememberPanelPosition { get; set; } = true;
    public PanelAnchor PanelAnchor           { get; set; } = PanelAnchor.Center;
    public int         PanelX                { get; set; } = -1;
    public int         PanelY                { get; set; } = -1;
    public int         PanelScale            { get; set; } = 100;
    public int         BundleTooltipScale    { get; set; } = 100;
    public int         SeedTooltipScale      { get; set; } = 100;

    public System.Collections.Generic.List<string> PlannedItems { get; set; } = new();
    public System.Collections.Generic.List<string> PlannedMuseumItems { get; set; } = new();
}

