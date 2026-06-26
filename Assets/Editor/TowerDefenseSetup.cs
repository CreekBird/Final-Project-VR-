#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// One-click project setup: run via Tools > Tower Defense > Setup Project.
/// Creates MainMenu and Game scenes, all GameObjects, UI, and placeholder prefabs.
/// </summary>
public static class TowerDefenseSetup
{
    const string SCENES_PATH  = "Assets/Scenes";
    const string PREFABS_PATH = "Assets/Prefabs";

    // ── Entry point ───────────────────────────────────────────────────────────

    [MenuItem("Tools/Tower Defense/Setup Entire Project")]
    public static void SetupProject()
    {
        // Ensure folders exist
        EnsureFolder(SCENES_PATH);
        EnsureFolder(PREFABS_PATH);
        EnsureFolder("Assets/Prefabs/Enemies");
        EnsureFolder("Assets/Prefabs/Towers");

        // 1. Build prefabs first (so scene can reference them)
        GameObject enemyPrefab      = CreateEnemyPrefab();
        GameObject basicTowerPrefab = CreateTowerPrefab("Tower_Basic",  new Color(0.2f, 0.6f, 1f),   0.4f, 1f,   30f);
        GameObject fastTowerPrefab  = CreateTowerPrefab("Tower_Fast",   new Color(0.2f, 1f,  0.4f),  0.3f, 2.5f, 15f);
        GameObject sniperTowerPrefab= CreateTowerPrefab("Tower_Sniper", new Color(1f,  0.6f, 0.2f),  0.7f, 0.5f, 80f);
        GameObject projectilePrefab = CreateProjectilePrefab();
        GameObject battlefieldPrefab= CreateBattlefieldPrefab();
        GameObject indicatorPrefab  = CreatePlacementIndicatorPrefab();

        // Assign projectile to towers
        AssignProjectileToPrefabs(basicTowerPrefab, fastTowerPrefab, sniperTowerPrefab, projectilePrefab);

        // 2. Create scenes
        CreateMainMenuScene();
        CreateGameScene(enemyPrefab, basicTowerPrefab, fastTowerPrefab, sniperTowerPrefab,
                        battlefieldPrefab, indicatorPrefab);

        // 3. Add scenes to Build Settings
        AddScenesToBuildSettings();

        Debug.Log("[TowerDefenseSetup] ✓ Project setup complete! Open MainMenu or Game scene to start.");
        EditorUtility.DisplayDialog("Setup Complete",
            "All scenes and prefabs created!\n\n" +
            "• MainMenu scene: Assets/Scenes/MainMenu.unity\n" +
            "• Game scene:     Assets/Scenes/Game.unity\n\n" +
            "Open the Game scene and press Play to test.\n" +
            "Build Settings has both scenes registered.",
            "OK");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PREFAB CREATION
    // ══════════════════════════════════════════════════════════════════════════

    static GameObject CreateEnemyPrefab()
    {
        // Root
        var root = new GameObject("Enemy");

        // Body — coloured capsule
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.up * 0.1f;
        body.transform.localScale    = Vector3.one * 0.06f;
        SetColor(body, new Color(0.9f, 0.2f, 0.2f));
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

        // Collider on root
        var col = root.AddComponent<CapsuleCollider>();
        col.radius = 0.04f;
        col.height = 0.12f;
        col.center = new Vector3(0, 0.06f, 0);

        // Scripts
        root.AddComponent<EnemyMovement>();
        var health = root.AddComponent<EnemyHealth>();

        // World-space health bar
        var canvasGO = new GameObject("HealthBarCanvas");
        canvasGO.transform.SetParent(root.transform);
        canvasGO.transform.localPosition = new Vector3(0, 0.16f, 0);
        canvasGO.transform.localScale    = Vector3.one * 0.002f;
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 10);

        // Background
        var bg = new GameObject("BG");
        bg.transform.SetParent(canvasGO.transform);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = Color.black;
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        // Fill
        var fill = new GameObject("Fill");
        fill.transform.SetParent(canvasGO.transform);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = Color.red;
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(0.5f, 1);
        fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;

        // Wire Slider (we fake it via a simple UI — attach the fill to health via SerializedObject)
        // Actually use a Slider for health bar
        var sliderGO = new GameObject("HealthSlider");
        sliderGO.transform.SetParent(canvasGO.transform);
        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
        var sliderRt = sliderGO.GetComponent<RectTransform>();
        sliderRt.anchorMin = Vector2.zero; sliderRt.anchorMax = Vector2.one;
        sliderRt.offsetMin = Vector2.zero; sliderRt.offsetMax = Vector2.zero;

        Object.DestroyImmediate(bg);
        Object.DestroyImmediate(fill);

        // Wire slider to EnemyHealth via SerializedObject
        var so = new SerializedObject(health);
        so.FindProperty("healthBarSlider").objectReferenceValue = slider;
        so.ApplyModifiedProperties();

        // Prefab
        string path = $"{PREFABS_PATH}/Enemies/Enemy.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"Created: {path}");
        return prefab;
    }

