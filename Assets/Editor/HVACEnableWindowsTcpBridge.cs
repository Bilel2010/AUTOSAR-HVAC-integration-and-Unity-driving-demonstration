using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class HVACEnableWindowsTcpBridge
{
    [MenuItem("HVAC/03 - Activer liaison TCP Windows")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("HVAC TCP : sortir de Play avant l'installation.");
            return;
        }
        HVACDashboardUI ui = Object.FindFirstObjectByType<HVACDashboardUI>();
        if (ui == null)
        {
            Debug.LogError("HVAC TCP : ouvrir Assets/Scenes/HVAC_Cabin_Demo.unity.");
            return;
        }
        // Disable any previous local-only thermal demo by name, without
        // requiring that optional script to be installed in this project.
        foreach (MonoBehaviour component in ui.GetComponents<MonoBehaviour>())
        {
            if (component != null && component.GetType().Name == "HVACLocalThermalDemo")
            {
                Undo.RecordObject(component, "Disable local HVAC model");
                component.enabled = false;
            }
        }
        if (ui.GetComponent<HVACTcpWindowsBridge>() == null)
            Undo.AddComponent<HVACTcpWindowsBridge>(ui.gameObject);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = ui.gameObject;
        Debug.Log("HVAC TCP : installation OK. Executer HVAC_Bridge_Start_TCP.m dans MATLAB, puis Unity Play.");
    }
}
