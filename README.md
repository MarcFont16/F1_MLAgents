# Autonomous F1 Driving using Reinforcement Learning in Unity

An AI project focused on training a Formula 1 vehicle to autonomously navigate the Spa-Francorchamps circuit using Unity ML-Agents and Deep Reinforcement Learning (PPO).

---

# Project Overview

The goal of this project is to implement an intelligent agent capable of controlling a high-performance racing vehicle. The agent learns optimal driving lines, acceleration/braking thresholds, and steering angles by interacting with a high-fidelity 3D environment via trial and error.

The project extends beyond standard video game AI by incorporating real-world engineering concepts such as:

- Domain Randomization
- Curriculum Learning
- Anti-Wobble Steering Metrics
- Off-Track Physics Simulation
- Full-Stack Live Telemetry

---

# Technical Stack

| Layer | Technology |
|---------|------------|
| Game Engine | Unity 2022.3 LTS |
| AI Framework | Unity ML-Agents (v3.0+) |
| Training Method | Proximal Policy Optimization (PPO) |
| Telemetry Backend | Node.js, Express, Socket.io |
| Network Protocols | UDP (Unity → Node.js), WebSockets (Node.js → Browser) |

## Key Unity Packages

- `com.unity.cloud.gltfast` — High-fidelity asset rendering.
- `com.unity.ml-agents` — Core environment-to-Python communication.

---

# Quick Start: Training Pipeline

## Option A — Local (Linux / WSL)

```bash
# 1. Prepare environment
sudo apt update && sudo apt install python3 python3-pip python3-venv

cd ~/GEN_ART

python3 -m venv venv
source venv/bin/activate

# 2. Install dependencies
pip install -r requirements.txt

# 3. Launch training
mlagents-learn config.yaml --run-id=Spa_Training_01
```

Ensure your project is symlinked:

```bash
ln -s /mnt/c/F1_MLAgents ~/GEN_ART
```

---

## Option B — Docker & Full Stack (Recommended)

### Build the image

```bash
sudo docker build -t f1-agent .
```

### Start all services in this exact order

#### 1. Start the Telemetry Server

```bash
cd F1_Dashboard
node server.js
```

#### 2. Open the Live Dashboard

```
http://localhost:3000
```

#### 3. Start Training (Active Experiment)

```bash
sudo docker run -it \
  --net=host \
  --security-opt seccomp=unconfined \
  -v ${PWD}:/app \
  f1-agent \
  mlagents-learn config.yaml \
  --run-id=Experiment_03_ProgressiveCurriculum
```

#### Compare Runs with TensorBoard

```bash
sudo docker run -it --rm \
  --net=host \
  -v ${PWD}:/app \
  f1-agent \
  tensorboard --logdir results
```

#### 4. Press PLAY in the Unity Editor

---

# Training History & Ablation Study

The project followed an iterative environment design strategy. Several experiments were intentionally abandoned and analyzed to improve learning efficiency.

## Baseline_Agent_01 (Abandoned)

- Pure environment without safety mechanisms.
- Severe reward stagnation due to local minima.
- Agent frequently became trapped in low-traction surfaces (grass and gravel).
- Failed to make meaningful progress.

## Experiment_02_SafetyWalls (Abandoned)

- Added distant safety walls to prevent permanent traps.
- Curriculum Learning speed multiplier increased too aggressively.
- Speed jumped from `60f` directly to `200f`.
- Produced a **Curriculum Shock** effect.
- Agent lacked steering precision and crashed immediately.

## Experiment_03_ProgressiveCurriculum (Successful)

- Progressive speed scaling introduced.
- Started at `40f`.
- Increased by `+5f` after each perfect lap.
- Eliminated curriculum shock.
- Stabilized policy learning.
- Maintained healthy exploration entropy.
- Achieved smooth convergence.

---

# Implementation Details

## 1. Circuit & Physical Environment

### Spa-Francorchamps Digital Twin

- Track geometry mapped using Mesh Colliders.
- Physics-accurate surface interactions.

### 92-Checkpoint System

- Dense reward-gate coverage.
- Divided into three dynamic sectors.

### Surface Materials

| Surface | Friction (μ) | Description |
|----------|------------|-------------|
| Asphalt | 0.80 | High grip base |
| Kerbs | 0.70 | Reduced grip at apexes |
| Grass | Low | Simulated low-traction |
| Gravel | Very Low | High-drag trap surface |

