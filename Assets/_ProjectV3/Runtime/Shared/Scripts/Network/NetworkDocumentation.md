# ProjectV3 Network Mimarisi Dokümantasyonu

## 1. Matchmaking Süreci

### 1.1. Nakama Matchmaking
- Kullanıcı oyun modunu seçtikten sonra `MatchmakingModel.JoinMatchmaking()` çağrılır
- Nakama'ya matchmaking isteği gönderilir (`Socket.AddMatchmakerAsync()`)
- Matchmaking kriterleri:
  - Oyun modu (FreeForAll/TeamDeathmatch)
  - Bölge (Region)
  - Min/Max oyuncu sayısı

```csharp
public async UniTask<IMatchmakerTicket> JoinMatchmaking(MatchmakingData data)
{
    var query = "*";
    var minCount = 2;
    var maxCount = 2;
    
    var stringProperties = new Dictionary<string, string>
    {
        { "region", data.Region }
    };

    var numericProperties = new Dictionary<string, double>
    {
        { "gameMode", (double)data.GameMode }
    };

    return await Socket.AddMatchmakerAsync(query, minCount, maxCount, stringProperties, numericProperties);
}
```

### 1.2. Match Bilgilerinin İletilmesi
- Match bulunduğunda `OnMatchmakerMatched` event'i tetiklenir
- `PvpServerModel.OnMatchFound()` çağrılır
- Server bilgileri (IP, port) alınır
- Match ID ve kullanıcı bilgileri server'a iletilir

### 1.3. Unity Server Başlatma
- `ServerBootstrapper` Unity server'ı başlatır
- `ProjectNetworkManager` üzerinden server ayarları yapılandırılır
- Match bilgileri `RegisterMatchData()` ile server'a kaydedilir

## 2. Network Protokolü ve Bağlantı

### 2.1. Transport Ayarları
Mirror'ın KCP (UDP tabanlı) transport'u kullanılmaktadır:

```csharp
// Transport Konfigürasyonu
maxMessageSize = 16384    // 16 KB
messageQueueSize = 10000
noDelay = 1              // Nagle algoritması devre dışı
sendTimeout = 5000       // 5 saniye
receiveTimeout = 5000    // 5 saniye
```

### 2.2. Bağlantı Doğrulama
- Match ID ve kullanıcı bilgileri `MatchInfoMessage` ile server'a gönderilir
- Server tarafında `RegisterMatchData()` ile match verileri doğrulanır
- Oyun modu ve takım bilgileri kontrol edilir

```csharp
[Server]
public void RegisterMatchData(int connectionId, IMatchmakerMatched matchData)
{
    _pendingMatches[connectionId] = matchData;
    GameModeType gameMode = GameModeType.FreeForAll;
    
    if (matchData.Self?.NumericProperties?.TryGetValue("gameMode", out double gameModeValue) == true)
    {
        gameMode = (GameModeType)((int)gameModeValue);
    }
    
    CombatArenaModel.Instance.SetGameMode(gameMode);
}
```

## 3. Hata Yönetimi

### 3.1. Timeout Süreleri
```csharp
connectionTimeout = 10f   // Bağlantı timeout
sendTimeout = 5000       // Gönderme timeout
receiveTimeout = 5000    // Alma timeout
DISCONNECT_TIMEOUT = 10f // Bağlantı kopma timeout
```

### 3.2. Yeniden Bağlanma Mekanizması
- `CleanupPreviousConnection()` ile eski bağlantı temizlenir
- Transport ayarları yeniden yapılandırılır
- Event listener'lar yeniden kaydedilir

```csharp
private async UniTask ConnectToGameServer(ServerInfo serverInfo)
{
    await CleanupPreviousConnection();
    ConfigureTransport(serverInfo);
    SubscribeToNetworkEvents();
    
    networkManager.StartClient();
    await WaitForConnection();
}
```

## 4. Önerilen İyileştirmeler

### 4.1. Güvenlik İyileştirmeleri
```csharp
public class ServerInfo
{
    public string host;
    public int port;
    public string authToken;        // Güvenli bağlantı için token
    public string matchSignature;   // Match doğrulama için imza
}
```

### 4.2. Gelişmiş Yeniden Bağlanma
```csharp
private async UniTask ReconnectWithRetry(int maxAttempts = 3)
{
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await ConnectToGameServer(lastServerInfo);
            return;
        }
        catch (Exception e)
        {
            if (attempt == maxAttempts) throw;
            await UniTask.Delay(TimeSpan.FromSeconds(attempt * 2));
        }
    }
}
```

### 4.3. Match State Yönetimi
```csharp
public class MatchState
{
    public string matchId;
    public GameModeType gameMode;
    public Dictionary<int, PlayerState> players;
    public float matchTime;
    public bool isActive;
}
```

## 5. Önemli Notlar

- Nakama ve Mirror entegrasyonu sağlam bir multiplayer altyapısı sağlar
- Hata yönetimi ve yeniden bağlanma senaryoları düşünülmüştür
- Transport ayarları optimize edilmiştir
- Match state senkronizasyonu için gerekli altyapı mevcuttur

## 6. Geliştirme Tavsiyeleri

1. **Güvenlik**:
   - Server bilgilerinin güvenli iletimi
   - Match doğrulama mekanizmaları
   - Anti-cheat önlemleri

