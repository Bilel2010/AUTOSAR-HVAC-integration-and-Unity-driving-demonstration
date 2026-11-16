using UnityEngine;

// Local prototype only. The authoritative AUTOSAR control remains in Simulink.
[RequireComponent(typeof(HVACDashboardUI))]
public class HVACLocalThermalDemo : MonoBehaviour
{
    [Header("Local demo parameters (not ROS 2)")]
    [Range(-20f, 60f)] public float outsideTempC = 28f;
    [Range(1f, 15f)] public float simulationSpeed = 5f;

    private const float SampleTime = 0.1f;
    private const float AmbientGain = 0.02f;
    private const float HeaterGain = 0.10f;
    private const float CoolingGain = 0.25f;

    private HVACDashboardUI dashboard;
    private bool heating;
    private bool cooling;
    private float accumulator;

    private void Start()
    {
        dashboard = GetComponent<HVACDashboardUI>();
        dashboard.cabinTempC = 18f;
        dashboard.targetTempC = 22.5f;
        dashboard.rosConnected = false;
        dashboard.sensorFault = false;
        dashboard.commLost = false;
        heating = false;
        cooling = false;
        accumulator = 0f;
        UpdateIndicators();
        Debug.Log("HVAC LOCAL DEMO : simulation thermique active (sans ROS 2 ni Simulink).");
    }

    private void Update()
    {
        if (dashboard == null) return;
        accumulator += Time.deltaTime * simulationSpeed;

        // Deterministic 100 ms discrete model, even at accelerated display speed.
        int ticks = 0;
        while (accumulator >= SampleTime && ticks < 100)
        {
            StepModel();
            accumulator -= SampleTime;
            ticks++;
        }
    }

    private void StepModel()
    {
        // Equivalent to the previously used heating/cooling relay hysteresis.
        float error = dashboard.targetTempC - dashboard.cabinTempC;
        heating = heating ? error > 0f : error >= 1f;
        cooling = cooling ? error < 0f : error <= -1f;

        float dTdt = AmbientGain * (outsideTempC - dashboard.cabinTempC)
                     + HeaterGain * (heating ? 1f : 0f)
                     - CoolingGain * (cooling ? 1f : 0f);
        dashboard.cabinTempC += SampleTime * dTdt;
        UpdateIndicators();
    }

    private void UpdateIndicators()
    {
        dashboard.heaterOn = heating;
        dashboard.acOn = cooling;
        dashboard.maintainOn = !heating && !cooling;
    }
}
