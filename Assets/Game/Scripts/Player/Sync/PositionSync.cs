using System;
using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using UnityEngine;

namespace Game.Scripts.Player.Sync
{
    [RequireComponent(typeof(PlayerController))]
    public class PositionSync : MonoBehaviour
    {
        [SerializeField] private float sendInterval = 0.05f;
        [Tooltip("Модели для чужих игроков (выбирается по ID). Если пусто — копируется модель своего игрока")]
        [SerializeField] private GameObject[] avatarPrefabs = Array.Empty<GameObject>();
        [Tooltip("Масштаб моделей из avatarPrefabs")]
        [SerializeField] private float avatarScale = 0.8f;
        [Tooltip("Скорость сглаживания позиции чужих игроков между пакетами")]
        [SerializeField] private float smoothing = 15f;

        private PlayerController _player;
        private RemoteAvatarFactory _avatarFactory;
        private PacketSubscription _subscription;
        private float _sendTimer;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _subscription = new PacketSubscription(OnPacket);
        }

        private void Start()
        {
            _avatarFactory = new RemoteAvatarFactory(transform, avatarPrefabs, avatarScale);
            _subscription.Bind(GameNet.Instance);
            Lobby.PlayerLeft += OnPlayerLeft;
        }

        private void OnDestroy()
        {
            _subscription?.Unbind();
            Lobby.PlayerLeft -= OnPlayerLeft;
            _avatarFactory?.Dispose();
            RemotePlayers.Clear();
        }

        private void Update()
        {
            RemotePlayers.SmoothAll(1f - Mathf.Exp(-smoothing * Time.deltaTime));
            SendLocalPosition();
        }

        private void SendLocalPosition()
        {
            if (!GameNet.TryGetConnected(out _)) return;

            _sendTimer += Time.deltaTime;
            if (_sendTimer < sendInterval) return;

            _sendTimer = 0f;
            PositionNetwork.Send(transform.position, transform.eulerAngles.y, _player.IsCrouching);
        }

        private void OnPacket(string localId, byte[] data)
        {
            if (data.Length == 0 || data[0] != PacketType.Position) return;
            if (!PositionNetwork.TryRead(data, out var message) || message.Sender == localId) return;

            if (!RemotePlayers.TryGet(message.Sender, out var remote))
            {
                remote = _avatarFactory.Create(message.Sender, message.Position, message.Yaw);
                RemotePlayers.Add(message.Sender, remote);
            }

            remote.SetTarget(message.Position, message.Yaw, message.Crouching);
        }

        private static void OnPlayerLeft(string id)
        {
            if (RemotePlayers.Remove(id, out var remote) && remote.Root != null) Destroy(remote.Root.gameObject);
        }
    }
}
