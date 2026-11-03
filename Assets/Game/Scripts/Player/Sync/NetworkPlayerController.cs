using Game.Scripts.Net;
using UnityEngine;

namespace Game.Scripts.Player.Sync
{
    [RequireComponent(typeof(PlayerController))]
    public class NetworkPlayerController : MonoBehaviour
    {
        private PlayerController _player;
        private INetTransport _manager;

        private void Awake() => _player = GetComponent<PlayerController>();

        private void Start()
        {
            if (GameNet.TryGetConnected(out _))
            {
                EnableControls();
                return;
            }

            _manager = GameNet.Transport;
            if (_manager != null) _manager.IdAssigned += OnIdAssigned;
        }

        private void OnDestroy() => Unsubscribe();

        private void OnIdAssigned()
        {
            Unsubscribe();
            EnableControls();
        }

        private void Unsubscribe()
        {
            if (_manager != null) _manager.IdAssigned -= OnIdAssigned;
            _manager = null;
        }

        private void EnableControls() => _player.enabled = true;
    }
}
