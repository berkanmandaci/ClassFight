using UnityEngine;
using Mirror;
using System.Collections;

namespace ProjectV3.Shared.Combat
{
    public class ArcherCharacterController : NetworkBehaviour, ICharacterController
    {
        [Header("Ok Ayarları")]
        [SerializeField] private GameObject _arrowPrefab;
        [SerializeField] private Transform _arrowSpawnPoint;
        [SerializeField] private float _arrowSpeed = 20f;
        [SerializeField] private float _arrowLifetime = 3f;

        private float _lastAttackTime;
        private readonly float _attackCooldown = 1f;

        public void OnMove(Vector2 moveInput)
        {
            // Base controller handles movement
        }

        public void OnAim(Vector2 aimInput)
        {
            // Base controller handles aiming
        }

        public void OnAttack()
        {
            if (Time.time - _lastAttackTime < _attackCooldown)
                return;

            _lastAttackTime = Time.time;
            CmdShootArrow(transform.position, transform.rotation);
            Debug.Log("OnAttack çağrıldı!");
        }

        [Command]
        private void CmdShootArrow(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            Debug.Log("CmdShootArrow çağrıldı!");
            // Server'da ok oluştur
            GameObject arrow = Instantiate(_arrowPrefab, _arrowSpawnPoint.position, spawnRotation);
            
            // Ok'a hız ver
            Rigidbody arrowRb = arrow.GetComponent<Rigidbody>();
            if (arrowRb != null)
            {
                arrowRb.linearVelocity = spawnRotation * Vector3.forward * _arrowSpeed;
            }

            // Network üzerinde spawn et
            NetworkServer.Spawn(arrow);

            // Belirli süre sonra ok'u yok et
            StartCoroutine(DestroyArrowAfterDelay(arrow));
            
            // Tüm clientlara ok atışını bildir
            RpcOnArrowShot();
        }

        private System.Collections.IEnumerator DestroyArrowAfterDelay(GameObject arrow)
        {
            yield return new WaitForSeconds(_arrowLifetime);
            if (arrow != null)
            {
                NetworkServer.Destroy(arrow);
            }
        }

        [ClientRpc]
        private void RpcOnArrowShot()
        {
            Debug.Log("Ok atış efekti oynatıldı!");
        }

        public void OnDash()
        {
            // Base controller handles dash
        }

        public void OnDodge()
        {
            // Base controller handles dodge
        }
    }
}
