using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace SeasonPlanner;

// Panel: 900×640 sabit. Font scale'leri okunabilir boyutta.
// dialogueFont ölçülü render: scale 0.55 → ~20px, 0.48 → ~17px, 0.42 → ~15px
public sealed class BundlePanelMenu : IClickableMenu
{
    // ── Panel ────────────────────────────────────────────────────────────
    private const int PW    = 900;
    private const int PH    = 640;
    private const int HeadH = 160;
    private const int FootH = 36;

    // ── Columns ──────────────────────────────────────────────────────────
    private const int TabColW = 130;
    private const int CalColW = 160;
    private const int SbW     = 12;
    private const int Pad     = 14;

    // ── Element sizes ────────────────────────────────────────────────────
    private const int TabH     = 52;
    private const int CardH    = 80;
    private const int IconSz   = 52;
    private const int SortBtnW = 80;
    private const int SortBtnH = 28;
    private const int ChipH    = 26;

    // ── Font scales ──────────────────────────────────────────────────────
    private const float FTitle  = 0.75f;   // panel başlığı
    private const float FName   = 0.60f;   // kart: item adı
    private const float FMid    = 0.50f;   // kart: bundle adı, tab, chip, sort
    private const float FSmall  = 0.44f;   // kart: tags, badge, footer
    private const float FTiny   = 0.40f;   // takvim sayıları, % label

    // ── Colours ──────────────────────────────────────────────────────────
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

    // ── Tabs ─────────────────────────────────────────────────────────────
    private static readonly BundleCategory?[] TabCats =
    {
        null, BundleCategory.Crop, BundleCategory.Fish,
        BundleCategory.Artisan, BundleCategory.Construction, BundleCategory.Other,
    };
    private static string TabLabel(int i) => i switch
    {
        0 => I18n.TabAll(), 1 => I18n.TabCrop(), 2 => I18n.TabFish(),
        3 => I18n.TabArtisan(), 4 => I18n.TabConstruction(), _ => I18n.TabOther(),
    };
    private static Color CatColor(BundleCategory? c) => c switch
    {
        BundleCategory.Crop => CGreen, BundleCategory.Fish => CBlue,
        BundleCategory.Artisan => CPurple, BundleCategory.Construction => CBrown,
        _ => COrange,
    };

    // ── Sort ─────────────────────────────────────────────────────────────
    private enum SortMode { Urgency, Name, Bundle }
    private string[] SortLabels => new[] { I18n.SortUrgency(), I18n.SortName(), I18n.SortBundle() };

    // ── State ─────────────────────────────────────────────────────────────
    private int      _tab, _scroll;
    private int      _hoverTab = -1, _hoverCard = -1, _hoverSort = -1;
    private bool     _drag;
    private int      _dragY0, _dragScr0;
    private SortMode _sort = SortMode.Urgency;

    private readonly IReadOnlyList<BundleItem> _all;
    private readonly ModConfig?                _config;
    private readonly HashSet<string>           _planned = new();
    private List<BundleItem>                   _vis     = new();

    // ── Computed layout ───────────────────────────────────────────────────
    private int _px, _py;
    private int _barY, _chipY, _sortY;
    private int _tcX, _tcY, _tcW, _tcH;
    private int _lX,  _lY,  _lW,  _lH;
    private int _sbX;
    private int _caX, _caY, _caW, _caH;
    private int _sortStartX;
    private int _urgentCount, _seasonCount;

    private int VisRows => _lH / CardH;
    private int MaxScr  => Math.Max(0, _vis.Count - VisRows);
    private static string PlanKey(BundleItem i) => $"{i.ItemId}:{i.BundleName}:{i.Quality}:{i.Quantity}";

    // ─────────────────────────────────────────────────────────────────────
    public BundlePanelMenu(IReadOnlyList<BundleItem> items, ModConfig? config = null)
        : base((Game1.uiViewport.Width - PW) / 2, (Game1.uiViewport.Height - PH) / 2, PW, PH)
    {
        _all    = items;
        _config = config;
        _px     = xPositionOnScreen;
        _py     = yPositionOnScreen;

        // Header rows
        _barY  = _py + 46;
        _chipY = _py + 72;
        _sortY = _py + 108;

        // Tab column
        _tcX = _px + Pad;
        _tcY = _py + HeadH;
        _tcW = TabColW - 4;
        _tcH = PH - HeadH - FootH;

        // Calendar
        _caW = CalColW - Pad;
        _caH = PH - HeadH - FootH - Pad * 2;
        _caX = _px + PW - Pad - _caW;
        _caY = _py + HeadH + Pad;

        // Card list
        int cardLeft  = _px + Pad + TabColW + 4;
        int cardRight = _caX - SbW - 4;
        _lX  = cardLeft;
        _lY  = _py + HeadH + 4;
        _lW  = cardRight - cardLeft;
        _lH  = PH - HeadH - FootH - 8;
        _sbX = cardRight;

        int totalSortW = 3 * SortBtnW + 2 * 10;
        _sortStartX = _lX + _lW - totalSortW;

        initialize(_px, _py, PW, PH);
        Refresh();
    }

