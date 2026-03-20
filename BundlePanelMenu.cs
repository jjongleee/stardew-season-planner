using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace SeasonPlanner;


public sealed class BundlePanelMenu : IClickableMenu
{

    private const int BasePW    = 900;
    private const int BasePH    = 640;
    private const int BaseHeadH = 160;
    private const int BaseFootH = 36;
    private const int BaseTabColW = 130;
    private const int BaseCalColW = 220;
    private const int BaseSbW     = 12;
    private const int BasePad     = 14;
    private const int BaseTabH    = 52;
    private const int BaseCardH   = 80;
    private const int BaseIconSz  = 52;
    private const int BaseSortBtnW = 80;
    private const int BaseSortBtnH = 28;
    private const int BaseChipH    = 26;

    private int PW, PH, HeadH, FootH;
    private int TabColW, CalColW, SbW, Pad;
    private int TabH, CardH, IconSz, SortBtnW, SortBtnH, ChipH;

    private float FTitle, FName, FMid, FSmall, FTiny;

    private float _scale;

    private static readonly Color CInk     = new(52,  30,  10);
    private static readonly Color CSub     = new(100, 72,  38);
    private static readonly Color CDiv     = new(178, 148, 100);
    private static readonly Color CUrgent  = new(192, 24,  24);
    private static readonly Color CWarn    = new(200, 108, 0);
    private static readonly Color CGold    = new(180, 140, 0);
    private static readonly Color CGreen   = new(24,  108, 24);
    private static readonly Color CBlue    = new(20,  88,  188);
    private static readonly Color CPurple  = new(118, 28,  138);
    private static readonly Color CBrown   = new(100, 58,  10);
    private static readonly Color COrange  = new(148, 78,  0);
    private static readonly Color CPlanned = new(28,  136, 28);
    private static readonly Color CTabSel  = new(255, 248, 220);
    private static readonly Color CTabHov  = new(236, 214, 172);
    private static readonly Color CTabNorm = new(208, 182, 138);
    private static readonly Color CBarBg   = new(188, 160, 120);
    private static readonly Color CBarGood = new(68,  148, 48);
    private static readonly Color CBarMid  = new(200, 108, 0);
    private static readonly Color CBarBad  = new(188, 28,  28);

    private static readonly BundleCategory?[] TabCats =
    {
        null, BundleCategory.Crop, BundleCategory.Fish,
        BundleCategory.Artisan, BundleCategory.Forage,
        BundleCategory.Construction, BundleCategory.Other,
        null, // index 7 = Sezon sekmesi (özel)
        null, // index 8 = Müze sekmesi (özel)
    };
    private static string TabLabel(int i) => i switch
    {
        0 => I18n.TabAll(), 1 => I18n.TabCrop(), 2 => I18n.TabFish(),
        3 => I18n.TabArtisan(), 4 => I18n.TabForage(),
        5 => I18n.TabConstruction(), 6 => I18n.TabOther(),
        7 => I18n.TabSeasonal(),
        _ => I18n.TabMuseum(),
    };
    private const int SeasonTab = 7;
    private const int MuseumTab = 8;
    private static Color CatColor(BundleCategory? c) => c switch
    {
        BundleCategory.Crop         => CGreen,
        BundleCategory.Fish         => CBlue,
        BundleCategory.Artisan      => CPurple,
        BundleCategory.Forage       => new Color(34, 120, 80),
        BundleCategory.Construction => CBrown,
        _                           => COrange,
    };

    private enum SortMode { Urgency, Name, Bundle }
    private string[] SortLabels => new[] { I18n.SortUrgency(), I18n.SortName(), I18n.SortBundle() };

    private enum ChipFilter { None, Urgent, Seasonal, Planned }
    private ChipFilter _chipFilter = ChipFilter.None;

    private int      _tab, _scroll;
    private int      _hoverTab = -1, _hoverCard = -1, _hoverSort = -1, _hoverChip = -1, _hoverCalDay = -1;
    private Rectangle[] _calCellRects = new Rectangle[29]; // 1-indexed
    private bool     _drag;
    private int      _dragY0, _dragScr0;
    private bool     _dragPanel;
    private int      _dragPanelX0, _dragPanelY0, _dragMouseX0, _dragMouseY0;
    private SortMode _sort = SortMode.Urgency;

    private string _searchText = string.Empty;
    private bool   _searchFocused;
    private Rectangle _searchRect;

    private readonly IReadOnlyList<BundleItem> _all;
    private readonly ModConfig?                _config;
    private readonly HashSet<string>           _planned = new();
    private readonly HashSet<string>           _completedPlanned = new();
    private readonly HashSet<string>           _plannedMuseum = new();
    private List<BundleItem>                   _vis     = new();
    private IReadOnlyList<BundleItem>          _allSeasonal = Array.Empty<BundleItem>();
    private IReadOnlyList<BundleItem>          _allMuseum   = Array.Empty<BundleItem>();

    private bool _showLog;
    private int  _logScroll;
    private Rectangle _logBtnRect;

    private static readonly Color CLogPlant  = new(192, 24,  24);
    private static readonly Color CLogRain   = new(30,  100, 200);
    private static readonly Color CLogQuest  = new(24,  108, 24);

    private int _px, _py;
    private int _barY, _chipY, _sortY;
    private Rectangle[] _chipRects = new Rectangle[4];
    private int _tcX, _tcY, _tcW, _tcH;
    private int _lX,  _lY,  _lW,  _lH;
    private int _sbX;
    private int _caX, _caY, _caW, _caH;
    private int _sortStartX;
    private int _urgentCount, _seasonCount;

    private int VisRows => _lH / CardH;
    private int MaxScr  => Math.Max(0, _vis.Count - VisRows);
    private static string PlanKey(BundleItem i) => $"{i.QualifiedItemId}:{i.BundleName}:{i.Quality}:{i.Quantity}";
    private static string LegacyPlanKey(BundleItem i) => $"{i.ItemId}:{i.BundleName}:{i.Quality}:{i.Quantity}";

    public BundlePanelMenu(IReadOnlyList<BundleItem> items, ModConfig? config = null)
        : base(0, 0, 1, 1)
    {
        _all    = items;
        _config = config;

        int sw = Game1.uiViewport.Width, sh = Game1.uiViewport.Height;

        float userScale = Math.Clamp((config?.PanelScale ?? 100) / 100f, 0.50f, 1.50f);

        int maxPW = Math.Min((int)(860 * userScale), (int)(sw * 0.88f));
        int maxPH = Math.Min((int)(620 * userScale), (int)(sh * 0.88f));

        float scaleW = maxPW / (float)BasePW;
        float scaleH = maxPH / (float)BasePH;
        _scale = Math.Clamp(Math.Min(scaleW, scaleH), 0.45f, 0.95f * userScale);

        int S(int v) => Math.Max(1, (int)(v * _scale));

        PW      = Math.Min(S(BasePW),  maxPW);
        PH      = Math.Min(S(BasePH),  maxPH);
        HeadH   = S(BaseHeadH);
        FootH   = S(BaseFootH);
        SbW     = S(BaseSbW);
        Pad     = S(BasePad);
        TabH    = S(BaseTabH);
        CardH   = S(BaseCardH);
        IconSz  = S(BaseIconSz);
        SortBtnW = S(BaseSortBtnW);
        SortBtnH = S(BaseSortBtnH);
        ChipH   = S(BaseChipH);

        bool showCalendar = sw >= 700 && _scale >= 0.60f;
        CalColW  = showCalendar ? S(BaseCalColW) : 0;
        TabColW  = sw < 600 ? S(90) : S(BaseTabColW);

        float fBase = Math.Clamp(_scale, 0.50f, 1.80f);
        FTitle = 0.72f * fBase;
        FName  = 0.58f * fBase;
        FMid   = 0.50f * fBase;
        FSmall = 0.44f * fBase;
        FTiny  = 0.38f * fBase;

        HeadH = Math.Max(S(110), HeadH);

        static int AnchorX(PanelAnchor a, int sw, int pw) => a switch
        {
            PanelAnchor.TopLeft    or PanelAnchor.MiddleLeft   or PanelAnchor.BottomLeft   => 16,
            PanelAnchor.TopRight   or PanelAnchor.MiddleRight  or PanelAnchor.BottomRight  => sw - pw - 16,
            _ => (sw - pw) / 2,
        };
        static int AnchorY(PanelAnchor a, int sh, int ph) => a switch
        {
            PanelAnchor.TopLeft    or PanelAnchor.TopCenter    or PanelAnchor.TopRight    => 16,
            PanelAnchor.BottomLeft or PanelAnchor.BottomCenter or PanelAnchor.BottomRight => sh - ph - 16,
            _ => Math.Max(0, (sh - ph) / 2),
        };

        PanelAnchor anchor = config?.PanelAnchor ?? PanelAnchor.Center;

        bool usesSaved = false;
        if (config is { RememberPanelPosition: true, PanelAnchor: PanelAnchor.Custom, PanelX: >= 0, PanelY: >= 0 })
        {
            int savedCx = config.PanelX + PW / 2;
            int savedCy = config.PanelY + PH / 2;
            bool fitsInViewport = config.PanelX + PW <= sw && config.PanelY + PH <= sh;
            bool isReasonable   = savedCx > sw * 0.05f && savedCx < sw * 0.95f
                                && savedCy > sh * 0.05f && savedCy < sh * 0.95f;
            usesSaved = fitsInViewport && isReasonable;
        }

        if (usesSaved)
        {
            _px = config!.PanelX;
            _py = config.PanelY;
        }
        else
        {
            _px = AnchorX(anchor, sw, PW);
            _py = AnchorY(anchor, sh, PH);
        }

        if (config?.PlannedItems is { Count: > 0 })
        {
            var allKeys = new HashSet<string>(_all.Select(PlanKey));
            foreach (var key in config.PlannedItems)
            {
                BundleItem? match = _all.FirstOrDefault(i => LegacyPlanKey(i) == key);
                string normalKey  = match is null ? key : PlanKey(match);
                _planned.Add(normalKey);
                if (!allKeys.Contains(normalKey))
                    _completedPlanned.Add(normalKey);
            }
        }

        if (config?.PlannedMuseumItems is { Count: > 0 })
        {
            foreach (var id in config.PlannedMuseumItems)
                _plannedMuseum.Add(id);
        }

        _barY  = _py + S(38);
        _chipY = _py + S(58);
        _sortY = _py + S(88);

        _tcX = _px + Pad;
        _tcY = _py + HeadH;
        _tcW = TabColW - 4;
        _tcH = PH - HeadH - FootH;

        _caW = CalColW > 0 ? CalColW - Pad : 0;
        _caH = PH - HeadH - FootH - Pad * 2;
        _caX = _px + PW - Pad - _caW;
        _caY = _py + HeadH + Pad;

        int cardLeft  = _px + Pad + TabColW + 4;
        int cardRight = CalColW > 0 ? _caX - SbW - 4 : _px + PW - SbW - 4;
        _lX  = cardLeft;
        _lY  = _py + HeadH + 4;
        _lW  = cardRight - cardLeft;
        _lH  = PH - HeadH - FootH - 8;
        _sbX = cardRight;

        int totalSortW = 3 * SortBtnW + 2 * 10;
        _sortStartX = _lX + _lW - totalSortW;

        initialize(_px, _py, PW, PH);

        upperRightCloseButton = new StardewValley.Menus.ClickableTextureComponent(
            new Rectangle(_px + PW - 36, _py - 8, 48, 48),
            Game1.mouseCursors,
            new Rectangle(337, 494, 12, 12),
            4f);

        _allSeasonal = ModEntry.Instance?.Scanner?.GetAllSeasonalItems()
                    ?? Array.Empty<BundleItem>();
        _allMuseum   = ModEntry.Instance?.Scanner?.GetMuseumItems()
                    ?? Array.Empty<BundleItem>();

        Refresh();
    }

