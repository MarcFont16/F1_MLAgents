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

## Quick Start: Training Pipeline

To initialize the training environment on Linux/WSL, ensure your project is linked (e.g., `ln -s /mnt/c/F1_MLAgents ~/GEN_ART`) and run the following:

```bash
# 1. Prepare Environment
sudo apt update && sudo apt install python3 python3-pip python3-venv
cd ~/GEN_ART
python3 -m venv venv
source venv/bin/activate

# 2. Install Dependencies
pip install -r requirements.txt

# 3. Launch Training
mlagents-learn config.yaml --run-id=Spa_Training_01

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

### 5. AI Perception & Reward System
- **Ray Perception Sensor 3D:** Integrated a 15-ray spatial vision system (7 per direction + center) with a 180° Field of View and a 25m cast length, specifically tuned to detect `Wall` tags for high-speed collision avoidance.
- **Reward Function:** - **Checkpoints (Extrinsic):** `+1.0` reward triggered via `RaceManager` upon sequential checkpoint validation.
    - **Speed Incentive:** Micro-rewards scaled by current speed (`currentActualSpeed / moveSpeed`) to encourage continuous forward momentum.
    - **Penalties:** Integrated strictly into the collision logic to discourage reckless driving.

### 6. Training Configuration (`config.yaml`)
- Engineered a custom **Proximal Policy Optimization (PPO)** configuration tailored for high-velocity environments.
- **Hyperparameters:** Scaled up learning capacity (`batch_size: 2048`, `buffer_size: 20480`) and enforced long-term planning (`gamma: 0.993`).
- **Network Settings:** Deployed a deep neural network (3 hidden layers, 256 units each) with observation normalization enabled to bridge the numerical gap between ray cast distances (0-1) and vehicle speeds (0-280).

---

## Next Milestones

* [ ] Execute initial PPO training session and monitor learning metrics via TensorBoard.
* [ ] Implement `TrackStateManager` to dynamically adjust track physics (e.g., dry vs. wet asphalt) during training.
* [ ] Fine-tune the AI's cornering behavior and brake-point optimization based on early behavioral observations.