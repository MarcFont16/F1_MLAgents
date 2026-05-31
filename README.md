# Autonomous F1 Driving using Reinforcement Learning in Unity

An AI project focused on training a Formula 1 vehicle to autonomously navigate the Spa-Francorchamps circuit using Unity ML-Agents and Deep Reinforcement Learning (PPO).

## Project Overview
The goal of this project is to implement an intelligent agent capable of controlling a high-performance racing vehicle. The agent learns optimal driving lines, acceleration/braking thresholds, and steering angles by interacting with a high-fidelity 3D environment via trial and error. The project extends beyond standard video game AI by incorporating real-world engineering concepts like **Domain Randomization**, **Curriculum Learning**, and **Full-Stack Live Telemetry**.

---

## Technical Stack & Environment Setup

- **Game Engine:** Unity 2022.3 LTS
- **AI Framework:** Unity ML-Agents (v3.0+)
- **Training Method:** Proximal Policy Optimization (PPO) via Python backend
- **Telemetry Backend:** Node.js, Express, Socket.io
- **Network Protocols:** UDP (Unity to Node.js), WebSockets (Node.js to Browser)

### Key Configurations
- **`com.unity.cloud.gltfast`**: High-fidelity asset rendering.
- **`com.unity.ml-agents`**: Core environment-to-Python communication.

---

## Quick Start: Training Pipeline

### Option A — Local (Linux/WSL)

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
```

### Option B — Docker & Full Architecture (Recommended)

A `Dockerfile` is provided to run the training pipeline in a fully isolated, reproducible environment.

**1. Build the image:**
```bash
sudo docker build -t f1-agent .
```

**2. Run the Full Stack Architecture:**

To ensure telemetry and training synchronize perfectly, start the services in this exact order:

```bash
# 1. Start the Telemetry Server
cd F1_Dashboard
node server.js

# 2. Open the Live Dashboard in your browser: http://localhost:3000

# 3. Run Docker training
sudo docker run -it --net=host --security-opt seccomp=unconfined -v ${PWD}:/app f1-agent mlagents-learn config.yaml --run-id=spa_training_01 --resume

sudo docker run -it --net=host --security-opt seccomp=unconfined -v ${PWD}:/app f1-agent mlagents-learn config.yaml --run-id=spa_training_01 --resume

# 4. Press PLAY in the Unity Editor
```

**Dockerfile:**
```dockerfile
FROM python:3.9-slim-bullseye
WORKDIR /app

# System tools
RUN apt-get update && apt-get install -y git build-essential

# Install dependencies
COPY requirements.txt .
RUN pip install --upgrade pip && \
    pip install mlagents mlagents-envs && \
    pip install torch>=2.1.0 protobuf==3.20.3 six && \
    pip install -r requirements.txt

COPY . .

