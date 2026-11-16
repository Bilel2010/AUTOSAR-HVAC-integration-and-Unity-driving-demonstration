#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// AUTOSAR HVAC immersive demonstration - self-contained Editor scene generator.
// Menu: HVAC / 05 - Creer cockpit conduite immersive
// Requires existing HVACDashboardUI and HVACTcpWindowsBridge scripts from validated kit.
public static class HVACImmersiveSceneBuilder
{
    const string Folder = "Assets/HVAC_Immersive";
    const string MatFolder = Folder + "/GeneratedMaterials";
    const string SceneFolder = "Assets/Scenes";
    const string ScenePath = SceneFolder + "/HVAC_Drive_Immersive.unity";
    const string TextureFolder = Folder + "/GeneratedTextures";

    static Transform sceneRoot;
    static Material leather, black, carbon, silver, darkMetal, chrome, cyan, orange, blueGlass;
    static Material road, laneWhite, grass, treeGreen, treeDark, bark, mountain, mountainSnow, skyAccent;
    static Material upholstery, windowEdge, screen, roadShoulder;
    static Mesh coneMesh, ringMesh;
    static Sprite roundSprite;
    static Font font;

    [MenuItem("HVAC/05 - Creer cockpit conduite immersive")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("AUTOSAR HVAC", "Arretez Play dans Unity avant la generation.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        EnsureFolder(Folder);
        EnsureFolder(MatFolder);
        EnsureFolder(TextureFolder);
        string path = AssetDatabase.GenerateUniqueAssetPath(ScenePath);
        if (path != ScenePath) Debug.Log("HVAC : une scene existe deja, nouvelle scene creee : " + path);
        var old = UnityEngine.Object.FindFirstObjectByType<HVACTcpWindowsBridge>(FindObjectsInactive.Include);
        string oldHost = old != null ? old.host : "127.0.0.1";
        int oldPort = old != null ? old.port : 16555;
        var oldDashboard = UnityEngine.Object.FindFirstObjectByType<HVACDashboardUI>(FindObjectsInactive.Include);
        float oldTarget = oldDashboard != null ? oldDashboard.targetTempC : 22.5f;

        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        sceneRoot = new GameObject("HVAC_IMMERSIVE_SCENE").transform;
        PrepareMaterials();
        CreateNature();
        CreateCabin();
        CreateAtmosphere();
        var root = new GameObject("AUTOSAR_HVAC_Cabin");
        root.transform.SetParent(sceneRoot, false);
        var dashboard = root.AddComponent<HVACDashboardUI>();
        dashboard.targetTempC = oldTarget;
        dashboard.cabinTempC = 18f;
        dashboard.enabled = false; // Hides legacy OnGUI; bridge still reads/writes this component.
        var tcp = root.AddComponent<HVACTcpWindowsBridge>();
        tcp.host = oldHost;
        tcp.port = oldPort;
        tcp.showConnectionOverlay = false; // New overlay replaces the old legacy overlay.
        var runtime = root.AddComponent<HVACImmersiveRuntime>();
        runtime.dashboard = dashboard;
        runtime.roadSections = roadSections.ToArray();
        runtime.wheel = steeringRoot;
        runtime.airStreaks = airStreaks.ToArray();
        runtime.airRenderers = airRenderers.ToArray();
        runtime.rainParticles = rain;
        runtime.snowParticles = snow;
        runtime.sun = sunlight;
        runtime.currentWeather = HVACImmersiveRuntime.Weather.HotSun;
        CreateHud(runtime);
        EditorSceneManager.MarkSceneDirty(newScene);
        if (!EditorSceneManager.SaveScene(newScene, path))
            throw new IOException("Unity n'a pas pu sauvegarder la scene " + path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("AUTOSAR HVAC - Driving Experience",
            "Nouvelle scene creee :\n" + path + "\n\n" +
            "Cockpit, route animee, paysages, 4 meteo, UI professionnelle.\n" +
            "Anciennes scenes et scripts MATLAB/SIMULINK inchanges.\n\n" +
            "Avant Play: assurez-vous que la passerelle MATLAB est demarree.\n" +
            "IMPORTANT: la meteo exterieure est visuelle et ne change PAS encore ThermalPlant.", "OK");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static Material MaterialAsset(string name, Color color, float metallic = 0f, float smooth = .35f, bool emission = false)
    {
        string path = MatFolder + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            if (shader == null) throw new Exception("Aucun shader URP Lit / Standard / Unlit trouve.");
            m = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (emission)
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * 2f);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static void PrepareMaterials()
    {
        leather = MaterialAsset("Leather_Perforated_Charcoal", new Color(.075f, .082f, .105f), .05f, .48f);
        black = MaterialAsset("Black_Piano", new Color(.018f, .027f, .045f), .28f, .85f);
        carbon = MaterialAsset("Matte_Carbon", new Color(.051f, .066f, .085f), .18f, .43f);
        silver = MaterialAsset("Brushed_Aluminum", new Color(.45f, .51f, .57f), .82f, .60f);
        darkMetal = MaterialAsset("Dark_Metal", new Color(.15f, .18f, .22f), .68f, .54f);
        chrome = MaterialAsset("Chromium", new Color(.70f, .77f, .80f), 1f, .82f);
        cyan = MaterialAsset("LED_IceBlue", new Color(.06f, .65f, .98f), .0f, .62f, true);
        orange = MaterialAsset("LED_Amber", new Color(1f, .34f, .07f), .0f, .55f, true);
        blueGlass = MaterialAsset("Blue_Digital_Glass", new Color(.018f, .08f, .16f), .32f, .86f);
        road = MaterialAsset("Asphalt_PBR_Base", new Color(.085f, .105f, .13f), .08f, .22f);
        laneWhite = MaterialAsset("Road_Lines", new Color(.79f, .84f, .81f), .0f, .23f);
        grass = MaterialAsset("Landscape_Grass", new Color(.15f, .31f, .22f));
        roadShoulder = MaterialAsset("Road_Shoulder", new Color(.28f, .28f, .27f));
        treeGreen = MaterialAsset("Spruce_Green", new Color(.11f, .24f, .19f));
        treeDark = MaterialAsset("Forest_Dark", new Color(.055f, .16f, .14f));
        bark = MaterialAsset("Bark", new Color(.22f, .16f, .13f));
        mountain = MaterialAsset("Mountain_Rock", new Color(.27f, .35f, .42f));
        mountainSnow = MaterialAsset("Mountain_Snow", new Color(.77f, .85f, .89f));
        skyAccent = MaterialAsset("Horizon_Hills", new Color(.27f, .43f, .48f));
        upholstery = MaterialAsset("Seat_Backing", new Color(.135f, .15f, .18f), .02f, .33f);
        windowEdge = MaterialAsset("Door_Trim", new Color(.042f, .048f, .062f), .15f, .40f);
        screen = MaterialAsset("Infotainment_Display", new Color(.01f, .045f, .085f), .0f, .38f, true);
        coneMesh = GetMesh("Cone_8_Sides", () => MakeCone(8));
        ringMesh = GetMesh("Steering_Torus", () => MakeTorus(48, 9, .36f, .034f));
    }

    static Mesh GetMesh(string name, Func<Mesh> create)
    {
        string path = Folder + "/" + name + ".asset";
        Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (m == null) { m = create(); m.name = name; AssetDatabase.CreateAsset(m, path); }
        return m;
    }

    static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3? rot = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (rot.HasValue) go.transform.localEulerAngles = rot.Value;
        var c = go.GetComponent<Collider>(); if (c != null) UnityEngine.Object.DestroyImmediate(c);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject Cylinder(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3? rot = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (rot.HasValue) go.transform.localEulerAngles = rot.Value;
        var c = go.GetComponent<Collider>(); if (c != null) UnityEngine.Object.DestroyImmediate(c);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject Sphere(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        var c = go.GetComponent<Collider>(); if (c != null) UnityEngine.Object.DestroyImmediate(c);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject MeshObject(string name, Transform parent, Mesh mesh, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static void Beam(string name, Transform p, Vector3 start, Vector3 end, float thickness, Material mat)
    {
        Vector3 d = end - start;
        var go = Cylinder(name, p, (start + end) * .5f,
            new Vector3(thickness, d.magnitude * .5f, thickness), mat);
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
    }

    static Mesh MakeCone(int sides)
    {
        var verts = new List<Vector3>(); var tris = new List<int>();
        verts.Add(new Vector3(0f, 1f, 0f));
        for (int i = 0; i <= sides; i++)
        {
            float a = i * Mathf.PI * 2f / sides;
            verts.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
            if (i > 0) { tris.Add(0); tris.Add(i + 1); tris.Add(i); }
        }
        var m = new Mesh(); m.SetVertices(verts); m.SetTriangles(tris, 0);
        m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    static Mesh MakeTorus(int ringSteps, int tubeSteps, float radius, float tube)
    {
        var v = new List<Vector3>(); var tri = new List<int>();
        for (int i = 0; i <= ringSteps; i++)
        {
            float u = i * Mathf.PI * 2f / ringSteps;
            for (int j = 0; j <= tubeSteps; j++)
            {
                float t = j * Mathf.PI * 2f / tubeSteps;
                float r = radius + tube * Mathf.Cos(t);
                v.Add(new Vector3(r * Mathf.Cos(u), r * Mathf.Sin(u), tube * Mathf.Sin(t)));
                if (i < ringSteps && j < tubeSteps)
                {
                    int a = i * (tubeSteps + 1) + j, b = a + tubeSteps + 1;
                    tri.Add(a); tri.Add(b); tri.Add(a+1);
                    tri.Add(a+1); tri.Add(b); tri.Add(b+1);
                }
            }
        }
        var m = new Mesh(); m.SetVertices(v); m.SetTriangles(tri, 0);
        m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    static List<Transform> roadSections = new List<Transform>();
    static Transform steeringRoot;
    static List<Transform> airStreaks = new List<Transform>();
    static List<Renderer> airRenderers = new List<Renderer>();
    static ParticleSystem rain, snow;
    static Light sunlight;

    static void CreateNature()
    {
        roadSections.Clear();
        var world = new GameObject("01_ENVIRONNEMENT_NATUREL_ANIME").transform;
        world.SetParent(sceneRoot, false);
        Cube("Terrain lointain | verts", world, new Vector3(0f, -2.0f, 180f), new Vector3(480f, 1f, 450f), grass);
        for (int i = 0; i < 11; i++)
        {
            float length = 36f;
            var section = new GameObject("Road Stream Segment " + (i + 1)).transform;
            section.SetParent(world, false);
            section.localPosition = new Vector3(0f, 0f, 6f + i * length);
            roadSections.Add(section);
            Cube("Asphalt", section, new Vector3(0, -.06f, 0), new Vector3(10.6f, .16f, length + .02f), road);
            Cube("Left road edge", section, new Vector3(-5.28f, .022f, 0), new Vector3(.09f, .007f, length), laneWhite);
            Cube("Right road edge", section, new Vector3(5.28f, .022f, 0), new Vector3(.09f, .007f, length), laneWhite);
            Cube("Left bank", section, new Vector3(-12f, -.09f, 0), new Vector3(13.5f, .2f, length), grass);
            Cube("Right bank", section, new Vector3(12f, -.09f, 0), new Vector3(13.5f, .2f, length), grass);
            Cube("Left shoulder", section, new Vector3(-5.65f, .017f, 0), new Vector3(.6f, .013f, length), roadShoulder);
            Cube("Right shoulder", section, new Vector3(5.65f, .017f, 0), new Vector3(.6f, .013f, length), roadShoulder);
            for (int k = 0; k < 3; k++)
            {
                Cube("Road dashed markings", section, new Vector3(0, .024f, -13f + k * 12f),
                    new Vector3(.13f, .013f, 5.8f), laneWhite);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Cube("Safety barrier horizontal", section, new Vector3(side * 7.1f, .67f, 0),
                    new Vector3(.08f, .13f, length), silver);
                for (int k = 0; k < 4; k++)
                {
                    float z = -13f + k * 9.7f;
                    Cube("Guardrail supports", section, new Vector3(side * 7.1f, .36f, z),
                        new Vector3(.14f, .73f, .12f), darkMetal);
                }
                for (int k = 0; k < 4; k++)
                {
                    float z = -15f + k * 9f;
                    float x = side * (12f + ((i * 11 + k * 7) % 6));
                    float size = 2.1f + ((i * 7 + k * 3) % 4) * .64f;
                    Pine(section, x, z, size, (i+k)%2 == 0 ? treeGreen : treeDark);
                }
            }
        }
        Mountains(world);
        var sunObj = new GameObject("Sunlight - weather controlled");
        sunObj.transform.SetParent(sceneRoot);
        sunlight = sunObj.AddComponent<Light>();
        sunlight.type = LightType.Directional;
        sunlight.intensity = 1.35f;
        sunlight.color = new Color(1f, .9f, .72f);
        sunlight.shadows = LightShadows.Soft;
        sunObj.transform.rotation = Quaternion.Euler(36f, -34f, 0f);
    }

    static void Pine(Transform parent, float x, float z, float size, Material leaves)
    {
        var p = new GameObject("Tree - spruce").transform;
        p.SetParent(parent, false);
        p.localPosition = new Vector3(x, 0f, z);
        Cylinder("Trunk", p, new Vector3(0, size*.33f, 0),
            new Vector3(.11f*size, size*.35f, .11f*size), bark);
        for (int n = 0; n < 3; n++)
            MeshObject("Needle Crown", p, coneMesh,
                new Vector3(0f, size * (.40f + n * .24f), 0f),
                new Vector3(size * (.56f - n * .105f), size * .48f, size * (.56f - n * .105f)), leaves);
    }

    static void Mountains(Transform parent)
    {
        var p = new GameObject("Mountain ridgeline / panoramic horizon").transform;
        p.SetParent(parent, false);
        for (int tier = 0; tier < 3; tier++)
        {
            float z = 155f + tier * 72f;
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            int count = 28;
            for (int i = 0; i <= count; i++)
            {
                float x = -220f + i * 440f / count;
                float h = 17f + 29f * Mathf.PerlinNoise((i + tier*77)*.31f, .3f) + 14f * Mathf.Abs(Mathf.Sin(i * .63f + tier));
                vertices.Add(new Vector3(x, -2.4f, z));
                vertices.Add(new Vector3(x, h, z));
                if (i < count)
                {
                    int a = i * 2;
                    triangles.Add(a); triangles.Add(a+1); triangles.Add(a+2);
                    triangles.Add(a+1); triangles.Add(a+3); triangles.Add(a+2);
                }
            }
            Mesh mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals();
            string path = Folder + "/Mountain_Ridge_" + tier + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old == null) { AssetDatabase.CreateAsset(mesh, path); old = mesh; }
            else UnityEngine.Object.DestroyImmediate(mesh);
            var ridge = MeshObject("Mountain " + tier, p, old, Vector3.zero, Vector3.one, tier == 0 ? skyAccent : mountain);
            ridge.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    static void CreateCabin()
    {
        airStreaks.Clear(); airRenderers.Clear();
        var cabin = new GameObject("02_COCKPIT_REPLACEABLE_WITH_REAL_FBX").transform;
        cabin.SetParent(sceneRoot, false);
        Cube("Vehicle interior floor", cabin, new Vector3(0, .11f, 0), new Vector3(4.3f, .22f, 6f), black);
        Cube("Left door", cabin, new Vector3(-2.13f, .84f, -.55f), new Vector3(.21f, 1.36f, 4.2f), leather);
        Cube("Right door", cabin, new Vector3(2.13f, .84f, -.55f), new Vector3(.21f, 1.36f, 4.2f), leather);
        Cube("Windshield top edge", cabin, new Vector3(0, 2.73f, 2.4f), new Vector3(4.18f, .18f, .22f), black);
        Beam("Left A pillar", cabin, new Vector3(-2.06f, 1.26f, 2.15f), new Vector3(-1.89f, 2.72f, 2.35f), .12f, black);
        Beam("Right A pillar", cabin, new Vector3(2.06f, 1.26f, 2.15f), new Vector3(1.89f, 2.72f, 2.35f), .12f, black);
        Cube("Cabin roof", cabin, new Vector3(0, 2.91f, -.9f), new Vector3(4.18f, .18f, 3.05f), black);
        Cube("Windshield bottom trim", cabin, new Vector3(0, 1.42f, 2.45f), new Vector3(4.05f, .08f, .21f), black);
        Cube("Dash top sculpt", cabin, new Vector3(0, 1.33f, 1.96f), new Vector3(4.10f, .42f, 1.05f), leather);
        Cube("Dash upper panel", cabin, new Vector3(0, 1.58f, 2.03f), new Vector3(4.10f, .17f, 1.1f), carbon);
        Cube("Dash continuous chrome trim", cabin, new Vector3(0, 1.18f, 1.47f), new Vector3(3.99f, .035f, .05f), silver);
        Cube("Dash blue LED strip", cabin, new Vector3(0, 1.25f, 1.43f), new Vector3(3.96f, .018f, .035f), cyan);
        Cube("Lower instrument bar", cabin, new Vector3(-1.15f, 1.51f, 1.35f), new Vector3(1.30f, .52f, .14f), black);
        Cube("Instrument screen", cabin, new Vector3(-1.15f, 1.56f, 1.26f), new Vector3(1.14f, .39f, .025f), blueGlass);
        Cube("Instrument glowing top line", cabin, new Vector3(-1.15f, 1.76f, 1.23f), new Vector3(1.12f, .018f, .025f), cyan);
        Cube("Central infotainment screen housing", cabin, new Vector3(.42f, 1.67f, 1.39f), new Vector3(1.69f, .89f, .14f), black);
        Cube("Central infotainment screen", cabin, new Vector3(.42f, 1.67f, 1.305f), new Vector3(1.52f, .73f, .026f), screen);
        Cube("Screen HVAC upper accent", cabin, new Vector3(.43f, 1.88f, 1.28f), new Vector3(1.33f, .024f, .025f), cyan);
        Cube("Screen HVAC air path", cabin, new Vector3(.43f, 1.62f, 1.27f), new Vector3(1.1f, .035f, .02f), orange);
        Cube("Vent background", cabin, new Vector3(1.53f, 1.40f, 1.40f), new Vector3(.9f, .29f, .07f), black);
        for (int i = 0; i < 5; i++)
            Cube("Vent lamella", cabin, new Vector3(1.53f, 1.28f + i*.054f, 1.355f),
                new Vector3(.83f, .017f, .03f), silver);
        Cube("Center stack", cabin, new Vector3(.46f, .87f, 1.19f), new Vector3(.94f, .83f, 1.1f), carbon);
        Cube("Gear select base", cabin, new Vector3(.42f, .55f, .61f), new Vector3(.71f, .08f, .52f), black);
        Cube("Gear selector", cabin, new Vector3(.47f, .83f, .60f), new Vector3(.16f, .46f, .19f), chrome, new Vector3(-17f, 0f, 0f));
        Cylinder("Gear trim", cabin, new Vector3(.55f, .72f, .32f), new Vector3(.17f, .045f, .17f), silver);
        for (int xSign = -1; xSign <= 1; xSign += 2)
        {
            Cube("Door floating trim", cabin, new Vector3(xSign * 2.03f, 1.13f, -.21f), new Vector3(.12f, .12f, 3.1f), carbon);
            Cube("Door ambient LED", cabin, new Vector3(xSign * 1.985f, 1.25f, -.25f), new Vector3(.025f, .018f, 3.3f), cyan);
        }
        Seats(cabin, -1.05f); Seats(cabin, 1.05f);
        var wheel = new GameObject("Steering_wheel_motion").transform;
        wheel.SetParent(cabin, false);
        wheel.localPosition = new Vector3(-1.11f, 1.33f, .94f);
        steeringRoot = wheel;
        MeshObject("Perforated steering wheel rim", wheel, ringMesh, Vector3.zero, Vector3.one, leather);
        Cylinder("Steering central boss", wheel, Vector3.zero, new Vector3(.16f, .037f, .16f), darkMetal, new Vector3(90f,0f,0f));
        Cube("Steering left spoke", wheel, new Vector3(-.15f, -.055f, 0f), new Vector3(.22f, .07f, .07f), chrome, new Vector3(0,0,-14f));
        Cube("Steering right spoke", wheel, new Vector3(.15f, -.055f, 0f), new Vector3(.22f, .07f, .07f), chrome, new Vector3(0,0,14f));
        Cube("Steering lower spoke", wheel, new Vector3(0, -.18f, .02f), new Vector3(.095f, .28f, .07f), darkMetal);
        Sphere("Steering emblem", wheel, new Vector3(0, 0, -.035f), new Vector3(.12f,.12f,.025f), chrome);
        Cube("Rear view mirror", cabin, new Vector3(0, 2.38f, 2.2f), new Vector3(.75f,.18f,.07f), black);
        Cube("Mirror glass", cabin, new Vector3(0, 2.38f, 2.152f), new Vector3(.66f,.13f,.012f), silver);
        // Airflow lights, enabled ONLY by received AUTOSAR heating/cooling states.
        for (int i = 0; i < 22; i++)
        {
            float x = -.45f + (i % 11) * .085f;
            float z = .96f - ((i / 11) * .10f);
            var streak = Cube("HVAC_Real_Command_Airflow_" + i, cabin,
                new Vector3(x, 1.30f + (i % 3)*.035f, z),
                new Vector3(.024f, .011f, .28f), cyan);
            streak.transform.localEulerAngles = new Vector3(0f, (i % 2 == 0 ? -22f : 22f), 0f);
            var r = streak.GetComponent<Renderer>();
            r.enabled = false;
            airStreaks.Add(streak.transform); airRenderers.Add(r);
        }
        var cameraObject = new GameObject("Main Camera - DRIVER VIEW");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(sceneRoot, false);
        cameraObject.transform.position = new Vector3(-.84f, 1.96f, -.49f);
        cameraObject.transform.LookAt(new Vector3(0f, 1.46f, 13f));
        Camera cam = cameraObject.AddComponent<Camera>();
        cam.fieldOfView = 67f;
        cam.nearClipPlane = .045f;
        cam.farClipPlane = 550f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(.65f, .73f, .81f);
        cameraObject.AddComponent<AudioListener>();
    }

    static void Seats(Transform cabin, float x)
    {
        var seat = new GameObject(x < 0 ? "Luxury_driver_seat" : "Luxury_passenger_seat").transform;
        seat.SetParent(cabin, false);
        seat.localPosition = new Vector3(x, .0f, -1.32f);
        Cube("Cushion", seat, new Vector3(0, .60f, .03f), new Vector3(1.15f, .27f, 1.20f), leather);
        Cube("Backrest", seat, new Vector3(0, 1.30f, -.59f), new Vector3(1.13f, 1.27f, .23f), leather, new Vector3(-11f,0f,0f));
        Cube("Headrest", seat, new Vector3(0, 2.11f, -.68f), new Vector3(.55f,.33f,.24f), upholstery);
        Cube("Left side bolster", seat, new Vector3(-.51f, 1.35f, -.58f), new Vector3(.12f,1.10f,.31f), upholstery);
        Cube("Right side bolster", seat, new Vector3(.51f, 1.35f, -.58f), new Vector3(.12f,1.10f,.31f), upholstery);
        for (int j = 0; j < 5; j++)
        {
            Cube("Vertical stitched trim", seat, new Vector3(-.34f + j*.17f,1.29f,-.735f),
                new Vector3(.008f,.82f,.012f), silver, new Vector3(-11f,0f,0f));
        }
    }

    static ParticleSystem MakeWeatherParticles(string name, Transform parent, Color color, float size, float ySpeed, int maxParticles)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = new Vector3(0f, 9f, 17f);
        var ps = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 1.2f;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(21f, 3f, 43f);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(ySpeed);
        vel.x = new ParticleSystem.MinMaxCurve(-1.1f);
        var emission = ps.emission;
        emission.rateOverTime = 500f;
        emission.enabled = false;
        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        Material particle = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/" + name + ".mat");
        if (particle == null)
        {
            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Particles/Unlit") : Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            particle = new Material(shader);
            particle.name = name;
            AssetDatabase.CreateAsset(particle, MatFolder + "/" + name + ".mat");
        }
        renderer.sharedMaterial = particle;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return ps;
    }

    static void CreateAtmosphere()
    {
        var atmosphere = new GameObject("03_DYNAMIC_WEATHER").transform;
        atmosphere.SetParent(sceneRoot, false);
        rain = MakeWeatherParticles("Rain_Visible", atmosphere, new Color(.70f,.86f,1f,.7f), .05f, -24f, 1200);
        snow = MakeWeatherParticles("Snow_Visible", atmosphere, new Color(1f,1f,1f,.95f), .16f, -4f, 600);
        var e = snow.emission; e.rateOverTime = 125f; e.enabled = false;
        var p = new GameObject("Cockpit subtle ambient blue fill");
        p.transform.SetParent(sceneRoot, false);
        p.transform.position = new Vector3(.3f, 1.98f, 1.0f);
        var fill = p.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.range = 4f;
        fill.intensity = .7f;
        fill.color = new Color(.12f, .45f, 1f);
        fill.shadows = LightShadows.None;
    }

    static Sprite RoundedSprite()
    {
        if (roundSprite != null) return roundSprite;
        string path = TextureFolder + "/RoundedUI.png";
        if (!File.Exists(path))
        {
            int size = 64;
            var tx = new Texture2D(size,size, TextureFormat.RGBA32, false);
            Color[] p = new Color[size * size];
            float radius = 17f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x+.5f-size/2f) - (size/2f-radius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y+.5f-size/2f) - (size/2f-radius), 0f);
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx*dx + dy*dy) + .5f);
                    p[y*size+x] = new Color(1,1,1,alpha);
                }
            tx.SetPixels(p); tx.Apply();
            File.WriteAllBytes(path, tx.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tx);
            AssetDatabase.ImportAsset(path);
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(18,18,18,18);
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        roundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return roundSprite;
    }

    static Font DefaultFont()
    {
        if (font != null) return font;
        // No copyrighted font is bundled. Use a font already installed on host computer.
        font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Liberation Sans" }, 16);
        return font;
    }

    static RectTransform Rect(string name, Transform parent, Vector2 anchored, Vector2 size, Vector2 anchor)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = anchor;
        r.anchoredPosition = anchored;
        r.sizeDelta = size;
        return r;
    }

    static Image Panel(string name, Transform parent, Vector2 pos, Vector2 size, Color color, Vector2 anchor)
    {
        var r = Rect(name, parent, pos, size, anchor);
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = RoundedSprite();
        img.type = Image.Type.Sliced;
        img.color = color;
        return img;
    }

    static Text Label(string name, Transform parent, string value, Vector2 pos, Vector2 size,
        int fontSize = 20, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft,
        FontStyle style = FontStyle.Normal, Vector2? anchor = null)
    {
        RectTransform r = Rect(name, parent, pos, size, anchor ?? new Vector2(0,1));
        var txt = r.gameObject.AddComponent<Text>();
        txt.font = DefaultFont(); txt.fontSize = fontSize; txt.text = value;
        txt.color = color ?? Color.white;
        txt.alignment = align; txt.fontStyle = style;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Truncate;
        txt.raycastTarget = false;
        return txt;
    }

    static Button ButtonUI(string name, Transform parent, string caption, Vector2 pos,
        Vector2 size, Color background, int fontSize = 18)
    {
        Image bg = Panel(name, parent, pos, size, background, new Vector2(0,1));
        var button = bg.gameObject.AddComponent<Button>();
        var c = button.colors;
        c.normalColor = Color.white;
        c.highlightedColor = new Color(.81f, .93f, 1f);
        c.pressedColor = new Color(.60f, .85f, 1f);
        c.selectedColor = Color.white;
        button.colors = c;
        Label("Caption", bg.transform, caption, new Vector2(0,0), size, fontSize,
            Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(.5f,.5f));
        return button;
    }

    static void CreateHud(HVACImmersiveRuntime rt)
    {
        var canvasObj = new GameObject("04_PREMIUM_AUTOSAR_HMI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(sceneRoot, false);
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600,900);
        scaler.matchWidthOrHeight = .5f;
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("EventSystem - UI Buttons");
            events.transform.SetParent(sceneRoot, false);
            events.AddComponent<EventSystem>();
            // Supports new Input System without a direct package dependency.
            Type inputSystemModule = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule != null && typeof(BaseInputModule).IsAssignableFrom(inputSystemModule))
            {
                var module = events.AddComponent(inputSystemModule);
                var method = inputSystemModule.GetMethod("AssignDefaultActions");
                if (method != null) method.Invoke(module, null);
            }
            else events.AddComponent<StandaloneInputModule>();
        }
        Transform c = canvasObj.transform;
        Panel("Top left speed card", c, new Vector2(28,-29), new Vector2(260,108), new Color(.009f,.022f,.047f,.88f), new Vector2(0,1));
        Label("Driving title", c, "DRIVE EXPERIENCE", new Vector2(45,-38), new Vector2(230,34), 20,
            new Color(.65f,.82f,1f), TextAnchor.MiddleLeft, FontStyle.Bold);
        rt.speedText = Label("Visual speed", c, "78 km/h", new Vector2(46,-72), new Vector2(225,45), 34,
            Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        Label("Heading subtitle", c, "CONDUITE SIMULEE • PAYSAGE DYNAMIQUE", new Vector2(28,-143), new Vector2(610,32), 16,
            new Color(.91f,.95f,1f), TextAnchor.MiddleLeft);
        Panel("Weather selector back", c, new Vector2(360,-26), new Vector2(745,75), new Color(.009f,.027f,.05f,.86f), new Vector2(0,1));
        rt.sunnyButton = ButtonUI("Weather HotSun", c, "☀  SOLEIL", new Vector2(377,-39),new Vector2(125,48),new Color(.35f,.24f,.10f,.95f),15);
        rt.rainyButton = ButtonUI("Weather Rain", c, "PLUIE", new Vector2(513,-39),new Vector2(105,48),new Color(.07f,.20f,.35f,.95f),16);
        rt.foggyButton = ButtonUI("Weather Fog", c, "BRUME", new Vector2(629,-39),new Vector2(105,48),new Color(.14f,.22f,.28f,.95f),16);
        rt.snowyButton = ButtonUI("Weather Snow", c, "NEIGE", new Vector2(745,-39),new Vector2(105,48),new Color(.14f,.28f,.40f,.95f),16);
        rt.autoTourButton = ButtonUI("Tour Weather", c, "TOUR AUTO : OFF",new Vector2(861,-39),new Vector2(227,48),new Color(.08f,.34f,.42f,.95f),16);
        // Right overlay, dark tinted automotive interface.
        Image panel = Panel("Autonomous automotive climate card", c, new Vector2(-22,-25),
            new Vector2(444,772), new Color(.008f,.024f,.044f,.94f), new Vector2(1,1));
        Transform p = panel.transform;
        Panel("Top neon trim", p, new Vector2(22,-17), new Vector2(400,4),
            new Color(.08f,.64f,.98f,.95f), new Vector2(0,1));
        Label("Header", p, "AUTOSAR HVAC", new Vector2(28,-26), new Vector2(380,44), 31,
            Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        Label("Header subtitle", p, "UNITY 6  |  MATLAB  |  ROS 2", new Vector2(30,-67),new Vector2(390,31),16,
            new Color(.39f,.75f,.97f),TextAnchor.MiddleLeft);
        Label("Target label", p, "CONSIGNE HABITACLE", new Vector2(29,-110),new Vector2(380,27),17,
            new Color(.73f,.82f,.94f));
        rt.targetText = Label("Target Value",p,"22,5 °C",new Vector2(28,-137),new Vector2(275,70),49,
            Color.white,TextAnchor.MiddleLeft,FontStyle.Bold);
        rt.minusButton = ButtonUI("Decrease Setpoint",p,"-  0,5",new Vector2(30,-211),new Vector2(183,52),new Color(.08f,.20f,.34f,.96f),20);
        rt.plusButton = ButtonUI("Increase Setpoint",p,"+  0,5",new Vector2(229,-211),new Vector2(183,52),new Color(.08f,.20f,.34f,.96f),20);
        Label("Temperature interior",p,"HABITACLE",new Vector2(31,-284),new Vector2(180,28),17,
            new Color(.66f,.75f,.84f));
        Label("Temperature outside",p,"EXTERIEUR",new Vector2(236,-284),new Vector2(185,28),17,
            new Color(.66f,.75f,.84f));
        rt.cabinText = Label("Cabin temperature",p,"18,0 °C",new Vector2(30,-314),new Vector2(190,42),31,
            new Color(.55f,.80f,1f),TextAnchor.MiddleLeft,FontStyle.Bold);
        rt.outsideText = Label("Outside temperature visual",p,"34,0 °C",new Vector2(235,-314),new Vector2(194,42),31,
            new Color(1f,.68f,.37f),TextAnchor.MiddleLeft,FontStyle.Bold);
        rt.weatherText = Label("Weather description",p,"SOLEIL / CANICULE",new Vector2(30,-360),new Vector2(400,30),17,
            new Color(.73f,.84f,.93f));
        rt.heaterTile = Panel("HEATING CARD", p, new Vector2(28,-405),new Vector2(122,94),new Color(.12f,.19f,.26f,.95f),new Vector2(0,1));
        rt.coolerTile = Panel("COOLING CARD", p, new Vector2(159,-405),new Vector2(122,94),new Color(.12f,.19f,.26f,.95f),new Vector2(0,1));
        rt.maintainTile = Panel("MAINTAIN CARD", p, new Vector2(290,-405),new Vector2(122,94),new Color(.12f,.19f,.26f,.95f),new Vector2(0,1));
        Label("Heating label",rt.heaterTile.transform,"CHAUFFAGE",new Vector2(0,-13),new Vector2(122,29),16,Color.white,TextAnchor.MiddleCenter,FontStyle.Bold);
        Label("Cooling label",rt.coolerTile.transform,"CLIMAT.",new Vector2(0,-13),new Vector2(122,29),16,Color.white,TextAnchor.MiddleCenter,FontStyle.Bold);
        Label("Maintaining label",rt.maintainTile.transform,"MAINTIEN",new Vector2(0,-13),new Vector2(122,29),16,Color.white,TextAnchor.MiddleCenter,FontStyle.Bold);
        rt.heaterText = Label("Heat state",rt.heaterTile.transform,"INACTIF",new Vector2(0,-48),new Vector2(122,29),17,Color.white,TextAnchor.MiddleCenter,FontStyle.Bold);
        rt.coolerText = Label("Cool state",rt.coolerTile.transform,"INACTIF",new Vector2(0,-48),new Vector2(122,29),17,Color.white,TextAnchor.MiddleCenter,FontStyle.Bold);
        rt.maintainText = Label("Maintain state",rt.maintainTile.transform,"INACTIF",new Vector2(0,-48),new Vector2(122,29),17,Color.white,TextAnchor.MiddleCenter,FontStyle.Bold);
        rt.rosText = Label("ROS status",p,"ROS 2 : EN ATTENTE",new Vector2(33,-516),new Vector2(395,29),17,
            new Color(.34f,.95f,.69f),TextAnchor.MiddleLeft,FontStyle.Bold);
        rt.sensorText = Label("Fault status",p,"CAPTEUR : EN ATTENTE",new Vector2(33,-550),new Vector2(395,26),16,
            new Color(.8f,.9f,1f));
        rt.commText = Label("Communication fault",p,"TELEMETRIE : ABSENTE",new Vector2(33,-582),new Vector2(395,26),15,
            new Color(.8f,.9f,1f));
        Panel("Explanation back",p,new Vector2(22,-625),new Vector2(399,103),new Color(.07f,.14f,.23f,.98f),new Vector2(0,1));
        rt.explanationText = Label("AUTOSAR state explanation",p,
            "Demarrez MATLAB et Simulink pour visualiser les decisions AUTOSAR.",new Vector2(35,-635),new Vector2(375,84),16,
            Color.white,TextAnchor.UpperLeft);
        rt.noticeText = Label("Weather fidelity notice",c,
            "Meteo = visuelle | AUTOSAR = commandes Simulink",new Vector2(28,29),new Vector2(980,42),16,
            new Color(.95f,.97f,1f),TextAnchor.MiddleLeft,FontStyle.Normal,new Vector2(0,0));
    }
}
#endif
