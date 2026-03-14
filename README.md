# Stardew Season Planner / Sezon Planlayıcı

> **EN:** A SMAPI mod that helps you plan bundle progress and resource collection by season.
>
> **TR:** Mevsimlere göre paket ilerlemesini ve kaynak toplama planlamanıza yardımcı olan bir SMAPI modu.

[![SMAPI](https://img.shields.io/badge/SMAPI-5%2B-2b8a3e)](https://smapi.io)
[![Stardew Valley](https://img.shields.io/badge/Stardew%20Valley-1.6%2B-8a4b2b)](https://www.stardewvalley.net/)
[![License](https://img.shields.io/badge/License-MIT-1f6feb)](LICENSE)

---

## 📌 Overview / Genel Bakış

### English
Stardew Season Planner (SSP) is a SMAPI mod that scans your in-game bundle progress and provides a season-based planning view. It helps you know what items you still need, when they are in season, and what to prepare next so you can complete community center bundles efficiently.

### Türkçe
Stardew Season Planner (SSP), oyun içindeki paket ilerlemenizi tarayan ve mevsime göre planlamanızı sağlayan bir SMAPI modudur. Hangi eşyaların eksik olduğunu, hangi mevsimde bulunabileceğini ve bir sonraki adımda ne hazırlamanız gerektiğini göstererek başarı merkezini daha verimli tamamlamanızı sağlar.

---

## ✨ Features / Özellikler

- **Season-focused bundle tracking** / Mevsim bazlı paket takibi
- **Calendar synchronization** / Takvim ile senkronizasyon
- **Bundle scanning (auto-detect status)** / Paket tarama (durum otomatik algılama)
- **Configurable settings via SMAPI config menu** / SMAPI yapılandırma menüsü üzerinden ayarlar
- **Multi-language support (English + Türkçe)** / Çoklu dil desteği (İngilizce + Türkçe)
- **Optional debug logging** / İsteğe bağlı hata ayıklama kayıtları

---

## 🛠 Installation / Kurulum

### English
1. Install [SMAPI](https://smapi.io) (required).
2. Copy the `SeasonPlanner` folder into your Stardew Valley `Mods/` directory.
3. Launch Stardew Valley via SMAPI.

### Local Build Setup (GamePath)
To keep personal paths private, set your game folder locally:

1. Copy [Directory.Build.props.example](Directory.Build.props.example) to [Directory.Build.props](Directory.Build.props).
2. Edit the `<GamePath>` value inside [Directory.Build.props](Directory.Build.props).

This local file is ignored by git.

### Local Build Setup (GamePath)
To avoid committing personal paths, set your game folder locally:

1. Copy `Directory.Build.props.example` to `Directory.Build.props`.
2. Edit `<GamePath>` in `Directory.Build.props` to your Stardew Valley install path.

`Directory.Build.props` is ignored by git and stays private.

### Türkçe
1. [SMAPI](https://smapi.io) kurun (gereklidir).
2. `SeasonPlanner` klasörünü Stardew Valley `Mods/` dizinine kopyalayın.
3. Stardew Valley'i SMAPI ile başlatın.

### Yerel Derleme Ayarı (GamePath)
Kişisel yolları gizli tutmak için oyun klasörünü yerelde ayarlayın:

1. [Directory.Build.props.example](Directory.Build.props.example) dosyasını [Directory.Build.props](Directory.Build.props) olarak kopyalayın.
2. [Directory.Build.props](Directory.Build.props) içindeki `<GamePath>` değerini düzenleyin.

Bu yerel dosya git tarafından yok sayılır.

### Yerel Derleme Ayarı (GamePath)
Kişisel yolları repo'ya koymamak için oyun klasörünü yerelde ayarlayın:

1. `Directory.Build.props.example` dosyasını `Directory.Build.props` olarak kopyalayın.
2. `Directory.Build.props` içindeki `<GamePath>` değerini kendi kurulum yolunuzla değiştirin.

`Directory.Build.props` git tarafından yok sayılır ve gizli kalır.

---

## ⚙ Configuration / Yapılandırma

### English
Open the SMAPI in-game mod config menu and adjust settings to your liking. Typical options include:
- Enable / disable calendar overlay
- Show only missing bundle items
- Language selection (EN/TR)
- Enable debug logs

### Türkçe
SMAPI mod yapılandırma menüsünü açın ve ayarları ihtiyacınıza göre değiştirin. Yaygın seçenekler:
- Takvim kaplamasını etkinleştir / devre dışı bırak
- Sadece eksik paket öğelerini göster
- Dil seçimi (EN/TR)
- Hata ayıklama kayıtlarını etkinleştir

---

## 📝 Notes / Notlar

- The mod reads all bundle requirements from Stardew Valley data and updates dynamically as you complete bundles.
- If you encounter localization issues, check `i18n/default.json` and `i18n/tr.json`.
- This project is meant for Stardew Valley v1.6+ (SMAPI 5+).

---

## 🛠 Support / Destek

If you find a bug, please open an issue in the repository with as much detail as possible (game version, mods installed, reproduction steps).

---

## 🧩 Contribution / Katkıda Bulunma

Contributions are welcome! Feel free to fork, fix, and submit a PR. Please keep:
- Code style consistent with existing C# patterns
- Localization changes in `i18n/` files

---

## 📄 License / Lisans

MIT License. See [LICENSE](LICENSE).

## 🧾 Changelog / Degisim Gunlugu

See [CHANGELOG.md](CHANGELOG.md) for release history.

## 🔐 Security / Guvenlik

See [SECURITY.md](SECURITY.md) for reporting guidelines.
