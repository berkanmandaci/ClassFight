# ClassFight Dinamik Server Sistemi - Yapılacaklar Listesi

## 1. Server Tarafı Değişiklikleri ✅

### 1.1. Docker Compose Güncellemesi
- [ ] Unity Server container'larını çoğalt (3 adet)
- [ ] Her container için memory limit ekle
- [ ] Port mapping'leri güncelle (7777, 7778, 7779)
```yaml
unity-server-1:
  mem_limit: 200M
  ports:
    - "7777:7777/udp"
unity-server-2:
  mem_limit: 200M
  ports:
    - "7778:7777/udp"
unity-server-3:
  mem_limit: 200M
  ports:
    - "7779:7777/udp"
```

### 1.2. Nakama Match Handler Güncellemesi ✅
- [x] Server pool yönetim sistemi ekle
```typescript
const serverPool = [
    { id: 1, port: 7777, busy: false },
    { id: 2, port: 7778, busy: false },
    { id: 3, port: 7779, busy: false }
];
```
- [x] Match-Server eşleştirme sistemi ekle
- [x] Server durum takip sistemi ekle
- [x] Match bitiminde server cleanup sistemi ekle

### 1.3. Unity Server Build Ayarları
- [ ] Build parametrelerini güncelle (port dinamik olacak)
- [ ] Memory optimizasyonları ekle
- [ ] Network ayarlarını optimize et

## 2. Client Tarafı Değişiklikleri

### 2.1. NetworkManager Güncellemesi ✅
- [x] Dinamik port desteği ekle (`PvpServerModel.ConfigureTransport()` metodunda implementte edildi)
- [x] Server bilgisi alma sistemini güncelle (`PvpServerModel.GetServerInfo()` metodu eklendi)
- [x] Retry ve hata yönetimi mekanizmaları eklendi
- [x] Bağlantı durumu kontrolü ve timeout yönetimi eklendi (`WaitForConnection()` metodu)
- [x] Event yönetimi sistemi geliştirildi (`SubscribeToNetworkEvents()` ve `UnsubscribeFromNetworkEvents()`)

### 2.2. Matchmaking Sistemi Güncellemesi ✅
- [x] Matchmaking başlatma sistemi ekle (`MatchmakingModel.JoinMatchmaking()`)
- [x] Match bulma ve timeout yönetimi (`MatchmakingModel.WaitForMatch()`)
- [x] Retry mekanizması eklendi (MAX_RETRY_ATTEMPTS = 3)
- [x] Match iptal sistemi (`MatchmakingModel.CancelMatchmaking()`)
- [x] Match bilgisi senkronizasyonu (`PvpServerModel.OnMatchFound()`)

### 2.3. UI Güncellemeleri
- [ ] Matchmaking durumu göstergesi ekle
- [ ] Hata mesajları UI sistemi
- [ ] Bağlantı durumu göstergesi

## 3. Test Senaryoları

### 3.1. Temel Testler
- [ ] Tek match bağlantı testi
  - Matchmaking başlatma
  - Server bilgisi alma
  - Game server'a bağlanma
  - Match'e katılma
  
- [ ] Multiple match bağlantı testi
  - 3 farklı match oluşturma
  - Her match için farklı port kontrolü
  - Server pool yönetimi kontrolü
  
- [ ] Server dolu durumu testi
  - 3 match oluştur
  - 4. match için hata kontrolü
  - Hata mesajı kontrolü
  
- [ ] Match bitimi ve cleanup testi
  - Match bitir
  - Server serbest bırakma
  - Yeni match için server kullanılabilirlik kontrolü

### 3.2. Hata Senaryoları
- [ ] Bağlantı kopması durumu
  - İnternet bağlantısını kes
  - Retry mekanizması kontrolü
  - UI feedback kontrolü
  
- [ ] Server crash durumu
  - Server'ı zorla kapat
  - Hata yakalama kontrolü
  - Cleanup kontrolü
  
- [ ] Match oluşturma hatası
  - Nakama bağlantısını kes
  - Hata mesajı kontrolü
  - Retry mekanizması kontrolü

### 3.3. Yük Testleri
- [ ] Maximum eşzamanlı match testi
  - 3 match oluştur
  - Memory kullanımı kontrol
  - CPU kullanımı kontrol
  
- [ ] Maximum oyuncu testi
  - 6 oyuncu (3 match)
  - Network kullanımı kontrol
  - Performans kontrol

## 4. Monitoring ve Logging ✅

### 4.1. Server Monitoring
- [ ] Docker stats monitoring ekle
- [ ] Memory kullanım takibi
- [ ] Network kullanım takibi

### 4.2. Match Monitoring
- [ ] Aktif match sayısı takibi
- [ ] Match süresi takibi
- [ ] Oyuncu sayısı takibi

### 4.3. Error Logging
- [x] Server hata logları (`LogModel.Instance.Error()`)
- [x] Match hata logları (`LogModel.Instance.Log()`)
- [x] Network hata logları (`OnErrorMessage()` handler)

## 5. Optimizasyon

### 5.1. Memory Optimizasyonu
- [ ] Unity Server memory kullanımını optimize et
- [ ] Container memory limitlerini test et
- [ ] Garbage collection ayarlarını optimize et

### 5.2. Network Optimizasyonu
- [ ] KCP Transport ayarlarını optimize et
```csharp
// Transport ayarları
NoDelay = true
Interval = 20
FastResend = 2
```
- [ ] Paket boyutlarını optimize et
- [ ] Network kullanımını minimize et

## 6. Güvenlik

### 6.1. Network Güvenliği
- [ ] Port güvenliği kontrolleri
- [ ] UDP flood koruması
- [ ] Rate limiting ekle

### 6.2. Match Güvenliği
- [ ] Match token doğrulama sistemi
- [ ] Oyuncu doğrulama sistemi
- [ ] Anti-cheat önlemleri

## Notlar
1. Tüm değişiklikler t2.micro instance limitlerine göre optimize edilmeli
2. Her container max 200MB memory kullanmalı
3. Test aşamasında önce tek server ile başlayıp, sonra diğerlerini ekle
4. Hata durumlarına karşı retry mekanizmaları önemli
5. Client tarafında tüm hata durumları için kullanıcı geri bildirimi sağlanmalı
6. Network bağlantısı koptuğunda otomatik yeniden bağlanma desteği olmalı

## Yapılan Önemli Değişiklikler Açıklaması:

1. **NetworkManager Güncellemesi:**
   - `PvpServerModel` içinde server bilgisi alma sistemi implementte edildi
   - Dinamik port desteği ve transport konfigürasyonu eklendi
   - Bağlantı yönetimi ve timeout sistemi geliştirildi
   - Event yönetimi sistemi iyileştirildi

2. **Matchmaking Sistemi:**
   - Retry mekanizması ile güvenilir matchmaking sistemi
   - Match durumu takibi ve senkronizasyon
   - Hata yönetimi ve timeout kontrolleri
   - Match iptal sistemi

3. **Logging Sistemi:**
   - Detaylı log sistemi implementte edildi
   - Hata durumları için özel log mesajları
   - Network event'leri için log takibi

## Notlar
1. UI kısmı henüz implementte edilmedi (istenildiği gibi)
2. Test senaryoları henüz yazılmadı
3. Güvenlik önlemleri henüz eklenmedi
4. Memory ve network optimizasyonları yapılacak 