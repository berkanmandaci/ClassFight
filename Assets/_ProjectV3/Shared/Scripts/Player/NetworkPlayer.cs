using UnityEngine;
using Mirror;

namespace _ProjectV3.Shared.Scripts.Player
{
    public class NetworkPlayer : NetworkBehaviour
    {
        [Header("Player Settings")]
        [SerializeField] private float moveSpeed = 5f;
        
        [Header("Components")]
        [SerializeField] private Camera playerCamera;
        
        [SyncVar(hook = nameof(OnNameChanged))]
        private string playerName = "Player";

        [SyncVar]
        private Vector3 serverPosition;
        
        private void OnNameChanged(string oldName, string newName)
        {
            Debug.Log($"Player ismi değişti: {oldName} -> {newName}");
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            
            // Local player için kamerayı aktifleştir
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
            }
            
            CmdSetPlayerName($"Player_{Random.Range(1000, 9999)}");
        }

        private void Update()
        {
            if (!isLocalPlayer) 
            {
                // Local player değilse, server pozisyonuna yumuşak geçiş yap
                transform.position = Vector3.Lerp(transform.position, serverPosition, Time.deltaTime * 10f);
                return;
            }
            
            HandleMovement();
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;
            
            if (movement != Vector3.zero)
            {
                // Hareket ve rotasyon
                transform.Translate(movement * (moveSpeed * Time.deltaTime), Space.World);
                transform.forward = movement;
                
                // Server'a pozisyon güncelleme gönder
                CmdUpdatePosition(transform.position);
            }
        }

        [Command]
        private void CmdSetPlayerName(string newName)
        {
            playerName = newName;
            RpcOnPlayerSpawned();
        }

        [Command]
        private void CmdUpdatePosition(Vector3 newPosition)
        {
            // Server'da pozisyonu güncelle
            serverPosition = newPosition;
            RpcUpdatePosition(newPosition);
        }

        [ClientRpc]
        private void RpcUpdatePosition(Vector3 newPosition)
        {
            // Diğer clientlarda pozisyonu güncelle
            if (!isLocalPlayer)
            {
                serverPosition = newPosition;
            }
        }

        [ClientRpc]
        private void RpcOnPlayerSpawned()
        {
            Debug.Log($"Player spawned: {playerName}");
        }
    }
} 