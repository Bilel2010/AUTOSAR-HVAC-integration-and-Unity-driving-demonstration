using UnityEngine;
using UnityEngine.Rendering;

// Additional visual-only layer. It never writes HVACDashboardUI or ROS/TCP data.
// Runs after HVACImmersiveRuntime's weather update to keep the landscape legible.
[DefaultExecutionOrder(200)]
public sealed class HVACImmersiveAtmosphereV2 : MonoBehaviour
{
    public HVACImmersiveRuntime driving;
    public Camera driverCamera;
    public Light sunlight;
    public Material skybox;

    [Header("Visual atmosphere — no ThermalPlant effect")]
    [Range(0.001f, 0.025f)] public float fogSun = .0014f;
    [Range(0.001f, 0.025f)] public float fogRain = .0040f;
    [Range(0.001f, 0.025f)] public float fogMist = .0080f;
    [Range(0.001f, 0.025f)] public float fogSnow = .0050f;
    [Range(.1f, 8f)] public float transitionSpeed = 2f;

    float fogNow;
    Color fogColorNow;
    float sunlightIntensity;
    bool initialized;

    void Start()
    {
        if (driverCamera == null) driverCamera = Camera.main;
        if (driving == null) driving = GetComponent<HVACImmersiveRuntime>();
        if (sunlight == null && driving != null) sunlight = driving.sun;
        if (driverCamera != null)
        {
            driverCamera.clearFlags = skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            driverCamera.allowHDR = true;
        }
        if (skybox != null)
        {
            RenderSettings.skybox = skybox;
            if (sunlight != null) RenderSettings.sun = sunlight;
        }
        fogNow = RenderSettings.fogDensity;
        fogColorNow = RenderSettings.fogColor;
        sunlightIntensity = sunlight != null ? sunlight.intensity : 1f;
        initialized = true;
    }

    void LateUpdate()
    {
        if (!initialized || driving == null) return;
        float density = fogSun;
        float targetLight = 1.4f;
        Color mist = new Color(.72f, .83f, .94f);
        Color skyTint = new Color(.58f, .67f, .79f);
        Color groundTint = new Color(.30f, .35f, .40f);

        switch (driving.currentWeather)
        {
            case HVACImmersiveRuntime.Weather.Rain:
                density = fogRain;
                targetLight = .78f;
                mist = new Color(.49f, .60f, .73f);
                skyTint = new Color(.43f, .52f, .65f);
                groundTint = new Color(.30f, .34f, .38f);
                break;
            case HVACImmersiveRuntime.Weather.Fog:
                density = fogMist;
                targetLight = .86f;
                mist = new Color(.70f, .78f, .82f);
                skyTint = new Color(.66f, .72f, .75f);
                groundTint = new Color(.44f, .50f, .52f);
                break;
            case HVACImmersiveRuntime.Weather.Snow:
                density = fogSnow;
                targetLight = 1.0f;
                mist = new Color(.80f, .88f, .96f);
                skyTint = new Color(.74f, .81f, .91f);
                groundTint = new Color(.58f, .63f, .68f);
                break;
        }

        float f = 1f - Mathf.Exp(-Mathf.Max(.1f, transitionSpeed) * Time.deltaTime);
        fogNow = Mathf.Lerp(fogNow, density, f);
        fogColorNow = Color.Lerp(fogColorNow, mist, f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = fogNow;
        RenderSettings.fogColor = fogColorNow;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight,
            driving.currentWeather == HVACImmersiveRuntime.Weather.HotSun
              ? new Color(.73f, .76f, .83f)
              : new Color(.61f, .67f, .75f), f);

        if (sunlight != null)
        {
            sunlightIntensity = Mathf.Lerp(sunlightIntensity, targetLight, f);
            sunlight.intensity = sunlightIntensity;
        }
        if (skybox != null)
        {
            if (skybox.HasProperty("_SkyTint"))
                skybox.SetColor("_SkyTint", Color.Lerp(skybox.GetColor("_SkyTint"), skyTint, f));
            if (skybox.HasProperty("_GroundColor"))
                skybox.SetColor("_GroundColor", Color.Lerp(skybox.GetColor("_GroundColor"), groundTint, f));
        }
        if (driverCamera != null && skybox == null)
            driverCamera.backgroundColor = fogColorNow;
    }
}
