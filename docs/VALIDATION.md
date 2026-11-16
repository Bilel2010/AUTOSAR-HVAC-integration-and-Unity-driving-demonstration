# Validation Matrix

## Verified from MATLAB/Simulink interaction logs

| ID | Check | Evidence | Status |
|---|---|---|---|
| ROS2-17 | Unity TCP socket | `Server.Connected = 1` | PASS |
| ROS2-18 | Setpoint delivered Unity → bridge | `LastSetpoint = 24.00 °C` | PASS |
| ROS2-19 | Temperature ROS 2 → bridge | `HasCabinData = 1`; last measured age `0.03 s` | PASS |
| ROS2-20A | Applied setpoint after `ROS215_FallbackSwitch` | Display `24.00 °C` | PASS |
| ROS2-20B | Direct feed to AUTOSAR ModelReference input | `Source entree 1 = ROS215_FallbackSwitch` | PASS |
| ROS2-20C | Dynamic 20 °C test | Controller setpoint `20.00 °C`; AC=1, heater=0, maintain=0 | PASS |
| Unity V5 | Graphical HMI | Exterior-view capture shows ROS2 data, cabin `20.0 °C`, heater active | Observed |

## Testing already performed separately

- Sensor fault checks and safety-off in AUTOSAR SWC model.
- MIL/SIL scenarios and thermal plant unit tests.
- Watchdog fallback test in a separate Simulink test model.

Include the actual reports or test logs in `docs/evidence/` if publicly shareable. Do not invent numerical test coverage percentages.

## Not validated or not implemented in final V5

- Unity weather values propagated to Simulink outside-air thermal input.
- Quantitative end-to-end latency / deadlines / performance guarantees.
- Real-vehicle validation or AUTOSAR production conformance certification.
- Unity HMI rendering of a fully measured `CommLost` signal.

## Suggested final demonstration

1. Start bridge, Unity Play and the paced Simulink model.
2. Set Unity temperature 24 °C; show applied setpoint in Simulink.
3. Lower to 20 °C when cabin is warmer; show AC ON.
4. Set target above cabin temperature; show heater ON when conditions require it.
5. Select rain or snow: explain that these effects currently visualize weather, and are not connected to the ThermalPlant outside temperature.
6. Show the system architecture and note the watchdog tests done separately.
