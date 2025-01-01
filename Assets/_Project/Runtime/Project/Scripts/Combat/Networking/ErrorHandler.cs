using System;
using UnityEngine;
using _Project.Runtime.Core.Extensions.Singleton;

namespace _Project.Runtime.Project.Scripts.Combat.Networking
{
    public class ErrorHandler : Singleton<ErrorHandler>
    {
        // Hata tipleri
        public enum ErrorType
        {
            Network,
            Matchmaking,
            TeamBalance,
            PlayerSpawn,
            GameState,
            General
        }

        // Hata event'i
        public delegate void ErrorOccurredHandler(ErrorType type, string message, Exception exception = null);
        public event ErrorOccurredHandler OnErrorOccurred;

        // Hata loglama
        public void LogError(ErrorType type, string message, Exception exception = null)
        {
            Debug.LogError($"[{type}] {message}");
            if (exception != null)
            {
                Debug.LogException(exception);
            }

            OnErrorOccurred?.Invoke(type, message, exception);
        }

        // Hata yönetimi
        public void HandleNetworkError(string message, Exception exception = null)
        {
            LogError(ErrorType.Network, message, exception);
            // Network bağlantısını yeniden deneme veya oyuncuyu lobby'e gönderme gibi işlemler
        }

        public void HandleMatchmakingError(string message, Exception exception = null)
        {
            LogError(ErrorType.Matchmaking, message, exception);
            // Matchmaking'i yeniden başlatma veya alternatif sunucu arama gibi işlemler
        }

        public void HandleTeamBalanceError(string message, Exception exception = null)
        {
            LogError(ErrorType.TeamBalance, message, exception);
            // Takım dengesini düzeltme veya oyuncuları yeniden dağıtma gibi işlemler
        }

        public void HandlePlayerSpawnError(string message, Exception exception = null)
        {
            LogError(ErrorType.PlayerSpawn, message, exception);
            // Spawn noktasını değiştirme veya yeniden spawn etme gibi işlemler
        }

        public void HandleGameStateError(string message, Exception exception = null)
        {
            LogError(ErrorType.GameState, message, exception);
            // Oyun durumunu sıfırlama veya güvenli bir duruma getirme gibi işlemler
        }

        // Genel hata yakalayıcı
        public void HandleException(Exception exception)
        {
            LogError(ErrorType.General, "Beklenmeyen bir hata oluştu", exception);
            // Genel hata kurtarma işlemleri
        }
    }
} 