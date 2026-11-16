using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class HVACEnableLocalThermalDemo
{
    [MenuItem("HVAC/02 - Activer simulation thermique locale")]
    public static void EnableLocalSimulation()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("HVAC : quittez le mode Play avant d'activer la simulation locale.");
            return;
        }

        HVACDashboardUI dashboard = Object.FindFirstObjectByType<HVACDashboardUI>();
        if (dashboard == null)
        {
            Debug.LogError("HVAC : ouvrez d'abord la scene HVAC_Cabin_Demo.");
            return;
        }

        if (dashboard.GetComponent<HVACLocalThermalDemo>() == null)
            Undo.AddComponent<HVACLocalThermalDemo>(dashboard.gameObject);

        EditorSceneManager.MarkSceneDirty(dashboard.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = dashboard.gameObject;
        Debug.Log("HVAC : simulation thermique locale ajoutee. Cliquez sur Play puis +0.5/-0.5 C.");
    }
}