---

## 2. Vehicle Physics & Control Logic (`F1Agent.cs`)

### Advanced Steering Dynamics (Anti-Wobble)

#### Cubic Input Curve

```text
steering = input³
```

Provides:

- High precision near center.
- Full steering lock at extremes.

#### Strict Deadzone

- Inputs below 2% ignored.
- Eliminates micro-oscillations on straights.

#### Smooth Rack Mechanics

```csharp
Mathf.Lerp()
```

Used to simulate realistic steering rack delay.

### Off-Track Physics Simulation

#### Grass

```text
speed × 0.60
steering × 0.50
```

Low-traction "ice-like" behavior.

#### Gravel

```text
speed × 0.25
steering × 0.20
```

High-drag trap behavior.

---

## 3. AI Perception & Sensor Noise

### Ray Perception Sensor 3D

- 15-ray system.
- 7 rays per side + center ray.
- 180° field of view.

Detects:

- Walls
- Checkpoints

Allows anticipation of the optimal racing line.

### Sensor Noise

```csharp
Random.Range(-2f, 2f)
```

Injected into speed observations to:

- Prevent overfitting.
- Simulate real-world sensor inaccuracies.
- Improve generalization.

Observation normalization is applied before feeding data into the neural network.

---

## 4. Curriculum Learning & Domain Randomization

### Progressive Speed Scaling (`RaceManager.cs`)

After consecutive clean laps:

```text
+5f speed
+1f steering
```

Validated using a three-sector timing system.

### Weather Simulation

Random friction values:

```text
0.30 μ → 0.85 μ
```

Forces adaptation to:

- Dry conditions
- Damp conditions
- Wet conditions
- Heavy rain scenarios

---

## 5. Full-Stack Live Telemetry Dashboard

### Data Pipeline

```text
Unity
  ↓ UDP
Node.js Server
  ↓ WebSockets
React Dashboard
```

### TelemetrySender.cs

Streams:

- Vehicle physics data
- Neural network decisions

At:

```text
60 Hz
```

### Dashboard Features

- Live speed display
- Steering input monitor
- Three-sector timing
- Delta comparison colors
- Dynamic friction indicator

Designed to emulate a professional Formula 1 pit wall.

---

# Evaluation & Benchmark Results

A dedicated `EvaluationManager.cs` protocol was developed to evaluate robustness.

### Evaluation Procedure

- Agent speed fixed to baseline values.
- Traction isolated as the primary variable.
- 7 weather conditions.
- 20 episodes per condition.
- Results automatically exported to CSV.

## Results

| Condition | Track Grip (μ) | Success Rate | Avg Lap Time |
|------------|---------------|--------------|--------------|
| Optimal | 0.85 | 95% | 1:30.245 |
| Dry | 0.80 | 95% | 1:30.812 |
| Dusty | 0.70 | 90% | 1:32.405 |
| Damp | 0.60 | 85% | 1:34.120 |
| Wet | 0.50 | 80% | 1:37.550 |
| Heavy Rain | 0.40 | 65% | 1:42.890 |
| Storm | 0.30 | 50% | 1:48.210 |

### Key Finding

The trained agent demonstrates a generalized and cautious driving policy capable of adapting to severe traction loss without catastrophically failing due to overfitting to a single racing line.

---

# Project Roadmap

| Status | Task |
|---------|------|
| ✅ | Baseline PPO training session (`Baseline_Agent_01`) — metrics validated |
| ✅ | 92-checkpoint dense reward gate system |
| ✅ | Anti-Wobble steering logic and Off-Track Physics Simulation |
| ✅ | Ablation Study: Stagnation and Curriculum Shock analysis |
| ✅ | Train `Experiment_03_ProgressiveCurriculum` to stable lap completion |
| ✅ | Export TensorBoard performance graphs |
| ✅ | Build `EvaluationManager` and execute weather protocol |
| ✅ | Benchmark success rates and average lap times into CSV |
| ✅ | Final report (IEEE format) with TensorBoard curves and telemetry dashboard |

---

# Final Outcome

A complete reinforcement learning pipeline capable of training an autonomous Formula 1 agent inside Unity, featuring:

- PPO-based policy optimization
- Progressive Curriculum Learning
- Domain Randomization
- Realistic vehicle dynamics
- Live telemetry monitoring
- Robust weather-condition evaluation
- Full experiment tracking and benchmarking