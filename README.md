# 🏰 Castle Busters Clone (Unity 2D Physics Artillery Strategy)

![Unity 6](https://img.shields.io/badge/Engine-Unity%206%20(6000.5.8f1)-blue?logo=unity)
![Language](https://img.shields.io/badge/Language-C%23-green?logo=csharp)
![Platform](https://img.shields.io/badge/Platform-PC%20%2F%20Mobile-orange)
![License](https://img.shields.io/badge/License-MIT-lightgrey)

A high-performance **2D Physics Artillery & Castle Destruction Strategy Game** built with **Unity 6** and **C#**. Inspired by classic destruction physics and tactical turn-based artillery mechanics, this project features dynamic procedurally generated terrain, grid-based destructible castle structures, parabolic trajectory prediction, cluster/salvo projectile mechanics, and AI bot opponents.

---

## 🌟 Key Features

### ⚔️ Combat & Artillery Mechanics
- **Drag-and-Release Slingshot Launcher**: Intuitive drag physics interface with adjustable pull force, angle, and release logic.
- **Physics Parabolic Trajectory Predictor**: Real-time visual trajectory pathing supporting wind vectors and projectile mass.
- **Specialized Ammo Types**:
  - Standard Projectiles
  - Cluster Munitions (Splits into multiple bomblets upon impact or trigger)
  - Salvo Missiles (Multi-stage sub-missiles with procedural smoke trails)

### 🧱 Structural Physics & Castle Destruction
- **Modular Castle Grid Architecture**: Destructible facade grids built with custom physics blocks and dynamic sprite assignment.
- **Structural Integrity & Debris Simulation**: Blocks collapse based on impulse force, spawning interactive physics debris pieces on destruction.
- **Castle Suspension Physics**: Realistic chassis movement and suspension bounce reacting to heavy impacts and movement.

### 🗺️ Procedural Terrain & Environment
- **Procedural Mesh Terrain Generator**: Generates smooth, uneven terrain procedurally with automated polygon collider generation.
- **Deformable Terrain / Crater Carving**: Dynamic crater mask generation tooling to deform terrain upon explosive impacts.
- **Parallax Background & Dynamic Decor**: Layered parallax scrolling background manager and procedural terrain foliage distribution.

### 🧠 Game Loop & Bot AI
- **Turn-Based State Machine**: Fully event-driven `GameManager` and `TurnManager` supporting round limits, turn timers, and win/loss conditions.
- **Targeting Bot AI**: Autonomous AI implementation (`SimpleBotAI`) that calculates angles, forces, and target priority to challenge single-player sessions.

---

## 🛠️ Technical Stack & Architecture

- **Game Engine**: Unity 6 (6000.5.8f1)
- **Language**: C# (.NET 9 compatible)
- **Physics**: Unity 2D Physics Engine (Rigidbody2D, PolygonCollider2D, Physics Material 2D)
- **UI Framework**: Unity TextMeshPro & Modern Responsive Canvas UI
- **Input System**: New Unity Input System (`InputSystem_Actions`)

### Architecture Highlights
- **Clean Architecture & Namespace Scoping**: Divided cleanly into `CastleBusters.Core`, `CastleBusters.Environment`, `CastleBusters.Combat`, `CastleBusters.Units`, `CastleBusters.AI`, and `CastleBusters.UI`.
- **Editor Tooling**: Custom Unity Editor extensions for automated asset generation (`SmokePuffGenerator`, `CraterMaskTools`).
- **Event-Driven Gameplay**: Decoupled turn state notifications, castle destruction events, and sound/visual FX triggers.

---

## 📁 Repository Structure

```
Castle-Busters-Clone/
├── Assets/
│   ├── Scripts/
│   │   ├── AI/                 # AI Opponent targeting logic
│   │   ├── Combat/             # Launchers, trajectory, cluster & sub-missiles
│   │   ├── Core/               # GameManager, TurnManager, CameraController
│   │   ├── Editor/             # Custom Unity Editor tooling
│   │   ├── Environment/        # Castle grid, destructible blocks, terrain generator
│   │   ├── UI/                 # HUD, turn indicators, game over screens
│   │   └── Units/              # Soldiers & modular visual handlers
│   ├── Prefabs/                # Pre-configured game objects & projectiles
│   ├── Audio/                  # Sound FX & background music loops
│   ├── Shaders/                # Custom 2D shaders
│   └── Sprites/                # Art assets, facades, and UI elements
└── ProjectSettings/            # Unity project settings & tags
```

---

## 🚀 Getting Started

### Prerequisites
- **Unity Hub** with **Unity 6 (6000.5.8f1)** installed.
- Git LFS installed (`git lfs install` prior to cloning if large binary assets are tracked).

### Setup & Play
1. **Clone the Repository**:
   ```bash
   git clone https://github.com/YOUR_USERNAME/Castle-Busters-Clone.git
   ```
2. **Open in Unity**:
   - Open Unity Hub.
   - Click **Add** -> **Add project from disk**.
   - Select the `Castle Busters Clone` root folder.
   - Open using **Unity 6**.
3. **Run the Game**:
   - Navigate to `Assets/Scenes/`.
   - Open the main game scene and press **Play** in the Unity Editor.

---

## 📄 License
Distributed under the MIT License. See `LICENSE` for more information.
