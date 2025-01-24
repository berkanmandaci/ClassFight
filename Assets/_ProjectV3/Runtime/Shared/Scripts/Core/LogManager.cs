using UnityEngine;
using System;
using System.IO;

namespace ProjectV3.Shared.Core
{
    public class LogManager : MonoBehaviour
    {
        [SerializeField] private bool isServer;
        private string logFilePath;
        private StreamWriter logWriter;

        public void Initialize(bool isServerInstance)
        {
            isServer = isServerInstance;
            
            // Log dosyası için klasör oluştur
            string logDirectory = Path.Combine(Application.dataPath, "../Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            
            // Log dosyası adını oluştur
            string fileName = $"{(isServer ? "Server" : "Client")}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
            logFilePath = Path.Combine(logDirectory, fileName);

            // Log dosyasını oluştur
            logWriter = new StreamWriter(logFilePath, true);
            logWriter.AutoFlush = true;

            // Unity'nin log sistemine abone ol
            Application.logMessageReceived += HandleLog;

            WriteLog($"=== {(isServer ? "SERVER" : "CLIENT")} STARTED ===");
            WriteLog($"Application Path: {Application.dataPath}");
            WriteLog($"Log File: {logFilePath}");
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (logWriter == null) return;

            string prefix = type switch
            {
                LogType.Error => "[ERROR]",
                LogType.Assert => "[ASSERT]",
                LogType.Warning => "[WARNING]",
                LogType.Log => "[INFO]",
                LogType.Exception => "[EXCEPTION]",
                _ => "[UNKNOWN]"
            };

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] {prefix} {logString}";

            // Stack trace'i sadece hata ve exception'lar için ekle
            if ((type == LogType.Error || type == LogType.Exception) && !string.IsNullOrEmpty(stackTrace))
            {
                logMessage += $"\nStack Trace:\n{stackTrace}";
            }

            logWriter.WriteLine(logMessage);
        }

        private void WriteLog(string message)
        {
            if (logWriter != null)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                logWriter.WriteLine($"[{timestamp}] [SYSTEM] {message}");
            }
        }

        private void OnDestroy()
        {
            if (logWriter != null)
            {
                WriteLog($"=== {(isServer ? "SERVER" : "CLIENT")} STOPPED ===");
                
                Application.logMessageReceived -= HandleLog;
                logWriter.Close();
                logWriter.Dispose();
                logWriter = null;
            }
        }
    }
} 