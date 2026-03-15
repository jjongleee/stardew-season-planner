using StardewModdingAPI;

namespace SeasonPlanner;

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

    // Panel position persistence
    public bool RememberPanelPosition { get; set; } = true;
    public int  PanelX                { get; set; } = -1;
    public int  PanelY                { get; set; } = -1;

    // Advanced planner: persist user-planned bundle items.
    // Each planned item is stored as a stable key string (ItemId:BundleName:Quality:Quantity)
    public System.Collections.Generic.List<string> PlannedItems { get; set; } = new();
}