2. **Performans**:
   - Message batching optimizasyonu
   - State senkronizasyon optimizasyonu
   - Network kullanımı optimizasyonu

3. **Kullanıcı Deneyimi**:
   - Bağlantı durumu göstergeleri
   - Yeniden bağlanma UI/UX
   - Hata mesajları ve kullanıcı bildirimleri 

## 7. Combat Sistemi ve Maç Akışı

### 7.1. Maç Başlangıcı
- Tüm oyuncular hazır olduğunda `CombatArenaModel.CheckAllPlayersReady()` çağrılır
- Maç başlangıç geri sayımı başlatılır (`StartMatchCountdown()`)
- İlk round başlatılır (`StartNextRound()`)

```csharp
[Server]
private void CheckAllPlayersReady()
{
    if (_isCountdownStarted || _isMatchStarted) return;

    int totalPlayers = _combatUsers.Count;
    int readyPlayers = _readyPlayers.Count;

    if (totalPlayers > 0 && totalPlayers == readyPlayers)
    {
        _isCountdownStarted = true;
        StartMatchCountdown();
    }
}
```

### 7.2. Round Yönetimi

#### Round Başlangıcı
- Her round başlangıcında:
  - Round istatistikleri sıfırlanır (`ResetRoundStats()`)
  - Tüm oyuncular canlandırılır (`ReviveAllPlayers()`)
  - Round geri sayımı başlatılır (`StartRoundCountdown()`)

```csharp
[Server]
private async Task StartNextRound()
{
    if (_currentRound >= _maxRounds)
    {
        EndMatch();
        return;
    }

    _currentRound++;
    ResetRoundStats();
    ReviveAllPlayers();
    await StartRoundCountdown();
}
```

#### Round Bitişi
- Round şu durumlarda biter:
  - FFA modunda son oyuncu hayatta kalır
  - Tüm oyuncular ölür
  - Round süresi dolar
- Round sonu işlemleri:
  - En çok hasar veren ve en çok öldürme yapan oyuncular belirlenir
  - Round puanları dağıtılır
  - Toplam skorlar güncellenir

```csharp
[Server]
private async Task EndRound()
{
    _isRoundActive = false;
    
    // En çok hasarı ve kill'i yapanları bul
    var mostDamagePlayer = _playerRoundStats.OrderByDescending(x => x.Value.damageDealt).First();
    var mostKillsPlayer = _playerRoundStats.OrderByDescending(x => x.Value.kills).First();

    // Puanları dağıt
    mostDamagePlayer.Value.roundScore += 5;
    mostKillsPlayer.Value.roundScore += 5;

    // Round puanlarını toplam skora ekle
    foreach (var stat in _playerRoundStats)
    {
        _playerTotalScores[stat.Key] += stat.Value.roundScore;
    }
}
```

### 7.3. Maç Sonu

#### Maç Bitişi Koşulları
- Maksimum round sayısına ulaşıldığında
- Bir takım/oyuncu gereken puana ulaştığında
- Tüm oyuncular bağlantıyı kestiğinde

#### Kazanan Belirleme
- Oyuncular toplam puanlarına göre sıralanır
- İlk üç oyuncu belirlenir (1., 2. ve 3.)
- Sonuçlar tüm clientlara bildirilir

```csharp
[Server]
private void EndMatch()
{
    var sortedScores = _playerTotalScores.OrderByDescending(x => x.Value).ToList();

    int winnerId = sortedScores[0].Key;
    int secondId = sortedScores.Count > 1 ? sortedScores[1].Key : -1;
    int thirdId = sortedScores.Count > 2 ? sortedScores[2].Key : -1;

    RpcEndMatch(winnerId, secondId, thirdId);
}
```

### 7.4. Combat İstatistikleri

#### Oyuncu İstatistikleri
- Hasar verme/alma
- Öldürme/ölme sayısı
- Asist sayısı
- Karakter bazlı istatistikler (Archer/Warrior/Tank)

```csharp
public class CombatUserVo : NetworkBehaviour
{
    [SyncVar] private float _maxHealth = 100f;
    [SyncVar] private float _maxShield = 100f;
    [SyncVar] private float _currentHealth;
    [SyncVar] private float _shieldAmount;
    [SyncVar] private bool _isDead;
    
    [SyncVar] private int _totalDamageDealt;
    [SyncVar] private int _totalDamageTaken;
    [SyncVar] private int _kills;
    [SyncVar] private int _deaths;
    [SyncVar] private int _assists;
}
```

### 7.5. Senkronizasyon

#### State Senkronizasyonu
- Combat verileri `SyncVar` ile otomatik senkronize edilir
- Önemli olaylar `ClientRpc` ile tüm clientlara bildirilir
- Round ve maç durumları `NetworkBehaviour` üzerinden yönetilir

#### Event Sistemi
- Round başlangıç/bitiş eventleri
- Oyuncu ölüm/yeniden doğma eventleri
- Maç başlangıç/bitiş eventleri
- Skor güncelleme eventleri

```csharp
public event System.Action<int, float> OnRoundCountdownStarted;
public event System.Action<float> OnRoundCountdownUpdated;
public event System.Action<int> OnRoundStarted;
public event System.Action<int, Dictionary<int, RoundStats>, Dictionary<int, int>> OnRoundEnded;
public event System.Action<int, int, int> OnMatchEnded;
``` 