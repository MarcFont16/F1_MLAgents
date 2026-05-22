# Autonomous F1 Driving using Reinforcement Learning in Unity

An AI project focused on training a Formula 1 vehicle to autonomously navigate the Spa-Francorchamps circuit using Unity ML-Agents and Deep Reinforcement Learning (PPO).

## Project Overview
The goal of this project is to implement an intelligent agent capable of controlling a high-performance racing vehicle. The agent learns optimal driving lines, acceleration braking thresholds, and steering angles by interacting with a high-fidelity 3D environment via trial and error.

---

## Technical Stack & Environment Setup

- **Game Engine:** Unity 2022.3 LTS (Long Term Support)
- **AI Framework:** Unity ML-Agents (v3.0+)
- **Training Method:** Proximal Policy Optimization (PPO) via Python backend

### Dependencies & Package Management
To ensure native asset compatibility and physical accuracy, the following internal packages were configured via the Unity Package Manager:
1. **`com.unity.cloud.gltfast`**: Installed to enable native, high-fidelity rendering and texture mapping for `.glb` geographic/track assets without data loss during conversion.
2. **`com.unity.ml-agents`**: The core API providing the environment-to-Python socket communication bridge.

---

## Implementation Steps (So Far)

### 1. Circuit Integration & Optimization
- Imported the digital twin of the **Spa-Francorchamps** circuit.
- **Mesh Optimization:** Configured a global `Mesh Collider` on the track geometry to ensure static collision detection. 
  - *Critical Fix:* Explicitly mapped the track's geometry to the `Mesh` property within the collider component, resolving a `None (Mesh)` phantom-collision state that caused gravity to pull dynamic objects through the floor. This provides the mathematical surface ground truth required for the vehicle's physics engine and raycast sensors.

### 2. Vehicle Asset Configuration & Scaling
- Integrated a high-fidelity Formula 1 3D model into the scene hierarchy.
- **Scale Calibration:** Resolved standard coordinate export discrepancies (e.g., cm to meters conversion issues) by adjusting the local scale transform to `4x4x4`, achieving a realistic 1:1 proportion relative to the track width.
- **Stability Fixes:** Handled runtime physical anomalies and edge-case exceptions (such as infinite force feedback yielding `NaN` positional vectors) by resetting the local spatial transforms to safe coordinates.

### 3. Physics & Rigid Body Dynamics
- **Mass Matrix:** Implemented a standard `Rigidbody` component with a calibrated mass of **800 kg**, simulating real-world F1 curb-weight dynamics.
- **Bounding Boxes:** Configured a custom `Box Collider` around the vehicle's chassis bounds to manage dynamic interactions with the track's mesh collider.

### 4. Agent Architecture (C# Scripting)
Created the core `F1Agent.cs` script extending the ML-Agents `Agent` superclass. The architecture is structured around 5 structural lifecycle overrides:

```csharp
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class F1Agent : Agent
{
    // Triggered once at instantiation for structural caching
    public override void Initialize() { }

    // Manages environment resets when the vehicle crashes or goes off-track
    public override void OnEpisodeBegin() { }

    // Gathers vector observations (speed, orientation, track distance)
    public override void CollectObservations(VectorSensor sensor) { }

    // Translates the neural network's continuous tensor output into motor forces
    public override void OnActionReceived(ActionBuffers actions) { }

    // Fallback manual input mapping (WASD) for developer debugging
    public override void Heuristic(in ActionBuffers actionsOut) { }
}

---

### 5. Action Space & Heuristic Testing
- **Behavior Parameters:** Configured the agent to output exactly **2 Continuous Actions** (throttle/brake logic and steering logic) with `0` Discrete Branches.
- **Decision Requester:** Added to ping the neural network or heuristic class to output an action at regular intervals (default step: 5).
- **Manual Debugging:** Temporarily set the Behavior Type to `Heuristic Only` to map local keyboard inputs (WASD) to the action buffers, allowing manual debugging of the vehicle's physical behavior and movement constraints prior to ML training.

---

## Next Milestones

* [ ] Implement raycast-based proximity sensors (Vector Observations) to detect track boundaries.
* [ ] Define the reward function (e.g., positive rewards for forward velocity along the track vector, heavy penalties for wall collisions).
* [ ] Configure the `config.yaml` hyperparameters for the Python PPO trainer.