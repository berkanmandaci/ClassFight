using UnityEngine;

namespace ProjectV2.Shared
{
    public static class GameDefines
    {
        // Compilation symbols
        public const string SERVER_BUILD = "SERVER_BUILD";
        public const string CLIENT_BUILD = "CLIENT_BUILD";

        // Network settings
        public static class NetworkSettings
        {
            public const string DEFAULT_IP = "127.0.0.1";
            public const ushort DEFAULT_PORT = 27016;
            public const string GAME_SCENE_NAME = "GameScene";
            public const string LOBBY_NAME = "MainLobby";
        }

        // Match settings
        public static class MatchSettings
        {
            public const int MIN_PLAYERS = 2;
            public const int MAX_PLAYERS = 6;
            public const float MATCH_TIMEOUT = 300f; // 5 minutes
        }
    }

    // Shared enums
    public enum GameState
    {
        None,
        WaitingForPlayers,
        Starting,
        InProgress,
        Ending,
        Finished
    }

    public enum PlayerState
    {
        None,
        Connected,
        Ready,
        Playing,
        Dead,
        Disconnected
    }
} 