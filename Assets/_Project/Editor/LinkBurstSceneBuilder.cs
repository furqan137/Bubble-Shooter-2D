using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// One-click scene builder for Link Burst.
/// Menu: LinkBurst -> Build Complete Scene
/// </summary>
public class LinkBurstSceneBuilder : Editor
{
    [MenuItem("LinkBurst/Build Complete Scene")]
    public static void BuildScene()
    {
        // ── Clear scene ───────────────────────────────────────────────────────
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in roots)
            DestroyImmediate(go);

        // ── Camera ────────────────────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.5f;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.11f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0, 0, -10);
        camGO.AddComponent<AudioListener>();

        // ── Manager GameObjects ───────────────────────────────────────────────
        var gmGO   = new GameObject("GameManager");
        var bmGO   = new GameObject("BoardManager");
        var imGO   = new GameObject("InputManager");
        var smGO   = new GameObject("ScoreManager");
        var pmGO   = new GameObject("PowerUpManager");
        var amGO   = new GameObject("AudioManager");
        var uiGO   = new GameObject("UIManager");

        var gm  = gmGO.AddComponent<GameManager>();
        var bm  = bmGO.AddComponent<BoardManager>();
        var im  = imGO.AddComponent<InputManager>();
        smGO.AddComponent<ScoreManager>();
        pmGO.AddComponent<PowerUpManager>();
        amGO.AddComponent<AudioManager>();
        var uiM = uiGO.AddComponent<UIManager>();

        // ── Board root ────────────────────────────────────────────────────────
        var boardGO = new GameObject("Board");
        bm.boardParent = boardGO.transform;
        bm.gridWidth   = 6;
        bm.gridHeight  = 6;
        bm.cellSize    = 1.1f;
        bm.cellSpacing = 0.05f;

        // Node glow colors (6 colours matching NodeColor enum order)
        bm.nodeGlowColors = new Color[]
        {
            new Color(1.00f, 0.23f, 0.23f), // Red
            new Color(0.23f, 0.56f, 1.00f), // Blue
            new Color(0.23f, 1.00f, 0.48f), // Green
            new Color(1.00f, 0.88f, 0.23f), // Yellow
            new Color(0.69f, 0.23f, 1.00f), // Purple
            new Color(1.00f, 0.55f, 0.23f), // Orange
        };

        // ── Node Prefab ───────────────────────────────────────────────────────
        bm.nodePrefab = CreateNodePrefab(bm.nodeGlowColors);

        // ── Chain LineRenderer ────────────────────────────────────────────────
        var lineGO = new GameObject("ChainLine");
        var lr     = lineGO.AddComponent<LineRenderer>();
        lr.startWidth    = 0.09f;
        lr.endWidth      = 0.09f;
        lr.positionCount = 0;
        lr.useWorldSpace = true;
        lr.sortingOrder  = 10;
        var lineMat = new Material(Shader.Find("Sprites/Default"));
        lineMat.color = Color.white;
        lr.material = lineMat;
        im.chainLine      = lr;
        im.minimumChainLength = 2;
        im.validColor   = new Color(1f, 1f, 1f, 0.9f);
        im.pendingColor  = new Color(1f, 0.85f, 0f, 0.9f);

        // ── GameManager defaults ──────────────────────────────────────────────
        gm.defaultTargetScore = 500;
        gm.defaultStartEnergy = 30;
        gm.defaultGridWidth   = 6;
        gm.defaultGridHeight  = 6;

        // ── UI Canvas ─────────────────────────────────────────────────────────
        var canvasGO = new GameObject("UI Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Background image
        var bgGO  = MakeGO("Background", canvasGO);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.04f, 0.11f);
        StretchFull(bgGO);

        // ── Splash Screen ─────────────────────────────────────────────────────
        var splash   = MakePanel(canvasGO, "SplashScreen", new Color(0.06f, 0.02f, 0.18f));
        var splashCG = splash.AddComponent<CanvasGroup>();
        var logoTxt  = MakeTMP(splash, "LogoText", "LINK\nBURST", 110, Color.yellow);
        Anchor(logoTxt, 0.5f, 0.62f, 900, 260);
        MakeTMP(splash, "LoadingText", "loading...", 40, Color.white);
        Anchor(splash.transform.Find("LoadingText").gameObject, 0.5f, 0.28f, 500, 70);
        var splashComp    = splash.AddComponent<SplashScreen>();
        splashComp.logoGroup = splashCG;
        uiM.splashScreen  = splash;

