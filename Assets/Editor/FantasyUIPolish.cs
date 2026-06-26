using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Second-pass polish: imports the Kenney UI sprites, restyles all buttons and
/// panels in the "blue stone &amp; silver" theme, sets the "TOWER SIEGE" title,
/// and puts the dusk sky behind the main menu. Run: Tools ▸ Fantasy Art ▸ Polish UI &amp; Menu.
/// </summary>
public static class FantasyUIPolish
{
    const string UIDir   = "Assets/Art/UI/";
    const string SkyMat  = "Assets/Art/Skybox/SkyboxMat.mat";

    // Theme colours.
    static readonly Color Silver = new Color(0.85f, 0.90f, 1.00f);
    static readonly Color Gold   = new Color(1.00f, 0.86f, 0.45f);

    [MenuItem("Tools/Fantasy Art/Polish UI & Menu")]
    public static void Polish()
    {
        ImportSprites();

        Sprite btn   = Load("buttonLong_blue");
        Sprite btnSq = Load("buttonSquare_blue");
        Sprite panel = Load("panel_blue");
        Sprite inset = Load("panelInset_blue");

        string current = EditorSceneManager.GetActiveScene().path;

        RestyleScene("Assets/Scenes/MainMenu.unity", true,  btn, btnSq, panel, inset);
        RestyleScene("Assets/Scenes/Game.unity",     false, btn, btnSq, panel, inset);

        if (!string.IsNullOrEmpty(current))
            EditorSceneManager.OpenScene(current, OpenSceneMode.Single);

        AssetDatabase.SaveAssets();
        Debug.Log("[FantasyUI] Polish complete.");
    }

    static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(UIDir + name + ".png");

    static void ImportSprites()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/UI" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            bool changed = false;
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; changed = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }
            int b = path.Contains("panel") ? 24 : 16;
            var so = new SerializedObject(imp);
            var border = so.FindProperty("m_SpriteBorder");
            if (border != null && border.vector4Value != new Vector4(b, b, b, b))
            {
                border.vector4Value = new Vector4(b, b, b, b);
                so.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
            if (changed) imp.SaveAndReimport();
        }
    }

    static void RestyleScene(string scenePath, bool isMenu, Sprite btn, Sprite btnSq, Sprite panel, Sprite inset)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var roots = scene.GetRootGameObjects();
        var all = new List<GameObject>();
        foreach (var r in roots) CollectAll(r.transform, all);

        foreach (var go in all)
        {
            // Buttons.
            var button = go.GetComponent<Button>();
            if (button != null)
            {
                var img = go.GetComponent<Image>();
                if (img != null)
                {
                    bool square = go.name.ToLower().Contains("towerbtn") || go.name.ToLower().Contains("square");
                    img.sprite = square ? btnSq : btn;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                }
                var spr = button.spriteState;            // keep transition simple
                button.transition = Selectable.Transition.ColorTint;
                var cb = button.colors; cb.normalColor = Color.white; cb.highlightedColor = new Color(0.85f,0.9f,1f);
                cb.pressedColor = new Color(0.7f,0.78f,0.95f); button.colors = cb;
            }
            else
            {
                // Panel backgrounds (Image without a Button), by name.
                var img = go.GetComponent<Image>();
                if (img != null)
                {
                    string n = go.name.ToLower();
                    if (n.Contains("panel") || n == "topbar" || n.Contains("hud") || n == "background")
                    {
                        img.sprite = n.Contains("inset") ? inset : panel;
                        img.type = Image.Type.Sliced;
                        if (n == "background") img.color = new Color(1f,1f,1f,1f);
                    }
                }
            }

            // Text colours.
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.color = Silver;
                if (go.name == "TitleText") { tmp.text = "TOWER SIEGE"; tmp.color = Gold; tmp.fontStyle = FontStyles.Bold; }
                if (go.name == "SubtitleText") { tmp.text = "Defend the Realm"; tmp.color = Silver; }
            }
        }

        if (isMenu)
        {
            // Sky behind the menu.
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>(SkyMat);
            if (skyMat != null) RenderSettings.skybox = skyMat;
            foreach (var go in all)
            {
                if (go.name == "Background") go.SetActive(false);   // let the sky show through
                var cam = go.GetComponent<Camera>();
                if (cam != null) { cam.clearFlags = CameraClearFlags.Skybox; }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FantasyUI] Restyled " + scenePath);
    }

    static void CollectAll(Transform t, List<GameObject> list)
    {
        list.Add(t.gameObject);
        foreach (Transform c in t) CollectAll(c, list);
    }
}
