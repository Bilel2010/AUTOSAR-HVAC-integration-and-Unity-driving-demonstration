# Academic Presentation — AUTOSAR HVAC

**Suggested title:** Design and Simulation of an AUTOSAR-Based Automotive HVAC Control System with MATLAB/Simulink, ROS 2 and Unity 3D

**Target length:** ~16 minutes (including 3 minutes of live demo). Adapt to the professor's requirements.

| Slide | Title | Time | Core visual / content |
|---|---|---:|---|
| 1 | Title / team / course | 0:30 | One clean screenshot of final driving demo |
| 2 | Automotive thermal comfort problem | 1:00 | Hot / cold cabin scenarios; why automatic control matters |
| 3 | Objectives and scope | 1:00 | Functional requirements; software-only scope |
| 4 | Technology stack and rationale | 1:00 | AUTOSAR, MATLAB/Simulink, ROS 2, Unity icons |
| 5 | End-to-end system architecture | 1:30 | Block diagram; arrows for commands and telemetry |
| 6 | AUTOSAR Classic SWC design | 1:30 | Inputs, outputs, 100-ms runnable and fault handling |
| 7 | Control logic and safety | 1:30 | Hysteresis, invalid setpoint, fault safe-off, watchdog fallback |
| 8 | Discrete thermal plant | 1:00 | Simplified dynamic model and logged temperature curve |
| 9 | ROS 2 and Windows TCP bridge | 1:00 | Topic table, port, domain, and dataflow |
| 10 | Unity 3D driving HMI | 1:00 | V5 screenshot: temperature, setpoint, states |
| 11 | Validation and results | 1:00 | Test matrix: 24 °C, 20 °C/AC, telemetry, fallback tests |
| 12 | Live integrated demo | 3:00 | Unity action → Simulink response → Unity telemetry |
| 13 | Limitations, future work, conclusion | 1:00 | Visual-only weather; future ThermalPlant coupling |

## Speaking narrative

**Opening question:** « Comment un véhicule peut-il maintenir une température confortable alors que les conditions extérieures et les demandes du conducteur changent ? »

**Answer:** Model the automatic control with an AUTOSAR-style SWC, close the loop in Simulink, exchange data through ROS 2 and display the results in Unity.

**Key demonstration statement:** « L'interface Unity n'est pas le contrôleur : la décision de chauffer, refroidir ou maintenir vient du modèle Simulink AUTOSAR. »

**Scientifically honest limitation:** « Les changements météorologiques sont encore visuels ; la température extérieure affichée dans Unity n'est pas encore un stimulus thermique injecté dans notre plant. »

## Slides design guidance

- 16:9; white/light backgrounds for architecture/plots and dark background for Unity showcase.
- One main idea per slide; diagrams instead of paragraphs; annotate the data path with arrows.
- Show real screenshots and logs, not mockups as measured results.
- Add a QR/link to the repository on the final slide.
