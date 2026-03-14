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
}
