#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// HVAC FINAL V5 - runs exclusively in the Unity Editor.
// Copies the user's current V3/V4 scene, never overwrites existing scenes or model assets.
// No modifications to HVACDashboardUI, HVACTcpWindowsBridge or Simulink/ROS2.
public static class HVACFinalSceneV5
{
    const string CarName = "00_COCKPIT_3D_REEL";
    const string SourcePrefix = "HVAC_Drive_RealCockpit";
    const string Output = "Assets/Scenes/HVAC_Drive_FINAL_V5.unity";

    [MenuItem("HVAC/10 - FINALISER conduite AUTOSAR (V5)")]
    public static void CreateFinalScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("HVAC V5", "Arrete Play avant de finaliser la scene.", "OK");
            return;
        }
        Scene source = SceneManager.GetActiveScene();
        if (!source.IsValid() || string.IsNullOrEmpty(source.path) ||
            !source.name.StartsWith(SourcePrefix, StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("HVAC V5", "Ouvre HVAC_Drive_RealCockpit_V4_AutoCamera (ou V3), puis relance le menu 10.", "OK");
            return;
        }
        if (GameObject.Find(CarName) == null)
        {
            EditorUtility.DisplayDialog("HVAC V5", "Le prefab 00_COCKPIT_3D_REEL est introuvable dans la scene.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        string output = AssetDatabase.GenerateUniqueAssetPath(Output);
        if (!AssetDatabase.CopyAsset(source.path, output))
        {
            EditorUtility.DisplayDialog("HVAC V5", "Impossible de copier la scene. La version source reste intacte.", "OK");
            return;
        }
        Scene scene = EditorSceneManager.OpenScene(output, OpenSceneMode.Single);
        if (!Configure(out string info))
        {
            Debug.LogError("HVAC V5 : " + info);
            EditorUtility.DisplayDialog("HVAC V5", "La copie a ete creee, mais la configuration est incomplete.\n" + info, "OK");
            return;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("HVAC FINAL V5",
            "Nouvelle scene : " + output +
            "\n\nVoiture alignee sur la route et camera recalculee.\n" +
            "Une vue EXTERIEURE est disponible avec V pendant Play.\n" +
            "Touches : 1=Soleil 2=Pluie 3=Brume 4=Neige T=Tour auto.\n\n" + info +
            "\n\nIMPORTANT: les meteo restent visuelles, pas de modification de ThermalPlant.", "OK");
    }

    static bool Configure(out string report)
    {
        report = "";
        GameObject car = GameObject.Find(CarName);
        if (car == null) { report = "Voiture introuvable."; return false; }
        Transform carShell = Find(car.transform, "Sport Car");
        Transform hood = Find(car.transform, "Hood");
        Transform steering = Find(car.transform, "Sport Steering Wheel");
        if (carShell == null || !GetBounds(carShell, out Bounds shell))
        {
            report = "Le mesh Sport Car est introuvable. Impossible de calculer un cadrage fiable.";
            return false;
        }
        // The previous version estimated the eye from steering renderer bounds alone;
        // Phoenix GT uses imported offsets which can place the camera outside the cabin.
        // Infer forward from the hood and the largest horizontal dimension of the CAR BODY.
        bool longX = shell.size.x > shell.size.z;
        float sign;
        if (hood != null && GetBounds(hood, out Bounds hb))
        {
            float diff = longX ? hb.center.x - shell.center.x : hb.center.z - shell.center.z;
            sign = Mathf.Abs(diff) > 0.05f ? Mathf.Sign(diff) : 1f;
        }
        else
        {
            sign = 1f;
        }
        Vector3 guessedForward = longX ? Vector3.right * sign : Vector3.forward * sign;
        Quaternion align = Quaternion.FromToRotation(guessedForward, Vector3.forward);
        Undo.RecordObject(car.transform, "HVAC V5 - aligner voiture");
        car.transform.rotation = align * car.transform.rotation;
        if (!GetBounds(carShell, out shell))
        {
            report = "Impossible de lire la carrosserie apres rotation."; return false;
        }
        // Keep car centered on road. The existing environment scrolls along world +Z.
        car.transform.position += new Vector3(-shell.center.x, 0.025f - shell.min.y, -shell.center.z);
        if (!GetBounds(carShell, out shell))
        {
            report = "Impossible de recentrer le modele."; return false;
        }

        float driverX = shell.center.x - 0.36f; // left-hand drive fallback
        float driverZ = shell.center.z - 0.17f * shell.size.z;
        if (steering != null && GetBounds(steering, out Bounds sb))
        {
            // Accept the steering wheel only if its imported mesh truly lies within the body.
            if (sb.center.x > shell.min.x + 0.10f && sb.center.x < shell.max.x - 0.10f &&
                sb.center.z > shell.min.z + 0.20f && sb.center.z < shell.max.z - 0.20f &&
                sb.center.y > shell.min.y + 0.35f && sb.center.y < shell.max.y + 0.20f)
            {
                driverX = sb.center.x;
                driverZ = sb.center.z - 0.40f;
            }
        }
        driverX = Mathf.Clamp(driverX, shell.min.x + 0.20f, shell.max.x - 0.20f);
        driverZ = Mathf.Clamp(driverZ, shell.min.z + 0.55f, shell.max.z - 0.55f);
        float driverY = shell.min.y + Mathf.Clamp(shell.size.y * .74f, 1.00f, 1.27f);
        driverY = Mathf.Min(driverY, shell.max.y - .09f);
        Vector3 eye = new Vector3(driverX, driverY, driverZ);

        GameObject camObj = GameObject.Find("Main Camera - DRIVER VIEW");
        Camera cam = camObj != null ? camObj.GetComponent<Camera>() : Camera.main;
        if (cam == null) { report = "Main Camera - DRIVER VIEW introuvable."; return false; }
        Undo.RecordObject(cam.transform, "HVAC V5 - camera conducteur");
        Undo.RecordObject(cam, "HVAC V5 - FOV");
        cam.transform.position = eye;
        cam.transform.rotation = Quaternion.LookRotation(new Vector3(0f, -.025f, 1f), Vector3.up);
        cam.fieldOfView = 72f;
        cam.nearClipPlane = .02f;
        cam.farClipPlane = 900f;
        cam.enabled = true;
        cam.tag = "MainCamera";

        // Remove opaque meshes that block the lens when inside this imported coupe.
        // Only the COPIED SCENE instance is changed; the original FBX / prefab is untouched.
        var cockpitOccluders = new List<Renderer>();
        int hidden = 0;
        hidden += HideMeshByName(car.transform, "Roof", cockpitOccluders);
        hidden += HideMeshByName(car.transform, "Windshield", cockpitOccluders);
        hidden += HideMeshByName(car.transform, "Hood", cockpitOccluders);

        // Keep a single active gameplay camera and audio listener.
        foreach (Camera embedded in car.GetComponentsInChildren<Camera>(true))
            if (embedded != cam) { embedded.enabled = false; EditorUtility.SetDirty(embedded); }
        foreach (AudioListener audio in car.GetComponentsInChildren<AudioListener>(true))
            if (audio.gameObject != cam.gameObject) { audio.enabled = false; EditorUtility.SetDirty(audio); }

        GameObject oldPrimitive = GameObject.Find("02_COCKPIT_REPLACEABLE_WITH_REAL_FBX");
        if (oldPrimitive != null) oldPrimitive.SetActive(false);

        HVACImmersiveRuntime driving = UnityEngine.Object.FindFirstObjectByType<HVACImmersiveRuntime>(FindObjectsInactive.Include);
        HVACDashboardUI dashboard = UnityEngine.Object.FindFirstObjectByType<HVACDashboardUI>(FindObjectsInactive.Include);
        if (driving != null)
        {
            driving.wheel = null;
            driving.airStreaks = new Transform[0];
            driving.airRenderers = new Renderer[0];
            driving.autoWeatherTour = true;
            driving.autoWeatherIntervalSeconds = 24f;
            EditorUtility.SetDirty(driving);
        }

        HVACFinalExperienceV5 experience = cam.GetComponent<HVACFinalExperienceV5>();
        if (experience == null) experience = cam.gameObject.AddComponent<HVACFinalExperienceV5>();
        experience.driverCamera = cam;
        experience.driving = driving;
        experience.dashboard = dashboard;
        experience.cockpitPosition = eye;
        experience.cockpitRotation = cam.transform.rotation;
        experience.chasePosition = shell.center - Vector3.forward * (shell.size.z * .85f + 3.5f) + Vector3.up * 2.65f;
        experience.chaseLookAt = shell.center + Vector3.up * 1.1f + Vector3.forward * 5f;
        experience.mode = HVACFinalExperienceV5.ViewMode.Cockpit;
        experience.cockpitOccluders = cockpitOccluders.ToArray();
        EditorUtility.SetDirty(experience);
        EditorUtility.SetDirty(car.transform);
        EditorUtility.SetDirty(cam);

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas != null)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1600f, 900f);
                scaler.matchWidthOrHeight = 0.5f;
                EditorUtility.SetDirty(scaler);
            }
        }
        Selection.activeGameObject = cam.gameObject;
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.pivot = eye + cam.transform.forward * 4f;
            sv.rotation = cam.transform.rotation;
            sv.size = 4f;
            sv.Repaint();
        }
        report = string.Format(
            "Avant de voiture recalibre : {0}\nPosition camera : {1}\nGabarit carrosserie : {2}\nMeshes occultants desactives : {3}\nControls ROS/TCP conserves.",
            guessedForward, eye.ToString("F2"), shell.size.ToString("F2"), hidden);
        return true;
    }

    static int HideMeshByName(Transform parent, string target, List<Renderer> occluders)
    {
        Transform item = Find(parent, target);
        if (item == null) return 0;
        int n = 0;
        foreach (Renderer r in item.GetComponentsInChildren<Renderer>(true))
        {
            Undo.RecordObject(r, "HVAC V5 - degager la vue conducteur");
            r.enabled = false;
            EditorUtility.SetDirty(r);
            occluders.Add(r);
            n++;
        }
        return n;
    }

    static Transform Find(Transform root, string exactName)
    {
        if (root.name.Equals(exactName, StringComparison.OrdinalIgnoreCase)) return root;
        foreach (Transform child in root)
        {
            Transform t = Find(child, exactName);
            if (t != null) return t;
        }
        return null;
    }

    static bool GetBounds(Transform root, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
            if (!found) { bounds = r.bounds; found = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return found && bounds.size.sqrMagnitude > 0.01f;
    }
}
#endif
