# SyncBreaker Android 🌌📱

**SyncBreaker** — Cyberpunk temalı savunma oyununun **Unity 2D** ile geliştirilmiş Android versiyonu.

Orijinal web versiyonundan ([dormhi.github.io/syncbreaker](https://dormhi.github.io/syncbreaker/)) Unity'ye port edilmiş, **gerçek fizik mekanikleri** ve **genişletilebilir mimari** ile yeniden inşa edilmiştir.

## 🎮 Oynanış

Sisteminiz bir siber saldırı altında. Enfekte düğümleri temizleyip güvenlik duvarını yeniden kurmanız gerekiyor:

1. **Timing Bar** — Gösterge yeşil hedef bölgeye geldiğinde dokunun
2. **Code Breaker (Lockpick)** — Kilitli düğümlere erişmek için yön bulmacasını çözün
3. **Endless Mode** — Tüm düğümleri temizledikten sonra sonsuz dayanma modu

## 🆕 Web Versiyonundan Farklılıklar

- ⚡ **Fizik Tabanlı Mekanikler** — Unity Box2D ile gerçek fizik
- 🎨 **Gelişmiş Parçacık Efektleri** — GPU destekli particle systems
- 📱 **Native Android** — WebView değil, doğrudan native uygulama
- 🔧 **Modüler Mimari** — ScriptableObject ile kolayca genişletilebilir
- 🔊 **Gelişmiş Ses** — Unity AudioMixer ile 3D ses desteği

## 🛠 Gereksinimler

- Unity 2022.3 LTS (veya daha yeni)
- Android Build Support modülü
- Android SDK (Unity Hub üzerinden)

## 📦 Build

1. Unity Hub'da projeyi aç
2. **File → Build Settings → Android** → Switch Platform
3. **Player Settings** → Package Name: `com.syncbreaker.game`
4. **Build** → APK oluştur

## 📐 Mimari

```
Assets/Scripts/
├── Core/           # GameManager, StateManager, Utils
├── Gameplay/       # TimingBar, LevelManager, LockpickSystem, EndlessMode
├── Systems/        # EnergySystem, SoundManager, SaveSystem
└── UI/             # MenuUI, HubUI, GameplayUI, GameOverUI
```

## 🎓 Hakkında

İstanbul Arel Üniversitesi — Computer Graphics Final Project (Android versiyonu).
