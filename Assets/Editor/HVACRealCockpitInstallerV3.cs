#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Upgrade VISUEL uniquement. Ne modifie PAS les scripts de communication,
// les seuils, les decisions du controleur, ni le modele MATLAB/Simulink.
public sealed class HVACRealCockpitInstallerV3 : EditorWindow
{
    GameObject carModel;
    float carYaw = 0f;
    float desiredWidth = 1.95f;
    bool driverOnLeft = true;
    float cameraHeight = 1.28f;
    float cameraBackOffset = 0.22f;
    float cameraLateralOffset = 0.40f;
    float fieldOfView = 72f;

    [MenuItem("HVAC/07 - Installer un cockpit 3D reel (copie)")]
    static void OpenWindow()
    {
        var w = GetWindow<HVACRealCockpitInstallerV3>(true, "AUTOSAR HVAC - Cockpit 3D V3", true);
        w.minSize = new Vector2(450, 425);
        w.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("V3 - Integration d'un modele 3D reel", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Commence dans la scene HVAC_Drive_Immersive_V2. Importe d'abord un modele FBX/prefab " +
            "de voiture avec interieur detaille, puis depose-le ici. L'outil cree une COPIE de scene " +
            "et remplace uniquement le cockpit geometrique. Unity/MATLAB/ROS2 restent inchanges.",
            MessageType.Info);
        carModel = (GameObject)EditorGUILayout.ObjectField("Modele de voiture (prefab / FBX)", carModel, typeof(GameObject), false);
        carYaw = EditorGUILayout.Slider("Rotation Y du modele", carYaw, -180f, 180f);
        desiredWidth = EditorGUILayout.Slider("Largeur voiture en metres", desiredWidth, 1.6f, 2.4f);
        driverOnLeft = EditorGUILayout.Toggle("Conducteur a gauche", driverOnLeft);
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Camera conducteur (reglable ensuite dans la Scene)", EditorStyles.boldLabel);
        cameraHeight = EditorGUILayout.Slider("Hauteur camera / sol", cameraHeight, 0.95f, 1.60f);
        cameraBackOffset = EditorGUILayout.Slider("Camera en arriere / centre", cameraBackOffset, -0.9f, 1.2f);
        cameraLateralOffset = EditorGUILayout.Slider("Decalage lateral", cameraLateralOffset, .2f, .8f);
        fieldOfView = EditorGUILayout.Slider("Champ de vision", fieldOfView, 55f, 90f);
        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox("L'orientation et la camera peuvent varier selon le modele 3D. Ajuste Rotation Y et la camera dans Unity apres generation. Le modele 3D n'est PAS fourni par ce script.", MessageType.None);
        using (new EditorGUI.DisabledScope(carModel == null || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("CREER HVAC_Drive_RealCockpit_V3.unity", GUILayout.Height(42)))
                BuildCopy();
        }
    }

    static bool TryBounds(GameObject root, out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
            if (!found) { bounds = r.bounds; found = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return found && bounds.size.x > 0.0001f && bounds.size.y > 0.0001f;
    }

    void BuildCopy()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("HVAC V3", "Arrete le mode Play avant de continuer.", "OK");
            return;
        }
        var source = SceneManager.GetActiveScene();
        if (!source.IsValid() || !source.name.StartsWith("HVAC_Drive_Immersive"))
        {
            EditorUtility.DisplayDialog("HVAC V3", "Ouvre d'abord HVAC_Drive_Immersive_V2.unity.", "OK");
            return;
        }
        if (string.IsNullOrEmpty(source.path) || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        string dest = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/HVAC_Drive_RealCockpit_V3.unity");
        if (!AssetDatabase.CopyAsset(source.path, dest))
        {
            EditorUtility.DisplayDialog("HVAC V3", "Impossible de copier la scene source.", "OK");
            return;
        }
        Scene scene = EditorSceneManager.OpenScene(dest, OpenSceneMode.Single);
        var root = GameObject.Find("HVAC_IMMERSIVE_SCENE");
        var oldCabin = GameObject.Find("02_COCKPIT_REPLACEABLE_WITH_REAL_FBX");
        if (oldCabin == null)
        {
            EditorUtility.DisplayDialog("HVAC V3", "Ancien cockpit introuvable. Scene copiee mais pas modifiee.", "OK");
            return;
        }
        // Le moteur thermique, la telemetrie et les boutons Unity restent dans
        // AUTOSAR_HVAC_Cabin et 04_PREMIUM_AUTOSAR_HMI : ne pas les supprimer.
        var car = PrefabUtility.InstantiatePrefab(carModel, scene) as GameObject;
        if (car == null) car = Instantiate(carModel);
        car.name = "00_COCKPIT_3D_REEL";
        if (root != null) car.transform.SetParent(root.transform, true);
        car.transform.position = Vector3.zero;
        car.transform.rotation = Quaternion.Euler(0f, carYaw, 0f);

        if (!TryBounds(car, out Bounds b))
        {
            DestroyImmediate(car);
            EditorUtility.DisplayDialog("HVAC V3", "Le modele ne contient pas de Renderers 3D visibles. Verifie le prefab.", "OK");
            return;
        }
        // Mise a l'echelle en fonction de la largeur reelle du modele.
        float factor = Mathf.Clamp(desiredWidth / b.size.x, 0.001f, 1000f);
        car.transform.localScale = car.transform.localScale * factor;
        if (!TryBounds(car, out b)) return;
        car.transform.position += new Vector3(-b.center.x, -b.min.y + .035f, -b.center.z);
        if (!TryBounds(car, out b)) return;

        // Desactiver les cameras internes au prefab, sans toucher a Main Camera.
        foreach (var c in car.GetComponentsInChildren<Camera>(true)) c.enabled = false;
        foreach (var audio in car.GetComponentsInChildren<AudioListener>(true)) audio.enabled = false;
        oldCabin.SetActive(false);

        var cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            float x = (driverOnLeft ? -1f : 1f) * cameraLateralOffset;
            // L'avant du vehicule doit etre oriente vers le +Z monde.
            Vector3 rear = Vector3.back * cameraBackOffset;
            cam.transform.position = new Vector3(b.center.x + x, b.min.y + cameraHeight, b.center.z) + rear;
            cam.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            cam.fieldOfView = fieldOfView;
            cam.nearClipPlane = .025f;
            cam.farClipPlane = 750f;
            EditorUtility.SetDirty(cam);
        }
        var runtime = FindFirstObjectByType<HVACImmersiveRuntime>(FindObjectsInactive.Include);
        if (runtime != null)
        {
            // Le volant et les flux des cubes anciens ne doivent pas apparaitre.
            // Les donnees dashboard, ROS2 et les commandes restent intactes.
            runtime.wheel = null;
            runtime.airRenderers = new Renderer[0];
            runtime.airStreaks = new Transform[0];
            EditorUtility.SetDirty(runtime);
        }
        EditorUtility.SetDirty(car);
        EditorUtility.SetDirty(oldCabin);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = car;
        EditorUtility.DisplayDialog("HVAC V3", "Scene creee : " + dest +
            "\n\nLe cockpit geometrique est masque, le modele 3D est insere."
            + "\nLa liaison TCP/MATLAB/ROS2 et les boutons HVAC sont conserves."
            + "\n\nIMPORTANT : regle le cadrage dans Scene si necessaire."
            + "\nLa meteo de Unity reste visuelle seulement : non reliee au ThermalPlant.", "OK");
        Close();
    }
}
#endif
