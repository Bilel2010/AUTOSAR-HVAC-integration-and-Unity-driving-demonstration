using UnityEngine;

// Presentation-only component. It never changes HVACDashboardUI's target temperature,
// ROS/TCP connection, sensor data or the Simulink thermal model.
// Key V switches camera. Keys 1-4 change VISUAL weather, T toggles the weather tour.
public sealed class HVACFinalExperienceV5 : MonoBehaviour
{
    public enum ViewMode { Cockpit, Exterior }

    public Camera driverCamera;
    public HVACImmersiveRuntime driving;
    public HVACDashboardUI dashboard;
    public ViewMode mode = ViewMode.Cockpit;
    public Renderer[] cockpitOccluders;
    public Vector3 cockpitPosition;
    public Quaternion cockpitRotation = Quaternion.identity;
    public Vector3 chasePosition;
    public Vector3 chaseLookAt;
    [Range(1f, 12f)] public float cameraTransitionSpeed = 5f;
    public bool enableSubtleCameraMotion = true;
    public bool showKeyboardHint = true;
    public bool enableLiveAirflow = true;

    private LineRenderer[] airflow;
    private Material flowMaterial;
    private GUIStyle hintStyle;
    private bool wasFlowVisible;
    private ViewMode lastAppliedMode = (ViewMode)(-1);

    void Awake()
    {
        if (driverCamera == null) driverCamera = GetComponent<Camera>();
        if (driving == null) driving = FindFirstObjectByType<HVACImmersiveRuntime>();
        if (dashboard == null && driving != null) dashboard = driving.dashboard;
        if (driverCamera != null)
        {
            driverCamera.transform.SetPositionAndRotation(cockpitPosition, cockpitRotation);
            driverCamera.fieldOfView = 72f;
        }
        if (enableLiveAirflow) CreateAirflow();
        UpdateCarOccluders();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
            mode = mode == ViewMode.Cockpit ? ViewMode.Exterior : ViewMode.Cockpit;
        if (driving != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) driving.ChooseWeather(HVACImmersiveRuntime.Weather.HotSun);
            if (Input.GetKeyDown(KeyCode.Alpha2)) driving.ChooseWeather(HVACImmersiveRuntime.Weather.Rain);
            if (Input.GetKeyDown(KeyCode.Alpha3)) driving.ChooseWeather(HVACImmersiveRuntime.Weather.Fog);
            if (Input.GetKeyDown(KeyCode.Alpha4)) driving.ChooseWeather(HVACImmersiveRuntime.Weather.Snow);
            if (Input.GetKeyDown(KeyCode.T)) driving.autoWeatherTour = !driving.autoWeatherTour;
        }
        UpdateCarOccluders();
        UpdateAirflow();
    }

    void LateUpdate()
    {
        if (driverCamera == null) return;
        bool interior = mode == ViewMode.Cockpit;
        Vector3 targetPosition = interior ? cockpitPosition : chasePosition;
        Quaternion targetRotation = interior
            ? cockpitRotation
            : Quaternion.LookRotation((chaseLookAt - chasePosition).normalized, Vector3.up);
        if (enableSubtleCameraMotion && interior)
        {
            // Visual vibration only (does not move the car or alter vehicle dynamics).
            targetPosition += new Vector3(Mathf.Sin(Time.time * 2.7f) * .003f,
                Mathf.Sin(Time.time * 3.4f) * .002f, 0f);
        }
        float smoothing = 1f - Mathf.Exp(-cameraTransitionSpeed * Time.deltaTime);
        driverCamera.transform.position = Vector3.Lerp(driverCamera.transform.position, targetPosition, smoothing);
        driverCamera.transform.rotation = Quaternion.Slerp(driverCamera.transform.rotation, targetRotation, smoothing);
        driverCamera.fieldOfView = Mathf.Lerp(driverCamera.fieldOfView, interior ? 72f : 62f, smoothing);
    }

    void UpdateCarOccluders()
    {
        if (mode == lastAppliedMode) return;
        lastAppliedMode = mode;
        if (cockpitOccluders == null) return;
        bool exterior = mode == ViewMode.Exterior;
        foreach (Renderer r in cockpitOccluders)
            if (r != null) r.enabled = exterior;
    }

    void CreateAirflow()
    {
        if (driverCamera == null) return;
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("Unlit/Color");
        if (s == null) return;
        flowMaterial = new Material(s) { name = "HVAC V5 - Flux visuel" };
        airflow = new LineRenderer[6];
        for (int i = 0; i < airflow.Length; i++)
        {
            GameObject fx = new GameObject("HVAC_Airflow_Visual_" + i);
            fx.transform.SetParent(driverCamera.transform, false);
            LineRenderer line = fx.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 18;
            line.startWidth = .008f;
            line.endWidth = .001f;
            line.sharedMaterial = flowMaterial;
            line.numCapVertices = 3;
            line.enabled = false;
            airflow[i] = line;
        }
    }

    void UpdateAirflow()
    {
        if (airflow == null) return;
        bool ready = dashboard != null && dashboard.rosConnected && !dashboard.sensorFault;
        bool heat = ready && dashboard.heaterOn;
        bool cool = ready && dashboard.acOn;
        bool visible = enableLiveAirflow && mode == ViewMode.Cockpit && (heat || cool);
        if (!visible && !wasFlowVisible) return;
        wasFlowVisible = visible;
        Color col = heat ? new Color(1f, .44f, .11f, .76f) : new Color(.16f, .70f, 1f, .76f);
        for (int i = 0; i < airflow.Length; i++)
        {
            LineRenderer line = airflow[i];
            if (line == null) continue;
            line.enabled = visible;
            if (!visible) continue;
            line.startColor = col;
            line.endColor = new Color(col.r, col.g, col.b, 0f);
            float dir = i < 3 ? -1f : 1f;
            float u = (i % 3 - 1) * .12f;
            for (int j = 0; j < line.positionCount; j++)
            {
                float t = j / (float)(line.positionCount - 1);
                float sway = Mathf.Sin(Time.time * 3.8f + t * 8f + i) * .015f;
                Vector3 pos = new Vector3(dir * (.22f + .35f * t) + u + sway,
                    -.27f - t * .20f + .03f * Mathf.Sin(Time.time * 2f + j * .25f),
                    .72f + .55f * t);
                line.SetPosition(j, pos);
            }
        }
    }

    void OnGUI()
    {
        if (!showKeyboardHint) return;
        if (hintStyle == null)
        {
            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            hintStyle.normal.textColor = new Color(.90f, .95f, 1f);
        }
        string label = "V : cockpit / vue exterieure    |    1-4 : meteo    |    T : tour automatique";
        Rect back = new Rect(Mathf.Max(8f, Screen.width * .23f), Screen.height - 33f,
            Mathf.Min(Screen.width * .54f, 620f), 26f);
        Color prev = GUI.color;
        GUI.color = new Color(.03f, .08f, .14f, .76f);
        GUI.Box(back, GUIContent.none);
        GUI.color = prev;
        GUI.Label(back, label, hintStyle);
    }

    void OnDestroy()
    {
        if (flowMaterial != null) Destroy(flowMaterial);
    }
}
