using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Editor utility compatible with standard Unity 3D projects.
public static class HVACSceneBuilder
{
    [MenuItem("HVAC/01 - Generer habitacle demo")]
    public static void BuildScene()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform root = new GameObject("AUTOSAR_HVAC_Cabin").transform;

        Material dark = GetMaterial("HVAC_Dark", new Color(0.10f, 0.15f, 0.22f));
        Material gray = GetMaterial("HVAC_Gray", new Color(0.39f, 0.45f, 0.51f));
        Material seat = GetMaterial("HVAC_Seat", new Color(0.25f, 0.31f, 0.39f));
        Material accent = GetMaterial("HVAC_Accent", new Color(0.04f, 0.73f, 0.92f));
        Material glass = GetMaterial("HVAC_Windshield", new Color(0.18f, 0.32f, 0.47f));

        Box("Cabin floor", root, new Vector3(0, 0, 0), new Vector3(4.1f, .14f, 5.0f), dark);
        Box("Left door", root, new Vector3(-2.02f, .68f, 0), new Vector3(.11f, 1.27f, 4.6f), dark);
        Box("Right door", root, new Vector3(2.02f, .68f, 0), new Vector3(.11f, 1.27f, 4.6f), dark);
        Box("Front windscreen", root, new Vector3(0, 1.98f, 2.25f), new Vector3(3.95f, 1.02f, .08f), glass);
        Box("Dashboard", root, new Vector3(0, 1.15f, 1.45f), new Vector3(3.85f, .63f, .60f), dark);
        Box("Dashboard top", root, new Vector3(0, 1.5f, 1.38f), new Vector3(3.9f, .12f, .76f), gray);
        Box("Central display", root, new Vector3(.2f, 1.29f, 1.105f), new Vector3(.9f, .37f, .055f), accent);
        Box("Lower center console", root, new Vector3(.2f, .67f, .8f), new Vector3(.75f, .62f, .9f), dark);

        for (int i = -1; i <= 1; i += 2)
        {
            float x = 1.08f * i;
            Box("Air vent " + i, root, new Vector3(x, 1.28f, 1.104f),
                new Vector3(.6f, .14f, .075f), accent);
            Box("Seat base " + i, root, new Vector3(x, .43f, -.9f),
                new Vector3(1.05f, .24f, 1.2f), seat);
            Box("Seat back " + i, root, new Vector3(x, 1.02f, -1.42f),
                new Vector3(1.04f, 1.11f, .22f), seat);
            Box("Headrest " + i, root, new Vector3(x, 1.66f, -1.43f),
                new Vector3(.55f, .30f, .20f), gray);
        }

        GameObject steering = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        steering.name = "Steering wheel concept";
        steering.transform.SetParent(root);
        steering.transform.position = new Vector3(-1.1f, 1.15f, .85f);
        steering.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        steering.transform.localScale = new Vector3(.36f, .035f, .36f);
        steering.GetComponent<Renderer>().sharedMaterial = dark;
        Box("Steering center", root, new Vector3(-1.1f, 1.15f, .78f),
            new Vector3(.20f, .19f, .08f), accent);

        GameObject cameraObj = new GameObject("Main Camera");
        Camera camera = cameraObj.AddComponent<Camera>();
        cameraObj.tag = "MainCamera";
        cameraObj.transform.position = new Vector3(0f, 2.38f, -3.85f);
        cameraObj.transform.LookAt(new Vector3(0f, 1.03f, .92f));
        camera.fieldOfView = 58f;
        camera.clearFlags = CameraClearFlags.Skybox;

        GameObject lightObj = new GameObject("Sunlight");
        Light sun = lightObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.3f;
        lightObj.transform.rotation = Quaternion.Euler(43f, -28f, 0f);
        RenderSettings.ambientLight = new Color(.42f, .45f, .5f);

        root.gameObject.AddComponent<HVACDashboardUI>();
        AssetDatabase.SaveAssets();
        string scenePath = "Assets/Scenes/HVAC_Cabin_Demo.unity";
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, scenePath))
            throw new System.Exception("Scene non sauvegardee : " + scenePath);

        Selection.activeGameObject = root.gameObject;
        Debug.Log("UNITY3D-01 : SCENE CREEE -> " + scenePath +
                  " | Cliquez Play pour tester les boutons de consigne locaux.");
    }

    private static void Box(string name, Transform parent, Vector3 position,
                            Vector3 size, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Material GetMaterial(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material result = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (result != null) return result;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) throw new System.Exception("Aucun shader compatible trouve.");

        result = new Material(shader);
        result.color = color;
        AssetDatabase.CreateAsset(result, path);
        return result;
    }
}