    static GameObject CreateTowerPrefab(string name, Color color, float range, float fireRate, float damage)
    {
        var root = new GameObject(name);

        // Base — flat cylinder
        var baseMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseMesh.name = "Base";
        baseMesh.transform.SetParent(root.transform);
        baseMesh.transform.localPosition = new Vector3(0, 0.02f, 0);
        baseMesh.transform.localScale    = new Vector3(0.08f, 0.02f, 0.08f);
        SetColor(baseMesh, color * 0.6f);
        Object.DestroyImmediate(baseMesh.GetComponent<CapsuleCollider>());

        // Head — cube that rotates
        var head = new GameObject("Head");
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0, 0.06f, 0);

        var headMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headMesh.name = "HeadMesh";
        headMesh.transform.SetParent(head.transform);
        headMesh.transform.localPosition = Vector3.zero;
        headMesh.transform.localScale    = Vector3.one * 0.05f;
        SetColor(headMesh, color);
        Object.DestroyImmediate(headMesh.GetComponent<BoxCollider>());

        // Barrel — small capsule pointing forward
        var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "Barrel";
        barrel.transform.SetParent(head.transform);
        barrel.transform.localPosition = new Vector3(0, 0, 0.04f);
        barrel.transform.localScale    = new Vector3(0.012f, 0.03f, 0.012f);
        barrel.transform.localEulerAngles = new Vector3(90, 0, 0);
        SetColor(barrel, Color.gray);
        Object.DestroyImmediate(barrel.GetComponent<CapsuleCollider>());

        // Fire point
        var firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(head.transform);
        firePoint.transform.localPosition = new Vector3(0, 0, 0.07f);

        // Collider on root for raycasting placement
        var col = root.AddComponent<BoxCollider>();
        col.size   = new Vector3(0.08f, 0.08f, 0.08f);
        col.center = new Vector3(0, 0.04f, 0);

        // Tower script
        var tower = root.AddComponent<Tower>();
        var so = new SerializedObject(tower);
        so.FindProperty("range").floatValue    = range;
        so.FindProperty("fireRate").floatValue = fireRate;
        so.FindProperty("damage").floatValue   = damage;
        so.FindProperty("firePoint").objectReferenceValue     = firePoint.transform;
        so.FindProperty("rotatingHead").objectReferenceValue  = head.transform;
        so.ApplyModifiedProperties();

