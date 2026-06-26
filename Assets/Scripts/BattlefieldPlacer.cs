using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Handles the AR placement phase:
///   1. Detects horizontal planes via ARFoundation.
///   2. Shows a placement indicator at the screen centre.
///   3. On tap, instantiates the battlefield prefab, anchors it, then transitions to Build state.
///
/// Attach this to the same GameObject as ARSession / XR Origin (or any persistent root).
/// </summary>
[RequireComponent(typeof(ARRaycastManager))]
[RequireComponent(typeof(ARPlaneManager))]
public class BattlefieldPlacer : MonoBehaviour
{
    [Header("Prefabs & Indicator")]
    [Tooltip("The root battlefield prefab (must have a BattlefieldController component).")]
    [SerializeField] GameObject battlefieldPrefab;

    [Tooltip("Optional: a reticle / ring shown where the battlefield will land.")]
    [SerializeField] GameObject placementIndicator;

    [Header("UI Panels")]
    [Tooltip("UI shown while scanning (e.g. 'Point camera at a flat surface').")]
    [SerializeField] GameObject scanningPanel;

    [Tooltip("Root game UI to activate once the battlefield is placed.")]
    [SerializeField] GameObject gameUIPanel;

    [Header("Placement Tuning")]
    [Tooltip("Uniform scale applied to the spawned battlefield. The prefab is ~0.6 m; raise this to make the board span a wider portion of the screen.")]
    [SerializeField] float battlefieldScale = 1.8f;

    [Header("Force-Place Fallback")]
    [Tooltip("Seconds after scanning begins before the 'Place here' button appears, giving the player time to find a surface.")]
    [SerializeField] float forceButtonDelay = 7f;

    [Tooltip("How far in front of the camera the battlefield is dropped when force-placed (metres).")]
    [SerializeField] float forcePlaceDistance = 0.8f;

    [Tooltip("How far below eye level the force-placed battlefield sits (metres).")]
    [SerializeField] float forcePlaceDrop = 0.4f;

    // ── Private ───────────────────────────────────────────────────────────────
    ARRaycastManager  raycastManager;
    ARPlaneManager    planeManager;
    readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // A live, in-world instance of the reticle. The serialized 'placementIndicator'
    // is a prefab asset, so toggling it directly does nothing visible — we spawn
    // a real instance here and drive that instead.
    GameObject indicatorInstance;

    // Runtime-built UI for the force-place fallback button.
    GameObject forceButtonCanvas;
    float      placementTimer;

    bool battlefieldPlaced;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager   = GetComponent<ARPlaneManager>();

        if (placementIndicator != null)
        {
            indicatorInstance = Instantiate(placementIndicator);
            indicatorInstance.SetActive(false);
        }

        forceButtonCanvas = BuildForceButton();
        forceButtonCanvas.SetActive(false);
    }

    void Update()
    {
        if (battlefieldPlaced) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.State != GameManager.GameState.Placement) return;

        // Update placement indicator position every frame
        UpdateIndicator();

        // After a grace period, reveal the "Place here" fallback button.
        placementTimer += Time.deltaTime;
        if (forceButtonCanvas != null && !forceButtonCanvas.activeSelf &&
            placementTimer >= forceButtonDelay)
        {
            forceButtonCanvas.SetActive(true);
        }

        // Wait for a tap (finger on device, or mouse click in the Editor)
        if (!TryGetTapPosition(out Vector2 tapPosition)) return;

        // Ignore taps that land on the force-place button (let the button handle them).
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        if (raycastManager.Raycast(tapPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            PlaceBattlefield(hits[0].pose);
        }
    }

    /// <summary>
    /// Fallback used by the "Place here" button: drops the battlefield on a
    /// virtual surface in front of the camera, so the player is never stuck if
    /// plane detection is struggling.
    /// </summary>
    public void ForcePlace()
    {
        if (battlefieldPlaced) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 position = cam.transform.position
                         + forward * forcePlaceDistance
                         + Vector3.down * forcePlaceDrop;
        Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);

        PlaceBattlefield(new Pose(position, rotation));
    }

    /// <summary>
    /// Returns true and outputs the screen position of a "tap" this frame:
    /// a finger touch on device, or — in the Editor — a left mouse click so the
    /// placement flow can be tested in Play mode with XR Simulation.
    /// </summary>
    bool TryGetTapPosition(out Vector2 position)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                position = touch.position;
                return true;
            }
        }
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }
#endif
        position = default;
        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Move the reticle to the detected plane position at screen centre.</summary>
    void UpdateIndicator()
    {
        if (indicatorInstance == null) return;

        Vector2 centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (raycastManager.Raycast(centre, hits, TrackableType.PlaneWithinPolygon))
        {
            indicatorInstance.SetActive(true);
            indicatorInstance.transform.SetPositionAndRotation(
                hits[0].pose.position,
                hits[0].pose.rotation);
        }
        else
        {
            indicatorInstance.SetActive(false);
        }
    }

    /// <summary>Instantiate and anchor the battlefield, then kick off the game.</summary>
    void PlaceBattlefield(Pose pose)
    {
        battlefieldPlaced = true;

        // Spawn battlefield
        GameObject battlefield = Instantiate(battlefieldPrefab, pose.position, pose.rotation);
        battlefield.transform.localScale = Vector3.one * battlefieldScale;

        // Anchor it so ARCore keeps it locked to the real world
        if (!battlefield.TryGetComponent<ARAnchor>(out _))
            battlefield.AddComponent<ARAnchor>();

        // Hide placement helper + force-place button
        if (indicatorInstance != null) indicatorInstance.SetActive(false);
        if (forceButtonCanvas != null) forceButtonCanvas.SetActive(false);

        // Stop detecting new planes — we have what we need
        SetPlanesVisible(false);
        planeManager.enabled = false;

        // Switch UI
        if (scanningPanel != null) scanningPanel.SetActive(false);
        if (gameUIPanel   != null) gameUIPanel.SetActive(true);

        // Wire up the rest of the game systems
        if (battlefield.TryGetComponent<BattlefieldController>(out var controller))
        {
            WaveManager.Instance.Initialize(controller);
            TowerPlacer.Instance.Initialize(controller);
        }
        else
        {
            Debug.LogError("[BattlefieldPlacer] Battlefield prefab is missing a BattlefieldController component!");
        }

        // Move to Build state so the player can place towers
        GameManager.Instance.SetState(GameManager.GameState.Build);
    }

    /// <summary>Show or hide all currently tracked AR plane visuals.</summary>
    void SetPlanesVisible(bool visible)
    {
        foreach (var plane in planeManager.trackables)
            plane.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Builds the fallback "Place battlefield here" button entirely in code so it
    /// needs no manual wiring in the scene. A screen-space overlay button anchored
    /// near the bottom of the screen, hooked up to <see cref="ForcePlace"/>.
    /// </summary>
    GameObject BuildForceButton()
    {
        var canvasGO = new GameObject("ForcePlaceCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var btnGO = new GameObject("ForcePlaceButton");
        btnGO.transform.SetParent(canvasGO.transform, false);
        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.13f, 0.55f, 0.95f, 0.95f);
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(ForcePlace);

        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 200f);
        rt.sizeDelta = new Vector2(680f, 150f);

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(btnGO.transform, false);
        var txt = txtGO.AddComponent<Text>();
        txt.text = "Place battlefield here";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.fontSize = 46;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var trt = txtGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        return canvasGO;
    }
}
