// HVACPremiumCabinBuilder.cs
// Unity 6.x. Editor-only, non-destructive premium cabin demonstrator.
// Menu: HVAC > 04 - Creer cabine premium (copie securisee)
// IMPORTANT: Unity's active scene is duplicated before any change.
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HVACPremiumCabinBuilder
{
    private const string SceneFolder = "Assets/Scenes";
    private const string PremiumScenePath = SceneFolder + "/HVAC_Cabin_Premium.unity";
    private const string AssetFolder = "Assets/HVAC_Premium_Materials";
    private const string RootName = "HVAC_Premium_Interior_VISUAL_ONLY";

    private static Material body, floor, leather, stitch, dash, blackGlass;
    private static Material trim, brushedMetal, coolLed, amberLed, displayGlass;
    private static Transform root;

    [MenuItem("HVAC/04 - Creer cabine premium (copie securisee)")]
    public static void BuildPremiumCabin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("HVAC Premium", "Arreter Play avant de creer la nouvelle scene.", "OK");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog("HVAC Premium", "Enregistrer d'abord la scene HVAC_Cabin_Demo.unity.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(SceneFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "HVAC_Premium_Materials");
        }

        // The source scene remains unchanged: duplicate it and open the duplicate.
        if (scene.path != PremiumScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!AssetDatabase.CopyAsset(scene.path, PremiumScenePath))
            {
                EditorUtility.DisplayDialog("HVAC Premium", "Impossible de dupliquer la scene. Verifiez Assets/Scenes.", "OK");
                return;
            }
            AssetDatabase.Refresh();
            scene = EditorSceneManager.OpenScene(PremiumScenePath, OpenSceneMode.Single);
        }

        // Hide only pre-existing 3D renderers. Scripts, UI Canvas and bridge stay operational.
        GameObject previousRoot = GameObject.Find(RootName);
        if (previousRoot != null) UnityEngine.Object.DestroyImmediate(previousRoot);
        foreach (Renderer existing in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            existing.enabled = false;
        }

        body = Mat("Graphite_Body", new Color(.055f,.068f,.087f), 0.42f);
        floor = Mat("Cabin_Floor", new Color(.025f,.031f,.04f), 0.16f);
        leather = Mat("Seat_Leather", new Color(.145f,.17f,.2f), 0.35f);
        stitch = Mat("Seat_Stitching", new Color(.39f,.48f,.53f), .2f);
        dash = Mat("Dashboard", new Color(.075f,.083f,.104f), .25f);
        blackGlass = Mat("Deep_Black", new Color(.014f,.024f,.039f), .8f);
        trim = Mat("Soft_Trim", new Color(.16f,.21f,.26f), .46f);
        brushedMetal = Mat("Brushed_Aluminium", new Color(.44f,.51f,.56f), .75f);
        coolLed = EmissiveMat("Ambient_IceBlue", new Color(.10f,.74f,1f), 1.4f);
        amberLed = EmissiveMat("Amber_Accent", new Color(1f,.55f,.18f), .65f);
        displayGlass = EmissiveMat("Center_Display", new Color(.045f,.14f,.23f), .37f);

        root = new GameObject(RootName).transform;
        root.position = Vector3.zero;
        BuildShell();
        BuildSeats(-.88f);
        BuildSeats(.88f);
        BuildDashboard();
        BuildConsole();
        BuildSteeringWheel();
        BuildTrim();
        PositionCameraAndLighting();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root.gameObject;
        EditorUtility.DisplayDialog("HVAC Premium", 
            "Scene premium creee : Assets/Scenes/HVAC_Cabin_Premium.unity\n\n" +
            "Scene d'origine conservee. Interface Unity et passerelle TCP/ROS 2 intactes.\n" +
            "Appuyer sur Play avec la passerelle MATLAB demarree pour verifier la connexion.", "OK");
    }

    private static Material Mat(string name, Color col, float smoothness)
    {
        string path = AssetFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (s == null) throw new InvalidOperationException("Aucun shader Lit/Standard disponible.");
            material = new Material(s) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", col);
        if (material.HasProperty("_Color")) material.SetColor("_Color", col);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material EmissiveMat(string name, Color color, float intensity)
    {
        Material material = Mat(name, color * .22f, .35f);
        material.EnableKeyword("_EMISSION");
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * intensity);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material mat,
                                   Transform parent = null, Vector3? euler = null)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent ?? root, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
        ApplyMaterial(go, mat);
        return go;
    }

    private static GameObject Cylinder(string name, Vector3 position, Vector3 scale, Material mat,
                                       Vector3? euler = null)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(root, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
        ApplyMaterial(go, mat);
        return go;
    }

    private static void ApplyMaterial(GameObject go, Material material)
    {
        Collider c = go.GetComponent<Collider>();
        if (c != null) UnityEngine.Object.DestroyImmediate(c);
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void Tube(string name, Vector3 a, Vector3 b, float radius, Material material)
    {
        Vector3 direction = b - a;
        GameObject segment = Cylinder(name, (a + b) * .5f, new Vector3(radius, direction.magnitude * .5f, radius), material);
        segment.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
    }

    private static void BuildShell()
    {
        Cube("01 | Underfloor", new Vector3(0,.12f,0), new Vector3(4.3f,.21f,6.3f), floor);
        Cube("02 | Center carpet", new Vector3(0,.241f,-.3f), new Vector3(3.2f,.025f,4.7f), body);
        Cube("03 | Left door lower", new Vector3(-2.08f,.77f,-.33f), new Vector3(.16f,1.18f,4.5f), body);
        Cube("04 | Right door lower", new Vector3(2.08f,.77f,-.33f), new Vector3(.16f,1.18f,4.5f), body);
        Cube("05 | Rear wall", new Vector3(0,1.1f,-3.0f), new Vector3(4.15f,1.8f,.12f), body);
        Cube("06 | Front wall behind dashboard", new Vector3(0,.9f,2.33f), new Vector3(4.2f,1.45f,.14f), dash);
        Cube("07 | Left sill", new Vector3(-1.88f,.39f,-.25f), new Vector3(.28f,.18f,4.8f), trim);
        Cube("08 | Right sill", new Vector3(1.88f,.39f,-.25f), new Vector3(.28f,.18f,4.8f), trim);
        Cube("09 | Roof windscreen header", new Vector3(0,2.73f,1.45f), new Vector3(4.1f,.18f,.20f), body);
        Cube("10 | Left A pillar", new Vector3(-1.92f,2.00f,1.72f), new Vector3(.16f,1.56f,.20f), body, null, new Vector3(-13f,0,-10f));
        Cube("11 | Right A pillar", new Vector3(1.92f,2.00f,1.72f), new Vector3(.16f,1.56f,.20f), body, null, new Vector3(-13f,0,10f));
    }

    private static void BuildSeats(float x)
    {
        string side = x < 0 ? "Driver" : "Passenger";
        Cube(side + " | Seat cushion", new Vector3(x,.72f,-1.22f), new Vector3(1.24f,.31f,1.25f), leather, null, new Vector3(5f,0,0));
        Cube(side + " | Seat back", new Vector3(x,1.38f,-1.72f), new Vector3(1.18f,1.27f,.27f), leather, null, new Vector3(-11f,0,0));
        Cube(side + " | Headrest", new Vector3(x,2.22f,-1.82f), new Vector3(.57f,.35f,.22f), leather);
        Cube(side + " | Left bolster", new Vector3(x-.54f,.85f,-1.21f), new Vector3(.17f,.25f,1.11f), trim);
        Cube(side + " | Right bolster", new Vector3(x+.54f,.85f,-1.21f), new Vector3(.17f,.25f,1.11f), trim);
        Cube(side + " | Back center fabric", new Vector3(x,1.40f,-1.872f), new Vector3(.79f,1.02f,.035f), body, null, new Vector3(-11f,0,0));
        Cube(side + " | Seat frame", new Vector3(x,.47f,-1.21f), new Vector3(1.10f,.18f,1.02f), brushedMetal);
        for (int i = 0; i < 7; i++)
        {
            float z = -1.65f + i * .145f;
            Cube(side + " | Cushion stitch " + i, new Vector3(x,.89f,z), new Vector3(.78f,.009f,.012f), stitch);
        }
        for (int i = 0; i < 5; i++)
        {
            float y = 1.02f + i * .18f;
            Cube(side + " | Back stitching " + i, new Vector3(x,y,-1.907f), new Vector3(.62f,.009f,.01f), stitch);
        }
    }

    private static void BuildDashboard()
    {
        Cube("Dashboard | Main body", new Vector3(0,1.34f,1.52f), new Vector3(3.95f,.61f,.87f), dash);
        Cube("Dashboard | Upper lip", new Vector3(0,1.70f,1.50f), new Vector3(3.91f,.16f,.89f), body);
        Cube("Dashboard | Aluminum accent", new Vector3(0,1.19f,1.045f), new Vector3(3.70f,.055f,.038f), brushedMetal);
        Cube("Dashboard | Center screen frame", new Vector3(.40f,1.58f,.985f), new Vector3(1.34f,.77f,.11f), trim);
        Cube("Dashboard | Center screen", new Vector3(.40f,1.58f,.919f), new Vector3(1.17f,.60f,.018f), blackGlass);
        Cube("Dashboard | Screen content accent", new Vector3(.40f,1.77f,.905f), new Vector3(.86f,.037f,.015f), coolLed);
        Cube("Dashboard | Screen lower status", new Vector3(.40f,1.39f,.902f), new Vector3(.67f,.021f,.015f), displayGlass);
        Cube("Dashboard | Driver instrument shroud", new Vector3(-1.14f,1.68f,.93f), new Vector3(1.05f,.56f,.26f), body);
        Cube("Dashboard | Driver instrument glass", new Vector3(-1.14f,1.68f,.77f), new Vector3(.89f,.39f,.04f), blackGlass);
        Cube("Dashboard | Left instrument light", new Vector3(-1.35f,1.69f,.742f), new Vector3(.24f,.025f,.012f), coolLed);
        Cube("Dashboard | Right instrument light", new Vector3(-.94f,1.69f,.742f), new Vector3(.24f,.025f,.012f), coolLed);
        for (int i = 0; i < 3; i++)
        {
            float x = 1.23f + i * .21f;
            Cube("Dashboard | Air vent " + i, new Vector3(x,1.47f,1.055f), new Vector3(.14f,.30f,.08f), blackGlass);
            for (int j = 0; j < 4; j++)
            {
                Cube("Vent blade", new Vector3(x,1.37f+j*.06f,1.006f), new Vector3(.13f,.008f,.02f), brushedMetal);
            }
        }
    }

    private static void BuildConsole()
    {
        Cube("Console | Transmission tunnel", new Vector3(0,.65f,.02f), new Vector3(.65f,.59f,2.3f), dash);
        Cube("Console | Carbon top", new Vector3(0,.96f,.10f), new Vector3(.61f,.04f,1.6f), body);
        Cube("Console | Arm rest", new Vector3(0,1.05f,-.98f), new Vector3(.70f,.22f,.72f), leather);
        Cylinder("Console | Rotary dial", new Vector3(0,.99f,.56f), new Vector3(.14f,.026f,.14f), brushedMetal);
        Cylinder("Console | Inner dial", new Vector3(0,1.026f,.56f), new Vector3(.082f,.015f,.082f), blackGlass);
        Cube("Console | Status light", new Vector3(0,1.04f,-.08f), new Vector3(.34f,.018f,.02f), coolLed);
        Cube("Console | AC control panel", new Vector3(.38f,1.17f,1.08f), new Vector3(.94f,.12f,.08f), blackGlass);
        for (int i=0; i<3; i++)
        {
            float x = .08f+i*.30f;
            Cylinder("Console | Knob " + i, new Vector3(x,1.18f,.99f), new Vector3(.078f,.022f,.078f), brushedMetal, new Vector3(90f,0,0));
        }
    }

    private static void BuildSteeringWheel()
    {
        const float x=-1.14f, y=1.36f, z=.42f, radius=.39f;
        // Segment cylinders create a crisp, efficient steering ring visible through camera.
        const int segments=28;
        for (int i=0; i<segments; i++)
        {
            float t0 = i * Mathf.PI*2f/segments;
            float t1 = (i+1) * Mathf.PI*2f/segments;
            Vector3 a = new Vector3(x+radius*Mathf.Cos(t0),y+radius*Mathf.Sin(t0),z);
            Vector3 b = new Vector3(x+radius*Mathf.Cos(t1),y+radius*Mathf.Sin(t1),z);
            Tube("Steering | Wheel rim",a,b,.036f,leather);
        }
        Cylinder("Steering | Center hub", new Vector3(x,y,z), new Vector3(.15f,.045f,.15f), leather, new Vector3(90f,0,0));
        Cube("Steering | Left spoke", new Vector3(x-.19f,y,z), new Vector3(.34f,.045f,.05f), brushedMetal);
        Cube("Steering | Right spoke", new Vector3(x+.19f,y,z), new Vector3(.34f,.045f,.05f), brushedMetal);
        Cube("Steering | Bottom spoke", new Vector3(x,y-.17f,z), new Vector3(.048f,.3f,.05f), brushedMetal);
        Cylinder("Steering | Column", new Vector3(x,y,.71f), new Vector3(.083f,.23f,.083f), dash, new Vector3(90f,0,0));
    }

    private static void BuildTrim()
    {
        Cube("Ambient | Dashboard light strip",new Vector3(0,1.13f,1.012f),new Vector3(3.60f,.016f,.023f),coolLed);
        Cube("Ambient | Left door light",new Vector3(-1.98f,1.17f,-.43f),new Vector3(.024f,.017f,3.65f),coolLed);
        Cube("Ambient | Right door light",new Vector3(1.98f,1.17f,-.43f),new Vector3(.024f,.017f,3.65f),coolLed);
        Cube("Ambient | Footwell highlight",new Vector3(0,.25f,1.27f),new Vector3(1.66f,.011f,.023f),amberLed);
        for(int i=0;i<2;i++)
        {
            float x = i==0 ? -1.96f:1.96f;
            Cube("Door | Inner grip",new Vector3(x,1.01f,-.75f),new Vector3(.11f,.105f,.71f),brushedMetal);
        }
    }

    private static void PositionCameraAndLighting()
    {
        Camera camera = Camera.main;
        if (camera == null) camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (camera != null)
        {
            camera.transform.position = new Vector3(3.85f,2.90f,-6.60f);
            camera.transform.LookAt(new Vector3(0,1.21f,-.07f));
            camera.fieldOfView = 54f;
            camera.nearClipPlane = .05f;
            camera.farClipPlane = 80f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.048f,.065f,.09f);
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.63f,.69f,.79f);
        RenderSettings.fog = false;
        Light dir = null;
        foreach (Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional) { dir = l; break; }
        }
        if (dir == null)
        {
            GameObject lightObj = new GameObject("HVAC Premium | Key Light");
            dir = lightObj.AddComponent<Light>();
            dir.type = LightType.Directional;
        }
        dir.transform.rotation = Quaternion.Euler(42f,-36f,0);
        dir.intensity = 1.25f;
        dir.color = new Color(.91f,.95f,1f);
        dir.shadows = LightShadows.None;
    }
}
