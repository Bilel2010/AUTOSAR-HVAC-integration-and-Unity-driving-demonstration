using UnityEngine;

// Unity3D-01: local visualization only.
// ROS 2 values will be connected to these public fields in a later step.
public class HVACDashboardUI : MonoBehaviour
{
    [Range(16f, 30f)] public float targetTempC = 22.5f;
    public float cabinTempC = 18f;
    public bool rosConnected = false;
    public bool heaterOn = false;
    public bool acOn = false;
    public bool maintainOn = true;
    public bool sensorFault = false;
    public bool commLost = false;

    private GUIStyle titleStyle;
    private GUIStyle valueStyle;
    private GUIStyle noteStyle;

    private void OnGUI()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 21;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;

            valueStyle = new GUIStyle(GUI.skin.label);
            valueStyle.fontSize = 18;
            valueStyle.normal.textColor = Color.white;

            noteStyle = new GUIStyle(GUI.skin.label);
            noteStyle.fontSize = 12;
            noteStyle.normal.textColor = new Color(1f, 0.8f, 0.3f);
        }

        GUILayout.BeginArea(new Rect(18, 18, 380, 292), GUI.skin.box);
        GUILayout.Label("AUTOSAR HVAC | UNITY DEMO", titleStyle);
        GUILayout.Space(7);
        GUILayout.Label("Consigne : " + targetTempC.ToString("F1") + " °C", valueStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("- 0.5 °C", GUILayout.Height(32)))
            targetTempC = Mathf.Clamp(targetTempC - 0.5f, 16f, 30f);
        if (GUILayout.Button("+ 0.5 °C", GUILayout.Height(32)))
            targetTempC = Mathf.Clamp(targetTempC + 0.5f, 16f, 30f);
        GUILayout.EndHorizontal();
        GUILayout.Space(8);
        GUILayout.Label("Habitacle : " + cabinTempC.ToString("F1") + " °C", valueStyle);
        GUILayout.Label("Chauffage : " + (heaterOn ? "ON" : "OFF") +
                        "  |  Climatisation : " + (acOn ? "ON" : "OFF"));
        GUILayout.Label("Maintien : " + (maintainOn ? "ON" : "OFF") +
                        "  |  Defaut capteur : " + (sensorFault ? "OUI" : "NON"));
        GUILayout.Label("Communication perdue : " + (commLost ? "OUI" : "NON"));
        GUILayout.Space(8);
        GUILayout.Label(rosConnected ? "ROS 2 : CONNECTE" : "ROS 2 : NON CONNECTE", noteStyle);
        GUILayout.Label("Interface locale uniquement : aucune commande ROS 2 envoyee.", noteStyle);
        GUILayout.EndArea();
    }
}