    private void Refresh()
    {
        _scroll = 0;
        var cat = TabCats[_tab];
        var src = cat is null ? _all : _all.Where(i => i.Category == cat);
        _vis = _sort switch
        {
            SortMode.Name   => src.OrderBy(i => i.ItemName).ToList(),
            SortMode.Bundle => src.OrderBy(i => i.BundleName).ThenBy(i => i.ItemName).ToList(),
            _               => src.OrderBy(UrgencyScore).ToList(),
        };
        string season = Game1.currentSeason?.ToLower() ?? "";
        int today     = Game1.dayOfMonth;
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

    // ═════════════════════════════════════════════════════════════════════
    //  DRAW
    // ═════════════════════════════════════════════════════════════════════
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
        DrawCalendar(b);
        DrawCards(b);
        DrawScrollBar(b);
        DrawFooter(b);
        DrawHoverTooltip(b);

        upperRightCloseButton?.draw(b);
        drawMouse(b);
    }

    // ── Header ────────────────────────────────────────────────────────────
    private void DrawHeader(SpriteBatch b)
    {
        // Title
        string title = I18n.PanelTitle();
        Vector2 ts   = Game1.dialogueFont.MeasureString(title) * FTitle;
        b.DrawString(Game1.dialogueFont, title,
            new Vector2(_px + (PW - ts.X) / 2f, _py + 8),
            CInk, 0f, Vector2.Zero, FTitle, SpriteEffects.None, 0f);

        // Progress bar
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

        // Chip row
        int cx = barX;
        cx = DrawChip(b, cx, _chipY, $"{_all.Count} {I18n.ChipMissing()}",    CInk)     + 8;
        cx = DrawChip(b, cx, _chipY, $"{_urgentCount} {I18n.ChipUrgent()}",   _urgentCount > 0 ? CUrgent : CSub) + 8;
        cx = DrawChip(b, cx, _chipY, $"{_seasonCount} {I18n.ChipSeasonal()}", CGold)    + 8;
             DrawChip(b, cx, _chipY, $"{_planned.Count} {I18n.ChipPlanned()}", CPlanned);

        // Divider 1
        b.Draw(Game1.staminaRect,
            new Rectangle(_px + Pad, _py + 100, PW - Pad * 2, 1), CDiv * 0.40f);

        // Sort row
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

        // Divider 2
        b.Draw(Game1.staminaRect,
            new Rectangle(_px + Pad, _py + HeadH - 2, PW - Pad * 2, 2), CDiv * 0.50f);
    }

    private int DrawChip(SpriteBatch b, int x, int y, string text, Color col)
    {
        Vector2 sz = Game1.dialogueFont.MeasureString(text) * FMid;
        int w = (int)sz.X + 14, h = ChipH;
        b.Draw(Game1.staminaRect, new Rectangle(x, y, w, h), col * 0.10f);
        DrawRect(b, x, y, w, h, col * 0.30f);
        b.DrawString(Game1.dialogueFont, text,
            new Vector2(x + 7, y + (h - sz.Y) / 2f),
            col, 0f, Vector2.Zero, FMid, SpriteEffects.None, 0f);
        return x + w;
    }

    // ── Tab column ────────────────────────────────────────────────────────
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

            int cnt = TabCats[i] is null ? _all.Count : _all.Count(x => x.Category == TabCats[i]);
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