        // ── Main Menu ─────────────────────────────────────────────────────────
        var menu = MakePanel(canvasGO, "MainMenuScreen", new Color(0.06f, 0.02f, 0.18f));
        MakeTMP(menu, "Title", "LINK BURST", 96, Color.yellow);
        Anchor(menu.transform.Find("Title").gameObject, 0.5f, 0.76f, 900, 160);

        var playBtn = MakeButton(menu, "PlayButton", "PLAY", new Color(0.85f, 0.42f, 0.08f));
        Anchor(playBtn, 0.5f, 0.56f, 520, 110);
        BindBtn(playBtn, uiM, "OnPlayButton");

        var lvlBtn = MakeButton(menu, "LevelSelectButton", "LEVEL SELECT", new Color(0.28f, 0.18f, 0.60f));
        Anchor(lvlBtn, 0.5f, 0.43f, 520, 110);
        BindBtn(lvlBtn, uiM, "OnLevelSelectButton");

        var settBtn = MakeButton(menu, "SettingsButton", "SETTINGS", new Color(0.28f, 0.18f, 0.60f));
        Anchor(settBtn, 0.5f, 0.30f, 520, 110);
        BindBtn(settBtn, uiM, "ToggleSettings");
        uiM.mainMenuScreen = menu;

        // ── Level Select ──────────────────────────────────────────────────────
        var lvlSel = MakePanel(canvasGO, "LevelSelectScreen", new Color(0.06f, 0.02f, 0.18f));
        MakeTMP(lvlSel, "Title", "SELECT LEVEL", 66, Color.yellow);
        Anchor(lvlSel.transform.Find("Title").gameObject, 0.5f, 0.89f, 800, 100);
        var backBtn = MakeButton(lvlSel, "BackButton", "< BACK", new Color(0.28f, 0.18f, 0.60f));
        Anchor(backBtn, 0.15f, 0.89f, 200, 80);
        BindBtn(backBtn, uiM, "OnHomeButton");

