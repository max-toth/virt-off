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
| `SampleScene` | Main office scene |
| `Demo 3 - Office Set 1` | Office with 6 workplaces, created via `Tools/Create Demo 3` |

## Controls

| Key | Action |
|-----|--------|
| WASD | Move |
| Mouse | Look |
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

```
Assets/
├── Scripts/
│   ├── Net/
│   │   ├── NetManager.cs          — Custom UDP networking (server/client)
│   │   └── AppSetup.cs            — Auto-init net manager on scene load
│   ├── Voice/
│   │   ├── VoiceManager.cs        — Mic capture, Opus encode, send
│   │   └── VoiceReceiver.cs       — Opus decode, AudioSource playback
│   ├── PlayerController.cs        — FPS movement (WASD + mouse look)
│   └── NetworkPlayerController.cs — Enables PlayerController after net connect
├── Editor/
│   ├── CreatePlayerPrefab.cs      — Tool: create Player prefab
│   └── CreateDemo3Scene.cs        — Tool: generate office scene
└── Plugins/
    └── Concentus.dll              — Opus audio codec
```

## Running

1. Build and run the server (headless)
2. Launch client(s): `./Client.app -server <server-ip>`
3. If server is localhost, no arguments needed — connects to `127.0.0.1:7777`

## License

Internal project. Assets include third-party office models (`VNB - Office Set`, `LoafbrrAssets/Interiors_A`).