    // ── Calendar ──────────────────────────────────────────────────────────
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
            new Vector2(_caX + (_caW - ct.X) / 2f, _caY + 5),
            CInk, 0f, Vector2.Zero, FSmall, SpriteEffects.None, 0f);

        var critDays = new Dictionary<int, Color>();
        foreach (var item in _all.Where(i => i.Season == season && i.GrowDays > 0))
        {
            int ld = 28 - item.GrowDays; if (ld < 1) continue;
            int dl = ld - today;
            Color c = dl <= 3 ? CUrgent : dl <= 7 ? CWarn : CGold;
            if (!critDays.ContainsKey(ld) || c == CUrgent) critDays[ld] = c;
        }

        int legendH = 52;
        int gridTop = _caY + (int)ct.Y + 10;
        int gridH   = _caH - (int)ct.Y - 10 - legendH;
        int cellW   = _caW / 4;
        int cellH   = Math.Max(1, gridH / 7);

        for (int day = 1; day <= 28; day++)
        {
            int col = (day - 1) % 4, row = (day - 1) / 4;
            int dx = _caX + col * cellW, dy = gridTop + row * cellH;
            bool isToday = day == today, isCrit = critDays.ContainsKey(day), isPast = day < today;

            if (isToday)
                b.Draw(Game1.staminaRect, new Rectangle(dx+1,dy+1,cellW-2,cellH-2), CGold*0.28f);
            else if (isCrit)
                b.Draw(Game1.staminaRect, new Rectangle(dx+1,dy+1,cellW-2,cellH-2), critDays[day]*0.14f);

            string ds = day.ToString();
            Vector2 dv = Game1.dialogueFont.MeasureString(ds) * FTiny;
            Color dc = isToday ? CGold : isCrit ? critDays[day] : isPast ? CSub*0.28f : CSub;
            b.DrawString(Game1.dialogueFont, ds,
                new Vector2(dx + (cellW - dv.X)/2f, dy + (cellH - dv.Y)/2f),
                dc, 0f, Vector2.Zero, FTiny, SpriteEffects.None, 0f);

            if (isCrit && !isToday)
                b.Draw(Game1.staminaRect, new Rectangle(dx+cellW/2-2, dy+cellH-3, 4, 3), critDays[day]);
            if (isToday)
                DrawRect(b, dx+1, dy+1, cellW-2, cellH-2, CGold*0.82f);
            b.Draw(Game1.staminaRect, new Rectangle(dx, dy+cellH-1, cellW, 1), CDiv*0.08f);
        }

        int ly = _caY + _caH - legendH + 4;
        DrawLegendDot(b, _caX+6, ly,    CUrgent, "urgent");
        DrawLegendDot(b, _caX+6, ly+17, CWarn,   "soon");
        DrawLegendDot(b, _caX+6, ly+34, CGold,   "later");
    }

    private void DrawLegendDot(SpriteBatch b, int x, int y, Color col, string label)
    {
        b.Draw(Game1.staminaRect, new Rectangle(x, y+3, 8, 8), col*0.70f);
        b.DrawString(Game1.dialogueFont, label,
            new Vector2(x+12, y), CSub, 0f, Vector2.Zero, FTiny, SpriteEffects.None, 0f);
    }

    // ── Card list ─────────────────────────────────────────────────────────
    private void DrawCards(SpriteBatch b)
    {
        _scroll = Math.Clamp(_scroll, 0, MaxScr);

        if (_vis.Count == 0)
        {
            string msg = _tab == 0 ? I18n.PanelAllComplete() : I18n.PanelCategoryEmpty();
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
            bool planned = _planned.Contains(PlanKey(item));

            string badge    = string.Empty;
            Color  badgeCol = Color.Transparent;
            Color  accent   = CatColor(item.Category);
            bool   isUrgent = false, isWarn = false;

            if (item.GrowDays > 0 && item.Season == season)
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

            // Icon (52×52)
            try
            {
                new StardewValley.Object(item.ItemId.ToString(), 1)
                    .drawInMenu(b, new Vector2(_lX+8, cardY+(CardH-IconSz)/2f),
                        IconSz/64f, 1f, 1f, StackDrawType.Hide, Color.White, false);
            }
            catch { }

            // Text block
            int tx = _lX + 8 + IconSz + 10;

            // Name
            b.DrawString(Game1.dialogueFont,
                item.ItemName + (item.Quantity > 1 ? $"  x{item.Quantity}" : ""),
                new Vector2(tx, cardY+6), accent, 0f, Vector2.Zero, FName, SpriteEffects.None, 0f);

            // Bundle name
            b.DrawString(Game1.dialogueFont, item.BundleName,
                new Vector2(tx, cardY+30), CSub, 0f, Vector2.Zero, FMid, SpriteEffects.None, 0f);

            // Tags
            var tags = new List<string>();
            if (item.Quality > 0)        tags.Add(I18n.QualityLabel(item.Quality));
            if (item.Season != null)     tags.Add(I18n.SeasonLabel(item.Season));
            if (item.ShopSource != null) tags.Add($"@ {item.ShopSource}");
            if (item.GrowDays > 0)       tags.Add($"{item.GrowDays}g");
            if (item.RequiresRain)       tags.Add(I18n.BadgeRain());
            if (tags.Count > 0)
                b.DrawString(Game1.dialogueFont, string.Join("  ·  ", tags),
                    new Vector2(tx, cardY+50), CSub*0.70f, 0f, Vector2.Zero, FSmall, SpriteEffects.None, 0f);

            // Badge
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

            // Plan button
            const int PBW = 68, PBH = 26;
            int pbX = _lX + _lW - PBW - 8;
            int pbY = cardY + CardH - PBH - 6;
            b.Draw(Game1.staminaRect, new Rectangle(pbX, pbY, PBW, PBH),
                planned ? CPlanned*0.70f : CDiv*0.28f);
            DrawRect(b, pbX, pbY, PBW, PBH, planned ? CPlanned : CDiv*0.42f);
            string pbl   = planned ? I18n.PlannedBtn() : I18n.PlanBtn();
            Vector2 pbsz = Game1.dialogueFont.MeasureString(pbl) * FMid;
            b.DrawString(Game1.dialogueFont, pbl,
                new Vector2(pbX + (PBW - pbsz.X)/2f, pbY + (PBH - pbsz.Y)/2f),
                planned ? Color.White : CSub, 0f, Vector2.Zero, FMid, SpriteEffects.None, 0f);

            // Divider
            b.Draw(Game1.staminaRect,
                new Rectangle(_lX+4, cardY+CardH-1, _lW-8, 1), CDiv*0.18f);
        }

        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
    }

    // ── Scrollbar ─────────────────────────────────────────────────────────
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

    // ── Footer ────────────────────────────────────────────────────────────
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

        string hint = I18n.PanelFooterHint();
        Vector2 hsv = Game1.dialogueFont.MeasureString(hint) * FSmall;
        b.DrawString(Game1.dialogueFont, hint,
            new Vector2(_px + PW - hsv.X - Pad, fy + (FootH - hsv.Y) / 2f),
            CSub, 0f, Vector2.Zero, FSmall, SpriteEffects.None, 0f);
    }

    // ── Hover tooltip ─────────────────────────────────────────────────────
    private void DrawHoverTooltip(SpriteBatch b)
    {
        if (_hoverCard < 0 || _hoverCard >= _vis.Count) return;
        var item = _vis[_hoverCard];

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

    // ═════════════════════════════════════════════════════════════════════
    //  INPUT
    // ═════════════════════════════════════════════════════════════════════
    public override void receiveScrollWheelAction(int direction)
        => _scroll = Math.Clamp(_scroll - Math.Sign(direction), 0, MaxScr);

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
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
                const int PBW=68, PBH=26;
                int pbX = _lX+_lW-PBW-8, pbY = cardY+CardH-PBH-6;
                if (x>=pbX && x<=pbX+PBW && y>=pbY && y<=pbY+PBH)
                {
                    string key = PlanKey(item);
                    if (_planned.Contains(key)) _planned.Remove(key); else _planned.Add(key);
                    Refresh(); Game1.playSound("coin"); return;
                }
            }
        }
        if (MaxScr > 0 && x >= _sbX && x <= _sbX+SbW && y >= _lY && y <= _lY+_lH)
        { _drag=true; _dragY0=y; _dragScr0=_scroll; return; }

        base.receiveLeftClick(x, y, playSound);
    }

    public override void leftClickHeld(int x, int y)
    {
        if (!_drag) return;
        int th    = Math.Max(28, _lH * VisRows / Math.Max(1, _vis.Count));
        int range = _lH - th;
        if (range > 0)
            _scroll = Math.Clamp(_dragScr0 + (int)((float)(y-_dragY0)/range*MaxScr), 0, MaxScr);
    }

    public override void releaseLeftClick(int x, int y) => _drag = false;

    public override void performHoverAction(int x, int y)
    {
        _hoverTab=-1; _hoverCard=-1; _hoverSort=-1;
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
        base.performHoverAction(x, y);
    }

    public override void emergencyShutDown() { SavePositionPublic(); base.emergencyShutDown(); }

    public void SavePositionPublic()
    {
        if (_config is null || !_config.RememberPanelPosition) return;
        _config.PanelX = _px;
        _config.PanelY = _py;
    }

    public override void receiveRightClick(int x, int y, bool playSound = true) => exitThisMenu();
    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape) exitThisMenu();
        base.receiveKeyPress(key);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═════════════════════════════════════════════════════════════════════
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
