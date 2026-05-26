# Autonomous F1 Driving using Reinforcement Learning in Unity

An AI project focused on training a Formula 1 vehicle to autonomously navigate the Spa-Francorchamps circuit using Unity ML-Agents and Deep Reinforcement Learning (PPO).

## Project Overview
The goal of this project is to implement an intelligent agent capable of controlling a high-performance racing vehicle. The agent learns optimal driving lines, acceleration/braking thresholds, and steering angles by interacting with a high-fidelity 3D environment via trial and error.

---

## Technical Stack & Environment Setup

- **Game Engine:** Unity 2022.3 LTS (Long Term Support)
- **AI Framework:** Unity ML-Agents (v3.0+)
- **Training Method:** Proximal Policy Optimization (PPO) via Python backend

### Dependencies & Package Management
To ensure native asset compatibility and physical accuracy, the following internal packages were configured:

1. **`com.unity.cloud.gltfast`**: Enables native, high-fidelity rendering and texture mapping for track assets.
2. **`com.unity.ml-agents`**: The core API providing the environment-to-Python socket communication bridge.

---

## Implementation Steps

### 1. Circuit Integration & Optimization
- Imported the digital twin of the **Spa-Francorchamps** circuit.
- **Mesh Optimization:** Mapped the track's geometry to the `Mesh` property within a global `Mesh Collider`, resolving phantom-collision states and ensuring physics accuracy.

### 2. Vehicle Asset Configuration & Scaling
- Integrated a high-fidelity Formula 1 3D model into the scene hierarchy, nested within an `F1Contenidor` (Root GameObject).
- **Scale Calibration:** Resolved coordinate discrepancies by setting the mesh scale to `6x6x6`, achieving a realistic 1:1 proportion relative to the track width.
- **Collider & Friction:** Implemented a rigid `Box Collider` around the chassis. Configured a custom `Physic Material` with optimized static/dynamic friction to prevent lateral sliding on banked corners.

### 3. Agent Architecture & Reset Logic
- **Lifecycle Management:** Created `F1Agent.cs` extending the ML-Agents `Agent` superclass.
- **Spawn System:** Implemented an external `SpawnPoint` (Transform reference) to manage episode resets. The agent automatically teleports to grid coordinates and flushes all `Rigidbody` velocity vectors on `OnEpisodeBegin`.
- **Crash Detection & Safety Net:** Programmed terminal failure states. The episode automatically resets if the agent collides with bounding geometry (utilizing a custom `Wall` tag) or falls below a dynamic Y-axis safety threshold.

### 4. Action Space & Track Adaptation
- **Action Buffers:** Configured 2 Continuous Actions for physical movement (throttle/brake and steering). Defined realistic speed limits by separating `moveSpeed` and `reverseSpeed`.
- **Dynamic Slope Alignment:** Engineered a downward-facing `Physics.Raycast` system that calculates the track's surface normal. Used `Quaternion.Slerp` to continuously adapt the vehicle's pitch and roll to match the track's elevation changes in real-time.

### 5. Telemetry & Dual-Camera System
- **First-Person POV:** Parented the Main Camera to the vehicle chassis with customized pitch/position offsets to simulate a realistic perception of speed.
- **Orthographic Minimap:** Developed a custom `TopDownFollow.cs` script attached to an orthographic camera. It tracks the car's X/Z coordinates while maintaining a fixed relative height, rendered as a PiP radar display.

### 6. Race Management & Lap Timing
- **UI HUD:** Implemented a UI Canvas utilizing `TextMeshPro` to display a real-time chronometer with a custom semi-transparent background.
- **Race Manager Logic:** Created `RaceManager.cs` to handle time tracking, lap counting, and logging telemetry data.
- **Finish Line Trigger:** Engineered an invisible Box Collider (`Is Trigger`) across the starting grid. Conditionally programmed `FinishLine.cs` to initiate the timer upon the agent's first pass.

### 7. Race Instrumentation & UI
- **Sector Breakdown Logic:** Developed a 3-sector timing system mirroring official F1 telemetry. Each sector (S1, S2, S3) captures elapsed time, providing immediate feedback on driving performance.
- **Dynamic Color Feedback:** Implemented a real-time visualization system using *Rich Text* for `TextMeshPro`. Sector times are dynamically colored: **Purple** (Personal Best) or **Yellow** (Slower).
- **Checkpoint Sequence:** Integrated a list-based trigger system to ensure track adherence. The logic prevents shortcutting by validating the sequential order of passage.
- **Crash Recovery & UI Reset:** Programmed automatic state synchronization. Upon a collision or reset, the `RaceManager` triggers a hard-clean of the HUD (timer/sectors) for a fresh start.

---

## Next Milestones

* [ ] Implement raycast-based proximity sensors (Vector Observations) to detect track boundaries.
* [ ] Define the reward function (positive rewards for checkpoints, penalties for wall collisions).
* [ ] Configure `config.yaml` hyperparameters for the Python PPO trainer.