    private void Refresh()
    {
        _scroll = 0;

        if (_tab == SeasonTab)
        {
            _vis = _allSeasonal.OrderBy(i => string.IsNullOrEmpty(i.BundleName) ? 1 : 0)
                               .ThenBy(i => i.ItemName).ToList();
            string curSeason = Game1.currentSeason?.ToLower() ?? "";
            int curDay       = Game1.dayOfMonth;
            _urgentCount  = _all.Count(i => i.GrowDays > 0 && i.Season == curSeason && (28 - i.GrowDays) - curDay <= 5);
            _seasonCount  = _allSeasonal.Count;
            return;
        }

        if (_tab == MuseumTab)
        {
            var museumSrc = _allMuseum.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                string q = _searchText.Trim().ToLower();
                museumSrc = museumSrc.Where(i => i.ItemName.ToLower().Contains(q));
            }
            if (_chipFilter == ChipFilter.Planned)
                museumSrc = museumSrc.Where(i => _plannedMuseum.Contains(i.QualifiedItemId));
            _vis = museumSrc.OrderBy(i => i.IsMuseumDonated).ThenBy(i => i.ItemName).ToList();
            _urgentCount = 0;
            _seasonCount = 0;
            return;
        }

        var cat = TabCats[_tab];
        var src = cat is null ? _all : _all.Where(i => i.Category == cat);