        var containerGO = MakeGO("ButtonContainer", lvlSel);
        var grid = containerGO.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(185, 185);
        grid.spacing         = new Vector2(18, 18);
        grid.padding         = new RectOffset(40, 40, 30, 30);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        var cRT = containerGO.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0f, 0.08f);
        cRT.anchorMax = new Vector2(1f, 0.83f);
        cRT.sizeDelta = Vector2.zero;
        cRT.anchoredPosition = Vector2.zero;
        var lvlUI = lvlSel.AddComponent<LevelSelectUI>();
        lvlUI.buttonContainer  = containerGO.transform;
        uiM.levelSelectScreen  = lvlSel;

        // ── Gameplay Screen ───────────────────────────────────────────────────
        var gpScreen = MakePanel(canvasGO, "GameplayScreen", Color.clear);
        gpScreen.GetComponent<Image>().enabled = false;

        // Score & info
        var scoreT = MakeTMP(gpScreen, "ScoreText", "0", 60, Color.white);
        Anchor(scoreT, 0.5f, 0.935f, 420, 85);
        uiM.scoreText = scoreT.GetComponent<TextMeshProUGUI>();

        var targetT = MakeTMP(gpScreen, "TargetScoreText", "Goal: 500", 34, new Color(1f, 1f, 0.5f));
        Anchor(targetT, 0.5f, 0.884f, 420, 60);
        uiM.targetScoreText = targetT.GetComponent<TextMeshProUGUI>();

        var energyT = MakeTMP(gpScreen, "EnergyText", "30/30", 36, Color.cyan);
        Anchor(energyT, 0.18f, 0.935f, 220, 60);
        uiM.energyText = energyT.GetComponent<TextMeshProUGUI>();

        var levelT = MakeTMP(gpScreen, "LevelText", "Level 1", 36, Color.white);
        Anchor(levelT, 0.82f, 0.935f, 220, 60);
        uiM.levelText = levelT.GetComponent<TextMeshProUGUI>();

        var comboT = MakeTMP(gpScreen, "ComboText", "+50", 62, Color.yellow);
        Anchor(comboT, 0.5f, 0.62f, 420, 90);
        uiM.comboText = comboT.GetComponent<TextMeshProUGUI>();

        var comboM = MakeTMP(gpScreen, "ComboMultiText", "x2 COMBO!", 46, new Color(1f, 0.5f, 0f));
        Anchor(comboM, 0.5f, 0.57f, 420, 70);
        uiM.comboMultiText = comboM.GetComponent<TextMeshProUGUI>();

        // Pause button
        var pauseB = MakeButton(gpScreen, "PauseButton", "II", new Color(0.2f, 0.2f, 0.2f, 0.8f));
        Anchor(pauseB, 0.06f, 0.935f, 90, 90);
        BindBtn(pauseB, uiM, "OnPauseButton");

        // Power-up bar
        var boostB = MakeButton(gpScreen, "BoostButton", "BOOST\n3", new Color(0.15f, 0.55f, 0.95f));
        Anchor(boostB, 0.18f, 0.07f, 210, 90);
        BindBtn(boostB, uiM, "OnBoostButton");
        uiM.boostCountText = MakeTMP(boostB, "Count", "3", 28, Color.white).GetComponent<TextMeshProUGUI>();

        var bombB = MakeButton(gpScreen, "BombButton", "BOMB\n3", new Color(0.88f, 0.18f, 0.08f));
        Anchor(bombB, 0.50f, 0.07f, 210, 90);
        BindBtn(bombB, uiM, "OnBombButton");
        uiM.bombCountText = MakeTMP(bombB, "Count", "3", 28, Color.white).GetComponent<TextMeshProUGUI>();

        var nrgB = MakeButton(gpScreen, "EnergyButton", "+5 NRG\n3", new Color(0.08f, 0.65f, 0.18f));
        Anchor(nrgB, 0.82f, 0.07f, 210, 90);
        BindBtn(nrgB, uiM, "OnEnergyButton");
        uiM.energyCountText = MakeTMP(nrgB, "Count", "3", 28, Color.white).GetComponent<TextMeshProUGUI>();

        uiM.gameplayScreen = gpScreen;

        // ── Pause Menu ────────────────────────────────────────────────────────
        var pauseMenu = MakeModal(canvasGO, "PauseMenu", new Color(0.05f, 0.02f, 0.15f, 0.96f));
        MakeTMP(pauseMenu, "Title", "PAUSED", 74, Color.yellow);
        Anchor(pauseMenu.transform.Find("Title").gameObject, 0.5f, 0.70f, 600, 100);
        var rb1 = MakeButton(pauseMenu, "Resume", "RESUME", new Color(0.08f, 0.65f, 0.18f)); Anchor(rb1, 0.5f, 0.56f, 460, 95); BindBtn(rb1, uiM, "OnResumeButton");
        var rb2 = MakeButton(pauseMenu, "Restart", "RESTART", new Color(0.80f, 0.40f, 0.08f)); Anchor(rb2, 0.5f, 0.44f, 460, 95); BindBtn(rb2, uiM, "OnRestartButton");
        var rb3 = MakeButton(pauseMenu, "Home", "HOME", new Color(0.48f, 0.08f, 0.08f)); Anchor(rb3, 0.5f, 0.32f, 460, 95); BindBtn(rb3, uiM, "OnHomeButton");
        uiM.pauseMenu = pauseMenu;

        // ── Win Screen ────────────────────────────────────────────────────────
        var winScreen = MakeModal(canvasGO, "WinScreen", new Color(0.02f, 0.10f, 0.04f, 0.96f));
        MakeTMP(winScreen, "Title", "LEVEL COMPLETE!", 66, Color.yellow);
        Anchor(winScreen.transform.Find("Title").gameObject, 0.5f, 0.73f, 800, 100);
        var wsT = MakeTMP(winScreen, "WinScore", "0", 76, Color.white); Anchor(wsT, 0.5f, 0.61f, 500, 100);
        uiM.winScoreText = wsT.GetComponent<TextMeshProUGUI>();
        var whT = MakeTMP(winScreen, "HighScore", "Best: 0", 36, new Color(1f, 1f, 0.5f)); Anchor(whT, 0.5f, 0.53f, 440, 60);
        uiM.winHighScoreText = whT.GetComponent<TextMeshProUGUI>();
        var wb1 = MakeButton(winScreen, "Next", "NEXT", new Color(0.08f, 0.65f, 0.18f)); Anchor(wb1, 0.5f, 0.40f, 420, 95); BindBtn(wb1, uiM, "OnNextLevelButton");
        var wb2 = MakeButton(winScreen, "Home", "HOME", new Color(0.28f, 0.18f, 0.60f)); Anchor(wb2, 0.5f, 0.28f, 420, 95); BindBtn(wb2, uiM, "OnHomeButton");
        uiM.winScreen = winScreen;

        // ── Lose Screen ───────────────────────────────────────────────────────
        var loseScreen = MakeModal(canvasGO, "LoseScreen", new Color(0.14f, 0.02f, 0.02f, 0.96f));
        MakeTMP(loseScreen, "Title", "GAME OVER", 76, Color.red);
        Anchor(loseScreen.transform.Find("Title").gameObject, 0.5f, 0.66f, 700, 110);
        var lb1 = MakeButton(loseScreen, "Retry", "RETRY", new Color(0.80f, 0.40f, 0.08f)); Anchor(lb1, 0.5f, 0.50f, 440, 95); BindBtn(lb1, uiM, "OnRestartButton");
        var lb2 = MakeButton(loseScreen, "Home", "HOME", new Color(0.28f, 0.18f, 0.60f)); Anchor(lb2, 0.5f, 0.38f, 440, 95); BindBtn(lb2, uiM, "OnHomeButton");
        uiM.loseScreen = loseScreen;

        // ── Invalid Move Popup ────────────────────────────────────────────────
        var invPop = MakeModal(canvasGO, "InvalidMovePopup", new Color(0.55f, 0.04f, 0.04f, 0.93f));
        invPop.GetComponent<RectTransform>().sizeDelta = new Vector2(780, 110);
        MakeTMP(invPop, "Msg", "Connect 2 or more same-color nodes!", 34, Color.white);
        Anchor(invPop.transform.Find("Msg").gameObject, 0.5f, 0.5f, 700, 90);
        uiM.invalidMovePopup = invPop;

        // ── Boost Popup ───────────────────────────────────────────────────────
        uiM.boostPopup = MakePowerupPopup(canvasGO, "BoostPopup",
            "SPECIAL BOOST", "Clears multiple nodes in area!",
            new Color(0.08f, 0.25f, 0.75f, 0.96f), uiM, "OnConfirmBoost", "HideBoostPopup");

        // ── Bomb Popup ────────────────────────────────────────────────────────
        uiM.bombPopup = MakePowerupPopup(canvasGO, "BombPopup",
            "x2 BOMB", "Destroys surrounding tiles!",
            new Color(0.50f, 0.04f, 0.04f, 0.96f), uiM, "OnConfirmBomb", "HideBombPopup");

        // ── Energy Popup ──────────────────────────────────────────────────────
        uiM.energyPopup = MakePowerupPopup(canvasGO, "EnergyPopup",
            "+5 ENERGY", "Adds 5 moves to continue!",
            new Color(0.04f, 0.32f, 0.08f, 0.96f), uiM, "OnEnergyButton", "HideEnergyPopup");

        // ── Settings Panel ────────────────────────────────────────────────────
        var sett = MakeModal(canvasGO, "SettingsPanel", new Color(0.06f, 0.02f, 0.18f, 0.97f));
        MakeTMP(sett, "Title", "SETTINGS", 64, Color.yellow);
        Anchor(sett.transform.Find("Title").gameObject, 0.5f, 0.82f, 500, 90);
        var sndBtn = MakeButton(sett, "SoundBtn", "Sound  ON / OFF", new Color(0.28f, 0.18f, 0.60f)); Anchor(sndBtn, 0.5f, 0.62f, 540, 95); BindBtn(sndBtn, uiM, "ToggleSound");
        var musBtn = MakeButton(sett, "MusicBtn", "Music   ON / OFF", new Color(0.28f, 0.18f, 0.60f)); Anchor(musBtn, 0.5f, 0.50f, 540, 95); BindBtn(musBtn, uiM, "ToggleSound");
        var clsBtn = MakeButton(sett, "CloseBtn", "CLOSE", new Color(0.48f, 0.08f, 0.08f)); Anchor(clsBtn, 0.5f, 0.30f, 420, 90); BindBtn(clsBtn, uiM, "ToggleSettings");
        uiM.settingsPanel = sett;

        // ── Mark scene dirty & save ───────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("✅ Link Burst scene built! Press ▶ Play now.");
        EditorUtility.DisplayDialog("Link Burst ✅",
            "Scene built successfully!\n\nNow press ▶ Play to run the game.", "Let's Play!");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════════════════════

    static GameObject MakeGO(string name, GameObject parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void StretchFull(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
    }

    static GameObject MakePanel(GameObject parent, string name, Color col)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = col;
        StretchFull(go);
        return go;
    }

    // Centred modal (600x700)
    static GameObject MakeModal(GameObject parent, string name, Color col)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(760, 720);
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    static GameObject MakeTMP(GameObject parent, string name, string text, int size, Color col)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var t   = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = size;
        t.color     = col;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        return go;
    }

    static GameObject MakeButton(GameObject parent, string name, string label, Color col)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = col;
        go.AddComponent<Button>();
        var txt = MakeTMP(go, "Label", label, 38, Color.white);
        StretchFull(txt);
        return go;
    }

    // anchorX/Y are normalised (0-1), size in pixels
    static void Anchor(GameObject go, float ax, float ay, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(ax, ay);
        rt.anchorMax        = new Vector2(ax, ay);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
    }

    static void BindBtn(GameObject btnGO, UIManager target, string method)
    {
        var btn = btnGO.GetComponent<Button>();
        if (btn == null) return;
        var action = System.Delegate.CreateDelegate(
            typeof(UnityAction), target, method) as UnityAction;
        if (action != null)
            UnityEventTools.AddVoidPersistentListener(btn.onClick, action);
    }

    static GameObject MakePowerupPopup(GameObject canvas, string name,
        string title, string desc, Color bg, UIManager uiM,
        string useMethod, string hideMethod)
    {
        var pop = MakeModal(canvas, name, bg);
        pop.GetComponent<RectTransform>().sizeDelta = new Vector2(760, 580);

        MakeTMP(pop, "Title", title, 64, Color.yellow);
        Anchor(pop.transform.Find("Title").gameObject, 0.5f, 0.82f, 660, 95);

        MakeTMP(pop, "Desc", desc, 36, Color.white);
        Anchor(pop.transform.Find("Desc").gameObject, 0.5f, 0.60f, 640, 80);

        var useBtn = MakeButton(pop, "UseBtn", "USE", new Color(0.08f, 0.65f, 0.18f));
        Anchor(useBtn, 0.5f, 0.40f, 380, 95);
        BindBtn(useBtn, uiM, useMethod);

        var closeBtn = MakeButton(pop, "CloseBtn", "✕", new Color(0.48f, 0.08f, 0.08f));
        Anchor(closeBtn, 0.92f, 0.92f, 75, 75);
        BindBtn(closeBtn, uiM, hideMethod);

        return pop;
    }

    static GameObject CreateNodePrefab(Color[] glowColors)
    {
        // Ensure folders exist
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Nodes"))
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Nodes");

        var nodeGO = new GameObject("NodePrefab");

        // Main sprite renderer
        var sr    = nodeGO.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircle();
        sr.color  = Color.white;
        sr.sortingOrder = 1;

        // NodeController component
        var nc = nodeGO.AddComponent<NodeController>();
        nc.glowColors = glowColors;

        // Glow child (additive blend)
        var glowGO = new GameObject("Glow");
        glowGO.transform.SetParent(nodeGO.transform, false);
        glowGO.transform.localScale = Vector3.one * 1.5f;
        var gsr       = glowGO.AddComponent<SpriteRenderer>();
        gsr.sprite    = GetCircle();
        gsr.color     = new Color(1f, 1f, 1f, 0.35f);
        gsr.sortingOrder = 0;
        var addMat    = new Material(Shader.Find("Sprites/Default"));
        gsr.sharedMaterial = addMat;

        // Save as prefab
        string prefabPath = "Assets/_Project/Prefabs/Nodes/NodePrefab.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(nodeGO, prefabPath);
        DestroyImmediate(nodeGO);
        return prefab;
    }

    static Sprite GetCircle()
        => Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
}
