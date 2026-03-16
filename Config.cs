using StardewModdingAPI;

namespace SeasonPlanner;

/// <summary>Panel'in ekranda nereye sabitlendiğini belirler.</summary>
public enum PanelAnchor
{
    TopLeft, TopCenter, TopRight,
    MiddleLeft, Center, MiddleRight,
    BottomLeft, BottomCenter, BottomRight,
    Custom,   // Kullanıcı sürükleyerek konumlandırdı
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

    // Panel position
    public bool        RememberPanelPosition { get; set; } = true;
    public PanelAnchor PanelAnchor           { get; set; } = PanelAnchor.Center;
    public int         PanelX                { get; set; } = -1;
    public int         PanelY                { get; set; } = -1;

    // Advanced planner
    public System.Collections.Generic.List<string> PlannedItems { get; set; } = new();
}
