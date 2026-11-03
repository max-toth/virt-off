using System;

namespace Game.Scripts.Net
{
    /// <summary>Канал до сервера. События — только в главном потоке.</summary>
    public interface INetTransport
    {
        /// <summary>Id, выданный сервером; null до IdAssigned.</summary>
        string LocalId { get; }

        bool IsConnected { get; }

        /// <summary>Входящий пакет: id своего клиента и данные; data[0] — PacketType.</summary>
        event Action<string, byte[]> PacketReceived;

        /// <summary>Сервер выдал id; с этого момента IsConnected = true.</summary>
        event Action IdAssigned;

        /// <summary>Отправляет первые length байт; data после возврата можно переиспользовать.</summary>
        void Send(byte[] data, int length);
    }
}
