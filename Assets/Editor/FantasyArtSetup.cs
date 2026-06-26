using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// One-shot art integration: swaps the primitive tower/enemy visuals for the
/// downloaded fantasy 3D models, builds URP materials from the texture atlases,
/// auto-fits each model to the battlefield grid, and sets up the skybox.
/// Run via the menu: Tools ▸ Fantasy Art ▸ Apply All.
/// </summary>
public static class FantasyArtSetup
{
    const string TowersAtlas   = "Assets/Art/Towers/TowersAtlas.png";
    const string MonstersAtlas = "Assets/Art/Monsters/MonstersAtlas.png";
    const string SkyHdr        = "Assets/Art/Skybox/SkyDusk.hdr";

    // Visual heights (metres) tuned to the ~0.1 m grid cells.
    const float TowerHeight = 0.14f;
    const float EnemyHeight = 0.10f;

    [MenuItem("Tools/Fantasy Art/Apply All")]
    public static void ApplyAll()
    {
        var towerMat   = MakeMaterial("Assets/Art/Towers/TowersMat.mat",   TowersAtlas);
        var monsterMat = MakeMaterial("Assets/Art/Monsters/MonstersMat.mat", MonstersAtlas);

        SwapVisual("Assets/Prefabs/Towers/Tower_Basic.prefab",  "Assets/Art/Towers/ArcherTower.fbx", towerMat, TowerHeight, true);
        SwapVisual("Assets/Prefabs/Towers/Tower_Fast.prefab",   "Assets/Art/Towers/WizardTower.fbx", towerMat, TowerHeight, true);
        SwapVisual("Assets/Prefabs/Towers/Tower_Sniper.prefab", "Assets/Art/Towers/CannonTower.fbx", towerMat, TowerHeight, true);
        SwapVisual("Assets/Prefabs/Enemies/Enemy.prefab",       "Assets/Art/Monsters/Orc.fbx",       monsterMat, EnemyHeight, false);

        SetupSkybox();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FantasyArt] Apply All complete.");
    }

    static Material MakeMaterial(string path, string texPath)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader);
        var tex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
        }
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", 0.0f);
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) { existing.shader = shader; EditorUtility.CopySerialized(mat, existing); return existing; }
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    /// <summary>
    /// Strips primitive mesh visuals from a prefab and parents the fantasy model
    /// under its root, auto-scaled to targetHeight and grounded at y=0.
    /// For towers (isTower) it also lifts the FirePoint to the model's top.
    /// </summary>
    static void SwapVisual(string prefabPath, string modelPath, Material mat, float targetHeight, bool isTower)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null) { Debug.LogError("[FantasyArt] Missing model: " + modelPath); return; }

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            // Remove existing primitive mesh visuals (keep the GameObjects/pivots).
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
                Object.DestroyImmediate(mr);
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                Object.DestroyImmediate(mf);

            // Instantiate the fantasy model under the root.
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.name = "Visual";
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            // Auto-fit: scale so the model's height == targetHeight, then ground it.
            Bounds b = ComputeBounds(inst);
            if (b.size.y > 1e-5f)
            {
                float s = targetHeight / b.size.y;
                inst.transform.localScale = Vector3.one * s;
            }
            b = ComputeBounds(inst);
            inst.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);

            // Assign the atlas material to every renderer.
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var arr = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < arr.Length; i++) arr[i] = mat;
                r.sharedMaterials = arr;
            }

            if (isTower)
            {
                var fire = FindDeep(root.transform, "FirePoint");
                if (fire != null)
                {
                    Bounds top = ComputeBounds(inst);
                    fire.localPosition = new Vector3(0f, top.max.y, top.size.z * 0.5f);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log("[FantasyArt] Swapped " + prefabPath + " -> " + modelPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static Bounds ComputeBounds(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.1f);
        Bounds b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        // Convert world bounds to the instance's local space (instance is at origin-ish here).
        b.center -= go.transform.position;
        return b;
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t) { var r = FindDeep(c, name); if (r != null) return r; }
        return null;
    }

    static void SetupSkybox()
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture>(SkyHdr);
        if (tex == null) { Debug.LogWarning("[FantasyArt] Skybox HDR not found."); return; }
        var sky = new Material(Shader.Find("Skybox/Panoramic"));
        sky.SetTexture("_MainTex", tex);
        sky.SetFloat("_Mapping", 1f);          // Latitude-Longitude
        sky.SetFloat("_Exposure", 1.1f);
        const string skyPath = "Assets/Art/Skybox/SkyboxMat.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if (existing != null) EditorUtility.CopySerialized(sky, existing);
        else AssetDatabase.CreateAsset(sky, skyPath);
        Debug.Log("[FantasyArt] Skybox material created at " + skyPath);
    }
}
