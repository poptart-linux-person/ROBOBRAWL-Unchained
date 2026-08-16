# ROBOBRAWL: Unchained

Standalone multiplayer VR physics-combat sandbox built for Unity.

## Current foundation
- Unity 2022.3 LTS project structure
- OpenXR + XR Interaction Toolkit
- Unity Netcode for GameObjects + Unity Transport
- Server-authoritative robot spawning, combat state, health, weapon ownership, and ragdoll events
- Gorilla Tag-style arm-swing locomotion foundation
- Procedural colorful arena/hangout generated from primitives
- Physical weapon/item spawning
- Robot AI state machine with melee, dodge, block/parry, weapon pickup, disarm, grab, and knockback hooks
- Impact damage based on relative velocity and surface/material tags

The project intentionally uses procedural primitives and code-driven systems so it can open and run without external art assets.

## Open
Open the repository folder as a Unity 2022.3+ project. Install packages from `Packages/manifest.json`, then open `Assets/Scenes/RobotArena.unity`.

## Multiplayer
The prototype uses Unity Netcode for GameObjects with Unity Transport. A host can create the authoritative simulation and clients connect to it. The networking layer is kept behind small components so a dedicated-server/WebRTC transport can replace the transport later without rewriting combat logic.
