#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Copy-based upgrade for the working HVAC_Drive_Immersive scene.
// NEVER edits AUTOSAR, Simulink, MATLAB files or the existing ROS2 bridge.
public static class HVACImmersiveVisualUpgradeV2
{
    const string MaterialsFolder = "Assets/HVAC_Immersive/GeneratedMaterials";

    [MenuItem("HVAC/06 - Ameliorer scene immersive V2 (copie)")]
    public static void BuildPolishedCopy()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("HVAC V2", "Arretez Play avant de continuer.", "OK");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.name.StartsWith("HVAC_Drive_Immersive", StringComparison.Ordinal))
        {
            EditorUtility.DisplayDialog("HVAC V2",
                "Ouvrez la scene HVAC_Drive_Immersive dans Assets/Scenes avant de lancer le menu 06.", "OK");
            return;
        }
        if (string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog("HVAC V2", "Sauvegardez d'abord la scene actuelle.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string sourcePath = scene.path;
        string dest = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/HVAC_Drive_Immersive_V2.unity");
        if (!AssetDatabase.CopyAsset(sourcePath, dest))
        {
            Debug.LogError("HVAC V2 : copie de la scene impossible : " + sourcePath);
            return;
        }

        Scene copy = EditorSceneManager.OpenScene(dest, OpenSceneMode.Single);
        var runtime = UnityEngine.Object.FindFirstObjectByType<HVACImmersiveRuntime>(FindObjectsInactive.Include);
        if (runtime == null)
        {
            EditorUtility.DisplayDialog("HVAC V2", "Composant HVACImmersiveRuntime introuvable. Copie creee sans modification.", "OK");
            return;
        }

        // The old Canvas already contains all working buttons and text bindings.
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas != null)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.referenceResolution = new Vector2(1600f, 900f);
                scaler.matchWidthOrHeight = 1f; // Make panels fit shorter Game view windows.
                EditorUtility.SetDirty(scaler);
            }
            SetScale(canvas, "Autonomous automotive climate card", .91f);
            SetScale(canvas, "Weather selector back", .86f);
            SetScale(canvas, "Top left speed card", .90f);
            foreach (string n in new [] { "Weather HotSun", "Weather Rain", "Weather Fog", "Weather Snow", "Tour Weather" })
                SetScale(canvas, n, .96f);

            // Keep the small technical disclaimer inside the safe lower margin.
            RectTransform disclaimer = FindRect(canvas.transform, "Weather fidelity notice");
            if (disclaimer != null)
            {
                disclaimer.anchoredPosition = new Vector2(26f, 21f);
                disclaimer.sizeDelta = new Vector2(1050f, 34f);
                var text = disclaimer.GetComponent<Text>();
                if (text != null) text.fontSize = 14;
                EditorUtility.SetDirty(disclaimer);
            }
        }

        Camera camera = Camera.main;
        if (camera == null)
            camera = UnityEngine.Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
        if (camera != null)
        {
            camera.transform.position = new Vector3(-.88f, 2.16f, -.60f);
            camera.transform.LookAt(new Vector3(-.14f, 1.81f, 18f));
            camera.fieldOfView = 64f;
            camera.nearClipPlane = .055f;
            camera.farClipPlane = 520f;
            camera.allowHDR = true;
            EditorUtility.SetDirty(camera);
        }

        var cockpit = GameObject.Find("02_COCKPIT_REPLACEABLE_WITH_REAL_FBX");
        if (cockpit != null)
        {
            Transform topEdge = FindTransform(cockpit.transform, "Windshield top edge");
            if (topEdge != null)
            {
                topEdge.localPosition += new Vector3(0f, .14f, 0f);
                EditorUtility.SetDirty(topEdge);
            }
        }

        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder("Assets/HVAC_Immersive", "GeneratedMaterials");
        Material sky = CreateSkybox();
        if (sky != null)
        {
            RenderSettings.skybox = sky;
            RenderSettings.sun = runtime.sun;
            if (camera != null) camera.clearFlags = CameraClearFlags.Skybox;
        }
        // Runtime V2 only changes visuals. No writes to dashboard or commands.
        var atmos = runtime.GetComponent<HVACImmersiveAtmosphereV2>();
        if (atmos == null) atmos = runtime.gameObject.AddComponent<HVACImmersiveAtmosphereV2>();
        atmos.driving = runtime;
        atmos.driverCamera = camera;
        atmos.sunlight = runtime.sun;
        atmos.skybox = sky;
        EditorUtility.SetDirty(atmos);

        if (!EditorSceneManager.SaveScene(copy))
            Debug.LogError("HVAC V2 : impossible de sauvegarder " + dest);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = runtime.gameObject;
        EditorUtility.DisplayDialog("HVAC V2 - Mise a niveau appliquee",
            "Copie sauvegardee : " + dest + "\n\n" +
            "Ameliorations : cadrage conducteur, interface adaptee a la hauteur, " +
            "ciel procedural (si disponible), brume moins dense, transitions meteo.\n\n" +
            "La scene V1 et la communication MATLAB/ROS2 sont conservees.\n" +
            "Meteo = VISUELLE ; pas encore connectee au ThermalPlant.\n\n" +
            "Dans Game, selectionnez 16:9 et zoom Fit.", "OK");
    }

    static Material CreateSkybox()
    {
        Shader shader = Shader.Find("Skybox/Procedural");
        if (shader == null)
        {
            Debug.LogWarning("HVAC V2 : shader Skybox/Procedural indisponible, ciel couleur unie conserve.");
            return null;
        }
        string path = MaterialsFolder + "/HVAC_Atmosphere_Sky_V2.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            m.name = "HVAC_Atmosphere_Sky_V2";
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_SkyTint")) m.SetColor("_SkyTint", new Color(.58f,.67f,.79f));
        if (m.HasProperty("_GroundColor")) m.SetColor("_GroundColor", new Color(.30f,.35f,.40f));
        if (m.HasProperty("_AtmosphereThickness")) m.SetFloat("_AtmosphereThickness", .72f);
        if (m.HasProperty("_Exposure")) m.SetFloat("_Exposure", 1.2f);
        if (m.HasProperty("_SunSize")) m.SetFloat("_SunSize", .06f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static RectTransform FindRect(Transform root, string name)
    {
        var components = root.GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform child in components)
            if (child.name == name) return child;
        return null;
    }

    static Transform FindTransform(Transform root, string name)
    {
        var transforms = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in transforms)
            if (child.name == name) return child;
        return null;
    }

    static void SetScale(Canvas canvas, string name, float scale)
    {
        RectTransform r = FindRect(canvas.transform, name);
        if (r == null) return;
        r.localScale = Vector3.one * scale;
        EditorUtility.SetDirty(r);
    }
}
#endif
