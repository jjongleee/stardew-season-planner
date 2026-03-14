# Stardew Season Planner / Sezon Planlayıcı

> **EN:** A SMAPI mod that helps you plan bundle progress and resource collection by season.
>
> **TR:** Mevsimlere göre paket ilerlemesini ve kaynak toplama planlamanıza yardımcı olan bir SMAPI modu.

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

### Türkçe
1. [SMAPI](https://smapi.io) kurun (gereklidir).
2. `SeasonPlanner` klasörünü Stardew Valley `Mods/` dizinine kopyalayın.
3. Stardew Valley'i SMAPI ile başlatın.

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

This project is provided as-is; include the license you prefer (MIT, CC0, etc.).
