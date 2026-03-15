<div align="center">

# 🌱 Season Planner & Bundle Reminder

**A SMAPI mod for Stardew Valley that tracks your Community Center bundle progress,  
marks last planting deadlines on your calendar, and sends smart HUD alerts — so you never miss a season again.**

<br/>

[![SMAPI](https://img.shields.io/badge/SMAPI-4.1%2B-2b8a3e?style=for-the-badge&logo=data:image/png;base64,)](https://smapi.io)
[![Stardew Valley](https://img.shields.io/badge/Stardew%20Valley-1.6%2B-c0692e?style=for-the-badge)](https://www.stardewvalley.net/)
[![License: CC BY-NC-ND 4.0](https://img.shields.io/badge/License-CC%20BY--NC--ND%204.0-lightgrey?style=for-the-badge)](LICENSE)
[![Version](https://img.shields.io/badge/Version-1.0.0-blueviolet?style=for-the-badge)](manifest.json)
[![Language](https://img.shields.io/badge/Languages-EN%20%7C%20TR-informational?style=for-the-badge)](#-multi-language)

<br/>

> 🇺🇸 **English** | 🇹🇷 **[Türkçe aşağıda](#-türkçe)**

</div>

---

## 📸 Screenshots

> _Add your screenshots here! Place `.png` files in a `docs/` folder and reference them below._

<!--
<p align="center">
  <img src="docs/bundle-panel.png" width="600" alt="Bundle Panel"/>
  <br/><em>Bundle Panel — missing items grouped by category</em>
</p>
<p align="center">
  <img src="docs/calendar-markers.png" width="600" alt="Calendar Markers"/>
  <br/><em>Calendar — last planting days marked automatically</em>
</p>
<p align="center">
  <img src="docs/hud-notification.png" width="600" alt="HUD Notification"/>
  <br/><em>HUD alert on the morning of the last planting day</em>
</p>
-->

---

## ✨ Features

| Feature | Description |
|---|---|
| 📋 **Bundle Panel** | Lists every missing item grouped by category. Open with `F5` (configurable). |
| 📅 **Calendar Markers** | Automatically marks last planting days on the in-game calendar. |
| 🔔 **HUD Notifications** | Morning alerts when a planting deadline or a rain-fish opportunity is near. |
| 🧳 **Inventory Tooltip** | Shows which bundle an item belongs to when you hover it in your bag. |
| 📦 **Chest Tooltip** | Same bundle info when hovering items inside chests. |
| 🛒 **Shop Source** | Displays where to buy an item if it can be purchased. |
| 📌 **Plan Items** | Mark items as "planned" to filter and focus your to-do list. |
| 💾 **Persistent Panel Position** | Remembers where you moved the bundle panel between sessions. |
| 🌍 **Multi-language** | Full English and Turkish localization. |
| ⚙️ **GMCM Support** | All settings are configurable via [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) in-game. |

---

## 🛠️ Installation

### Requirements

- [Stardew Valley](https://www.stardewvalley.net/) `1.6+`
- [SMAPI](https://smapi.io) `4.1+`
- _(Optional)_ [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) for in-game settings UI

### Steps

1. Install **SMAPI** if you haven't already.
2. Download the latest release from the [Releases](https://github.com/devjawen/stardew-season-planner/releases) page.
3. Extract and copy the `SeasonPlanner` folder into your `Mods/` directory:
   ```
   Stardew Valley/
   └── Mods/
       └── SeasonPlanner/
           ├── SeasonPlanner.dll
           ├── manifest.json
           └── i18n/
   ```
4. Launch Stardew Valley through SMAPI.

---

## ⚙️ Configuration

All settings can be changed in-game via **Generic Mod Config Menu** (if installed), or by editing `config.json` in the mod folder.

| Setting | Default | Description |
|---|---|---|
| `ShowCalendarMarkers` | `true` | Mark last planting days on the calendar |
| `ShowHudNotifications` | `true` | Morning HUD alerts for deadlines and rain fish |
| `ShowInventoryTooltips` | `true` | Bundle info on inventory item hover |
| `ShowChestTooltips` | `true` | Bundle info on chest item hover |
| `FilterConstructionItems` | `true` | Hide Wood/Stone/etc. from the bundle list |
| `ShowShopSource` | `true` | Show where items can be bought |
| `CalendarWarningDaysLeft` | `7` | Days before deadline to start showing warnings |
| `PanelHotkey` | `F5` | Key to open/close the bundle panel |
| `RememberPanelPosition` | `true` | Restore panel position between sessions |

---

## 🎮 Hotkeys

| Key | Action |
|---|---|
| `F5` _(default)_ | Open / close the Missing Bundle Panel |
| `ESC` | Close the panel |
| `Scroll` | Navigate the item list |

> The panel hotkey can be changed in the GMCM settings or directly in `config.json`.

---

## 📋 Bundle Panel — Tabs

The panel organizes missing items into tabs for quick filtering:

| Tab | Contents |
|---|---|
| **All** | Every remaining item |
| **Crop** | Crops and forageables |
| **Fish** | Fish (with rain/season info) |
| **Artisan** | Artisan goods |
| **Build** | Construction materials |
| **Other** | Misc items |

Items are sorted by **urgency** (days remaining) or **name** — toggled via the sort button.

---

## 🌍 Multi-language

The mod ships with full localizations for:

| Language | File | Status |
|---|---|---|
| English | `i18n/default.json` | ✅ Complete |
| Türkçe | `i18n/tr.json` | ✅ Tam |

Contributions for additional languages are very welcome — see [Contributing](#-contributing).

---

## 👩‍💻 Building from Source

```bash
git clone https://github.com/devjawen/stardew-season-planner.git
cd stardew-season-planner
```

Set your local game path (never committed to git):

```bash
# Copy the example file
cp Directory.Build.props.example Directory.Build.props
```

Then edit `Directory.Build.props` and set `<GamePath>` to your Stardew Valley installation folder.

```xml
<GamePath>C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley</GamePath>
```

Build with:

```bash
dotnet build
```

---

## 🤝 Contributing

Contributions are welcome! Here's how:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/my-feature`)
3. **Commit** your changes
4. **Open a Pull Request**

Please follow these guidelines:
- Match the existing C# code style
- Add/update translations in `i18n/` when adding new strings
- Keep PRs focused — one feature or fix per PR

### Found a bug?

Open an [Issue](https://github.com/devjawen/stardew-season-planner/issues) and include:
- Your Stardew Valley version
- Your SMAPI version
- Other mods installed (SMAPI log preferred)
- Steps to reproduce

---

## 📄 License

This project is licensed under **[CC BY-NC-ND 4.0](LICENSE)**.

- ✅ You **may** use and share it with proper attribution.
- ❌ You **may not** use it commercially.
- ❌ You **may not** publish modified versions.

**© 2024 Jawen — All rights reserved under the above terms.**

---

## 🔐 Security

See [SECURITY.md](SECURITY.md) for vulnerability reporting guidelines.

---

<a name="-türkçe"></a>
<details>
<summary><strong>🇹🇷 Türkçe Açıklama (tıklayın)</strong></summary>

<br/>

## Stardew Season Planner — Türkçe

**Season Planner & Bundle Reminder**, Stardew Valley'deki Topluluk Merkezi paket ilerlemenizi takip eden, takvimde son ekim günlerini işaretleyen ve akıllı HUD bildirimleri gönderen bir SMAPI modudur.

### Özellikler

- 📋 **Paket Paneli** — Eksik eşyaları kategoriye göre gruplandırır. `F5` ile açılır (ayarlanabilir).
- 📅 **Takvim İşaretleri** — Son ekim günleri oyun takviminde otomatik işaretlenir.
- 🔔 **HUD Bildirimleri** — Sabah uyandığınızda ekim son günleri ve yağmur balığı fırsatları için uyarı görünür.
- 🧳 **Envanter Tooltip** — Çantanızdaki bir eşyanın üzerine geldiğinizde hangi pakete ait olduğunu gösterir.
- 📦 **Sandık Tooltip** — Sandıklardaki eşyalar için de aynı bilgi gösterilir.
- 🛒 **Mağaza Kaynağı** — Satın alınabilecek eşyaların nereden alınacağını gösterir.
- 📌 **Planlama** — Eşyaları "planlandı" olarak işaretleyerek listenizi filtreleyin.
- 💾 **Panel Konumu** — Paneli taşısanız bile konumu oturumlar arası hatırlanır.
- 🌍 **Çoklu Dil** — Tam İngilizce ve Türkçe çeviri.

### Kurulum

1. [SMAPI](https://smapi.io) kurun.
2. [Sürümler](https://github.com/devjawen/stardew-season-planner/releases) sayfasından son sürümü indirin.
3. `SeasonPlanner` klasörünü `Mods/` dizinine kopyalayın.
4. Stardew Valley'i SMAPI ile başlatın.

### Yapılandırma

Tüm ayarlar **Generic Mod Config Menu** üzerinden oyun içinde değiştirilebilir ya da `config.json` dosyası düzenlenebilir. Başlıca seçenekler:

| Ayar | Varsayılan | Açıklama |
|---|---|---|
| Takvim İşaretleri | Açık | Son ekim günlerini takvimde gösterir |
| HUD Bildirimleri | Açık | Sabah uyarıları gösterir |
| Envanter Tooltip | Açık | Çantada paket bilgisi gösterir |
| Sandık Tooltip | Açık | Sandıkta paket bilgisi gösterir |
| İnşaat Öğelerini Gizle | Açık | Listeden Odun/Taş vb. kaldırır |
| Mağaza Kaynağı | Açık | Eşyanın nereden alınacağını gösterir |
| Uyarı Eşiği (Gün) | 7 | Son güne kaç gün kala uyarı başlasın |
| Panel Kısayolu | F5 | Paneli açma/kapama tuşu |

### Katkıda Bulunma

Hata bulursanız veya yeni bir dil çevirisi eklemek isterseniz [Issue](https://github.com/devjawen/stardew-season-planner/issues) açabilir veya Pull Request gönderebilirsiniz.

### Lisans

**CC BY-NC-ND 4.0** — Atıf ile kullanabilir ve paylaşabilirsiniz; ticari kullanım ve değiştirilmiş sürüm yayınlamak yasaktır.

</details>

---

<div align="center">
  <sub>Made with ☕ and too many in-game seasons by <a href="https://github.com/devjawen">Jawen</a></sub>
</div>
