#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// AUTOSAR HVAC V4: camera setup ONLY.
// Does not modify MATLAB, ROS2, HVACDashboardUI, weather logic or the 3D asset files.
// Works with the existing Phoenix GT hierarchy:
// 00_COCKPIT_3D_REEL / Sport Body / Sport Steering Wheel, Hood.
public static class HVACAutoDriverCameraV4
{
    private const string CarName = "00_COCKPIT_3D_REEL";
    private const string DestName = "Assets/Scenes/HVAC_Drive_RealCockpit_V4_AutoCamera.unity";

    internal static float BehindWheel = 0.55f;  // meters behind steering wheel
    internal static float AboveWheel = 0.34f;   // meters above steering wheel center
    internal static float EyeLateral = 0f;      // meters, left/right
    internal static float LookDown = 0.03f;     // pitch downward (m/m)
    internal static float Fov = 68f;

    [MenuItem("HVAC/08 - AUTO camera conducteur (copie V4)")]
    public static void CreateAutomaticCameraScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("AUTOSAR HVAC", "Arrete Play avant de creer une nouvelle scene.", "OK");
            return;
        }
        Scene source = SceneManager.GetActiveScene();
        if (!source.IsValid() || string.IsNullOrEmpty(source.path))
        {
            EditorUtility.DisplayDialog("AUTOSAR HVAC", "Ouvre et sauvegarde d'abord HVAC_Drive_RealCockpit_V3.", "OK");
            return;
        }
        if (source.name.IndexOf("RealCockpit", StringComparison.OrdinalIgnoreCase) < 0)
        {
            EditorUtility.DisplayDialog("AUTOSAR HVAC", "Ouvre la scene HVAC_Drive_RealCockpit_V3 avant d'utiliser le menu 08.", "OK");
            return;
        }
        GameObject sourceCar = GameObject.Find(CarName);
        if (sourceCar == null || FindNamed(sourceCar.transform, "Sport Steering Wheel") == null)
        {
            EditorUtility.DisplayDialog("AUTOSAR HVAC", "Voiture ou volant Phoenix GT introuvable. Verifie que 00_COCKPIT_3D_REEL est present et actif.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        string dest = AssetDatabase.GenerateUniqueAssetPath(DestName);
        if (!AssetDatabase.CopyAsset(source.path, dest))
        {
            EditorUtility.DisplayDialog("AUTOSAR HVAC", "La copie de scene a echoue. La scene V3 reste intacte.", "OK");
            return;
        }

        Scene copy = EditorSceneManager.OpenScene(dest, OpenSceneMode.Single);
        if (!ApplyCamera(out string report))
        {
            Debug.LogError("HVAC V4: " + report);
            EditorUtility.DisplayDialog("AUTOSAR HVAC V4", "Copie creee, mais camera non modifiee :\n" + report, "OK");
            return;
        }
        EditorSceneManager.MarkSceneDirty(copy);
        EditorSceneManager.SaveScene(copy);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("AUTOSAR HVAC V4", "Camera placee automatiquement.\n\nNouvelle scene :\n" + dest +
            "\n\n" + report +
            "\n\nTeste dans Game. Si besoin : HVAC > 09 - Ajustement fin camera.\nLa scene V3 et AUTOSAR / ROS2 sont conserves.", "OK");
    }

    [MenuItem("HVAC/09 - Ajustement fin camera (optionnel)")]
    public static void OpenFineTuning() { HVACDriverCameraFineTuneWindow.ShowWindow(); }

    internal static bool ApplyCamera(out string report)
    {
        report = "";
        GameObject car = GameObject.Find(CarName);
        if (car == null) { report = "00_COCKPIT_3D_REEL absent."; return false; }
        Transform steering = FindNamed(car.transform, "Sport Steering Wheel");
        Transform hood = FindNamed(car.transform, "Hood");
        if (steering == null || hood == null)
        {
            report = "Volant et/ou capot introuvables dans le prefab Phoenix GT.";
            return false;
        }
        if (!TryWorldBounds(steering, out Bounds wb) || !TryWorldBounds(hood, out Bounds hb))
        {
            report = "Meshes du volant/capot indisponibles. Verifie le prefab et ses Renderers.";
            return false;
        }

        Vector3 forward = Vector3.ProjectOnPlane(hb.center - wb.center, Vector3.up);
        if (forward.sqrMagnitude < 0.03f)
        {
            // This fallback remains model dependent, hence optional fine tuning.
            forward = Vector3.ProjectOnPlane(car.transform.forward, Vector3.up);
        }
        if (forward.sqrMagnitude < 0.001f)
        {
            report = "Impossible de determiner l'avant du vehicule.";
            return false;
        }
        forward.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;

        // Anchor at true steering-wheel geometry, NOT at the imported prefab's pivot.
        Vector3 eye = wb.center - forward * BehindWheel + Vector3.up * AboveWheel + side * EyeLateral;
        Vector3 direction = (forward - Vector3.up * LookDown).normalized;

        Camera cam = null;
        GameObject namedCamera = GameObject.Find("Main Camera - DRIVER VIEW");
        if (namedCamera != null) cam = namedCamera.GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null) { report = "Camera principale introuvable."; return false; }

        Undo.RecordObject(cam.transform, "HVAC Camera conducteur automatique");
        Undo.RecordObject(cam, "HVAC Parametres camera");
        cam.transform.position = eye;
        cam.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        cam.fieldOfView = Fov;
        cam.nearClipPlane = 0.025f;
        cam.farClipPlane = 750f;
        cam.enabled = true;
        EditorUtility.SetDirty(cam.transform);
        EditorUtility.SetDirty(cam);

        // Ignore particle cameras/audio listeners of imported car; protect main camera.
        foreach (Camera embedded in car.GetComponentsInChildren<Camera>(true))
        {
            if (embedded != cam) { embedded.enabled = false; EditorUtility.SetDirty(embedded); }
        }
        foreach (AudioListener listener in car.GetComponentsInChildren<AudioListener>(true))
        {
            if (listener.gameObject != cam.gameObject) { listener.enabled = false; EditorUtility.SetDirty(listener); }
        }
        var oldCockpit = GameObject.Find("02_COCKPIT_REPLACEABLE_WITH_REAL_FBX");
        if (oldCockpit != null)
        {
            Undo.RecordObject(oldCockpit, "HVAC Masquer vieux cockpit");
            oldCockpit.SetActive(false);
            EditorUtility.SetDirty(oldCockpit);
        }
        Selection.activeGameObject = cam.gameObject;
        SceneView view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            view.pivot = eye + direction * 5f;
            view.rotation = cam.transform.rotation;
            view.size = 5f;
            view.Repaint();
        }
        report = string.Format("Volant detecte: {0}\nCamera: {1}\nAvant voiture: {2}\nChamp de vision: {3:0} degres",
            steering.name, eye.ToString("F2"), forward.ToString("F2"), cam.fieldOfView);
        return true;
    }

    internal static Transform FindNamed(Transform root, string name)
    {
        if (string.Equals(root.name, name, StringComparison.OrdinalIgnoreCase)) return root;
        foreach (Transform child in root)
        {
            Transform found = FindNamed(child, name);
            if (found != null) return found;
        }
        return null;
    }

    internal static bool TryWorldBounds(Transform root, out Bounds bounds)
    {
        bounds = new Bounds();
        bool got = false;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer) continue;
            if (!got) { bounds = renderer.bounds; got = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return got;
    }
}

