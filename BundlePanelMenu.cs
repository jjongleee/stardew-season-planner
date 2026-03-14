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
    // ── Sabitler ─────────────────────────────────────────────────────────
    private const int TW      = 800;   // toplam genişlik
    private const int TH      = 620;   // toplam yükseklik
    private const int TabW    = 140;   // sol sekme sütunu
    private const int TabH    = 48;    // her sekme yüksekliği
    private const int RowH    = 62;
    private const int Icon    = 44;
    private const int Pad     = 18;
    private const int HeadH   = 56;    // başlık yüksekliği
    private const int FootH   = 40;
    private const int SbW     = 8;     // scroll bar

    // ── Renkler ──────────────────────────────────────────────────────────
    private static readonly Color CTitle   = new(86, 22, 12);
    private static readonly Color CSub     = new(120, 95, 65);
    private static readonly Color CDiv     = new(180, 150, 110);
    private static readonly Color CUrgent  = new(210, 35, 35);
    private static readonly Color CWarn    = new(205, 125, 0);
    private static readonly Color CGreen   = new(30, 125, 30);
    private static readonly Color CBlue    = new(35, 105, 210);
    private static readonly Color CPurple  = new(135, 45, 155);
    private static readonly Color CBrown   = new(125, 75, 25);
    private static readonly Color COrange  = new(165, 95, 0);
    private static readonly Color CTabSel  = new(255, 248, 228);
    private static readonly Color CTabHov  = new(238, 220, 185);
    private static readonly Color CTabNorm = new(215, 190, 150);

    // ── Sekmeler ─────────────────────────────────────────────────────────
    private static readonly (string label, BundleCategory? cat)[] Tabs =
    {
        (null!, null),
        (null!, BundleCategory.Crop),
        (null!, BundleCategory.Fish),
        (null!, BundleCategory.Artisan),
        (null!, BundleCategory.Construction),
        (null!, BundleCategory.Other),
    };

    private static string TabLabel(int i) => i switch
    {
        0 => I18n.TabAll(),
        1 => I18n.TabCrop(),
        2 => I18n.TabFish(),
        3 => I18n.TabArtisan(),
        4 => I18n.TabConstruction(),
        _ => I18n.TabOther(),
    };

    // ── Durum ────────────────────────────────────────────────────────────
    private int  _tab;
    private int  _scroll;
    private int  _hoverTab = -1;
    private bool _drag;
    private int  _dragY0, _dragScr0;
    private readonly IReadOnlyList<BundleItem> _all;
    private List<BundleItem> _vis = new();

    // ── Koordinatlar (constructor'da hesaplanır) ──────────────────────────
    private readonly ModConfig _config;

    private int _px, _py;   // panel sol-üst
    private int _tcx, _tcy; // tab column sol-üst
    private int _lx, _ly;   // liste alanı sol-üst
    private int _lw, _lh;   // liste alanı boyutu
    private int _sbx;       // scroll bar x

    // Dragging
    private bool _dragging;
    private int  _dragOffsetX;
    private int  _dragOffsetY;

    private int VisRows => _lh / RowH;
    private int MaxScr  => Math.Max(0, _vis.Count - VisRows);

    // ─────────────────────────────────────────────────────────────────────
    public BundlePanelMenu(IReadOnlyList<BundleItem> items, ModConfig config)
        : base(
            config.RememberPanelPosition && config.PanelX >= 0
                ? config.PanelX
                : (Game1.uiViewport.Width  - TW) / 2,
            config.RememberPanelPosition && config.PanelY >= 0
                ? config.PanelY
                : (Game1.uiViewport.Height - TH) / 2,
            TW, TH)
    {
        _all = items;
        _config = config;

        // Koordinatları bir kez hesapla
        _px  = xPositionOnScreen;
        _py  = yPositionOnScreen;
        _tcx = _px + Pad;
        _tcy = _py + HeadH;
        _lx  = _px + TabW + Pad + Pad / 2;
        _ly  = _py + HeadH + 4;
        _lw  = TW - TabW - Pad * 2 - Pad / 2 - SbW - 6;
        _lh  = TH - HeadH - FootH - 12;
        _sbx = _lx + _lw + 4;

        initialize(_px, _py, TW, TH);
        Refresh();
    }

    public void SavePosition()
    {
        if (!_config.RememberPanelPosition) return;
        _config.PanelX = xPositionOnScreen;
        _config.PanelY = yPositionOnScreen;
    }

    private void Refresh()
    {
        _scroll = 0;
        var cat = Tabs[_tab].cat;
        _vis = cat is null ? _all.ToList()
                           : _all.Where(i => i.Category == cat).ToList();
    }

    // ── Draw ─────────────────────────────────────────────────────────────
    public override void draw(SpriteBatch b)
    {
        // Karartma
        b.Draw(Game1.fadeToBlackRect,
            new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
            Color.Black * 0.5f);

        // Panel çerçevesi
        drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            _px, _py, TW, TH, Color.White, drawShadow: true);

        DrawHeader(b);
        DrawTabs(b);       // scissor YOK — sekme sütunu her zaman görünür
        DrawList(b);       // scissor VAR
        DrawScrollBar(b);
        DrawFooter(b);

        upperRightCloseButton?.draw(b);
        drawMouse(b);
    }

    // ── Başlık ───────────────────────────────────────────────────────────
    private void DrawHeader(SpriteBatch b)
    {
        string title = I18n.PanelTitle();
        Vector2 ts   = Game1.dialogueFont.MeasureString(title);
        b.DrawString(Game1.dialogueFont, title,
            new Vector2(_px + (TW - ts.X) / 2f, _py + 10), CTitle);

        // Başlık altı çizgi
        b.Draw(Game1.staminaRect,
            new Rectangle(_px + Pad, _py + HeadH - 3, TW - Pad * 2, 2),
            CDiv * 0.55f);
    }

    // ── Sekme Sütunu ─────────────────────────────────────────────────────
    private void DrawTabs(SpriteBatch b)
    {
        int colH = TH - HeadH - FootH;

        // Sütun arka planı
        b.Draw(Game1.staminaRect,
            new Rectangle(_tcx, _tcy, TabW - Pad / 2, colH),
            new Color(200, 175, 135) * 0.3f);

        // Sağ ayırıcı
        b.Draw(Game1.staminaRect,
            new Rectangle(_tcx + TabW - Pad / 2, _tcy, 2, colH),
            CDiv * 0.45f);

        for (int i = 0; i < Tabs.Length; i++)
        {
            int ty   = _tcy + i * TabH;
            bool sel = i == _tab;
            bool hov = i == _hoverTab;

            // Sekme arka planı
            Color bg = sel ? CTabSel : hov ? CTabHov : CTabNorm;
            b.Draw(Game1.staminaRect,
                new Rectangle(_tcx, ty, TabW - Pad / 2, TabH - 1), bg * 0.65f);

            // Aktif sol kenar çizgisi
            if (sel)
                b.Draw(Game1.staminaRect,
                    new Rectangle(_tcx, ty, 5, TabH - 1),
                    CatColor(Tabs[i].cat));

            // Etiket
            string lbl = TabLabel(i);
            Vector2 ls = Game1.smallFont.MeasureString(lbl);
            b.DrawString(Game1.smallFont, lbl,
                new Vector2(_tcx + 12, ty + (TabH - ls.Y) / 2f - 7),
                sel ? CTitle : CSub);

            // Sayı
            int cnt = Tabs[i].cat is null ? _all.Count
                    : _all.Count(x => x.Category == Tabs[i].cat);
            b.DrawString(Game1.tinyFont, cnt.ToString(),
                new Vector2(_tcx + 12, ty + (TabH - ls.Y) / 2f + 9),
                sel ? CatColor(Tabs[i].cat) : CSub * 0.75f);

            // Alt ayırıcı
            if (i < Tabs.Length - 1)
                b.Draw(Game1.staminaRect,
                    new Rectangle(_tcx + 8, ty + TabH - 1, TabW - Pad - 8, 1),
                    CDiv * 0.25f);
        }
    }

    // ── Liste Alanı ──────────────────────────────────────────────────────
    private void DrawList(SpriteBatch b)
    {
        // Liste arka planı
        b.Draw(Game1.staminaRect,
            new Rectangle(_lx, _ly, _lw, _lh),
            new Color(240, 225, 195) * 0.2f);

        if (_vis.Count == 0)
        {
            string msg = _tab == 0 ? I18n.PanelAllComplete() : I18n.PanelCategoryEmpty();
            Vector2 ms = Game1.smallFont.MeasureString(msg);
            b.DrawString(Game1.smallFont, msg,
                new Vector2(_lx + (_lw - ms.X) / 2f, _ly + _lh / 2f - ms.Y / 2f),
                CGreen);
            return;
        }

        string season = Game1.currentSeason.ToLower();
        int today     = Game1.dayOfMonth;

        // Scissor — sadece liste alanını kırp
        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.PointClamp, null,
            new RasterizerState { ScissorTestEnable = true });
        b.GraphicsDevice.ScissorRectangle = new Rectangle(_lx, _ly, _lw, _lh);

        for (int i = _scroll; i < _vis.Count && i < _scroll + VisRows; i++)
        {
            var item = _vis[i];
            int ry   = _ly + (i - _scroll) * RowH;

            // Zebra
            if (i % 2 == 0)
                b.Draw(Game1.staminaRect,
                    new Rectangle(_lx, ry, _lw, RowH), Color.Black * 0.035f);

            // İkon
            try
            {
                new StardewValley.Object(item.ItemId.ToString(), 1)
                    .drawInMenu(b,
                        new Vector2(_lx + 6, ry + (RowH - Icon) / 2f),
                        (float)Icon / 64f, 1f, 1f,
                        StackDrawType.Hide, Color.White, false);
            }
            catch { }

            // Metin başlangıcı
            int tx = _lx + Icon + 14;

            // Aciliyet
            Color nc   = CatColor(item.Category);
            string bdg = string.Empty;
            Color  bbc = Color.Transparent;

            if (item.GrowDays > 0 && item.Season == season)
            {
                int dl = (28 - item.GrowDays) - today;
                if      (dl <= 0) { nc = CUrgent; bdg = I18n.BadgeToday();          bbc = CUrgent; }
                else if (dl <= 3) { nc = CUrgent; bdg = I18n.BadgeDaysLeft(dl);     bbc = CUrgent; }
                else if (dl <= 7) { nc = CWarn;   bdg = I18n.BadgeDaysLeft(dl);     bbc = CWarn;   }
            }
            else if (item.RequiresRain)
            {
                bdg = I18n.BadgeRain(); bbc = CBlue;
            }

            // Eşya adı
            string name = item.ItemName + (item.Quantity > 1 ? $"  x{item.Quantity}" : "");
            b.DrawString(Game1.smallFont, name, new Vector2(tx, ry + 9), nc);

            // Badge — sağa hizalı, scissor içinde
            if (!string.IsNullOrEmpty(bdg))
            {
                Vector2 bsz = Game1.tinyFont.MeasureString(bdg);
                int bx = _lx + _lw - (int)bsz.X - 14;
                int by = ry + 11;
                b.Draw(Game1.staminaRect,
                    new Rectangle(bx - 5, by - 3, (int)bsz.X + 10, (int)bsz.Y + 6),
                    bbc * 0.82f);
                b.DrawString(Game1.tinyFont, bdg, new Vector2(bx, by), Color.White);
            }

            var parts = new List<string> { item.BundleName };
            if (item.Quality > 0)        parts.Add(I18n.QualityLabel(item.Quality));
            if (item.ShopSource != null) parts.Add(item.ShopSource);
            if (item.Season != null)     parts.Add(I18n.SeasonLabel(item.Season));

            b.DrawString(Game1.smallFont,
                string.Join("  ·  ", parts),
                new Vector2(tx, ry + 33), CSub);

            // Satır ayırıcı
            b.Draw(Game1.staminaRect,
                new Rectangle(_lx + 2, ry + RowH - 1, _lw - 4, 1),
                CDiv * 0.3f);
        }

        // Scissor kapat
        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
    }

    // ── Scroll Bar ───────────────────────────────────────────────────────
    private void DrawScrollBar(SpriteBatch b)
    {
        if (MaxScr <= 0) return;

        b.Draw(Game1.staminaRect,
            new Rectangle(_sbx, _ly, SbW, _lh), Color.Black * 0.1f);

        int th = Math.Max(28, _lh * VisRows / Math.Max(1, _vis.Count));
        int ty = _ly + (MaxScr > 0
            ? (int)((float)_scroll / MaxScr * (_lh - th)) : 0);

        b.Draw(Game1.staminaRect,
            new Rectangle(_sbx + 1, ty, SbW - 2, th), CTitle * 0.5f);
    }

    // ── Footer ───────────────────────────────────────────────────────────
    private void DrawFooter(SpriteBatch b)
    {
        // Footer alanı: panelin altından FootH kadar yukarıda
        int fy = _py + TH - FootH;

        // Ayırıcı çizgi
        b.Draw(Game1.staminaRect,
            new Rectangle(_px + Pad, fy + 4, TW - Pad * 2, 2),
            CDiv * 0.5f);

        // Ortalanmış metin satırı
        int textY = fy + 12;

        string left = I18n.PanelItemsCount(_vis.Count);
        b.DrawString(Game1.smallFont, left,
            new Vector2(_lx, textY), CSub);

        string hint = I18n.PanelFooterHint();
        Vector2 hs  = Game1.smallFont.MeasureString(hint);
        b.DrawString(Game1.smallFont, hint,
            new Vector2(_px + TW - hs.X - Pad, textY), CSub);
    }

    // ── Input ─────────────────────────────────────────────────────────────
    public override void receiveScrollWheelAction(int direction)
        => _scroll = Math.Clamp(_scroll - Math.Sign(direction), 0, MaxScr);

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        // Allow dragging by clicking in header area
        if (y >= _py && y <= _py + HeadH && x >= _px && x <= _px + TW)
        {
            _dragging = true;
            _dragOffsetX = x - _px;
            _dragOffsetY = y - _py;
        }

        for (int i = 0; i < Tabs.Length; i++)
        {
            int ty = _tcy + i * TabH;
            if (x >= _tcx && x <= _tcx + TabW - Pad / 2 && y >= ty && y <= ty + TabH)
            {
                if (i != _tab) { _tab = i; Refresh(); Game1.playSound("smallSelect"); }
                return;
            }
        }

        if (MaxScr > 0 && x >= _sbx && x <= _sbx + SbW && y >= _ly && y <= _ly + _lh)
        {
            _drag = true; _dragY0 = y; _dragScr0 = _scroll;
            return;
        }

        base.receiveLeftClick(x, y, playSound);
    }

    public override void leftClickHeld(int x, int y)
    {
        if (_dragging)
        {
            xPositionOnScreen = Math.Clamp(x - _dragOffsetX, 0, Game1.uiViewport.Width - TW);
            yPositionOnScreen = Math.Clamp(y - _dragOffsetY, 0, Game1.uiViewport.Height - TH);

            // Recalculate dependent coordinates
            _px  = xPositionOnScreen;
            _py  = yPositionOnScreen;
            _tcx = _px + Pad;
            _tcy = _py + HeadH;
            _lx  = _px + TabW + Pad + Pad / 2;
            _ly  = _py + HeadH + 4;
            _sbx = _lx + _lw + 4;

            return;
        }

        if (!_drag) return;
        int th    = Math.Max(28, _lh * VisRows / Math.Max(1, _vis.Count));
        int range = _lh - th;
        if (range > 0)
            _scroll = Math.Clamp(
                _dragScr0 + (int)((float)(y - _dragY0) / range * MaxScr), 0, MaxScr);
    }

    public override void releaseLeftClick(int x, int y)
    {
        _drag = false;
        _dragging = false;
    }

    public override void performHoverAction(int x, int y)
    {
        _hoverTab = -1;
        for (int i = 0; i < Tabs.Length; i++)
        {
            int ty = _tcy + i * TabH;
            if (x >= _tcx && x <= _tcx + TabW - Pad / 2 && y >= ty && y <= ty + TabH)
            { _hoverTab = i; break; }
        }
        base.performHoverAction(x, y);
    }

    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        _dragging = false;
        exitThisMenu();
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape) exitThisMenu();
        base.receiveKeyPress(key);
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────
    private static Color CatColor(BundleCategory? cat) => cat switch
    {
        BundleCategory.Crop         => CGreen,
        BundleCategory.Fish         => CBlue,
        BundleCategory.Artisan      => CPurple,
        BundleCategory.Construction => CBrown,
        _                           => COrange,
    };
}
