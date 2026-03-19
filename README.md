<div align="center">

<img src="images/f5menu.png" alt="Season Planner Banner" width="600"/>

# Season Planner & Bundle Reminder

**Never miss a planting deadline or bundle item again.**  
A SMAPI mod for Stardew Valley that tracks your Community Center progress, marks deadlines on the calendar, and keeps you one step ahead every season.

<br/>

[![SMAPI](https://img.shields.io/badge/SMAPI-4.1%2B-2b8a3e?style=flat-square)](https://smapi.io)
[![Stardew Valley](https://img.shields.io/badge/Stardew%20Valley-1.6%2B-c0692e?style=flat-square)](https://www.stardewvalley.net/)
[![Version](https://img.shields.io/badge/Version-1.4.0-blueviolet?style=flat-square)](manifest.json)
[![License](https://img.shields.io/badge/License-CC%20BY--NC--ND%204.0-lightgrey?style=flat-square)](LICENSE)
[![Languages](https://img.shields.io/badge/Languages-EN%20%7C%20TR-informational?style=flat-square)](#-languages)

<br/>

[**Download on Nexus Mods**](https://www.nexusmods.com/stardewvalley/mods/43803) · [**GitHub Releases**](https://github.com/devjawen/stardew-season-planner/releases) · [**Report a Bug**](https://github.com/devjawen/stardew-season-planner/issues) · [**Discussions**](https://github.com/devjawen/stardew-season-planner/discussions)

</div>

---

## What it does

Open the bundle panel with `F5` and see every missing Community Center item at a glance — sorted by urgency, filtered by category, with planting deadlines, shop sources, and rain-fish alerts all in one place.

- **Bundle Panel** — missing items grouped by category (Crop, Fish, Artisan, Forage, Build, Other), sortable by urgency / name / bundle
- **Calendar Markers** — last planting days highlighted directly on the in-game calendar
- **HUD Alerts** — morning notifications for planting deadlines and rain-fish opportunities
- **Inventory & Chest Tooltips** — hover any item to see which bundle it belongs to
- **Seed Tooltips** — hover a seed to see grow time, season, last planting day, and greenhouse info
- **Planning** — mark items as "planned", filter to planned-only view, get notified when a planned item is completed
- **Responsive Layout** — panel scales to any screen size; calendar column hides on small screens
- **Mod Compatible** — works with Content Patcher mods, SVE, Cornucopia, Bonster's Crops, and more

---

## Screenshots

| Bundle Panel | Calendar | Tooltips |
|:---:|:---:|:---:|
| ![panel](images/inventory.jpg) | ![calendar](images/calendar.jpg) | ![shop](images/shop.jpg) |

---

## Installation

**Requirements**
- Stardew Valley `1.6+`
- [SMAPI](https://smapi.io) `4.1+`
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) *(optional, for in-game settings)*

**Steps**
1. Download the latest zip from [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/43803) or [GitHub Releases](https://github.com/devjawen/stardew-season-planner/releases)
2. Extract into your `Stardew Valley/Mods/` folder
3. Launch the game through SMAPI

```
Stardew Valley/Mods/SeasonPlanner/
├── SeasonPlanner.dll
├── manifest.json
└── i18n/
    ├── default.json
    └── tr.json
```

---

## Configuration

All settings are available in-game via **Generic Mod Config Menu**, or by editing `config.json`.

| Setting | Default | Description |
|---|:---:|---|
| Show Calendar Markers | ✅ | Highlight last planting days on the calendar |
| Show HUD Notifications | ✅ | Morning alerts for deadlines and rain fish |
| Show Inventory Tooltips | ✅ | Bundle info on hovered inventory items |
| Show Chest Tooltips | ✅ | Bundle info on hovered chest items |
| Show Shop Source | ✅ | Where to buy items, shown in tooltips |
| Filter Construction Items | ✅ | Hide Wood/Stone/etc. from the panel |
| Warning Threshold (Days) | `7` | How many days before deadline to start warning |
| Panel Hotkey | `F5` | Key to open/close the bundle panel |
| Remember Panel Position | ✅ | Save panel position between sessions |
| Panel Size (%) | `100` | Scale the bundle panel (50–150%) |
| Bundle Tooltip Size (%) | `100` | Scale the bundle info tooltip (50–200%) |
| Seed Tooltip Size (%) | `100` | Scale the planting info tooltip (50–200%) |

---

## Controls

| Input | Action |
|---|---|
| `F5` | Open / close the bundle panel |
| `ESC` or right-click | Close the panel |
| Scroll wheel | Navigate the item list |
| Click a tab | Filter by category |
| Click a chip | Filter by urgency / season / planned |
| Click "Plan" | Mark an item as planned |
| Drag panel header | Reposition the panel |

---

## Mod Compatibility

The mod reads `Data/Bundles`, `Data/Crops`, `Data/Shops`, and `Data/Fish` through SMAPI's content API, so any mod that patches those assets is automatically supported.

Explicitly tested / declared compatible:

| Mod | Support |
|---|---|
| Content Patcher | ✅ Full |
| Stardew Valley Expanded | ✅ Full |
| Cornucopia — More Crops | ✅ Full |
| Cornucopia — Cooking Recipes | ✅ Full |
| Bonster's Crops | ✅ Full |
| Culinary Delight | ✅ Full |
| Better Things | ✅ Full |
| Json Assets | ✅ Full |
| Dynamic Game Assets | ✅ Full |
| Generic Mod Config Menu | ✅ Full |

If you find an item from another mod that isn't showing correctly, open an [issue](https://github.com/devjawen/stardew-season-planner/issues) with the mod name and SMAPI log.

---

## Languages

| Language | File | Status |
|---|---|:---:|
| English | `i18n/default.json` | ✅ |
| Türkçe | `i18n/tr.json` | ✅ |
| *Your language?* | `i18n/xx.json` | 🙌 |

To add a translation: copy `i18n/default.json`, rename it to your language code, translate the values, and submit a PR.

---

## Building from Source

```bash
git clone https://github.com/devjawen/stardew-season-planner.git
cd stardew-season-planner
```

Edit `Directory.Build.props` and set your game path:

```xml
<Project>
  <PropertyGroup>
    <GamePath>C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley</GamePath>
  </PropertyGroup>
</Project>
```

Then build:

```bash
dotnet build -c Release
```

The mod is automatically copied to your `Mods/` folder and a release zip is generated in `bin/Release/net6.0/`.

---

## Contributing

Bug reports, translations, and pull requests are welcome.

- **Bug?** → [Open an issue](https://github.com/devjawen/stardew-season-planner/issues) with your SMAPI log and mod list
- **Feature idea?** → [Start a discussion](https://github.com/devjawen/stardew-season-planner/discussions)
- **Code contribution?** → Fork → branch → PR, one feature per PR please

---

## License

[CC BY-NC-ND 4.0](LICENSE) — free to use and share with attribution; no commercial use, no modified redistributions.

© 2024 Jawen

---

<details>
<summary><strong>🇹🇷 Türkçe</strong></summary>

<br/>

## Season Planner & Bundle Reminder

Stardew Valley'de Topluluk Merkezi paket ilerlemenizi takip eden, takvimde son ekim günlerini işaretleyen ve akıllı HUD bildirimleri gönderen bir SMAPI modudur.

### Ne yapar?

`F5` ile paket panelini açın ve tüm eksik eşyaları tek ekranda görün — aciliyete göre sıralanmış, kategoriye göre filtrelenmiş, ekim son günleri, mağaza kaynakları ve yağmur balığı uyarılarıyla birlikte.

- **Paket Paneli** — eksik eşyalar kategoriye göre gruplandırılmış (Ürün, Balık, Zanaat, Toplama, İnşaat, Diğer)
- **Takvim İşaretleri** — son ekim günleri oyun takviminde otomatik işaretlenir
- **HUD Uyarıları** — sabah bildirimleri: ekim son günleri ve yağmur balığı fırsatları
- **Envanter & Sandık Tooltip** — eşyanın üzerine gelin, hangi pakete ait olduğunu görün
- **Tohum Tooltip** — tohumun üzerine gelin, büyüme süresi, mevsim ve son ekim günü bilgisi
- **Planlama** — eşyaları "planlandı" olarak işaretleyin, tamamlandığında bildirim alın
- **Duyarlı Arayüz** — panel her ekran boyutuna uyum sağlar
- **Mod Uyumlu** — SVE, Cornucopia, Bonster ve diğer Content Patcher modlarıyla çalışır

### Kurulum

1. [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/43803) veya [GitHub Releases](https://github.com/devjawen/stardew-season-planner/releases) sayfasından indirin
2. `Stardew Valley/Mods/` klasörüne çıkarın
3. Oyunu SMAPI ile başlatın

**Gereksinimler:** Stardew Valley 1.6+, SMAPI 4.1+, *(isteğe bağlı)* Generic Mod Config Menu

### Kontroller

| Tuş | İşlem |
|---|---|
| `F5` | Paneli aç / kapat |
| `ESC` veya sağ tık | Paneli kapat |
| Scroll | Listede gezin |
| "Planla" butonu | Eşyayı planlandı olarak işaretle |
| Panel başlığını sürükle | Paneli taşı |

### Lisans

[CC BY-NC-ND 4.0](LICENSE) — atıf ile kullanabilir ve paylaşabilirsiniz; ticari kullanım ve değiştirilmiş sürüm yayınlamak yasaktır.

</details>

---

<div align="center">
  <sub>Made with ☕ by <a href="https://github.com/devjawen"><b>Jawen</b></a></sub>
</div>
