# Autonomous F1 Driving using Reinforcement Learning in Unity

An AI project focused on training a Formula 1 vehicle to autonomously navigate the Spa-Francorchamps circuit using Unity ML-Agents and Deep Reinforcement Learning (PPO).

## Project Overview
The goal of this project is to implement an intelligent agent capable of controlling a high-performance racing vehicle. The agent learns optimal driving lines, acceleration/braking thresholds, and steering angles by interacting with a high-fidelity 3D environment via trial and error.

---

## Technical Stack & Environment Setup

- **Game Engine:** Unity 2022.3 LTS
- **AI Framework:** Unity ML-Agents (v3.0+)
- **Training Method:** Proximal Policy Optimization (PPO) via Python backend

### Key Configurations
- **`com.unity.cloud.gltfast`**: High-fidelity asset rendering.
- **`com.unity.ml-agents`**: Core environment-to-Python communication.

---

## Implementation Details

### 1. Circuit & Physical Environment
- **Spa-Francorchamps Digital Twin:** Mapped track geometry using `Mesh Collider` for physics accuracy.
- **Physic Materials:** Custom friction coefficients applied to different surfaces:
    - **Asphalt (0.8):** High grip base.
    - **Kerbs (0.7):** Slight reduction in grip for corner apexes.
    - **Grass/Gravel:** Penalizing surfaces with reduced dynamic/static friction.

### 2. Vehicle Physics & Control Logic (`F1Agent.cs`)
- **Anti-Ghosting System:** Implemented a robust `SphereCast` predictive check that detects walls before movement occurs, preventing high-speed tunneling and wall-clipping.
- **Collision Handling:**
    - **Frontal Crashes:** Immediate terminal episode reset with negative reward (`-1.0f`).
    - **Lateral Scrapes:** Penalty system (`-0.5f`) with velocity reduction (`20%` speed retention) to force the agent to recover without resetting.
- **Dynamics:** - `moveSpeed`: 280f
    - `turnSpeed`: 110f
    - `continuous dynamic` collision detection enabled for high-speed precision.

### 3. Agent Lifecycle & Reset System
- **Global Positioning:** Switched from local to global `transform.position` sync to ensure consistent resets to the `SpawnPoint` regardless of parent container hierarchy.
- **Inertia Cleanup:** Forced `rb.velocity` and `angularVelocity` to zero on `OnEpisodeBegin` to prevent physics glitches during respawns.

### 4. Instrumentation & UI
- **Sector Timing:** 3-sector system with dynamic color feedback (Purple/Yellow) for performance tracking.
- **Telemetry:** Real-time HUD via `TextMeshPro` managing lap timing and crash recovery resets.

---

## Next Milestones

* [ ] Integrate **Ray Perception Sensors** (Vector Observations) to allow the agent to "see" track boundaries.
* [ ] Define final Reward Function (Checkpoint rewards vs. Crash/Surface penalties).
* [ ] Training: Configure `config.yaml` hyperparameters and initiate PPO training via mlagents-learn.