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

A waveform visualizer and connection status are displayed in the HUD (UI Toolkit).

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
│   │   │   ├── NetClient.cs           — Connects before the first scene (ServerConfig / -server arg), installs the network PlayerLoop step
│   │   │   ├── NetManager.cs          — UDP transport: receive thread → main-thread queue, PacketReceived / IdAssigned events
│   │   │   ├── NetworkPlayerLoop.cs   — PreUpdate step: incoming packets are handled before any Update
│   │   │   ├── GameNet.cs             — Access point to the active connection (Instance, LocalId, TryGetConnected)
│   │   │   ├── Lobby.cs               — Players present in the room (by voice / position packets), join / leave events
│   │   │   ├── NetDiagnostics.cs      — Network logs, enabled by NET_DEBUG
│   │   │   ├── Config/ServerConfig.cs — ScriptableObject with server address / port (asset: Resources/ServerConfig)
│   │   │   ├── Models/LobbyPlayer.cs  — Lobby player: id, muted, speaking
│   │   │   └── Packets/               — PacketType, PacketWriter, PacketReader, PacketSubscription
│   │   ├── Player/
│   │   │   ├── PlayerController.cs        — Local player: wires input to look / crouch / motor, respawn
│   │   │   ├── PlayerInputReader.cs       — Keyboard + mouse → PlayerInputState
│   │   │   ├── PlayerLook.cs              — Mouse look and camera shake
│   │   │   ├── PlayerMotor.cs             — Gravity, jump, knockback, CharacterController.Move
│   │   │   ├── PlayerCrouch.cs            — Crouch height and stand-up check
│   │   │   ├── PlayerPropPusher.cs        — Pushes props the player walks into
│   │   │   ├── Models/                    — PlayerInputState
│   │   │   ├── Animation/
│   │   │   │   ├── ProceduralWalk.cs      — Procedural animation driver (speed → gait → pose layers)
│   │   │   │   ├── WalkRig.cs             — Rigify metarig bones and rest pose
│   │   │   │   ├── IPoseLayer.cs          — Pose layer strategy
│   │   │   │   ├── LocomotionLayer.cs, HitReactionLayer.cs, CarryLayer.cs, AttackLayer.cs
│   │   │   │   └── Models/                — GaitState, GaitSettings, PoseAmplitudes
│   │   │   └── Sync/
│   │   │       ├── PositionSync.cs        — Sends local position, applies remote positions
│   │   │       ├── PositionNetwork.cs     — POSITION packet
│   │   │       ├── RemotePlayers.cs       — Registry of remote players (TryGetWalk)
│   │   │       ├── RemotePlayer.cs        — Remote avatar + smoothing towards the last received state
│   │   │       ├── RemoteAvatarFactory.cs — Prefab by id / copy of local model / capsule
│   │   │       ├── RemoteAvatar.cs        — Hit target marker, collider lookup
│   │   │       ├── NetworkPlayerController.cs — Enables controls once the server assigned an id
│   │   │       └── Models/                — PositionMessage
│   │   ├── Combat/
│   │   │   ├── PlayerCombat.cs        — Local player input: routes to attacker / carrier, reacts to being hit
│   │   │   ├── MeleeAttacker.cs       — Punch / slap: target search, knockback, prop push
│   │   │   ├── PropCarrier.cs         — Aim, pick up, hold, throw, drop
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
│   │   │   ├── VoiceManager.cs        — Own voice: mic → Opus → server, hotkeys M / R / U / J
│   │   │   ├── VoiceReceiver.cs       — Other players' voice: one VoiceStream per speaker
│   │   │   ├── VoiceNetwork.cs        — VOICE packet
│   │   │   ├── VoiceFormat.cs         — 16 kHz mono, 640-sample (40 ms) frames
│   │   │   ├── VoiceDiagnostics.cs    — Voice logs, enabled by VOICE_DEBUG
│   │   │   ├── Capture/               — MicrophoneCapture, VoiceEncoder, MicTest
│   │   │   ├── Playback/              — VoiceStream (Opus decode → ring buffer → AudioSource)
│   │   │   └── Models/                — VoicePacket
│   │   ├── Audio/
│   │   │   ├── Models/Sfx.cs          — Sound ids (sent over the network)
│   │   │   ├── GameSounds.cs          — Plays SFX from a pooled set of AudioSources
│   │   │   └── Synthesis/             — Fallback sounds when Resources/Sounds/<Sfx>/ is empty
│   │   │       ├── ISfxSynthesizer.cs — Strategy interface
│   │   │       ├── *Synthesizer.cs    — Punch / Slap / Whoosh / Impact strategies
│   │   │       ├── SfxSynthesizerFactory.cs — Picks a strategy for an Sfx
│   │   │       └── ProceduralClip.cs  — Renders a strategy into an AudioClip
│   │   ├── UI/                    — Presenters (MVP): logic only, layout lives in Assets/Game/UI
│   │   │   ├── UiClasses.cs           — Shared USS class names (hidden)
│   │   │   ├── Menu/
│   │   │   │   ├── GameMenu.cs            — Esc menu: pages, open/close, OpenChanged event
│   │   │   │   ├── LobbyListPresenter.cs  — Players in the lobby, mute buttons
│   │   │   │   ├── PlayerRowView.cs       — One row of the player list
│   │   │   │   ├── SettingsPresenter.cs   — Tabs, reset, back
│   │   │   │   ├── AudioSettingsPresenter.cs, ControlsSettingsPresenter.cs, GraphicsSettingsPresenter.cs
│   │   │   │   └── SliderBinding.cs       — Slider + value label
│   │   │   └── Hud/
│   │   │       ├── HudPresenter.cs        — Crosshair, carry hint, "Esc — menu" hint
│   │   │       └── VoiceDebugPresenter.cs — Voice debug panel with mic waveform
│   │   └── Settings/
│   │       ├── GameSettings.cs        — Client settings (mic, volume, mouse, video), MicDeviceChanged event
│   │       └── GameSettingsStorage.cs — PlayerPrefs persistence
│   ├── Editor/
│   │   ├── CreateDemo3Scene.cs        — Tool: generate office scene
│   │   ├── GameUiSetup.cs             — Tool: Tools/UI/Setup Game UI (PanelSettings, GameUI prefab, adds it to the main scene)
│   │   └── MainSceneLoader.cs         — Opens the main scene on Editor startup
│   ├── Prefabs/                       — Player, Workplace
│   ├── UI/                            — UI Toolkit assets: UXML layouts, USS styles, theme, PanelSettings, GameUI prefab
│   │   ├── Theme/                     — GameTheme.tss, Common.uss (colors, base controls)
│   │   ├── Menu/                      — PauseMenu.uxml/.uss, PlayerRow.uxml
│   │   └── Hud/                       — Hud.uxml/.uss
│   ├── Scenes/                        — Demo 3 - Office Set 1 (main)
│   └── Resources/                     — ServerConfig.asset, Sounds/<Sfx>/ (SFX clips, one folder per sound)
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
