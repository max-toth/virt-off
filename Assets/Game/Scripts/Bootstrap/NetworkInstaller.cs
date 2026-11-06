using Game.Scripts.Combat;
using Game.Scripts.Combat.Props;
using Game.Scripts.Net;
using Game.Scripts.Net.Packets;
using Game.Scripts.Player.Sync;
using Game.Scripts.Voice;
using VContainer;
using VContainer.Unity;

namespace Game.Scripts.Bootstrap
{
    /// <summary>Протокол поверх INetTransport: сам транспорт регистрирует вызывающий (сокет в игре, фейк в тестах).</summary>
    public sealed class NetworkInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<PacketRouter>(Lifetime.Singleton);
            builder.Register<PacketWriter>(Lifetime.Singleton);

            builder.RegisterEntryPoint<Lobby>().AsSelf();
            builder.RegisterEntryPoint<CombatNetwork>().AsSelf();
            builder.RegisterEntryPoint<PositionNetwork>().AsSelf();
            builder.RegisterEntryPoint<VoiceNetwork>().AsSelf();
            builder.RegisterEntryPoint<PlayerNameNetwork>().AsSelf();
            builder.Register<NetPropSender>(Lifetime.Singleton);
        }
    }
}