CMD ["mlagents-learn", "config.yaml", "--run-id=spa_training_01"]
```

---

## Implementation Details

### 1. Circuit & Physical Environment
- **Spa-Francorchamps Digital Twin:** Mapped track geometry using `Mesh Collider` for physics accuracy.
- **Physic Materials:** Custom friction coefficients applied to different surfaces:
    - **Asphalt (0.8):** High grip base.
    - **Kerbs (0.7):** Slight reduction in grip for corner apexes.
    - **Grass/Gravel:** Penalizing surfaces with reduced dynamic/static friction.

### 2. Vehicle Physics & Control Logic (`F1Agent.cs`)
- **Virtual Steering Wheel:** Applied `Mathf.Lerp` to the neural network's turning output to simulate realistic, smooth steering rack mechanics, preventing jerky AI movements.
- **Anti-Ghosting System:** Implemented a robust `SphereCast` predictive check that detects walls before movement occurs, preventing high-speed tunneling and wall-clipping.
- **Collision Handling:**
    - **Frontal Crashes:** Immediate terminal episode reset with negative reward (`-1.0f`).
    - **Lateral Scrapes:** Penalty system (`-0.5f`) with velocity reduction (`20%` speed retention) to force the agent to recover without resetting.
- **Dynamics:**
    - `moveSpeed`: Starts at a stable `150f` to allow neural network convergence, dynamically scales up via Curriculum Learning.
    - `turnSpeed`: `80f`

### 3. AI Perception & Sensor Noise
- **Ray Perception Sensor 3D:** Integrated a 15-ray spatial vision system (7 per direction + center) with a 180° Field of View and a 25m cast length.
- **Real-World Sensor Noise:** Injected random noise variations into the speed observations (`Random.Range(-2f, 2f)`). This prevents the AI from overfitting to perfect mathematical data, simulating the inaccuracy of real-life LiDAR/Speed sensors.

### 4. Reward System Optimization
- **Dense Reward Gates:** Invisible trigger colliders along the racing line provide consistent micro-rewards.
- **Time Penalty:** Implemented a continuous negative reward (`-0.001f` per step) to force the AI to optimize its racing line and minimize lap times.
- **Speed Incentive:** Micro-rewards scaled by current speed (`currentActualSpeed / moveSpeed`) to encourage continuous forward momentum.

### 5. Curriculum Learning & Domain Randomization
- **Automated AI Progression:** The `RaceManager` autonomously monitors the AI's success rate.
- **Speed Scaling:** After 5 consecutive collision-free laps, the environment automatically bumps the AI's maximum speed to `200f`.
- **Weather Simulation (Domain Randomization):** After 10 consecutive perfect laps, the system introduces uncertainty. Track friction is randomly altered between `0.30 μ` (storm/survival) and `0.85 μ` (optimal grip) at the start of each episode, forcing the agent to develop a robust, adaptable driving policy rather than memorizing a fixed path.

### 6. Full-Stack Live Telemetry Dashboard
- **UDP Broadcast:** A custom Unity script (`TelemetrySender.cs`) extracts physics and neural network decision data at 60Hz and fires it outside the game engine via a UDP socket.
- **Node.js WebSocket Server:** A lightweight backend intercepts the UDP packets and broadcasts them to the web.
- **React/HTML Frontend:** A modern, glassmorphism-styled web interface displays live steering inputs, current speed, and dynamic track friction changes in real-time, functioning as a professional F1 pit wall monitor.

### 7. Training Configuration (`config.yaml`)
- Engineered a custom **Proximal Policy Optimization (PPO)** configuration tailored for high-velocity environments.
- **Hyperparameters:** Scaled up learning capacity (`batch_size: 2048`, `buffer_size: 20480`) and enforced long-term planning (`gamma: 0.993`).
- **Network Settings:** Deployed a deep neural network (3 hidden layers, 256 units each) with observation normalization enabled.

---

## Next Milestones / Pending Tasks

* [x] Execute initial PPO training session and validate learning metrics *(Baseline established)*.
* [x] **Incentive System:** Implemented a continuous Time Penalty (`-0.001f` per step) to force lap-time optimization.
* [x] **Domain Randomization (Adaptability Training):** Implemented a randomized friction system to dynamically alter track conditions between `0.30` (storm) and `0.85` (optimal).
* [ ] **Model Training Pipeline:**
    * [ ] Train the Baseline Agent (fixed `0.80 μ` friction, domain randomization disabled).
    * [ ] Train the Adaptive Agent (dynamic randomization between `0.30 μ` and `0.85 μ` enabled).
* [ ] **Metrics & Visualization:**
    * [ ] Monitor policy convergence and export training graphs (policy loss, cumulative reward) using TensorBoard.
* [ ] **Rigorous Evaluation Suite:**
    * [ ] Run the 7-stage weather protocol (100 test episodes per condition) for both trained models.
    * [ ] Benchmark and compare Success Rates (%) and average lap times.
* [ ] **Final Documentation:**
    * [ ] Document final results, insert TensorBoard curves, and include live telemetry dashboard screenshots into the final project report.# Autonomous F1 Driving using Reinforcement Learning in Unity

An AI project focused on training a Formula 1 vehicle to autonomously navigate the Spa-Francorchamps circuit using Unity ML-Agents and Deep Reinforcement Learning (PPO).

---

## Project Overview

The goal of this project is to implement an intelligent agent capable of controlling a high-performance racing vehicle. The agent learns optimal driving lines, acceleration/braking thresholds, and steering angles by interacting with a high-fidelity 3D environment. The project incorporates advanced engineering concepts like Domain Randomization, Curriculum Learning, Anti-Wobble Control, and Full-Stack Live Telemetry.

---

## Technical Stack & Environment Setup

| Component | Details |
|---|---|
| Game Engine | Unity 2022.3 LTS |
| AI Framework | Unity ML-Agents (v3.0+) |
| Training Method | Proximal Policy Optimization (PPO) via Python backend |
| Telemetry Backend | Node.js, Express, Socket.io |
| Network Protocols | UDP (Unity → Node.js), WebSockets (Node.js → Browser) |

---

## Implementation Details

### 1. Vehicle Physics & Control Logic (`F1Agent.cs`)

**Vehicle Dynamics**

| Parameter | Value | Description |
|---|---|---|
| `moveSpeed` | `80f` | Forward speed (reduced from 150 for stability) |
| `turnSpeed` | `80f` | Rotation speed applied to the chassis |
| `steeringSnappiness` | `5f` | How fast the virtual driver turns the wheel (lower = smoother) |

**Steering Pipeline**

The agent outputs a normalised steering target (`-1.0` to `1.0`). A virtual wheel interpolates the current angle toward the target each fixed timestep, then the smoothed value is applied to the chassis rotation:

```
targetTurnInput → Lerp (steeringSnappiness) → currentTurnInput → transform.Rotate
```

**Anti-Wobble System**

- **Deadzone Logic:** Steering inputs below 10% are ignored (`0f`), eliminating micro-oscillations on straight lines.
- **Steering Penalty (Jerk reduction):** Exponential penalty for erratic steering changes (`-Mathf.Abs(targetTurnInput) * 0.0025f`), forcing the agent to prioritise stable, smooth trajectories.
- **Cubic Sensitivity:** Non-linear steering input conversion (`input³`) ensures precision around centre while maintaining maximum turn authority.

**Safety Systems**

- **SphereCast:** Predictive wall-collision detection.
- **Collision Handling:** Immediate episode reset for frontal crashes (`-1.0f`), velocity reduction for lateral scrapes (`-0.5f`).

**Dynamic Stability**

Automatic surface-normal alignment via `Raycast` keeps the vehicle grounded on track elevation changes (e.g. Eau Rouge).

---

### 2. Perception & Navigation

**Dense Checkpoint System — 71 checkpoints across Spa**

| Sector | Checkpoint Indices | Count |
|---|---|---|
| Sector 1 | 0 – 17 | 18 |
| Sector 2 | 18 – 53 | 36 |
| Sector 3 | 54 – 70 | 17 |

**Sensor Augmentation**

The `Ray Perception Sensor 3D` detects both `Wall` and `Checkpoint` tags, allowing the agent to see the path ahead from up to 100 m away.

---

### 3. Reward System

| Signal | Value | Purpose |
|---|---|---|
| Checkpoint collected | positive | Reinforce correct racing line |
| Time step | `-0.001f` | Penalise slow or idle laps |
| Steering movement | negative | Incentivise smooth inputs |
| Wall scrape | `-0.5f` | Discourage lateral contact |
| Frontal crash | `-1.0f` | Hard reset on major collision |

---

### 4. Curriculum Learning & Domain Randomization

- **Adaptive Difficulty:** `RaceManager` monitors performance and automates speed scaling to `200f` after 5 consecutive perfect laps.
- **Weather Simulation:** Dynamic friction randomisation (`0.30 μ` – `0.85 μ`) activates after 10 consecutive laps, training a robust policy capable of handling changing track grip.

---

## Training Pipeline

Resume training safely with the following command to preserve all progress:

```bash
# launch training pipeline with resume enabled
sudo docker run -it --net=host --security-opt seccomp=unconfined \
  -v ${PWD}:/app f1-agent \
  mlagents-learn config.yaml --run-id=spa_training_01 --resume
```

---

## Next Milestones

- [x] Core Physics & Steering — non-linear steering and wobble-reduction penalties
- [x] Navigation — 71-checkpoint system with dynamic sector tracking
- [x] Sensor Fusion — perception sensors updated to detect checkpoints for improved line-finding
- [ ] **Adaptive Agent Evaluation**
  - [ ] Train for 10M steps with new steering stability rewards
  - [ ] Validate lap-time consistency at high-grip (`0.85 μ`) and low-grip (`0.30 μ`)
- [ ] **Rigorous Evaluation Suite**
  - [ ] Benchmark success rates (%) and lap times under the 7-stage weather protocol
- [ ] **Documentation** — finalise report with TensorBoard graphs (cumulative reward, policy loss, steering stability metrics)
