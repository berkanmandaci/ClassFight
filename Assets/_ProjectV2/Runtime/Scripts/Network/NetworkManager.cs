using UnityEngine;
using Mirror;
using ProjectV2.Shared;

namespace ProjectV2.Network
{
    public class NetworkManager : Mirror.NetworkManager
    {
        [Header("Game Settings")]
        [SerializeField] private int minPlayers = GameDefines.MatchSettings.MIN_PLAYERS;
        [SerializeField] private int maxPlayers = GameDefines.MatchSettings.MAX_PLAYERS;
        [SerializeField] private string gameSceneName = GameDefines.NetworkSettings.GAME_SCENE_NAME;

        public static new NetworkManager singleton { get; private set; }

        #region Unity Callbacks

        public override void Awake()
        {
            if (singleton != null)
            {
                Destroy(gameObject);
                return;
            }

            singleton = this;
            base.Awake();
        }

        public override void OnDestroy()
        {
            if (singleton == this) singleton = null;
            base.OnDestroy();
        }

        #endregion

        #region Server Callbacks

        public override void OnStartServer()
        {
            Debug.Log("Server started!");
        }

        public override void OnStopServer()
        {
            Debug.Log("Server stopped!");
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            if (numPlayers >= maxPlayers)
            {
                Debug.Log($"Server is full! Max players: {maxPlayers}");
                conn.Disconnect();
                return;
            }

            Debug.Log($"Client connected: {conn.address}");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            Debug.Log($"Client disconnected: {conn.address}");
            base.OnServerDisconnect(conn);
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            Transform startPos = GetStartPosition();
            GameObject player = startPos != null
                ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                : Instantiate(playerPrefab);

            NetworkServer.AddPlayerForConnection(conn, player);
            Debug.Log($"Player added for client {conn.address}");
        }

        public override void OnServerSceneChanged(string sceneName)
        {
            Debug.Log($"Server loaded scene: {sceneName}");
        }

        #endregion

        #region Client Callbacks

        public override void OnClientConnect()
        {
            Debug.Log("Connected to server!");
            base.OnClientConnect();
        }

        public override void OnClientDisconnect()
        {
            Debug.Log("Disconnected from server!");
            base.OnClientDisconnect();
        }

        public override void OnStartClient()
        {
            Debug.Log("Client started!");
        }

        public override void OnStopClient()
        {
            Debug.Log("Client stopped!");
        }

        public override void OnClientSceneChanged()
        {
            Debug.Log("Client scene changed!");
            base.OnClientSceneChanged();
        }

        #endregion

        #region Start & Stop

        public void StartHost()
        {
            Debug.Log("Starting host...");
            base.StartHost();
        }

        public void StartServer()
        {
            Debug.Log("Starting server...");
            base.StartServer();
        }

        public void StartClient()
        {
            Debug.Log("Starting client...");
            base.StartClient();
        }

        public void StopHost()
        {
            Debug.Log("Stopping host...");
            base.StopHost();
        }

        public void StopServer()
        {
            Debug.Log("Stopping server...");
            base.StopServer();
        }

        public void StopClient()
        {
            Debug.Log("Stopping client...");
            base.StopClient();
        }

        #endregion
    }
} 