        string path = $"{PREFABS_PATH}/Towers/{name}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"Created: {path}");
        return prefab;
    }

    static GameObject CreateProjectilePrefab()
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Projectile";
        root.transform.localScale = Vector3.one * 0.015f;
        SetColor(root, Color.yellow);
        Object.DestroyImmediate(root.GetComponent<SphereCollider>());
        root.AddComponent<Projectile>();

        string path = $"{PREFABS_PATH}/Projectile.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"Created: {path}");
        return prefab;
    }

    static void AssignProjectileToPrefabs(GameObject basic, GameObject fast, GameObject sniper, GameObject proj)
    {
        foreach (var towerPrefab in new[] { basic, fast, sniper })
        {
            // Open prefab, assign projectile, save
            string assetPath = AssetDatabase.GetAssetPath(towerPrefab);
            var instance = PrefabUtility.LoadPrefabContents(assetPath);
            var tower = instance.GetComponent<Tower>();
            if (tower != null)
            {
                var so = new SerializedObject(tower);
                so.FindProperty("projectilePrefab").objectReferenceValue = proj;
                so.ApplyModifiedProperties();
            }
            PrefabUtility.SaveAsPrefabAsset(instance, assetPath);
            PrefabUtility.UnloadPrefabContents(instance);
        }
    }

    static GameObject CreateBattlefieldPrefab()
    {
        var root = new GameObject("Battlefield");

        // Ground plane (thin cube as floor visual)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform);
        ground.transform.localPosition = new Vector3(0, -0.005f, 0);
        ground.transform.localScale    = new Vector3(0.6f, 0.005f, 0.6f);
        SetColor(ground, new Color(0.25f, 0.5f, 0.25f));
        Object.DestroyImmediate(ground.GetComponent<BoxCollider>());

        // Path — a simple S-curve of 6 waypoints arranged on the ground
        var waypointsParent = new GameObject("Waypoints");
        waypointsParent.transform.SetParent(root.transform);

        // Waypoint positions form a zigzag path
        Vector3[] wpPositions =
        {
            new Vector3(-0.22f, 0.01f,  0.22f),   // 0 – spawn (back-left)
            new Vector3(-0.22f, 0.01f, -0.05f),   // 1
            new Vector3( 0.00f, 0.01f, -0.05f),   // 2
            new Vector3( 0.00f, 0.01f,  0.10f),   // 3
            new Vector3( 0.22f, 0.01f,  0.10f),   // 4
            new Vector3( 0.22f, 0.01f, -0.22f),   // 5 – base (front-right)
        };

        var waypoints = new Transform[wpPositions.Length];
        for (int i = 0; i < wpPositions.Length; i++)
        {
            var wp = new GameObject($"Waypoint_{i:D2}");
            wp.transform.SetParent(waypointsParent.transform);
            wp.transform.localPosition = wpPositions[i];
            waypoints[i] = wp.transform;
        }

        // Spawn point (same as waypoint 0)
        var spawnPoint = new GameObject("SpawnPoint");
        spawnPoint.transform.SetParent(root.transform);
        spawnPoint.transform.localPosition = wpPositions[0];

        // Base marker (at last waypoint)
        var baseMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseMarker.name = "Base";
        baseMarker.transform.SetParent(root.transform);
        baseMarker.transform.localPosition = wpPositions[wpPositions.Length - 1] + Vector3.up * 0.03f;
        baseMarker.transform.localScale    = new Vector3(0.06f, 0.06f, 0.06f);
        SetColor(baseMarker, new Color(0.2f, 0.4f, 1f));
        Object.DestroyImmediate(baseMarker.GetComponent<BoxCollider>());

        // Grid cells — 3×3 on each side of the path, avoiding path tiles
        var gridParent = new GameObject("GridCells");
        gridParent.transform.SetParent(root.transform);

        int gridLayer = LayerMask.NameToLayer("Grid");
        if (gridLayer < 0) gridLayer = 0;   // fallback if layer not created yet

        var gridCellList = new List<GridCell>();

        // Define cell positions (avoid path waypoints)
        Vector3[] cellPositions =
        {
            // Left column
            new Vector3(-0.12f, 0.001f,  0.22f),
            new Vector3(-0.12f, 0.001f,  0.08f),
            new Vector3(-0.22f, 0.001f,  0.08f),
            // Middle area
            new Vector3( 0.12f, 0.001f,  0.22f),
            new Vector3( 0.12f, 0.001f,  0.08f),
            new Vector3( 0.00f, 0.001f,  0.22f),
            // Right side
            new Vector3( 0.22f, 0.001f,  0.22f),
            new Vector3( 0.12f, 0.001f, -0.10f),
            new Vector3( 0.00f, 0.001f, -0.18f),
            new Vector3(-0.12f, 0.001f, -0.10f),
            new Vector3(-0.12f, 0.001f, -0.22f),
            new Vector3( 0.12f, 0.001f, -0.22f),
        };

        foreach (var pos in cellPositions)
        {
            var cell = new GameObject("GridCell");
            cell.transform.SetParent(gridParent.transform);
            cell.transform.localPosition = pos;
            if (gridLayer >= 0) cell.layer = gridLayer;

            // Visual quad
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Visual";
            quad.transform.SetParent(cell.transform);
            quad.transform.localPosition    = Vector3.zero;
            quad.transform.localRotation    = Quaternion.Euler(90, 0, 0);
            quad.transform.localScale       = Vector3.one * 0.085f;
            SetColor(quad, new Color(0.2f, 0.9f, 0.2f, 0.4f), true);
            Object.DestroyImmediate(quad.GetComponent<MeshCollider>());

            // Collider on root cell
            var bc = cell.AddComponent<BoxCollider>();
            bc.size   = new Vector3(0.085f, 0.01f, 0.085f);
            bc.center = Vector3.zero;

            var gc = cell.AddComponent<GridCell>();
            var soGC = new SerializedObject(gc);
            soGC.FindProperty("cellRenderer").objectReferenceValue = quad.GetComponent<Renderer>();
            soGC.ApplyModifiedProperties();

            gridCellList.Add(gc);
        }

        // BattlefieldController
        var bc2 = root.AddComponent<BattlefieldController>();
        var soBC = new SerializedObject(bc2);
        var wpProp = soBC.FindProperty("waypoints");
        wpProp.arraySize = waypoints.Length;
        for (int i = 0; i < waypoints.Length; i++)
            wpProp.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];

        soBC.FindProperty("spawnPoint").objectReferenceValue = spawnPoint.transform;

        var gcProp = soBC.FindProperty("gridCells");
        gcProp.arraySize = gridCellList.Count;
        for (int i = 0; i < gridCellList.Count; i++)
            gcProp.GetArrayElementAtIndex(i).objectReferenceValue = gridCellList[i];

        soBC.ApplyModifiedProperties();

        string path = $"{PREFABS_PATH}/Battlefield.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"Created: {path}");
        return prefab;
    }

    static GameObject CreatePlacementIndicatorPrefab()
    {
        var root = new GameObject("PlacementIndicator");

        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Ring";
        ring.transform.SetParent(root.transform);
        ring.transform.localScale = new Vector3(0.15f, 0.002f, 0.15f);
        SetColor(ring, new Color(0.2f, 1f, 0.4f, 0.5f), true);
        Object.DestroyImmediate(ring.GetComponent<CapsuleCollider>());

        string path = $"{PREFABS_PATH}/PlacementIndicator.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MAIN MENU SCENE
    // ══════════════════════════════════════════════════════════════════════════

    static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "MainMenu";

        // Background camera already there
        var cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null) cam.backgroundColor = new Color(0.05f, 0.05f, 0.15f);

        // Canvas
        var canvasGO = new GameObject("MainMenuCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Event system
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // Background panel
        var bg = CreateUIPanel(canvasGO.transform, "Background", new Color(0.05f, 0.05f, 0.2f, 0.95f));
        StretchToFill(bg.GetComponent<RectTransform>());

        // Title
        var titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(canvasGO.transform);
        var title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text      = "AR Tower Defense";
        title.fontSize  = 60;
        title.color     = Color.white;
        title.alignment = TextAlignmentOptions.Center;
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.1f, 0.6f);
        titleRt.anchorMax = new Vector2(0.9f, 0.85f);
        titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;

        // Subtitle
        var subGO = new GameObject("SubtitleText");
        subGO.transform.SetParent(canvasGO.transform);
        var sub = subGO.AddComponent<TextMeshProUGUI>();
        sub.text      = "Defend your base in Augmented Reality!";
        sub.fontSize  = 24;
        sub.color     = new Color(0.7f, 0.9f, 1f);
        sub.alignment = TextAlignmentOptions.Center;
        var subRt = sub.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.15f, 0.50f);
        subRt.anchorMax = new Vector2(0.85f, 0.60f);
        subRt.offsetMin = subRt.offsetMax = Vector2.zero;

        // Play Button
        var playBtn = CreateButton(canvasGO.transform, "PlayButton", "PLAY",
                                   new Color(0.1f, 0.7f, 0.3f), new Vector2(0.3f, 0.30f), new Vector2(0.7f, 0.43f));

        // Quit Button
        var quitBtn = CreateButton(canvasGO.transform, "QuitButton", "QUIT",
                                   new Color(0.7f, 0.1f, 0.1f), new Vector2(0.35f, 0.15f), new Vector2(0.65f, 0.27f));

        // MainMenuManager
        var mmGO = new GameObject("MainMenuManager");
        var mm   = mmGO.AddComponent<MainMenuManager>();

        // Wire buttons via persistent listeners
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            playBtn.GetComponent<Button>().onClick,
            mm.PlayGame);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            quitBtn.GetComponent<Button>().onClick,
            mm.QuitGame);

        EditorSceneManager.SaveScene(scene, $"{SCENES_PATH}/MainMenu.unity");
        Debug.Log("Saved: Assets/Scenes/MainMenu.unity");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GAME SCENE
    // ══════════════════════════════════════════════════════════════════════════

    static void CreateGameScene(
        GameObject enemyPrefab,
        GameObject basicTower, GameObject fastTower, GameObject sniperTower,
        GameObject battlefieldPrefab, GameObject indicatorPrefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Game";

        // ── AR Session ────────────────────────────────────────────────────────
        var arSession = new GameObject("AR Session");
        arSession.AddComponent<ARSession>();

        // ── XR Origin ─────────────────────────────────────────────────────────
        var xrOrigin = new GameObject("XR Origin (AR)");
        var arPlaneMgr    = xrOrigin.AddComponent<ARPlaneManager>();
        var arRaycastMgr  = xrOrigin.AddComponent<ARRaycastManager>();
        var arAnchorMgr   = xrOrigin.AddComponent<ARAnchorManager>();

        // Camera Offset
        var camOffset = new GameObject("Camera Offset");
        camOffset.transform.SetParent(xrOrigin.transform);

        // AR Camera
        var arCamGO = new GameObject("Main Camera");
        arCamGO.tag = "MainCamera";
        arCamGO.transform.SetParent(camOffset.transform);
        var arCam = arCamGO.AddComponent<Camera>();
        arCam.clearFlags       = CameraClearFlags.Color;
        arCam.backgroundColor  = Color.black;
        arCam.nearClipPlane    = 0.01f;
        arCamGO.AddComponent<ARCameraManager>();
        arCamGO.AddComponent<ARCameraBackground>();

        // ── Managers (persistent GameObjects) ─────────────────────────────────
        var gameManagerGO = new GameObject("GameManager");
        gameManagerGO.AddComponent<GameManager>();

        var waveManagerGO = new GameObject("WaveManager");
        var waveManager   = waveManagerGO.AddComponent<WaveManager>();

        var towerPlacerGO = new GameObject("TowerPlacer");
        var towerPlacer   = towerPlacerGO.AddComponent<TowerPlacer>();

        // TowerPlacer options
        var soTP = new SerializedObject(towerPlacer);
        var optionsProp = soTP.FindProperty("towerOptions");
        optionsProp.arraySize = 3;
        SetTowerOption(optionsProp.GetArrayElementAtIndex(0), "Basic",  basicTower,   100);
        SetTowerOption(optionsProp.GetArrayElementAtIndex(1), "Fast",   fastTower,    75);
        SetTowerOption(optionsProp.GetArrayElementAtIndex(2), "Sniper", sniperTower,  150);
        // Grid layer mask — layer 0 = Default for now (user should create "Grid" layer)
        soTP.FindProperty("gridLayerMask").intValue = 1;  // Layer 0 = Default
        soTP.ApplyModifiedProperties();

        // WaveManager waves
        var soWM = new SerializedObject(waveManager);
        var wavesProp = soWM.FindProperty("waves");
        wavesProp.arraySize = 5;
        SetWave(wavesProp.GetArrayElementAtIndex(0), enemyPrefab, 5,  1.5f, 30);
        SetWave(wavesProp.GetArrayElementAtIndex(1), enemyPrefab, 8,  1.2f, 40);
        SetWave(wavesProp.GetArrayElementAtIndex(2), enemyPrefab, 12, 1.0f, 50);
        SetWave(wavesProp.GetArrayElementAtIndex(3), enemyPrefab, 15, 0.8f, 60);
        SetWave(wavesProp.GetArrayElementAtIndex(4), enemyPrefab, 20, 0.6f, 100);
        soWM.FindProperty("timeBetweenWaves").floatValue = 5f;
        soWM.ApplyModifiedProperties();

        // ── Event System ──────────────────────────────────────────────────────
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // ── UI Canvas ─────────────────────────────────────────────────────────
        var canvasGO = new GameObject("GameUICanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Scanning Panel ────────────────────────────────────────────────────
        var scanPanel = CreateUIPanel(canvasGO.transform, "ScanningPanel", new Color(0, 0, 0, 0.7f));
        StretchToFill(scanPanel.GetComponent<RectTransform>());
        var scanText = AddTMPLabel(scanPanel.transform, "ScanText",
            "Point your camera at a flat surface,\nthen tap to place the battlefield.",
            36, Color.white);
        CenterText(scanText.GetComponent<RectTransform>(), new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.6f));

        // ── HUD Panel ─────────────────────────────────────────────────────────
        var hudPanel = new GameObject("HUDPanel");
        hudPanel.transform.SetParent(canvasGO.transform, false);
        hudPanel.SetActive(false);

        // Top bar background
        var topBar = CreateUIPanel(hudPanel.transform, "TopBar", new Color(0, 0, 0, 0.75f));
        var topBarRt = topBar.GetComponent<RectTransform>();
        topBarRt.anchorMin = new Vector2(0, 0.92f); topBarRt.anchorMax = Vector2.one;
        topBarRt.offsetMin = topBarRt.offsetMax = Vector2.zero;

        var livesText = AddTMPLabel(topBar.transform, "LivesText", "♥  10", 32, Color.red);
        AnchorText(livesText.GetComponent<RectTransform>(), new Vector2(0.02f, 0), new Vector2(0.30f, 1f));

        var goldText = AddTMPLabel(topBar.transform, "GoldText", "$  150", 32, Color.yellow);
        AnchorText(goldText.GetComponent<RectTransform>(), new Vector2(0.35f, 0), new Vector2(0.65f, 1f));

        var waveText = AddTMPLabel(topBar.transform, "WaveText", "Wave –", 32, Color.white);
        AnchorText(waveText.GetComponent<RectTransform>(), new Vector2(0.68f, 0), new Vector2(0.98f, 1f));

        // ── Tower Shop Panel ──────────────────────────────────────────────────
        var shopPanel = CreateUIPanel(hudPanel.transform, "ShopPanel", new Color(0, 0, 0, 0.8f));
        var shopRt = shopPanel.GetComponent<RectTransform>();
        shopRt.anchorMin = new Vector2(0, 0); shopRt.anchorMax = new Vector2(1, 0.14f);
        shopRt.offsetMin = shopRt.offsetMax = Vector2.zero;

        // Tower buttons
        string[] towerNames   = {"Basic Tower", "Fast Tower", "Sniper Tower"};
        int[]    towerCosts   = {100, 75, 150};
        Color[]  towerColors  = {new Color(0.2f, 0.6f, 1f), new Color(0.2f, 1f, 0.4f), new Color(1f, 0.6f, 0.2f)};

        float btnWidth = 1f / 3f;
        var towerShopButtons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            var btnGO = CreateButton(shopPanel.transform, $"TowerBtn_{i}", towerNames[i],
                                     towerColors[i],
                                     new Vector2(i * btnWidth + 0.01f, 0.05f),
                                     new Vector2((i + 1) * btnWidth - 0.01f, 0.95f));

            // Add cost label
            var costLbl = AddTMPLabel(btnGO.transform, "CostLabel", $"{towerCosts[i]}g", 22, Color.yellow);
            var costRt = costLbl.GetComponent<RectTransform>();
            costRt.anchorMin = new Vector2(0, 0); costRt.anchorMax = new Vector2(1, 0.35f);
            costRt.offsetMin = costRt.offsetMax = Vector2.zero;
            costLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            var shopBtn = btnGO.AddComponent<TowerShopButton>();
            var soSB = new SerializedObject(shopBtn);
            soSB.FindProperty("towerIndex").intValue = i;
            soSB.FindProperty("costLabel").objectReferenceValue = costLbl.GetComponent<TextMeshProUGUI>();
            soSB.ApplyModifiedProperties();

            towerShopButtons[i] = btnGO.GetComponent<Button>();
        }

        // ── Send Wave Button ──────────────────────────────────────────────────
        var sendWaveBtn = CreateButton(hudPanel.transform, "SendWaveButton", "Send Wave 1",
                                       new Color(0.8f, 0.3f, 0.1f),
                                       new Vector2(0.25f, 0.14f), new Vector2(0.75f, 0.22f));

        // ── Game Over Panel ───────────────────────────────────────────────────
        var gameOverPanel = CreateUIPanel(canvasGO.transform, "GameOverPanel", new Color(0.6f, 0f, 0f, 0.92f));
        StretchToFill(gameOverPanel.GetComponent<RectTransform>());
        gameOverPanel.SetActive(false);
        AddTMPLabel(gameOverPanel.transform, "GOTitle",    "GAME OVER",      72, Color.white).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        AnchorText(gameOverPanel.transform.Find("GOTitle").GetComponent<RectTransform>(), new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.75f));
        var restartBtn = CreateButton(gameOverPanel.transform, "RestartBtn", "RESTART",
                                      new Color(0.2f, 0.6f, 1f),
                                      new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.48f));
        var goMenuBtn  = CreateButton(gameOverPanel.transform, "MenuBtn", "MAIN MENU",
                                      new Color(0.4f, 0.4f, 0.4f),
                                      new Vector2(0.2f, 0.20f), new Vector2(0.8f, 0.33f));

        // ── Victory Panel ─────────────────────────────────────────────────────
        var victoryPanel = CreateUIPanel(canvasGO.transform, "VictoryPanel", new Color(0f, 0.5f, 0f, 0.92f));
        StretchToFill(victoryPanel.GetComponent<RectTransform>());
        victoryPanel.SetActive(false);
        AddTMPLabel(victoryPanel.transform, "VTitle", "VICTORY!",         72, Color.yellow).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        AnchorText(victoryPanel.transform.Find("VTitle").GetComponent<RectTransform>(), new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.75f));
        var vMenuBtn   = CreateButton(victoryPanel.transform, "VMenuBtn", "MAIN MENU",
                                      new Color(0.2f, 0.6f, 1f),
                                      new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.48f));

        // ── UIManager ─────────────────────────────────────────────────────────
        var uiManagerGO = new GameObject("UIManager");
        var uiManager   = uiManagerGO.AddComponent<UIManager>();
        var soUI = new SerializedObject(uiManager);
        soUI.FindProperty("scanningPanel").objectReferenceValue = scanPanel;
        soUI.FindProperty("hudPanel").objectReferenceValue      = hudPanel;
        soUI.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
        soUI.FindProperty("victoryPanel").objectReferenceValue  = victoryPanel;
        soUI.FindProperty("livesText").objectReferenceValue     = livesText.GetComponent<TextMeshProUGUI>();
        soUI.FindProperty("goldText").objectReferenceValue      = goldText.GetComponent<TextMeshProUGUI>();
        soUI.FindProperty("waveText").objectReferenceValue      = waveText.GetComponent<TextMeshProUGUI>();
        soUI.FindProperty("sendWaveButton").objectReferenceValue = sendWaveBtn.GetComponent<Button>();
        soUI.FindProperty("shopPanel").objectReferenceValue     = shopPanel;
        soUI.ApplyModifiedProperties();

        // Wire restart / menu buttons
        var gm = gameManagerGO.GetComponent<GameManager>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            restartBtn.GetComponent<Button>().onClick, gm.RestartGame);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            goMenuBtn.GetComponent<Button>().onClick,  gm.LoadMainMenu);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            vMenuBtn.GetComponent<Button>().onClick,   gm.LoadMainMenu);

        // ── BattlefieldPlacer (on XR Origin) ─────────────────────────────────
        var placer = xrOrigin.AddComponent<BattlefieldPlacer>();
        var soP = new SerializedObject(placer);
        soP.FindProperty("battlefieldPrefab").objectReferenceValue = battlefieldPrefab;
        soP.FindProperty("placementIndicator").objectReferenceValue = indicatorPrefab != null
            ? (Object)indicatorPrefab : null;
        soP.FindProperty("scanningPanel").objectReferenceValue  = scanPanel;
        soP.FindProperty("gameUIPanel").objectReferenceValue    = hudPanel;
        soP.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, $"{SCENES_PATH}/Game.unity");
        Debug.Log("Saved: Assets/Scenes/Game.unity");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BUILD SETTINGS
    // ══════════════════════════════════════════════════════════════════════════

    static void AddScenesToBuildSettings()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene($"{SCENES_PATH}/MainMenu.unity", true),
            new EditorBuildSettingsScene($"{SCENES_PATH}/Game.unity",     true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("Build Settings updated with MainMenu (0) and Game (1).");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string child  = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    static void SetColor(GameObject go, Color color, bool transparent = false)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        if (transparent)
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend",   0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }
        r.sharedMaterial = mat;
    }

    static GameObject CreateUIPanel(Transform parent, string name, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static void StretchToFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static GameObject CreateButton(Transform parent, string name, string label, Color color,
                                   Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        var cb  = btn.colors;
        cb.highlightedColor = color * 1.2f;
        cb.pressedColor     = color * 0.8f;
        btn.colors = cb;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Label inside button
        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 30;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var trt = tmp.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        return go;
    }

    static GameObject AddTMPLabel(Transform parent, string name, string text, float size, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    static void CenterText(RectTransform rt, Vector2 amin, Vector2 amax)
    {
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void AnchorText(RectTransform rt, Vector2 amin, Vector2 amax)
    {
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void SetTowerOption(SerializedProperty prop, string displayName, GameObject prefab, int cost)
    {
        prop.FindPropertyRelative("displayName").stringValue              = displayName;
        prop.FindPropertyRelative("prefab").objectReferenceValue          = prefab;
        prop.FindPropertyRelative("cost").intValue                        = cost;
    }

    static void SetWave(SerializedProperty prop, GameObject enemyPrefab, int count, float interval, int bonus)
    {
        prop.FindPropertyRelative("enemyPrefab").objectReferenceValue     = enemyPrefab;
        prop.FindPropertyRelative("count").intValue                       = count;
        prop.FindPropertyRelative("spawnInterval").floatValue             = interval;
        prop.FindPropertyRelative("completionBonus").intValue             = bonus;
    }
}
#endif
