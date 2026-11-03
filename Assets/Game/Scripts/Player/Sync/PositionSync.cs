using System;
using Game.Scripts.Net;
using Game.Scripts.Player.Sync.Models;
using UnityEngine;
using VContainer;

namespace Game.Scripts.Player.Sync
{
    [RequireComponent(typeof(PlayerController))]
    public class PositionSync : MonoBehaviour
    {
        [SerializeField] private float sendInterval = 0.05f;
        [Tooltip("Стоя на месте позиция отправляется не чаще: этого хватает, чтобы другие видели игрока в лобби")]
        [SerializeField] private float keepAliveInterval = 1f;
        [Tooltip("Модели для чужих игроков (выбирается по ID). Если пусто — копируется модель своего игрока")]
        [SerializeField] private GameObject[] avatarPrefabs = Array.Empty<GameObject>();
        [Tooltip("Масштаб моделей из avatarPrefabs")]
        [SerializeField] private float avatarScale = 0.8f;
        [Tooltip("Скорость сглаживания позиции чужих игроков между пакетами")]
        [SerializeField] private float smoothing = 15f;

        private PlayerController _player;
        private PositionNetwork _network;
        private Lobby _lobby;
        private RemotePlayers _remotePlayers;
        private RemoteAvatarFactory _avatarFactory;
        private PositionSendFilter _sendFilter;
        private float _sendTimer;

        [Inject]
        public void Construct(PositionNetwork network, Lobby lobby, RemotePlayers remotePlayers)
        {
            _network = network;
            _lobby = lobby;
            _remotePlayers = remotePlayers;
        }

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _sendFilter = new PositionSendFilter(keepAliveInterval);
        }

        private void Start()
        {
            _avatarFactory = new RemoteAvatarFactory(transform, avatarPrefabs, avatarScale, _remotePlayers);
            _network.Received += OnPositionReceived;
            _lobby.PlayerLeft += OnPlayerLeft;
        }

        private void OnDestroy()
        {
            if (_network != null) _network.Received -= OnPositionReceived;
            if (_lobby != null) _lobby.PlayerLeft -= OnPlayerLeft;
            _avatarFactory?.Dispose();
            _remotePlayers?.Clear();
        }

        private void Update()
        {
            _remotePlayers.SmoothAll(1f - Mathf.Exp(-smoothing * Time.deltaTime));
            SendLocalPosition();
        }

        private void SendLocalPosition()
        {
            if (!_network.IsConnected) return;

            _sendTimer += Time.deltaTime;
            if (_sendTimer < sendInterval) return;

            _sendTimer = 0f;
            var position = transform.position;
            var yaw = transform.eulerAngles.y;
            if (_sendFilter.ShouldSend(position, yaw, _player.IsCrouching, Time.unscaledTime))
                _network.Send(position, yaw, _player.IsCrouching);
        }

        private void OnPositionReceived(PositionMessage message)
        {
            if (!_remotePlayers.TryGet(message.Sender, out var remote))
            {
                remote = _avatarFactory.Create(message.Sender, message.Position, message.Yaw);
                _remotePlayers.Add(message.Sender, remote);
            }

            remote.SetTarget(message.Position, message.Yaw, message.Crouching);
        }

        private void OnPlayerLeft(string id)
        {
            if (_remotePlayers.Remove(id, out var remote) && remote.Root != null) Destroy(remote.Root.gameObject);
        }
    }
}