        string season = Game1.currentSeason?.ToLower() ?? "";
        src = _chipFilter switch
        {
            ChipFilter.Urgent   => src.Where(i => i.GrowDays > 0 && i.Season == season
                                    && (28 - i.GrowDays) - Game1.dayOfMonth <= 5),
            ChipFilter.Seasonal => src.Where(i => i.Season == season),
            ChipFilter.Planned  => src.Where(i => _planned.Contains(PlanKey(i))),
            _                   => src,
        };

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            string q = _searchText.Trim().ToLower();
            src = src.Where(i => i.ItemName.ToLower().Contains(q)
                              || i.BundleName.ToLower().Contains(q));
        }

        _vis = _sort switch
        {
            SortMode.Name   => src.OrderBy(i => i.ItemName).ToList(),
            SortMode.Bundle => src.OrderBy(i => i.BundleName).ThenBy(i => i.ItemName).ToList(),
            _               => src.OrderBy(UrgencyScore).ToList(),
        };
        int today        = Game1.dayOfMonth;
        _urgentCount  = _all.Count(i => i.GrowDays > 0 && i.Season == season && (28 - i.GrowDays) - today <= 5);
        _seasonCount  = _all.Count(i => i.Season == season);
    }

    private static int UrgencyScore(BundleItem i)
    {
        string season = Game1.currentSeason?.ToLower() ?? "";
        int today     = Game1.dayOfMonth;
        if (i.GrowDays > 0 && i.Season == season)
        {
            int dl = (28 - i.GrowDays) - today;
            if (dl <= 0) return 0; if (dl <= 3) return 1; if (dl <= 7) return 2; return 3;
        }
        return i.RequiresRain ? 4 : 10 + (int)i.Category;
    }



    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect,
            new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
            Color.Black * 0.48f);
        drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            _px, _py, PW, PH, Color.White, drawShadow: true);
        b.Draw(Game1.staminaRect,
            new Rectangle(_px + 6, _py + 6, PW - 12, PH - 12),
            new Color(252, 244, 224) * 0.12f);

        DrawHeader(b);
        DrawTabs(b);
        if (CalColW > 0) DrawCalendar(b);
        DrawCards(b);
        DrawScrollBar(b);
        DrawFooter(b);
        DrawHoverTooltip(b);

        if (_showLog) DrawLogOverlay(b);

        upperRightCloseButton?.draw(b);
        drawMouse(b);
    }

    private void DrawHeader(SpriteBatch b)
    {

        string title = I18n.PanelTitle();
        Vector2 ts   = Game1.dialogueFont.MeasureString(title) * FTitle;
        b.DrawString(Game1.dialogueFont, title,
            new Vector2(_px + (PW - ts.X) / 2f, _py + 8),
            CInk, 0f, Vector2.Zero, FTitle, SpriteEffects.None, 0f);

        int barX = _lX, barW = _lW + SbW + 2, barH = 12;
        int miss  = _all.Select(i => i.BundleName).Distinct().Count();
        float pct = Math.Clamp(1f - (float)miss / Math.Max(miss, 30), 0f, 1f);

        b.Draw(Game1.staminaRect, new Rectangle(barX, _barY, barW, barH), CBarBg * 0.35f);
        Color fc = _urgentCount > 0 ? CBarBad : pct < 0.5f ? CBarMid : CBarGood;
        int fw = (int)(barW * pct);
        if (fw > 2) b.Draw(Game1.staminaRect, new Rectangle(barX, _barY, fw, barH), fc * 0.80f);
        DrawRect(b, barX, _barY, barW, barH, CDiv * 0.50f);

        string ps  = $"{(int)(pct * 100)}%";
        Vector2 pv = Game1.dialogueFont.MeasureString(ps) * FTiny;
        b.DrawString(Game1.dialogueFont, ps,
            new Vector2(barX + barW + 6, _barY + (barH - pv.Y) / 2f),
            CSub, 0f, Vector2.Zero, FTiny, SpriteEffects.None, 0f);

        int cx = barX;
        int cw0 = DrawChip(b, cx, _chipY, $"{_all.Count} {I18n.ChipMissing()}",     CInk,    _chipFilter == ChipFilter.None,     _hoverChip == 0); _chipRects[0] = new Rectangle(cx, _chipY, cw0, ChipH); cx += cw0 + 8;
        int cw1 = DrawChip(b, cx, _chipY, $"{_urgentCount} {I18n.ChipUrgent()}",    _urgentCount > 0 ? CUrgent : CSub, _chipFilter == ChipFilter.Urgent,   _hoverChip == 1); _chipRects[1] = new Rectangle(cx, _chipY, cw1, ChipH); cx += cw1 + 8;
        int cw2 = DrawChip(b, cx, _chipY, $"{_seasonCount} {I18n.ChipSeasonal()}",  CGold,   _chipFilter == ChipFilter.Seasonal, _hoverChip == 2); _chipRects[2] = new Rectangle(cx, _chipY, cw2, ChipH); cx += cw2 + 8;
        int cw3 = DrawChip(b, cx, _chipY, $"{_planned.Count} {I18n.ChipPlanned()}", CPlanned,_chipFilter == ChipFilter.Planned,  _hoverChip == 3); _chipRects[3] = new Rectangle(cx, _chipY, cw3, ChipH);

        b.Draw(Game1.staminaRect,
            new Rectangle(_px + Pad, _py + 100, PW - Pad * 2, 1), CDiv * 0.40f);

        int sbW = Math.Min(160, (int)(160 * _scale));
        int sbH = ChipH;
        int sbX = _px + PW - Pad - sbW;
        int sbY = _chipY;
        _searchRect = new Rectangle(sbX, sbY, sbW, sbH);

        Color searchBg  = _searchFocused ? new Color(255, 252, 235) : new Color(240, 228, 200);
        Color searchBrd = _searchFocused ? CInk * 0.80f : CDiv * 0.60f;
        b.Draw(Game1.staminaRect, _searchRect, searchBg);
        DrawRect(b, sbX, sbY, sbW, sbH, searchBrd);

        string searchDisplay = string.IsNullOrEmpty(_searchText)
            ? (_searchFocused ? "|" : I18n.SearchPlaceholder())
            : (_searchFocused ? _searchText + "|" : _searchText);
        Color searchTextColor = string.IsNullOrEmpty(_searchText) && !_searchFocused ? CSub * 0.50f : CInk;
        Vector2 stv = Game1.dialogueFont.MeasureString(searchDisplay) * FMid;
        b.DrawString(Game1.dialogueFont, searchDisplay,
            new Vector2(sbX + 8, sbY + (sbH - stv.Y) / 2f),
            searchTextColor, 0f, Vector2.Zero, FMid, SpriteEffects.None, 0f);

        b.Draw(Game1.staminaRect,
            new Rectangle(_px + Pad, _py + 100, PW - Pad * 2, 1), CDiv * 0.40f);

        string sl   = $"{I18n.SortLabel()}:";
        Vector2 slv = Game1.dialogueFont.MeasureString(sl) * FMid;
        b.DrawString(Game1.dialogueFont, sl,
            new Vector2(_lX, _sortY + (SortBtnH - slv.Y) / 2f),
            CSub, 0f, Vector2.Zero, FMid, SpriteEffects.None, 0f);

        var labels = SortLabels;
        for (int i = 0; i < labels.Length; i++)
        {
            int  bx  = _sortStartX + i * (SortBtnW + 10);
            bool sel = (int)_sort == i;
            bool hov = _hoverSort == i;
            Color bg = sel ? CInk * 0.80f : hov ? CDiv * 0.50f : CDiv * 0.22f;
            Color tc = sel ? Color.White   : CInk;
            b.Draw(Game1.staminaRect, new Rectangle(bx, _sortY, SortBtnW, SortBtnH), bg);
            DrawRect(b, bx, _sortY, SortBtnW, SortBtnH, CDiv * (sel ? 0.90f : 0.45f));
            Vector2 lv = Game1.dialogueFont.MeasureString(labels[i]) * FMid;
            b.DrawString(Game1.dialogueFont, labels[i],
                new Vector2(bx + (SortBtnW - lv.X) / 2f, _sortY + (SortBtnH - lv.Y) / 2f),
                tc, 0f, Vector2.Zero, FMid, SpriteEffects.None, 0f);
        }

        b.Draw(Game1.staminaRect,
            new Rectangle(_px + Pad, _py + HeadH - 2, PW - Pad * 2, 2), CDiv * 0.50f);
    }

    private int DrawChip(SpriteBatch b, int x, int y, string text, Color col,
        bool selected = false, bool hovered = false)
    {
        Vector2 sz = Game1.dialogueFont.MeasureString(text) * FMid;
        int w = (int)sz.X + 14, h = ChipH;
        float bgAlpha  = selected ? 0.35f : hovered ? 0.20f : 0.10f;
        float brdAlpha = selected ? 0.90f : hovered ? 0.60f : 0.30f;
        b.Draw(Game1.staminaRect, new Rectangle(x, y, w, h), col * bgAlpha);
        DrawRect(b, x, y, w, h, col * brdAlpha);

        if (selected)
            b.Draw(Game1.staminaRect, new Rectangle(x + 2, y + h - 3, w - 4, 2), col * 0.80f);
        b.DrawString(Game1.dialogueFont, text,
            new Vector2(x + 7, y + (h - sz.Y) / 2f),
            selected ? col : hovered ? col * 0.85f : col,
            0f, Vector2.Zero, FMid, SpriteEffects.None, 0f);
        return w;
    }

    private void DrawTabs(SpriteBatch b)
    {
        b.Draw(Game1.staminaRect,
            new Rectangle(_tcX, _tcY, _tcW, _tcH), new Color(200, 174, 132) * 0.18f);
        b.Draw(Game1.staminaRect,
            new Rectangle(_tcX + _tcW, _tcY, 2, _tcH), CDiv * 0.32f);

        for (int i = 0; i < TabCats.Length; i++)
        {
            int ty = _tcY + i * TabH;
            bool sel = i == _tab, hov = i == _hoverTab;
            Color bg = sel ? CTabSel : hov ? CTabHov : CTabNorm;
            b.Draw(Game1.staminaRect, new Rectangle(_tcX, ty, _tcW, TabH - 1), bg * 0.62f);
            if (sel)
                b.Draw(Game1.staminaRect, new Rectangle(_tcX, ty, 4, TabH - 1), CatColor(TabCats[i]));

            string lbl = TabLabel(i);
            Vector2 ls = Game1.dialogueFont.MeasureString(lbl) * FMid;
            b.DrawString(Game1.dialogueFont, lbl,
                new Vector2(_tcX + 12, ty + (TabH - ls.Y) / 2f),
                sel ? CInk : CSub, 0f, Vector2.Zero, FMid, SpriteEffects.None, 0f);

            int cnt = i == SeasonTab ? _allSeasonal.Count
                    : i == MuseumTab ? _allMuseum.Count(m => !m.IsMuseumDonated)
                    : TabCats[i] is null ? _all.Count : _all.Count(x => x.Category == TabCats[i]);
            string cs = cnt.ToString();
            Vector2 cv = Game1.dialogueFont.MeasureString(cs) * FTiny;
            int bx = _tcX + _tcW - (int)cv.X - 8;
            int by = ty + (TabH - (int)cv.Y) / 2;
            b.Draw(Game1.staminaRect,
                new Rectangle(bx - 3, by - 2, (int)cv.X + 6, (int)cv.Y + 4),
                CatColor(TabCats[i]) * (sel ? 0.78f : 0.38f));
            b.DrawString(Game1.dialogueFont, cs,
                new Vector2(bx, by),
                sel ? Color.White : CSub, 0f, Vector2.Zero, FTiny, SpriteEffects.None, 0f);

            if (i < TabCats.Length - 1)
                b.Draw(Game1.staminaRect,
                    new Rectangle(_tcX + 6, ty + TabH - 1, _tcW - 12, 1), CDiv * 0.14f);
        }
    }

    private void DrawCalendar(SpriteBatch b)
    {
        string season = Game1.currentSeason?.ToLower() ?? "";
        int today     = Game1.dayOfMonth;

        b.Draw(Game1.staminaRect,
            new Rectangle(_caX, _caY, _caW, _caH), new Color(200, 174, 132) * 0.14f);
        DrawRect(b, _caX, _caY, _caW, _caH, CDiv * 0.30f);

        string calTitle = CapFirst(season);
        Vector2 ct = Game1.dialogueFont.MeasureString(calTitle) * FSmall;
        b.DrawString(Game1.dialogueFont, calTitle,
            new Vector2(_caX + (_caW - ct.X) / 2f, _caY + 4),
            CInk, 0f, Vector2.Zero, FSmall, SpriteEffects.None, 0f);

        var critDays = new Dictionary<int, (Color color, List<string> items)>();
        foreach (var item in _all.Where(i => i.GrowDays > 0
            && (i.Season == season || (i.IsGreenhouse && i.GrowDays > 0))))
        {
            int ld = 28 - item.GrowDays;
            if (ld < 1) continue;
            int dl = ld - today;
            Color c = dl <= 0 ? CUrgent : dl <= 3 ? CUrgent : dl <= 7 ? CWarn : CGold;
            if (!critDays.ContainsKey(ld))
                critDays[ld] = (c, new List<string>());
            var cur = critDays[ld];
            if (c == CUrgent) critDays[ld] = (CUrgent, cur.items);
            else if (c == CWarn && cur.color == CGold) critDays[ld] = (CWarn, cur.items);
            critDays[ld].items.Add(item.ItemName);
        }

        int legendH = 48;
        int dayHeaderH = (int)(Game1.dialogueFont.MeasureString("A").Y * FTiny) + 2;
        int weekHeaderH = dayHeaderH + 2;
        int gridTop = _caY + (int)ct.Y + 8 + weekHeaderH;
        int gridH   = _caH - (int)ct.Y - 8 - weekHeaderH - legendH;
        int cellW   = _caW / 7;
        int cellH   = Math.Max(1, gridH / 4);

        string[] dayNames = {
            I18n.CalWeekMon(), I18n.CalWeekTue(), I18n.CalWeekWed(), I18n.CalWeekThu(),
            I18n.CalWeekFri(), I18n.CalWeekSat(), I18n.CalWeekSun()
        };
        for (int d = 0; d < 7; d++)
        {
            int dx = _caX + d * cellW;
            Vector2 dnv = Game1.dialogueFont.MeasureString(dayNames[d]) * FTiny;
            b.DrawString(Game1.dialogueFont, dayNames[d],
                new Vector2(dx + (cellW - dnv.X) / 2f, _caY + (int)ct.Y + 6),
                CSub * 0.70f, 0f, Vector2.Zero, FTiny, SpriteEffects.None, 0f);
        }

        b.Draw(Game1.staminaRect,
            new Rectangle(_caX + 2, gridTop - 2, _caW - 4, 1), CDiv * 0.40f);

        for (int day = 1; day <= 28; day++)
        {
            int col = (day - 1) % 7, row = (day - 1) / 7;
            int dx = _caX + col * cellW;
            int dy = gridTop + row * cellH;
            bool isToday = day == today;
            bool isCrit  = critDays.ContainsKey(day);
            bool isPast  = day < today;
            bool isHover = day == _hoverCalDay;

            _calCellRects[day] = new Rectangle(dx, dy, cellW, cellH);

            if (isToday)
                b.Draw(Game1.staminaRect, new Rectangle(dx+1,dy+1,cellW-2,cellH-2), CGold * 0.30f);
            else if (isHover)
                b.Draw(Game1.staminaRect, new Rectangle(dx+1,dy+1,cellW-2,cellH-2), CDiv * 0.35f);
            else if (isCrit && !isPast)
                b.Draw(Game1.staminaRect, new Rectangle(dx+1,dy+1,cellW-2,cellH-2), critDays[day].color * 0.12f);

            string ds = day.ToString();
            Vector2 dv = Game1.dialogueFont.MeasureString(ds) * FTiny;
            Color dc = isToday ? CGold
                     : isCrit && !isPast ? critDays[day].color
                     : isPast ? CSub * 0.30f
                     : CSub;
            b.DrawString(Game1.dialogueFont, ds,
                new Vector2(dx + (cellW - dv.X) / 2f, dy + 2),
                dc, 0f, Vector2.Zero, FTiny, SpriteEffects.None, 0f);

            if (isCrit && !isPast)
            {
                int count = critDays[day].items.Count;
                string badge = count.ToString();
                Vector2 bv = Game1.dialogueFont.MeasureString(badge) * FTiny;
                int bx = dx + cellW - (int)bv.X - 3;
                int by = dy + cellH - (int)bv.Y - 1;
                b.Draw(Game1.staminaRect,
                    new Rectangle(bx - 2, by - 1, (int)bv.X + 4, (int)bv.Y + 2),
                    critDays[day].color * 0.80f);
                b.DrawString(Game1.dialogueFont, badge,
                    new Vector2(bx, by),
                    Color.White, 0f, Vector2.Zero, FTiny, SpriteEffects.None, 0f);
            }

            if (isToday)
                DrawRect(b, dx+1, dy+1, cellW-2, cellH-2, CGold * 0.90f);

            b.Draw(Game1.staminaRect, new Rectangle(dx, dy+cellH-1, cellW, 1), CDiv * 0.10f);
            if (col < 6)
                b.Draw(Game1.staminaRect, new Rectangle(dx+cellW-1, dy, 1, cellH), CDiv * 0.10f);
        }

        int ly = _caY + _caH - legendH + 4;
        DrawLegendDot(b, _caX+4, ly,    CUrgent, I18n.CalLegendUrgent());
        DrawLegendDot(b, _caX+4, ly+16, CWarn,   I18n.CalLegendSoon());
        DrawLegendDot(b, _caX+4, ly+32, CGold,   I18n.CalLegendLater());
    }

    private void DrawLegendDot(SpriteBatch b, int x, int y, Color col, string label)
    {
        b.Draw(Game1.staminaRect, new Rectangle(x, y+3, 8, 8), col*0.70f);

        int maxTextW = _caX + _caW - (x + 12) - 2;
        if (maxTextW <= 0) return;

        Vector2 tv = Game1.dialogueFont.MeasureString(label) * FTiny;
        if (tv.X * FTiny > maxTextW)
        {
            string trimmed = label;
            while (trimmed.Length > 1 &&
                   Game1.dialogueFont.MeasureString(trimmed + "…").X * FTiny > maxTextW)
                trimmed = trimmed[..^1];
            label = trimmed + "…";
        }

        b.DrawString(Game1.dialogueFont, label,
            new Vector2(x+12, y), CSub, 0f, Vector2.Zero, FTiny, SpriteEffects.None, 0f);
    }

    private void DrawCards(SpriteBatch b)
    {
        _scroll = Math.Clamp(_scroll, 0, MaxScr);

        if (_vis.Count == 0)
        {
            string msg = _tab == MuseumTab ? I18n.MuseumAllDonated()
                       : _tab == 0 ? I18n.PanelAllComplete() : I18n.PanelCategoryEmpty();
            Vector2 ms = Game1.dialogueFont.MeasureString(msg) * FMid;
            b.DrawString(Game1.dialogueFont, msg,
                new Vector2(_lX + (_lW - ms.X)/2f, _lY + _lH/2f - ms.Y/2f),
                CGreen, 0f, Vector2.Zero, FMid, SpriteEffects.None, 0f);
            return;
        }

        string season = Game1.currentSeason?.ToLower() ?? "";
        int today     = Game1.dayOfMonth;

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.PointClamp, null,
            new RasterizerState { ScissorTestEnable = true });
        b.GraphicsDevice.ScissorRectangle = new Rectangle(_lX, _lY, _lW, _lH);

        for (int i = _scroll; i < _vis.Count && i < _scroll + VisRows; i++)
        {
            var  item    = _vis[i];
            int  cardY   = _lY + (i - _scroll) * CardH;
            bool hov     = i == _hoverCard;
            bool planned = _tab == MuseumTab
                ? _plannedMuseum.Contains(item.QualifiedItemId)
                : _planned.Contains(PlanKey(item));

            string badge    = string.Empty;
            Color  badgeCol = Color.Transparent;
            Color  accent   = CatColor(item.Category);
            bool   isUrgent = false, isWarn = false;

            if (_tab == MuseumTab)
            {
                if (item.IsMuseumDonated)
                {
                    accent = CGreen;
                    badge  = I18n.MuseumDonatedBadge();
                    badgeCol = CGreen;
                }
                else
                {
                    accent = new Color(160, 100, 20);
                }
            }
            else if (item.GrowDays > 0 && item.Season == season)
            {
                int dl = (28 - item.GrowDays) - today;
                if      (dl <= 0) { isUrgent=true; badge=I18n.BadgeToday();      badgeCol=CUrgent; accent=CUrgent; }
                else if (dl <= 3) { isUrgent=true; badge=I18n.BadgeDaysLeft(dl); badgeCol=CUrgent; accent=CUrgent; }
                else if (dl <= 7) { isWarn=true;   badge=I18n.BadgeDaysLeft(dl); badgeCol=CWarn;   accent=CWarn;   }
            }
            else if (item.RequiresRain) { badge=I18n.BadgeRain(); badgeCol=CBlue; }
            if (planned) accent = CPlanned;

            Color cardBg = planned  ? CPlanned*0.07f : isUrgent ? CUrgent*0.08f
                         : isWarn   ? CWarn*0.05f    : hov      ? Color.White*0.07f
                         : i%2==0   ? Color.Black*0.015f : Color.White*0.028f;
            b.Draw(Game1.staminaRect, new Rectangle(_lX, cardY, _lW, CardH), cardBg);
            b.Draw(Game1.staminaRect, new Rectangle(_lX, cardY, isUrgent?5:3, CardH), accent*0.72f);

            try
            {
                Item? iconItem = null;
                try { iconItem = ItemRegistry.Create(item.QualifiedItemId, 1, item.Quality, allowNull: true); }
                catch { }
                if (iconItem is null && item.ItemId > 0)
                    iconItem = new StardewValley.Object(item.ItemId.ToString(), 1);
                iconItem?.drawInMenu(b, new Vector2(_lX+8, cardY+(CardH-IconSz)/2f),
                    IconSz/64f, 1f, 1f, StackDrawType.Hide, Color.White, false);
            }
            catch { }

            int tx = _lX + 8 + IconSz + 10;

            b.DrawString(Game1.dialogueFont,
                item.ItemName + (item.Quantity > 1 ? $"  x{item.Quantity}" : ""),
                new Vector2(tx, cardY+6), accent, 0f, Vector2.Zero, FName, SpriteEffects.None, 0f);

            b.DrawString(Game1.dialogueFont, item.BundleName,
                new Vector2(tx, cardY+30), CSub, 0f, Vector2.Zero, FMid, SpriteEffects.None, 0f);

            var tags = new List<string>();
            if (item.Quality > 0)        tags.Add(I18n.QualityLabel(item.Quality));
            if (item.Season != null)     tags.Add(I18n.SeasonLabel(item.Season));
            else if (item.IsGreenhouse && item.GrowDays > 0) tags.Add(I18n.SeedTooltipGreenhouseAvailable().Trim());
            if (item.ShopSource != null) tags.Add($"@ {item.ShopSource}");
            if (item.GrowDays > 0)       tags.Add($"{item.GrowDays}g");
            if (item.RequiresRain)       tags.Add(I18n.BadgeRain());
            if (tags.Count > 0)
                b.DrawString(Game1.dialogueFont, string.Join("  ·  ", tags),
                    new Vector2(tx, cardY+50), CSub*0.70f, 0f, Vector2.Zero, FSmall, SpriteEffects.None, 0f);

            if (!string.IsNullOrEmpty(badge))
            {
                float nameW = Game1.dialogueFont.MeasureString(
                    item.ItemName + (item.Quantity > 1 ? $"  x{item.Quantity}" : "")).X * FName;
                int bx = tx + (int)nameW + 8;
                int by = cardY + 6;
                Vector2 bsz = Game1.dialogueFont.MeasureString(badge) * FSmall;
                int bw = (int)bsz.X + 8, bh = (int)bsz.Y + 4;
                b.Draw(Game1.staminaRect, new Rectangle(bx-2, by, bw, bh), badgeCol*0.80f);
                DrawRect(b, bx-2, by, bw, bh, badgeCol);
                b.DrawString(Game1.dialogueFont, badge,
                    new Vector2(bx+2, by + (bh - bsz.Y)/2f),
                    Color.White, 0f, Vector2.Zero, FSmall, SpriteEffects.None, 0f);
            }

            string pbl    = planned ? I18n.PlannedBtn() : (_tab == MuseumTab && item.IsMuseumDonated ? I18n.CompletedBtn() : I18n.PlanBtn());
            float  pbScale = FSmall;
            Vector2 pbsz  = Game1.dialogueFont.MeasureString(pbl) * pbScale;
            int PBW = (int)pbsz.X + 10;
            int PBH = (int)pbsz.Y + 6;
            int pbX = _lX + _lW - PBW - 8;
            int pbY = cardY + CardH - PBH - 6;
            b.Draw(Game1.staminaRect, new Rectangle(pbX, pbY, PBW, PBH),
                planned ? CPlanned*0.70f : CDiv*0.28f);
            DrawRect(b, pbX, pbY, PBW, PBH, planned ? CPlanned : CDiv*0.42f);
            b.DrawString(Game1.dialogueFont, pbl,
                new Vector2(pbX + (PBW - pbsz.X)/2f, pbY + (PBH - pbsz.Y)/2f),
                planned ? Color.White : CSub, 0f, Vector2.Zero, pbScale, SpriteEffects.None, 0f);

            b.Draw(Game1.staminaRect,
                new Rectangle(_lX+4, cardY+CardH-1, _lW-8, 1), CDiv*0.18f);
        }

        DrawCompletedPlannedRows(b);

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
    }

    private void DrawCompletedPlannedRows(SpriteBatch b)
    {
        if (_completedPlanned.Count == 0) return;

        int startRow = _vis.Count - _scroll;
        int maxRows  = VisRows;
        if (startRow >= maxRows) return; // scroll aşağıdaysa görünmez

        Color doneColor = new Color(34, 160, 34);
        int row = startRow;

        foreach (var key in _completedPlanned)
        {
            if (row >= maxRows) break;
            int cardY = _lY + row * CardH;

            b.Draw(Game1.staminaRect, new Rectangle(_lX, cardY, _lW, CardH), doneColor * 0.06f);
            b.Draw(Game1.staminaRect, new Rectangle(_lX, cardY, 3, CardH), doneColor * 0.60f);

            var parts      = key.Split(':');
            string itemDisp   = parts.Length > 0 ? parts[0].Replace("(O)", "") : key;
            string bundleDisp = parts.Length > 1 ? parts[1] : string.Empty;

            int tx = _lX + 8 + IconSz + 10;
            b.DrawString(Game1.dialogueFont,
                I18n.CompletedBtn() + "  " + itemDisp,
                new Vector2(tx, cardY + 6), doneColor, 0f, Vector2.Zero, FName, SpriteEffects.None, 0f);

            if (!string.IsNullOrEmpty(bundleDisp))
                b.DrawString(Game1.dialogueFont, bundleDisp,
                    new Vector2(tx, cardY + 30), doneColor * 0.65f, 0f, Vector2.Zero, FMid, SpriteEffects.None, 0f);

            b.Draw(Game1.staminaRect,
                new Rectangle(_lX + 4, cardY + CardH - 1, _lW - 8, 1), CDiv * 0.18f);
            row++;
        }
    }

    private void DrawScrollBar(SpriteBatch b)
    {
        if (MaxScr <= 0) return;
        b.Draw(Game1.staminaRect, new Rectangle(_sbX, _lY, SbW, _lH), CBarBg*0.18f);
        DrawRect(b, _sbX, _lY, SbW, _lH, CDiv*0.22f);
        int th = Math.Max(28, _lH * VisRows / Math.Max(1, _vis.Count));
        int ty = _lY + (MaxScr > 0 ? (int)((float)_scroll / MaxScr * (_lH - th)) : 0);
        b.Draw(Game1.staminaRect, new Rectangle(_sbX+2, ty+2, SbW-4, th-4), CInk*0.28f);
        DrawRect(b, _sbX+1, ty+1, SbW-2, th-2, CInk*0.42f);
    }

    private void DrawFooter(SpriteBatch b)
    {
        int fy = _py + PH - FootH;
        b.Draw(Game1.staminaRect,
            new Rectangle(_px + Pad, fy + 3, PW - Pad*2, 1), CDiv*0.38f);

        string cnt  = I18n.PanelItemsCount(_vis.Count);
        Vector2 csv = Game1.dialogueFont.MeasureString(cnt) * FSmall;
        b.DrawString(Game1.dialogueFont, cnt,
            new Vector2(_lX, fy + (FootH - csv.Y) / 2f),
            CSub, 0f, Vector2.Zero, FSmall, SpriteEffects.None, 0f);

        string logLbl = I18n.PanelLogBtn();
        Vector2 llv   = Game1.dialogueFont.MeasureString(logLbl) * FSmall;
        int lbW = (int)llv.X + 12, lbH = (int)llv.Y + 6;
        int lbX = _lX + (int)csv.X + 16;
        int lbY = fy + (FootH - lbH) / 2;
        _logBtnRect = new Rectangle(lbX, lbY, lbW, lbH);
        Color logBg  = _showLog ? CInk * 0.75f : CDiv * 0.28f;
        Color logBrd = _showLog ? CInk          : CDiv * 0.50f;
        Color logTc  = _showLog ? Color.White   : CSub;
        b.Draw(Game1.staminaRect, _logBtnRect, logBg);
        DrawRect(b, lbX, lbY, lbW, lbH, logBrd);
        b.DrawString(Game1.dialogueFont, logLbl,
            new Vector2(lbX + 6, lbY + (lbH - llv.Y) / 2f),
            logTc, 0f, Vector2.Zero, FSmall, SpriteEffects.None, 0f);

        string hint = I18n.PanelFooterHint();
        Vector2 hsv = Game1.dialogueFont.MeasureString(hint) * FSmall;
        b.DrawString(Game1.dialogueFont, hint,
            new Vector2(_px + PW - hsv.X - Pad, fy + (FootH - hsv.Y) / 2f),
            CSub, 0f, Vector2.Zero, FSmall, SpriteEffects.None, 0f);
    }

    private void DrawLogOverlay(SpriteBatch b)
    {
        var log = ModEntry.GetNotificationLog();

        int ow = Math.Min(PW - Pad * 2, 600);
        int oh = Math.Min(PH - HeadH - FootH - Pad * 2, 400);
        int ox = _px + (PW - ow) / 2;
        int oy = _py + HeadH + (PH - HeadH - FootH - oh) / 2;

        b.Draw(Game1.staminaRect, new Rectangle(ox + 5, oy + 5, ow, oh), Color.Black * 0.40f);
        drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            ox, oy, ow, oh, Color.White, drawShadow: false);
        b.Draw(Game1.staminaRect, new Rectangle(ox + 6, oy + 6, ow - 12, oh - 12),
            new Color(245, 236, 210) * 0.98f);

        int headerH = (int)(Game1.dialogueFont.MeasureString("A").Y * FName) + 20;
        b.Draw(Game1.staminaRect, new Rectangle(ox + 6, oy + 6, ow - 12, headerH),
            new Color(80, 50, 20) * 0.12f);

        string title = I18n.PanelLogTitle();
        Vector2 tv   = Game1.dialogueFont.MeasureString(title) * FName;
        b.DrawString(Game1.dialogueFont, title,
            new Vector2(ox + Pad, oy + (headerH - tv.Y) / 2f),
            CInk, 0f, Vector2.Zero, FName, SpriteEffects.None, 0f);

        string countStr = $"({log.Count})";
        Vector2 cv = Game1.dialogueFont.MeasureString(countStr) * FSmall;
        b.DrawString(Game1.dialogueFont, countStr,
            new Vector2(ox + Pad + tv.X + 8, oy + (headerH - cv.Y) / 2f),
            CSub * 0.70f, 0f, Vector2.Zero, FSmall, SpriteEffects.None, 0f);

        string closeStr = I18n.PanelLogClose();
        Vector2 clv = Game1.dialogueFont.MeasureString(closeStr) * FSmall;
        int clW = (int)clv.X + 16, clH = (int)clv.Y + 8;
        int clX = ox + ow - clW - 10;
        int clY = oy + (headerH - clH) / 2;
        b.Draw(Game1.staminaRect, new Rectangle(clX, clY, clW, clH), CInk * 0.65f);
        DrawRect(b, clX, clY, clW, clH, CInk * 0.90f);
        b.DrawString(Game1.dialogueFont, closeStr,
            new Vector2(clX + 8, clY + (clH - clv.Y) / 2f),
            Color.White, 0f, Vector2.Zero, FSmall, SpriteEffects.None, 0f);

        b.Draw(Game1.staminaRect,
            new Rectangle(ox + 6, oy + 6 + headerH, ow - 12, 1), CDiv * 0.60f);

        int listPad  = 8;
        int listTop  = oy + 6 + headerH + 2;
        int sbW2     = 10;
        int listW    = ow - 12 - listPad * 2 - sbW2 - 4;
        int listH    = oh - headerH - 14;
        int listX    = ox + 6 + listPad;

        int cardPad  = 8;
        int accentW  = 4;
        float fEntry = FSmall;
        float fSub2  = FTiny;
        float entryLineH = Game1.dialogueFont.MeasureString("A").Y * fEntry;
        float subLineH   = Game1.dialogueFont.MeasureString("A").Y * fSub2;
        int   cardH2     = (int)entryLineH + (int)subLineH + cardPad * 2 + 4;

        var wrappedEntries = new List<(string line1, string line2, Color accent, Color textColor)>();
        foreach (var (msg, type) in log)
        {
            Color accent2 = type == HUDMessage.error_type    ? CLogPlant
                          : type == HUDMessage.newQuest_type ? CLogQuest
                          : CLogRain;
            Color textCol = type == HUDMessage.error_type    ? new Color(160, 20, 20)
                          : type == HUDMessage.newQuest_type ? new Color(20, 110, 20)
                          : new Color(20, 80, 180);

            int maxW = listW - accentW - cardPad * 2 - 4;
            var (l1, l2) = WrapLogMessage(msg, maxW, fEntry, fSub2);
            wrappedEntries.Add((l1, l2, accent2, textCol));
        }

        int totalCards = wrappedEntries.Count;
        int visCards   = Math.Max(1, listH / cardH2);
        int maxLogScr  = Math.Max(0, totalCards - visCards);
        _logScroll = Math.Clamp(_logScroll, 0, maxLogScr);

        int sbX2 = ox + ow - 6 - sbW2;
        b.Draw(Game1.staminaRect, new Rectangle(sbX2, listTop + 2, sbW2, listH - 4), CBarBg * 0.20f);
        DrawRect(b, sbX2, listTop + 2, sbW2, listH - 4, CDiv * 0.25f);
        if (maxLogScr > 0)
        {
            int th2 = Math.Max(20, (listH - 4) * visCards / Math.Max(1, totalCards));
            int ty2 = listTop + 2 + (int)((float)_logScroll / maxLogScr * (listH - 4 - th2));
            b.Draw(Game1.staminaRect, new Rectangle(sbX2 + 1, ty2 + 1, sbW2 - 2, th2 - 2), CInk * 0.30f);
            DrawRect(b, sbX2, ty2, sbW2, th2, CInk * 0.50f);
        }

        if (log.Count == 0)
        {
            string empty = I18n.PanelLogEmpty();
            Vector2 ev   = Game1.dialogueFont.MeasureString(empty) * fEntry;
            b.DrawString(Game1.dialogueFont, empty,
                new Vector2(ox + (ow - ev.X) / 2f, listTop + listH / 2f - ev.Y / 2f),
                CSub * 0.55f, 0f, Vector2.Zero, fEntry, SpriteEffects.None, 0f);
            return;
        }

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.PointClamp, null,
            new RasterizerState { ScissorTestEnable = true });
        b.GraphicsDevice.ScissorRectangle = new Rectangle(listX, listTop, listW + accentW + cardPad, listH);

        for (int i = _logScroll; i < wrappedEntries.Count && i < _logScroll + visCards + 1; i++)
        {
            var (l1, l2, accent2, textCol) = wrappedEntries[i];
            int cy = listTop + (i - _logScroll) * cardH2;

            bool isEven = i % 2 == 0;
            b.Draw(Game1.staminaRect,
                new Rectangle(listX, cy, listW + accentW + 2, cardH2),
                isEven ? Color.White * 0.06f : Color.Black * 0.04f);

            b.Draw(Game1.staminaRect,
                new Rectangle(listX, cy + 2, accentW, cardH2 - 4), accent2 * 0.85f);

            int tx2 = listX + accentW + cardPad;
            b.DrawString(Game1.dialogueFont, l1,
                new Vector2(tx2, cy + cardPad),
                textCol, 0f, Vector2.Zero, fEntry, SpriteEffects.None, 0f);

            if (!string.IsNullOrEmpty(l2))
                b.DrawString(Game1.dialogueFont, l2,
                    new Vector2(tx2 + 8, cy + cardPad + (int)entryLineH + 2),
                    textCol * 0.65f, 0f, Vector2.Zero, fSub2, SpriteEffects.None, 0f);

            if (i < wrappedEntries.Count - 1)
                b.Draw(Game1.staminaRect,
                    new Rectangle(listX + accentW + 2, cy + cardH2 - 1, listW - 2, 1),
                    CDiv * 0.20f);
        }

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
    }

    private (string line1, string line2) WrapLogMessage(string msg, int maxPixelW, float scale1, float scale2)
    {
        float w1 = Game1.dialogueFont.MeasureString(msg).X * scale1;
        if (w1 <= maxPixelW)
            return (msg, string.Empty);

        int splitAt = msg.Length;
        while (splitAt > 1)
        {
            splitAt--;
            if (Game1.dialogueFont.MeasureString(msg[..splitAt]).X * scale1 <= maxPixelW)
            {
                int space = msg.LastIndexOf(' ', splitAt);
                if (space > msg.Length / 3)
                    splitAt = space;
                break;
            }
        }

        string part1 = msg[..splitAt].TrimEnd();
        string part2 = msg[splitAt..].TrimStart();

        float w2 = Game1.dialogueFont.MeasureString(part2).X * scale2;
        if (w2 > maxPixelW)
        {
            int cut = part2.Length;
            while (cut > 1 && Game1.dialogueFont.MeasureString(part2[..cut] + "...").X * scale2 > maxPixelW)
                cut--;
            part2 = part2[..cut] + "...";
        }

        return (part1, part2);
    }

    private void DrawHoverTooltip(SpriteBatch b)
    {
        if (_hoverCalDay >= 1 && _hoverCalDay <= 28)
        {
            DrawCalDayTooltip(b, _hoverCalDay);
            return;
        }

        if (_hoverCard < 0 || _hoverCard >= _vis.Count) return;
        var item = _vis[_hoverCard];

        if (_tab == MuseumTab)
        {
            var mlines = new List<(string text, Color col)>
            {
                (item.ItemName, CInk),
                (item.BundleName, CSub),
                (item.IsMuseumDonated ? I18n.MuseumDonated().Trim() : I18n.MuseumNeeded().Trim(),
                 item.IsMuseumDonated ? CGreen : CUrgent),
            };
            if (_plannedMuseum.Contains(item.QualifiedItemId))
                mlines.Add((I18n.InfoPlanned(), CPlanned));

            const float S2 = 0.50f;
            float maxW2  = mlines.Max(l => Game1.dialogueFont.MeasureString(l.text).X * S2);
            float lineH2 = Game1.dialogueFont.MeasureString("A").Y * S2 + 4f;
            int tw2 = (int)maxW2 + 24, th2 = (int)(mlines.Count * lineH2) + 20;
            int mx2 = Game1.getMouseX(), my2 = Game1.getMouseY();
            int tx2 = mx2 - tw2 - 16; if (tx2 < 4) tx2 = mx2 + 20;
            int ty2 = Math.Clamp(my2 - th2/2, 4, Game1.uiViewport.Height - th2 - 4);
            b.Draw(Game1.staminaRect, new Rectangle(tx2+3, ty2+3, tw2, th2), Color.Black*0.26f);
            b.Draw(Game1.staminaRect, new Rectangle(tx2, ty2, tw2, th2), new Color(252, 244, 224));
            DrawRect(b, tx2, ty2, tw2, th2, CDiv);
            for (int i = 0; i < mlines.Count; i++)
                b.DrawString(Game1.dialogueFont, mlines[i].text,
                    new Vector2(tx2+12, ty2+10+i*lineH2),
                    mlines[i].col, 0f, Vector2.Zero, S2, SpriteEffects.None, 0f);
            return;
        }

        var lines = new List<(string text, Color col)>
        {
            (item.ItemName + (item.Quantity > 1 ? $" x{item.Quantity}" : ""), CInk),
            ($"{I18n.InfoBundle()}: {item.BundleName}", CSub),
            ($"{I18n.InfoCategory()}: {I18n.CategoryLabel(item.Category)}", CSub),
        };
        if (item.Quality > 0)
            lines.Add(($"{I18n.InfoQuality()}: {I18n.QualityLabel(item.Quality)}", CSub));
        if (item.Season != null)
            lines.Add(($"{I18n.InfoSeason()}: {I18n.SeasonLabel(item.Season)}", CSub));
        else if (item.IsGreenhouse && item.GrowDays > 0)
            lines.Add(($"{I18n.InfoSeason()}: {I18n.SeedTooltipGreenhouseAvailable().Trim()}", new Color(34, 139, 34)));
        if (item.GrowDays > 0)
        {
            int ld = 28 - item.GrowDays, dl = ld - Game1.dayOfMonth;
            string cur = Game1.currentSeason?.ToLower() ?? "";
            lines.Add(($"{I18n.InfoGrow()}: {item.GrowDays}g  |  {I18n.InfoLastPlant()}: {ld}", CSub));
            if (item.Season == cur)
            {
                Color dlc = dl <= 0 ? CUrgent : dl <= 3 ? CUrgent : dl <= 7 ? CWarn : CGreen;
                lines.Add((dl <= 0 ? I18n.InfoLastDay() : $"{I18n.InfoDaysLeft()}: {dl}", dlc));
            }
        }
        if (item.RequiresRain)       lines.Add((I18n.InfoRain(), CBlue));
        if (item.ShopSource != null) lines.Add(($"{I18n.InfoShop()}: {item.ShopSource}", CSub));
        if (_planned.Contains(PlanKey(item))) lines.Add((I18n.InfoPlanned(), CPlanned));

        if (!string.IsNullOrWhiteSpace(item.QualifiedItemId))
        {
            string rawId = item.QualifiedItemId.StartsWith("(", StringComparison.Ordinal)
                ? item.QualifiedItemId[(item.QualifiedItemId.IndexOf(')') + 1)..]
                : item.QualifiedItemId;
            if (!int.TryParse(rawId, out _))
            {
                int dotIdx = rawId.IndexOf('.');
                string modPrefix = dotIdx > 0 ? rawId[..dotIdx] : rawId;
                lines.Add(($"Mod: {modPrefix}", new Color(120, 80, 160)));
            }
        }

        const float S = 0.50f;
        float maxW  = lines.Max(l => Game1.dialogueFont.MeasureString(l.text).X * S);
        float lineH = Game1.dialogueFont.MeasureString("A").Y * S + 4f;
        int tw = (int)maxW + 24, th = (int)(lines.Count * lineH) + 20;

        int mx = Game1.getMouseX(), my = Game1.getMouseY();
        int tx = mx - tw - 16; if (tx < 4) tx = mx + 20;
        int ty = Math.Clamp(my - th/2, 4, Game1.uiViewport.Height - th - 4);

        b.Draw(Game1.staminaRect, new Rectangle(tx+3, ty+3, tw, th), Color.Black*0.26f);
        b.Draw(Game1.staminaRect, new Rectangle(tx, ty, tw, th), new Color(252, 244, 224));
        DrawRect(b, tx, ty, tw, th, CDiv);
        for (int i = 0; i < lines.Count; i++)
            b.DrawString(Game1.dialogueFont, lines[i].text,
                new Vector2(tx+12, ty+10+i*lineH),
                lines[i].col, 0f, Vector2.Zero, S, SpriteEffects.None, 0f);
    }

    private void DrawCalDayTooltip(SpriteBatch b, int day)
    {
        string season = Game1.currentSeason?.ToLower() ?? "";
        int today     = Game1.dayOfMonth;
        int dl        = day - today;

        var items = _all.Where(i => i.GrowDays > 0
            && (i.Season == season || i.IsGreenhouse)
            && (28 - i.GrowDays) == day).ToList();

        if (items.Count == 0 && day != today) return;

        var lines = new List<(string text, Color col)>();

        string dayLabel = day == today ? $"{I18n.CalDay(day)}  < {I18n.CalToday()}" : I18n.CalDay(day);
        lines.Add((dayLabel, day == today ? CGold : CInk));

        if (dl < 0)
            lines.Add(($"({-dl} {I18n.CalDaysAgo()})", CSub * 0.60f));
        else if (dl == 0)
            lines.Add((I18n.CalToday(), CGold));
        else
            lines.Add(($"{dl} {I18n.CalDaysLater()}", CSub));

        if (items.Count > 0)
        {
            lines.Add(("- - - - - - - - -", CDiv));
            lines.Add((I18n.CalLastPlantDay(), CSub));
            foreach (var item in items.Take(8))
            {
                int ddl = day - today;
                Color ic = ddl <= 0 ? CUrgent : ddl <= 3 ? CUrgent : ddl <= 7 ? CWarn : CGold;
                lines.Add(($"  - {item.ItemName}  ({item.BundleName})", ic));
            }
            if (items.Count > 8)
                lines.Add(($"  + {items.Count - 8} daha...", CSub * 0.70f));
        }

        const float S = 0.50f;
        float maxW  = lines.Max(l => Game1.dialogueFont.MeasureString(l.text).X * S);
        float lineH = Game1.dialogueFont.MeasureString("A").Y * S + 4f;
        int tw = (int)maxW + 24, th = (int)(lines.Count * lineH) + 20;

        int mx = Game1.getMouseX(), my = Game1.getMouseY();
        int tx = mx - tw - 16; if (tx < 4) tx = mx + 20;
        int ty = Math.Clamp(my - th / 2, 4, Game1.uiViewport.Height - th - 4);

        b.Draw(Game1.staminaRect, new Rectangle(tx+3, ty+3, tw, th), Color.Black*0.26f);
        b.Draw(Game1.staminaRect, new Rectangle(tx, ty, tw, th), new Color(252, 244, 224));
        DrawRect(b, tx, ty, tw, th, CDiv);
        for (int i = 0; i < lines.Count; i++)
            b.DrawString(Game1.dialogueFont, lines[i].text,
                new Vector2(tx+12, ty+10+i*lineH),
                lines[i].col, 0f, Vector2.Zero, S, SpriteEffects.None, 0f);
    }



    public override void receiveScrollWheelAction(int direction)
    {
        if (_showLog)
        {
            var log = ModEntry.GetNotificationLog();
            int oh = Math.Min(PH - HeadH - FootH - Pad * 2, 400);
            int headerH = (int)(Game1.dialogueFont.MeasureString("A").Y * FName) + 20;
            int listH   = oh - headerH - 14;
            float entryLineH = Game1.dialogueFont.MeasureString("A").Y * FSmall;
            float subLineH   = Game1.dialogueFont.MeasureString("A").Y * FTiny;
            int cardH2  = (int)entryLineH + (int)subLineH + 8 * 2 + 4;
            int visCards = Math.Max(1, listH / cardH2);
            _logScroll = Math.Clamp(_logScroll - Math.Sign(direction), 0, Math.Max(0, log.Count - visCards));
            return;
        }
        _scroll = Math.Clamp(_scroll - Math.Sign(direction), 0, MaxScr);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (upperRightCloseButton is not null && upperRightCloseButton.containsPoint(x, y))
        {
            exitThisMenu();
            return;
        }

        if (_showLog)
        {
            var log = ModEntry.GetNotificationLog();
            int ow = Math.Min(PW - Pad * 2, 600);
            int oh = Math.Min(PH - HeadH - FootH - Pad * 2, 400);
            int ox = _px + (PW - ow) / 2;
            int oy = _py + HeadH + (PH - HeadH - FootH - oh) / 2;

            int headerH = (int)(Game1.dialogueFont.MeasureString("A").Y * FName) + 20;
            int clW = (int)(Game1.dialogueFont.MeasureString(I18n.PanelLogClose()).X * FSmall) + 16;
            int clH = (int)(Game1.dialogueFont.MeasureString("A").Y * FSmall) + 8;
            int clX = ox + ow - clW - 10;
            int clY = oy + (headerH - clH) / 2;

            if (x >= clX && x <= clX + clW && y >= clY && y <= clY + clH)
            {
                _showLog = false;
                Game1.playSound("smallSelect");
                return;
            }

            if (x >= ox && x <= ox + ow && y >= oy && y <= oy + oh)
                return;

            _showLog = false;
            return;
        }

        if (_logBtnRect.Contains(x, y))
        {
            _showLog = !_showLog;
            _logScroll = 0;
            Game1.playSound("smallSelect");
            return;
        }

        if (_searchRect.Contains(x, y))
        {
            _searchFocused = true;
            Game1.playSound("smallSelect");
            return;
        }
        else
        {
            _searchFocused = false;
        }

        ChipFilter[] filters = { ChipFilter.None, ChipFilter.Urgent, ChipFilter.Seasonal, ChipFilter.Planned };
        for (int i = 0; i < _chipRects.Length; i++)
        {
            if (_chipRects[i].Contains(x, y))
            {
                _chipFilter = _chipFilter == filters[i] ? ChipFilter.None : filters[i];
                Refresh(); Game1.playSound("smallSelect"); return;
            }
        }

        for (int i = 0; i < 3; i++)
        {
            int bx = _sortStartX + i * (SortBtnW + 10);
            if (x >= bx && x <= bx+SortBtnW && y >= _sortY && y <= _sortY+SortBtnH)
            { _sort=(SortMode)i; Refresh(); Game1.playSound("smallSelect"); return; }
        }
        for (int i = 0; i < TabCats.Length; i++)
        {
            int ty = _tcY + i * TabH;
            if (x >= _tcX && x <= _tcX+_tcW && y >= ty && y <= ty+TabH)
            { if (i!=_tab){_tab=i; Refresh(); Game1.playSound("smallSelect");} return; }
        }
        if (x >= _lX && x <= _lX+_lW && y >= _lY && y <= _lY+_lH)
        {
            int idx = (y - _lY) / CardH + _scroll;
            if (idx >= 0 && idx < _vis.Count)
            {
                var item  = _vis[idx];
                int cardY = _lY + (idx - _scroll) * CardH;
                string pblClick = _planned.Contains(PlanKey(_vis[idx])) ? I18n.PlannedBtn() : I18n.PlanBtn();
                Vector2 pbszClick = Game1.dialogueFont.MeasureString(pblClick) * FSmall;
                int PBW = (int)pbszClick.X + 10, PBH = (int)pbszClick.Y + 6;
                int pbX = _lX+_lW-PBW-8, pbY = cardY+CardH-PBH-6;
                if (x>=pbX && x<=pbX+PBW && y>=pbY && y<=pbY+PBH)
                {
                    if (_tab == MuseumTab)
                    {
                        string museumKey = item.QualifiedItemId;
                        if (_plannedMuseum.Contains(museumKey)) _plannedMuseum.Remove(museumKey);
                        else _plannedMuseum.Add(museumKey);
                    }
                    else
                    {
                        string key = PlanKey(item);
                        if (_planned.Contains(key)) _planned.Remove(key); else _planned.Add(key);
                    }
                    Refresh(); Game1.playSound("coin"); return;
                }
            }
        }
        if (MaxScr > 0 && x >= _sbX && x <= _sbX+SbW && y >= _lY && y <= _lY+_lH)
        { _drag=true; _dragY0=y; _dragScr0=_scroll; return; }

        bool inHeader = x >= _px && x <= _px+PW && y >= _py && y <= _py+HeadH;
        if (inHeader)
        {
            _dragPanel  = true;
            _dragPanelX0 = _px; _dragPanelY0 = _py;
            _dragMouseX0 = x;   _dragMouseY0  = y;
            return;
        }

        base.receiveLeftClick(x, y, playSound);
    }

    public override void leftClickHeld(int x, int y)
    {
        if (_drag)
        {
            int th    = Math.Max(28, _lH * VisRows / Math.Max(1, _vis.Count));
            int range = _lH - th;
            if (range > 0)
                _scroll = Math.Clamp(_dragScr0 + (int)((float)(y-_dragY0)/range*MaxScr), 0, MaxScr);
            return;
        }

        if (_dragPanel)
        {
            int sw = Game1.uiViewport.Width, sh = Game1.uiViewport.Height;
            int nx = Math.Clamp(_dragPanelX0 + (x - _dragMouseX0), 0, sw - PW);
            int ny = Math.Clamp(_dragPanelY0 + (y - _dragMouseY0), 0, sh - PH);
            MovePanel(nx, ny);
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        _drag = false;
        if (_dragPanel)
        {
            _dragPanel = false;

            if (_config is { RememberPanelPosition: true })
            {
                _config.PanelX      = _px;
                _config.PanelY      = _py;
                _config.PanelAnchor = PanelAnchor.Custom;
            }
        }
    }

    public override void performHoverAction(int x, int y)
    {
        _hoverTab=-1; _hoverCard=-1; _hoverSort=-1; _hoverChip=-1; _hoverCalDay=-1;
        for (int i = 0; i < _chipRects.Length; i++)
            if (_chipRects[i].Contains(x, y)) { _hoverChip = i; break; }
        for (int i = 0; i < 3; i++)
        {
            int bx = _sortStartX + i*(SortBtnW+10);
            if (x>=bx && x<=bx+SortBtnW && y>=_sortY && y<=_sortY+SortBtnH) { _hoverSort=i; break; }
        }
        for (int i = 0; i < TabCats.Length; i++)
        {
            int ty = _tcY + i*TabH;
            if (x>=_tcX && x<=_tcX+_tcW && y>=ty && y<=ty+TabH) { _hoverTab=i; break; }
        }
        if (x>=_lX && x<=_lX+_lW && y>=_lY && y<=_lY+_lH)
        {
            int idx = (y-_lY)/CardH + _scroll;
            if (idx>=0 && idx<_vis.Count) _hoverCard=idx;
        }
        if (CalColW > 0)
        {
            for (int day = 1; day <= 28; day++)
                if (_calCellRects[day].Contains(x, y)) { _hoverCalDay = day; break; }
        }

        bool inHeader = x >= _px && x <= _px+PW && y >= _py && y <= _py+HeadH;
        if (inHeader || _dragPanel)
            Game1.mouseCursor = Game1.cursor_grab;
        base.performHoverAction(x, y);
    }

    public override void emergencyShutDown() { SavePositionPublic(); base.emergencyShutDown(); }

    public void SavePositionPublic()
    {
        if (_config is null) return;
        if (_config.RememberPanelPosition)
        {
            int sw = Game1.uiViewport.Width, sh = Game1.uiViewport.Height;
            if (_px >= 0 && _py >= 0 && _px + PW <= sw && _py + PH <= sh)
            {
                _config.PanelX      = _px;
                _config.PanelY      = _py;
                _config.PanelAnchor = PanelAnchor.Custom;
            }
        }
        _config.PlannedItems.Clear();
        _config.PlannedItems.AddRange(_planned);
        _config.PlannedMuseumItems.Clear();
        _config.PlannedMuseumItems.AddRange(_plannedMuseum);
    }

    public override void receiveRightClick(int x, int y, bool playSound = true) => exitThisMenu();
    public override void receiveKeyPress(Keys key)
    {
        if (_searchFocused)
        {
            if (key == Keys.Escape)
            {
                if (!string.IsNullOrEmpty(_searchText))
                {
                    _searchText = string.Empty;
                    Refresh();
                }
                else
                {
                    _searchFocused = false;
                }
                return;
            }
            if (key == Keys.Back)
            {
                if (_searchText.Length > 0)
                {
                    _searchText = _searchText[..^1];
                    Refresh();
                }
                return;
            }

            char? c = KeyToChar(key);
            if (c.HasValue && _searchText.Length < 30)
            {
                _searchText += c.Value;
                Refresh();
            }
            return;
        }

        if (key == Keys.Escape) exitThisMenu();
        base.receiveKeyPress(key);
    }

    private static char? KeyToChar(Keys key)
    {
        bool shift = Keyboard.GetState().IsKeyDown(Keys.LeftShift)
                  || Keyboard.GetState().IsKeyDown(Keys.RightShift);

        if (key >= Keys.A && key <= Keys.Z)
            return shift ? (char)('A' + (key - Keys.A)) : (char)('a' + (key - Keys.A));
        if (key >= Keys.D0 && key <= Keys.D9)
            return (char)('0' + (key - Keys.D0));
        if (key == Keys.Space) return ' ';
        return null;
    }



    private void MovePanel(int nx, int ny)
    {
        _px = nx; _py = ny;

        int S(int v) => Math.Max(1, (int)(v * _scale));

        _barY  = _py + S(38);
        _chipY = _py + S(58);
        _sortY = _py + S(88);

        _tcX = _px + Pad;
        _tcY = _py + HeadH;

        _caX = _px + PW - Pad - _caW;
        _caY = _py + HeadH + Pad;

        int cardLeft  = _px + Pad + TabColW + 4;
        int cardRight = CalColW > 0 ? _caX - SbW - 4 : _px + PW - SbW - 4;
        _lX  = cardLeft;
        _lY  = _py + HeadH + 4;
        _lW  = cardRight - cardLeft;
        _sbX = cardRight;

        int totalSortW = 3 * SortBtnW + 2 * 10;
        _sortStartX = _lX + _lW - totalSortW;

        initialize(_px, _py, PW, PH);
        if (upperRightCloseButton != null)
            upperRightCloseButton.bounds = new Rectangle(_px + PW - 36, _py - 8, 48, 48);
    }

    private static void DrawRect(SpriteBatch b, int x, int y, int w, int h, Color c)
    {
        b.Draw(Game1.staminaRect, new Rectangle(x,     y,     w, 1), c);
        b.Draw(Game1.staminaRect, new Rectangle(x,     y+h-1, w, 1), c);
        b.Draw(Game1.staminaRect, new Rectangle(x,     y,     1, h), c);
        b.Draw(Game1.staminaRect, new Rectangle(x+w-1, y,     1, h), c);
    }

    private static string CapFirst(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
}

