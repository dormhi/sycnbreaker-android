# SyncBreaker Android - Agent Guidelines & Project Manifesto

Bu dosya, SyncBreaker Android projesinin temel amacını, mimari kararlarını, kodlama standartlarını ve genel yol haritasını içermektedir. Tüm AI asistanlar, bu projede çalışırken buradaki kurallara ve bağlama uymalıdır.

## 🎯 Projenin Temel Amacı
SyncBreaker Android, orijinalinde web tabanlı olarak geliştirilmiş bir oyunun **mobil (Android) odaklı, Unity motoru kullanılarak** yeniden geliştirilmiş halidir. 
Oyunun temelinde "Timing Bar" (zamanlama) ve "Lockpick" (kilit açma) gibi refleks ve ritim odaklı mini oyunlar bulunur.

**Neden Unity?** 
Kullanıcı, "gerçek fizik mekaniklerinin daha güzel olacağını" ve "güncellemeye çok açık (yeni bölümler, boss savaşları, multiplayer gibi) bir oyun yapmak istediğini" belirtti. Bu nedenle düz web teknolojileri (Capacitor vb.) yerine Unity tercih edilerek, fizik tabanlı, atalet ve titreşim gibi hislerin daha doğal verildiği, genişletilebilir bir mimari kuruldu.

## 🏗️ Mimari ve Kodlama Tarzı

### 1. Temel Desenler (Core Patterns)
- **Singleton Pattern:** Sahneler arası kalıcılık gerektiren temel yöneticiler için kullanılır (`GameManager.cs`).
- **State Machine (FSM):** Oyunun akışı (Menü, Oyun İçi, Mini Oyun, Game Over) `StateManager.cs` üzerinden `IStateHandler` arayüzü ile yönetilir. Her durum kendi mantığını kendi handler'ı içinde (örn. `LevelStateHandler.cs`, `LockpickStateHandler.cs`) hapseder.
- **Data-Driven Design (Veri Güdümlü Tasarım):** Bölüm ayarları, temalar ve düşman/zorluk konfigürasyonları `ScriptableObject`'ler kullanılarak yapılır (örn. `LevelData.cs`). Bu sayede kod yazmadan sadece editör üzerinden yeni bölümler eklenebilir.

### 2. Kodlama Standartları (C#)
- **Namespace Kullanımı:** Scriptler mantıksal olarak `SyncBreaker.Core`, `SyncBreaker.Gameplay`, `SyncBreaker.UI`, `SyncBreaker.Systems` gibi namespace'lere bölünmelidir.
- **Bağımlılıkların Ayrılması (Decoupling):** Gameplay mantığı (örn. `TimingBar.cs`) ile UI mantığı (örn. `GameplayUI.cs`) kesinlikle birbirinden ayrı olmalıdır. UI scriptleri, Gameplay scriptlerine referans alıp olayları (events) dinlemeli (`OnHit`, `OnNodeSolved` vb.), oynanış mantığına doğrudan müdahale etmemelidir.
- **Girdi Yönetimi:** Girdiler `TouchInputHandler.cs` gibi merkezi scriptlerde toplanır. Gameplay scriptleri `Input.GetTouch` çağırmaz, InputHandler'ın fırlattığı `OnTap` veya `OnSwipe` event'lerini dinler.
- **Modern Unity API:** Obsolete olmuş API'ler yerine güncelleri kullanılmalıdır (örn. `FindFirstObjectByType` yerine `FindAnyObjectByType`).

### 3. Oyun Hissi ve Fizik (Game Feel)
- Matematiksel kesinlikten ziyade **"canlı ve organik"** bir his hedeflenir.
- Örneğin Timing Bar'da sadece düz bir Lerp kullanmak yerine, atalet (momentum) ve sönümleme (damping) gibi fiziksel kurallar kodlanmıştır.
- Lockpick sisteminde imleç anında dönmeye başlamaz, bir **angular momentum** (açısal ivmelenme) ile hızlanır; düğümler çözüldüğünde **damped spring** (yay) mantığıyla titrer (vibration).

## 🗺️ Geliştirme Planı (7 Günlük Sprint)

Şu anki durum: **Gün 3 Tamamlandı, Gün 4 Bekliyor.**

- **Gün 1: Proje Altyapısı & Temel Mimari (✅ TAMAMLANDI)**
  - Unity projesi kurulumu, klasör düzeni, GameManager, StateManager, Utils.
- **Gün 2: Timing Bar Mekaniği (✅ TAMAMLANDI)**
  - `LevelData` (ScriptableObject), TimingBar fiziği, LevelManager, TouchInputHandler, GameplayUI.
- **Gün 3: Lockpick Sistemi (✅ TAMAMLANDI)**
  - Fizik tabanlı dairesel mini oyun (angular momentum, spring vibration), swipe girdisi, UI entegrasyonu, State Handler'lar.
- **Gün 4: UI, Menüler & Enerji (⏳ SIRADAKİ)**
  - MainMenuUI, HubUI, GameOverUI.
  - Can/Enerji sistemi ve SaveSystem (Oyun kaydetme).
- **Gün 5: Görsel Efektler & Temalar**
  - ScriptableObject tabanlı LevelThemes.
  - ParticleSystem ile hit/patlama efektleri.
- **Gün 6: Ses, Endless Mode & Offline**
  - SoundManager (AudioMixer).
  - Endless Mode entegrasyonunun tamamlanması (Dalga/Wave sistemi).
- **Gün 7: Build, Test & Dağıtım**
  - Son cila, performans optimizasyonu.
  - Android APK/AAB build ve test süreçleri.

## ⚠️ Kurallar / Hatırlatmalar
- Asla sahnedeki objeleri manuel bulmak için pahalı işlemleri (örn. `FindObjectOfType`) `Update` içinde çağırmayın. `Awake`/`Start` içinde önbelleğe alın veya event'ler ile haberleştirin.
- Mobil cihazları (Android) hedeflediğimiz için gereksiz GC (Garbage Collection) oluşturacak döngü içi bellek tahsislerinden (new object(), string birleştirme vb.) kaçının.
- Her büyük geliştirme (Gün tamamlanması) sonrası mutlaka GitHub'a (git commit & push) kodu gönderin.
