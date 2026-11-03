# Virt-Off — Virtual Office with Voice Chat

A Unity-based virtual office environment with built-in voice communication. Features a custom UDP networking layer with Opus audio compression (Concentus) for low-latency voice chat, designed as a headless server + standalone client architecture.

## Tech Stack

- **Unity** 6 (6000.0.x) with URP
- **Networking:** Custom UDP socket-based (`NetManager`) — lightweight, no NGO dependency
- **Voice:** Opus codec via [Concentus](https://github.com/lostromb/concentus) (16kHz, 24kbps, 40ms frames)
- **Input:** Unity Input System
- **Build targets:** macOS standalone (client), headless server

## Architecture

```
┌──────────────────┐         UDP          ┌──────────────────┐
│  Server (headless) │ ◄──────────────────► │  Client (standalone) │
│  Port 7777         │    voice + control  │  Player + Mic + Audio │
└──────────────────┘                      └──────────────────┘
```

- **Server:** Headless build (`-batchmode -nographics`). Detects headless mode automatically via `GraphicsDeviceType.Null`. Relays voice packets between clients.
- **Client:** Standalone macOS app. Connects to server IP (default `127.0.0.1:7777`, overridable via `-server <ip>` argument).

## Scenes

| Scene | Description |
|-------|-------------|
| `Demo 3 - Office Set 1` | Main scene (the only one in Build Settings). Office with 6 workplaces, created via `Tools/Create Demo 3`. Opens automatically when the Editor starts (`Tools/Open Main Scene`, Ctrl+Shift+M) |
| `SampleScene`, `Demo 1/2` | Legacy / template scenes, not included in the build |

## Controls

| Key | Action |
|-----|--------|
| WASD | Move |
| Shift | Run |
| Space | Jump |
| Ctrl (hold) | Crouch |
| Mouse | Look |
| LMB | Punch (throw when holding an item) |
| RMB | Slap |
| E | Pick up / put down an item (chairs, trash bins…) |
| Esc | Menu (lobby, respawn, settings, quit) |
| M | Toggle microphone mute |

A waveform visualizer and connection status are displayed in the HUD (Unity GUI).

## Building

### Server
```
Build Settings → Server Build (Player Settings → Dedicated Server)
```

### Client
Standard standalone build (macOS standalone target).

## Project Structure

Own content lives in `Assets/Game/`, third-party packs in `Assets/ThirdParty/` (do not edit them — put your own prefabs into `Game/Prefabs`).

```
Assets/
├── Game/                          — project content
│   ├── Scripts/
│   │   ├── Net/
│   │   │   ├── AppSetup.cs            — Bootstraps networking before the first scene loads
│   │   │   ├── NetComponent.cs        — Owns NetManager, connects to server (ServerConfig / -server arg)
│   │   │   ├── GameNet.cs             — Global access point to the active NetManager
│   │   │   ├── NetManager.cs          — Custom UDP client
│   │   │   ├── PacketType.cs          — Packet ids used by combat and props
│   │   │   ├── PacketWriter.cs        — Allocation-free packet serialization (reused buffer)
│   │   │   ├── PacketReader.cs        — Bounds-checked packet parsing
│   │   │   ├── PacketSubscription.cs  — Subscribes a handler to incoming packets once
│   │   │   ├── MainThreadQueue.cs     — Dispatches network callbacks onto the Unity main thread
│   │   │   ├── ServerConfig.cs        — ScriptableObject with server address/port
│   │   │   ├── Lobby.cs               — Lobby state
│   │   │   └── IdCodec.cs             — Network id encoding
│   │   ├── Player/
│   │   │   ├── PlayerController.cs        — FPS movement (WASD + mouse look)
│   │   │   ├── NetworkPlayerController.cs — Enables PlayerController after net connect
│   │   │   ├── PositionSync.cs            — Sends/receives player transforms
│   │   │   ├── RemoteAvatar.cs            — Representation of remote players
│   │   │   └── ProceduralWalk.cs          — Procedural walk animation
│   │   ├── Combat/
│   │   │   ├── PlayerCombat.cs        — Local player input: routes to attacker / carrier, reacts to being hit
│   │   │   ├── MeleeAttacker.cs       — Punch / slap: target search, knockback, prop push
│   │   │   ├── PropCarrier.cs         — Aim, pick up, hold, throw, drop
│   │   │   ├── CombatHud.cs           — Crosshair and hints
│   │   │   ├── CombatNetwork.cs       — ATTACK / SOUND packets, HitReceived event
│   │   │   ├── RemoteCombatPresenter.cs — Other players' hits: animations and sounds
│   │   │   ├── CombatSounds.cs        — Which sound a hit plays
│   │   │   ├── Models/                — AttackKind, AttackSettings, MeleeSettings, CarrySettings, HitMessage, SoundMessage
│   │   │   └── Props/
│   │   │       ├── NetProp.cs             — Ownership state: pickup, release, grant, sync loop
│   │   │       ├── NetPropImpact.cs       — Collisions: push props, impact sound, hit remote players
│   │   │       ├── PropSettleDetector.cs  — When a thrown prop has come to rest
│   │   │       ├── PropRemoteFollower.cs  — Smoothly follows another client's prop state
│   │   │       ├── PropHolderCollision.cs — Ignores collisions with the holding player
│   │   │       ├── NetPropRegistry.cs     — Finds props in the scene, id / server id / collider lookup
│   │   │       ├── NetPropFactory.cs      — Adds collider, Rigidbody and components to a scene object
│   │   │       ├── NetPropSender.cs       — Outgoing prop packets
│   │   │       ├── NetPropReceiver.cs     — Incoming prop packets (single subscription)
│   │   │       ├── NetPropDiagnostics.cs  — Logs prop events
│   │   │       └── Models/                — PropStateFlags
│   │   ├── Voice/
│   │   │   ├── VoiceManager.cs        — Mic capture, Opus encode, send
│   │   │   └── VoiceReceiver.cs       — Opus decode, AudioSource playback
│   │   ├── Audio/
│   │   │   ├── Models/Sfx.cs          — Sound ids (sent over the network)
│   │   │   ├── GameSounds.cs          — Plays SFX from a pooled set of AudioSources
│   │   │   └── Synthesis/             — Fallback sounds when Resources/Sounds/<Sfx>/ is empty
│   │   │       ├── ISfxSynthesizer.cs — Strategy interface
│   │   │       ├── *Synthesizer.cs    — Punch / Slap / Whoosh / Impact strategies
│   │   │       ├── SfxSynthesizerFactory.cs — Picks a strategy for an Sfx
│   │   │       └── ProceduralClip.cs  — Renders a strategy into an AudioClip
│   │   ├── UI/
│   │   │   └── GameMenu.cs            — Esc menu (lobby, respawn, settings, quit)
│   │   └── Settings/
│   │       └── GameSettings.cs        — Player settings
│   ├── Editor/
│   │   ├── CreateDemo3Scene.cs        — Tool: generate office scene
│   │   └── MainSceneLoader.cs         — Opens the main scene on Editor startup
│   ├── Prefabs/                       — Player, Workplace
│   ├── Scenes/                        — Demo 3 - Office Set 1 (main)
│   └── Resources/Sounds/<Sfx>/        — SFX clips, one folder per sound
├── ThirdParty/                    — external asset packs
│   ├── VNB - Office Set/              — low-poly office furniture (VNB-Leo)
│   ├── Floreswa/                      — low-poly characters (player model: male01_1)
│   └── LoafbrrAssets/Interiors_A/     — interior kit: walls, floors, stairs (CC0)
├── Plugins/
│   └── Concentus.dll                  — Opus audio codec
└── Settings/                      — URP render pipeline assets
```

## Running

1. Build and run the server (headless)
2. Launch client(s): `./Client.app -server <server-ip>`
3. If server is localhost, no arguments needed — connects to `127.0.0.1:7777`

## License

Internal project. Assets include third-party office models (`Assets/ThirdParty/`: VNB - Office Set, Floreswa, LoafbrrAssets/Interiors_A).
