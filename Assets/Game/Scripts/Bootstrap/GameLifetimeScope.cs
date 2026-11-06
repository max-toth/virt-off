using Game.Scripts.Audio;
using Game.Scripts.Combat;
using Game.Scripts.Combat.Props;
using Game.Scripts.Common;
using Game.Scripts.Controls;
using Game.Scripts.Net;
using Game.Scripts.Net.Config;
using Game.Scripts.Player;
using Game.Scripts.Player.Sync;
using Game.Scripts.Settings;
using Game.Scripts.UI.Hud;
using Game.Scripts.UI.Menu;
using Game.Scripts.Voice;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Scripts.Bootstrap
{
    /// <summary>Точка сборки игровой сцены. Компоненты сцены получают зависимости до своего Awake:
    /// LifetimeScope выполняется раньше остальных скриптов.</summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IClock, UnityClock>(Lifetime.Singleton);
            builder.Register<GameSettingsStorage>(Lifetime.Singleton);
            builder.Register<GameSettings>(Lifetime.Singleton);
            builder.Register<GameplayInput>(Lifetime.Singleton);
            builder.Register<PlayerInputReader>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameSounds>().AsSelf();
            builder.Register<CombatSounds>(Lifetime.Singleton);

            RegisterStartup(builder);
            builder.Register<NetManager>(Lifetime.Singleton).AsSelf().As<INetTransport>();
            new NetworkInstaller().Install(builder);
            builder.RegisterEntryPoint<NetworkSession>().AsSelf();
            builder.RegisterEntryPoint<PlayerNames>().AsSelf();
            builder.RegisterEntryPoint<NicknamePublisher>();

            builder.Register<RemotePlayers>(Lifetime.Singleton);
            builder.RegisterEntryPoint<RemoteCombatPresenter>();
            builder.RegisterEntryPoint<NetPropRegistry>().AsSelf();
            builder.RegisterEntryPoint<NetPropReceiver>();
            builder.RegisterEntryPoint<PropManifestPublisher>();

            builder.RegisterComponentInHierarchy<PlayerController>();
            builder.RegisterComponentInHierarchy<PlayerCombat>();
            builder.RegisterComponentInHierarchy<PlayerPropPusher>();
            builder.RegisterComponentInHierarchy<PositionSync>();
            builder.RegisterComponentInHierarchy<VoiceManager>();
            builder.RegisterComponentInHierarchy<VoiceReceiver>();

            RegisterIfPresent<GameMenu>(builder);
            RegisterIfPresent<HudPresenter>(builder);
            RegisterIfPresent<VoiceDebugPresenter>(builder);
        }

        // Сервер из -server подключается сразу; иначе адрес выбирают в стартовом меню, если оно есть в сцене.
        private static void RegisterStartup(IContainerBuilder builder)
        {
            var hasCommandLineServer = ServerEndpoint.TryGetFromCommandLine(out var commandLineServer);
            var hasMenu = FindAnyObjectByType<GameMenu>(FindObjectsInactive.Include) != null;
            builder.RegisterInstance(hasCommandLineServer ? commandLineServer : ServerEndpoint.FromConfig());
            builder.RegisterInstance(new StartupOptions(hasMenu && !hasCommandLineServer));
        }

        // GameUI есть не в каждой сцене (в Demo 1 его нет), а от интерфейса ничего не зависит.
        private static void RegisterIfPresent<T>(IContainerBuilder builder) where T : MonoBehaviour
        {
            var component = FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (component != null) builder.RegisterComponent(component);
        }
    }
}
