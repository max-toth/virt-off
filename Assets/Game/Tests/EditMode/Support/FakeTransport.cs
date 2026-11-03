using System;
using System.Collections.Generic;
using Game.Scripts.Net;
using Game.Scripts.Net.Packets;

namespace Game.Tests.EditMode.Support
{
    /// <summary>Транспорт в памяти: запоминает отправленное, входящие пакеты подаются через Receive.</summary>
    public sealed class FakeTransport : INetTransport
    {
        public const string DefaultId = "me";

        public FakeTransport() => Router = new PacketRouter(this);

        /// <summary>Маршрутизатор входящих пакетов этого транспорта.</summary>
        public PacketRouter Router { get; }

        public string LocalId { get; private set; }
        public bool IsConnected { get; private set; }

        public List<byte[]> Sent { get; } = new();

        public byte[] LastSent => Sent.Count > 0 ? Sent[^1] : null;

        public event Action<string, byte[]> PacketReceived;
        public event Action IdAssigned;

        public static FakeTransport Connected(string id = DefaultId)
        {
            var transport = new FakeTransport();
            transport.AssignId(id);
            return transport;
        }

        public void AssignId(string id)
        {
            LocalId = id;
            IsConnected = true;
            IdAssigned?.Invoke();
        }

        public void Receive(byte[] data) => PacketReceived?.Invoke(LocalId, data);

        // Копия: отправитель переиспользует буфер.
        public void Send(byte[] data, int length) => Sent.Add(data.AsSpan(0, length).ToArray());
    }
}