public sealed class HVACDriverCameraFineTuneWindow : EditorWindow
{
    [MenuItem("HVAC/09 - Ajustement fin camera (optionnel)", true)]
    static bool CanOpen() { return !EditorApplication.isPlayingOrWillChangePlaymode; }

    public static void ShowWindow()
    {
        var window = GetWindow<HVACDriverCameraFineTuneWindow>("HVAC - Camera Conducteur");
        window.minSize = new Vector2(390, 240);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Camera automatique PHOENIX GT", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("La camera est calculee depuis le volant et le capot. Ces reglages sont FACULTATIFS et ne touchent jamais au controleur HVAC.", MessageType.Info);
        HVACAutoDriverCameraV4.BehindWheel = EditorGUILayout.Slider("Distance derriere volant (m)", HVACAutoDriverCameraV4.BehindWheel, 0.20f, 1.10f);
        HVACAutoDriverCameraV4.AboveWheel = EditorGUILayout.Slider("Hauteur au-dessus volant (m)", HVACAutoDriverCameraV4.AboveWheel, 0.10f, 0.70f);
        HVACAutoDriverCameraV4.EyeLateral = EditorGUILayout.Slider("Decalage lateral (m)", HVACAutoDriverCameraV4.EyeLateral, -0.25f, 0.25f);
        HVACAutoDriverCameraV4.LookDown = EditorGUILayout.Slider("Inclinaison vers bas", HVACAutoDriverCameraV4.LookDown, -0.15f, 0.18f);
        HVACAutoDriverCameraV4.Fov = EditorGUILayout.Slider("Champ de vision", HVACAutoDriverCameraV4.Fov, 55f, 85f);
        GUILayout.Space(8);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("APPLIQUER LE CADRAGE", GUILayout.Height(34)))
            {
                if (!HVACAutoDriverCameraV4.ApplyCamera(out string report))
                    EditorUtility.DisplayDialog("AUTOSAR HVAC", report, "OK");
                else
                {
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                    Debug.Log("HVAC V4: " + report);
                }
            }
        }
    }
}
#endif
