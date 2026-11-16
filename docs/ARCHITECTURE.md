# System Architecture

## Functional decomposition

1. **Unity HMI:** sends a requested cabin setpoint in 0.5 °C increments; displays current cabin temperature and returned control states; simulates a driving environment and four visual weather modes.
2. **Windows MATLAB bridge (`HVACWindowsBridge`):** TCP server listening on `127.0.0.1:16555`; bridges Unity messages to ROS 2 and broadcasts received HVAC telemetry.
3. **Simulink communication layer:** subscribes to `/hvac/setpoint`, rejects invalid setpoints, retains the last valid command, and provides timeout fallback handling.
4. **AUTOSAR-style HVAC SWC:** `HVAC_Control_SWC_AUTOSAR_v3_FaultHandling`; makes heating/cooling/maintain decisions and handles sensor faults.
5. **Thermal plant:** `HVAC_Thermal_Plant_Discrete`; models cabin temperature response.
6. **Telemetry:** ROS 2 topic messages for temperature and control states, displayed in Unity.

## ROS 2 topic mapping

| Topic | Type | Role |
|---|---|---|
| `/hvac/setpoint` | `std_msgs/Float64` | Setpoint to Simulink |
| `/hvac/cabin_temp` | `std_msgs/Float64` | Cabin temperature |
| `/hvac/heater_cmd` | `std_msgs/Bool` | Heater command |
| `/hvac/ac_cmd` | `std_msgs/Bool` | AC command |
| `/hvac/maintain` | `std_msgs/Bool` | Maintain mode |
| `/hvac/sensor_fault` | `std_msgs/Bool` | Sensor fault |

## Operational boundaries

- Target temperature validity interval: **16–30 °C**.
- Loss-of-communication fallback: **22.5 °C** after the configured simulated timeout (**10 s** in the tested model).
- AUTOSAR runnable period: approximately **100 ms** in the tested software component configuration.
- **Caution:** Simulink model pacing and wall-clock timing are not equivalent. Do not label the end-to-end system real-time certified.
- Unity outside temperature is not coupled to the thermal plant in the final visual demo. This is a proposed future extension.

## Engineering improvements

- Connect Unity outside temperature to an explicitly validated external-input port on the Simulink thermal plant.
- Propagate actual `CommLost` status to the Unity HMI instead of inferred connection status.
- Add quantitative tests on response time, temperature tracking error and safety behavior.
- Replace visually simulated driving with validated vehicle dynamics only if in scope.
