using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;

/// <summary>
/// Builds the complete Link Burst scene with one menu click.
/// Menu: LinkBurst -> Build Complete Scene
/// </summary>
public class LinkBurstSceneBuilder : Editor
{
    [MenuItem("LinkBurst/Build Complete Scene")]
    public static void BuildScene()
    {
        // Clear existing objects
        foreach (var go in FindObjectsOfType<GameObject>())
        {
            if (go.transform.parent == null && go.name != "Main Camera")
                DestroyImmediate(go);
        }

        // ── Camera ────────────────────────────────────────────────────────────
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            cam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.11f);
        cam.transform.position = new Vector3(0, 0, -10);

        // ── Managers root ─────────────────────────────────────────────────────
        var managers = new GameObject("[Managers]");

        var gmGO = CreateChild(managers, "GameManager");
        var gm   = gmGO.AddComponent<GameManager>();

        var bmGO = CreateChild(managers, "BoardManager");
        var bm   = bmGO.AddComponent<BoardManager>();

        var imGO = CreateChild(managers, "InputManager");
        var im   = imGO.AddComponent<InputManager>();

        var smGO = CreateChild(managers, "ScoreManager");
        smGO.AddComponent<ScoreManager>();

        var pmGO = CreateChild(managers, "PowerUpManager");
        pmGO.AddComponent<PowerUpManager>();

        var amGO = CreateChild(managers, "AudioManager");
        amGO.AddComponent<AudioManager>();

        var uiMgrGO = CreateChild(managers, "UIManager");
        var uiMgr   = uiMgrGO.AddComponent<UIManager>();

        // ── Board root ────────────────────────────────────────────────────────
        var board = new GameObject("Board");
        bm.boardParent = board.transform;

        // ── Node prefab (built-in white circle sprite) ────────────────────────
        var nodePrefab = BuildNodePrefab();
        bm.nodePrefab = nodePrefab;

        // ── Node colors ───────────────────────────────────────────────────────
        bm.nodeGlowColors = new Color[]
        {
            new Color(1.00f, 0.23f, 0.23f),   // Red
            new Color(0.23f, 0.56f, 1.00f),   // Blue
            new Color(0.23f, 1.00f, 0.48f),   // Green
            new Color(1.00f, 0.88f, 0.23f),   // Yellow
            new Color(0.69f, 0.23f, 1.00f),   // Purple
            new Color(1.00f, 0.55f, 0.23f),   // Orange
        };

        // ── Chain LineRenderer ────────────────────────────────────────────────
        var lineGO = new GameObject("ChainLine");
        var lr     = lineGO.AddComponent<LineRenderer>();
        lr.startWidth     = 0.08f;
        lr.endWidth       = 0.08f;
        lr.positionCount  = 0;
        lr.material       = new Material(Shader.Find("Sprites/Default"));
        lr.startColor     = Color.white;
        lr.endColor       = Color.white;
        lr.sortingOrder   = 10;
        im.chainLine = lr;

        // ── UI Canvas ─────────────────────────────────────────────────────────
        var canvasGO  = new GameObject("UI Canvas");
        var canvas    = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Background ────────────────────────────────────────────────────────
        var bg = CreateImage(canvasGO, "Background", new Color(0.04f, 0.04f, 0.11f));
        bg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
        bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        // ── Splash Screen ─────────────────────────────────────────────────────
        var splash = CreatePanel(canvasGO, "SplashScreen", new Color(0.06f, 0.02f, 0.18f));
        var splashCG = splash.AddComponent<CanvasGroup>();
        var logoTxt  = CreateTMP(splash, "LogoText", "LINK BURST", 80, Color.white);
        SetAnchored(logoTxt, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(900, 160));
        var loadingTxt = CreateTMP(splash, "LoadingText", "loading...", 36, Color.white);
        SetAnchored(loadingTxt, new Vector2(0.5f, 0.3f), Vector2.zero, new Vector2(600, 80));
        var splashComp = splash.AddComponent<SplashScreen>();
        splashComp.logoGroup = splashCG;
        uiMgr.splashScreen = splash;

        // ── Main Menu ─────────────────────────────────────────────────────────
        var menu = CreatePanel(canvasGO, "MainMenuScreen", new Color(0.06f, 0.02f, 0.18f, 0f));
        var menuTitle = CreateTMP(menu, "Title", "LINK BURST", 90, Color.yellow);
        SetAnchored(menuTitle, new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(900, 160));

        var playBtn = CreateButton(menu, "PlayButton", "PLAY", new Color(0.8f, 0.4f, 0.1f));
        SetAnchored(playBtn, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(500, 100));
        AddButtonCallback(playBtn, uiMgrGO, "OnPlayButton");

        var lvlBtn = CreateButton(menu, "LevelSelectButton", "LEVEL SELECT", new Color(0.3f, 0.2f, 0.6f));
        SetAnchored(lvlBtn, new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(500, 100));
        AddButtonCallback(lvlBtn, uiMgrGO, "OnLevelSelectButton");

        var settBtn = CreateButton(menu, "SettingsButton", "SETTINGS", new Color(0.3f, 0.2f, 0.6f));
        SetAnchored(settBtn, new Vector2(0.5f, 0.29f), Vector2.zero, new Vector2(500, 100));
        AddButtonCallback(settBtn, uiMgrGO, "ToggleSettings");

        uiMgr.mainMenuScreen = menu;

        // ── Level Select ──────────────────────────────────────────────────────
        var lvlSel = CreatePanel(canvasGO, "LevelSelectScreen", new Color(0.06f, 0.02f, 0.18f, 0f));
        var lvlTitle = CreateTMP(lvlSel, "Title", "SELECT LEVEL", 60, Color.yellow);
        SetAnchored(lvlTitle, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(800, 100));
        var lvlUI = lvlSel.AddComponent<LevelSelectUI>();
        // Button container for level buttons
        var btnContainer = new GameObject("ButtonContainer");
        btnContainer.transform.SetParent(lvlSel.transform, false);
        var grid = btnContainer.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(180, 180);
        grid.spacing         = new Vector2(20, 20);
        grid.padding         = new RectOffset(40, 40, 40, 40);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        var btnContainerRT = btnContainer.GetComponent<RectTransform>();
        btnContainerRT.anchorMin = new Vector2(0, 0.1f);
        btnContainerRT.anchorMax = new Vector2(1, 0.85f);
        btnContainerRT.sizeDelta = Vector2.zero;
        lvlUI.buttonContainer = btnContainer.transform;
        uiMgr.levelSelectScreen = lvlSel;

        // ── Gameplay Screen ───────────────────────────────────────────────────
        var gameplay = CreatePanel(canvasGO, "GameplayScreen", Color.clear);
        gameplay.GetComponent<Image>().enabled = false;

        // HUD top bar
        var scoreLbl = CreateTMP(gameplay, "ScoreText", "0", 54, Color.white);
        SetAnchored(scoreLbl, new Vector2(0.5f, 0.93f), Vector2.zero, new Vector2(400, 80));
        uiMgr.scoreText = scoreLbl.GetComponent<TextMeshProUGUI>();

        var targetLbl = CreateTMP(gameplay, "TargetScoreText", "Goal: 500", 32, new Color(1,1,0.5f));
        SetAnchored(targetLbl, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(400, 60));
        uiMgr.targetScoreText = targetLbl.GetComponent<TextMeshProUGUI>();

        var energyLbl = CreateTMP(gameplay, "EnergyText", "30/30", 36, Color.cyan);
        SetAnchored(energyLbl, new Vector2(0.15f, 0.93f), Vector2.zero, new Vector2(200, 60));
        uiMgr.energyText = energyLbl.GetComponent<TextMeshProUGUI>();

        var levelLbl = CreateTMP(gameplay, "LevelText", "Level 1", 36, Color.white);
        SetAnchored(levelLbl, new Vector2(0.85f, 0.93f), Vector2.zero, new Vector2(200, 60));
        uiMgr.levelText = levelLbl.GetComponent<TextMeshProUGUI>();

        // Combo floating text
        var comboLbl = CreateTMP(gameplay, "ComboText", "+50", 56, Color.yellow);
        SetAnchored(comboLbl, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(400, 80));
        uiMgr.comboText = comboLbl.GetComponent<TextMeshProUGUI>();

        var comboMulti = CreateTMP(gameplay, "ComboMultiText", "x2 COMBO!", 44, new Color(1,0.5f,0));
        SetAnchored(comboMulti, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(400, 70));
        uiMgr.comboMultiText = comboMulti.GetComponent<TextMeshProUGUI>();

        // Power-up buttons
        var boostBtn = CreateButton(gameplay, "BoostButton", "BOOST", new Color(0.2f, 0.6f, 1f));
        SetAnchored(boostBtn, new Vector2(0.18f, 0.08f), Vector2.zero, new Vector2(200, 80));
        AddButtonCallback(boostBtn, uiMgrGO, "OnBoostButton");
        var boostCount = CreateTMP(boostBtn, "BoostCount", "3", 28, Color.white);
        uiMgr.boostCountText = boostCount.GetComponent<TextMeshProUGUI>();

        var bombBtn = CreateButton(gameplay, "BombButton", "BOMB", new Color(0.9f, 0.2f, 0.1f));
        SetAnchored(bombBtn, new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(200, 80));
        AddButtonCallback(bombBtn, uiMgrGO, "OnBombButton");
        var bombCount = CreateTMP(bombBtn, "BombCount", "3", 28, Color.white);
        uiMgr.bombCountText = bombCount.GetComponent<TextMeshProUGUI>();

        var energyBtn = CreateButton(gameplay, "EnergyButton", "+5 NRG", new Color(0.1f, 0.7f, 0.2f));
        SetAnchored(energyBtn, new Vector2(0.82f, 0.08f), Vector2.zero, new Vector2(200, 80));
        AddButtonCallback(energyBtn, uiMgrGO, "OnEnergyButton");
        var energyCount = CreateTMP(energyBtn, "EnergyCount", "3", 28, Color.white);
        uiMgr.energyCountText = energyCount.GetComponent<TextMeshProUGUI>();

        // Pause button
        var pauseBtn = CreateButton(gameplay, "PauseButton", "II", new Color(0.3f, 0.3f, 0.3f));
        SetAnchored(pauseBtn, new Vector2(0.05f, 0.94f), Vector2.zero, new Vector2(80, 80));
        AddButtonCallback(pauseBtn, uiMgrGO, "OnPauseButton");

        uiMgr.gameplayScreen = gameplay;

        // ── Pause Menu ────────────────────────────────────────────────────────
        var pauseMenu = CreatePanel(canvasGO, "PauseMenu", new Color(0.05f, 0.02f, 0.15f, 0.95f));
        CreateTMP(pauseMenu, "Title", "PAUSED", 70, Color.yellow);
        SetAnchored(pauseMenu.transform.Find("Title").gameObject, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(600, 100));
        var resumeBtn = CreateButton(pauseMenu, "ResumeButton", "RESUME", new Color(0.1f, 0.7f, 0.2f));
        SetAnchored(resumeBtn, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(450, 90));
        AddButtonCallback(resumeBtn, uiMgrGO, "OnResumeButton");
        var restartBtn = CreateButton(pauseMenu, "RestartButton", "RESTART", new Color(0.8f, 0.4f, 0.1f));
        SetAnchored(restartBtn, new Vector2(0.5f, 0.43f), Vector2.zero, new Vector2(450, 90));
        AddButtonCallback(restartBtn, uiMgrGO, "OnRestartButton");
        var homeBtn1 = CreateButton(pauseMenu, "HomeButton", "HOME", new Color(0.5f, 0.1f, 0.1f));
        SetAnchored(homeBtn1, new Vector2(0.5f, 0.31f), Vector2.zero, new Vector2(450, 90));
        AddButtonCallback(homeBtn1, uiMgrGO, "OnHomeButton");
        uiMgr.pauseMenu = pauseMenu;

        // ── Win Screen ────────────────────────────────────────────────────────
        var winScreen = CreatePanel(canvasGO, "WinScreen", new Color(0.02f, 0.1f, 0.05f, 0.95f));
        CreateTMP(winScreen, "Title", "LEVEL COMPLETE!", 64, Color.yellow);
        SetAnchored(winScreen.transform.Find("Title").gameObject, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(800, 100));
        var winScore = CreateTMP(winScreen, "WinScoreText", "0", 72, Color.white);
        SetAnchored(winScore, new Vector2(0.5f, 0.60f), Vector2.zero, new Vector2(500, 100));
        uiMgr.winScoreText = winScore.GetComponent<TextMeshProUGUI>();
        var winHS = CreateTMP(winScreen, "WinHighScore", "Best: 0", 36, new Color(1,1,0.5f));
        SetAnchored(winHS, new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(400, 60));
        uiMgr.winHighScoreText = winHS.GetComponent<TextMeshProUGUI>();
        var nextBtn = CreateButton(winScreen, "NextButton", "NEXT", new Color(0.1f, 0.7f, 0.2f));
        SetAnchored(nextBtn, new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(400, 90));
        AddButtonCallback(nextBtn, uiMgrGO, "OnNextLevelButton");
        var homeBtn2 = CreateButton(winScreen, "HomeButton", "HOME", new Color(0.3f, 0.2f, 0.6f));
        SetAnchored(homeBtn2, new Vector2(0.5f, 0.26f), Vector2.zero, new Vector2(400, 90));
        AddButtonCallback(homeBtn2, uiMgrGO, "OnHomeButton");
        uiMgr.winScreen = winScreen;

        // ── Lose Screen ───────────────────────────────────────────────────────
        var loseScreen = CreatePanel(canvasGO, "LoseScreen", new Color(0.15f, 0.02f, 0.02f, 0.95f));
        CreateTMP(loseScreen, "Title", "GAME OVER", 72, Color.red);
        SetAnchored(loseScreen.transform.Find("Title").gameObject, new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(700, 100));
        var retryBtn = CreateButton(loseScreen, "RetryButton", "RETRY", new Color(0.8f, 0.4f, 0.1f));
        SetAnchored(retryBtn, new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(420, 90));
        AddButtonCallback(retryBtn, uiMgrGO, "OnRestartButton");
        var homeBtn3 = CreateButton(loseScreen, "HomeButton", "HOME", new Color(0.3f, 0.2f, 0.6f));
        SetAnchored(homeBtn3, new Vector2(0.5f, 0.36f), Vector2.zero, new Vector2(420, 90));
        AddButtonCallback(homeBtn3, uiMgrGO, "OnHomeButton");
        uiMgr.loseScreen = loseScreen;

        // ── Invalid Move Popup ────────────────────────────────────────────────
        var invalidPopup = CreatePanel(canvasGO, "InvalidMovePopup", new Color(0.6f, 0.05f, 0.05f, 0.92f));
        SetAnchored(invalidPopup, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 100));
        CreateTMP(invalidPopup, "Msg", "Need 2+ connected same-color nodes!", 32, Color.white);
        uiMgr.invalidMovePopup = invalidPopup;

        // ── Boost Popup ───────────────────────────────────────────────────────
        var boostPopup = BuildPowerupPopup(canvasGO, "BoostPopup", "SPECIAL BOOST",
            "Clears multiple nodes in area!", new Color(0.1f, 0.3f, 0.8f, 0.95f), uiMgrGO, "OnConfirmBoost", "HideBoostPopup");
        uiMgr.boostPopup = boostPopup;

        // ── Bomb Popup ────────────────────────────────────────────────────────
        var bombPopup = BuildPowerupPopup(canvasGO, "BombPopup", "x2 BOMB",
            "Destroys surrounding tiles!", new Color(0.5f, 0.05f, 0.05f, 0.95f), uiMgrGO, "OnConfirmBomb", "HideBombPopup");
        uiMgr.bombPopup = bombPopup;

        // ── Energy Popup ──────────────────────────────────────────────────────
        var energyPopup = BuildPowerupPopup(canvasGO, "EnergyPopup", "+5 ENERGY",
            "Adds extra energy to boost chain reactions!", new Color(0.05f, 0.35f, 0.1f, 0.95f), uiMgrGO, "OnEnergyButton", "HideEnergyPopup");
        uiMgr.energyPopup = energyPopup;

        // ── Settings Panel ────────────────────────────────────────────────────
        var settings = CreatePanel(canvasGO, "SettingsPanel", new Color(0.06f, 0.02f, 0.18f, 0.97f));
        SetAnchored(settings, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 500));
        CreateTMP(settings, "Title", "SETTINGS", 60, Color.yellow);
        SetAnchored(settings.transform.Find("Title").gameObject, new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(500, 80));
        var soundBtn = CreateButton(settings, "SoundButton", "Sound: ON/OFF", new Color(0.3f, 0.2f, 0.6f));
        SetAnchored(soundBtn, new Vector2(0.5f, 0.60f), Vector2.zero, new Vector2(500, 90));
        AddButtonCallback(soundBtn, uiMgrGO, "ToggleSound");
        var closeSettBtn = CreateButton(settings, "CloseButton", "CLOSE", new Color(0.5f, 0.1f, 0.1f));
        SetAnchored(closeSettBtn, new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(400, 80));
        AddButtonCallback(closeSettBtn, uiMgrGO, "ToggleSettings");
        uiMgr.settingsPanel = settings;

        // ── Default level data on GameManager ────────────────────────────────
        gm.defaultTargetScore = 500;
        gm.defaultStartEnergy = 30;
        gm.defaultGridWidth   = 6;
        gm.defaultGridHeight  = 6;

        // ── Save scene ────────────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("✅ Link Burst scene built! Press Play to run the game.");
        EditorUtility.DisplayDialog("Link Burst", "Scene built successfully!\n\nPress ▶ Play to run the game.", "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static GameObject CreateChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static GameObject CreatePanel(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    static Image CreateImage(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static GameObject CreateTMP(GameObject parent, string name, string text, int size, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        return go;
    }

    static GameObject CreateButton(GameObject parent, string name, string label, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        go.AddComponent<Button>();
        var txt = CreateTMP(go, "Label", label, 36, Color.white);
        var rt  = txt.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        return go;
    }

    static void SetAnchored(GameObject go, Vector2 anchorPos, Vector2 pivot, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchorPos;
        rt.anchorMax        = anchorPos;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = size;
    }

    static void AddButtonCallback(GameObject btnGO, GameObject target, string methodName)
    {
        var btn = btnGO.GetComponent<Button>();
        if (btn == null) return;
        var evt = new UnityEngine.Events.UnityAction(
            System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction),
                target.GetComponent<UIManager>(), methodName) as UnityEngine.Events.UnityAction);
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btn.onClick, evt);
    }

    static GameObject BuildPowerupPopup(GameObject canvas, string name, string title,
        string desc, Color bg, GameObject uiMgrGO, string useMethod, string closeMethod)
    {
        var popup = CreatePanel(canvas, name, bg);
        SetAnchored(popup, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 550));
        popup.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        popup.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);

        CreateTMP(popup, "Title", title, 60, Color.yellow);
        SetAnchored(popup.transform.Find("Title").gameObject, new Vector2(0.5f, 0.80f), Vector2.zero, new Vector2(600, 90));

        CreateTMP(popup, "Desc", desc, 34, Color.white);
        SetAnchored(popup.transform.Find("Desc").gameObject, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(600, 80));

        var useBtn = CreateButton(popup, "UseButton", "USE", new Color(0.1f, 0.65f, 0.2f));
        SetAnchored(useBtn, new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(350, 90));
        AddButtonCallback(useBtn, uiMgrGO, useMethod);

        var closeBtn = CreateButton(popup, "CloseButton", "X", new Color(0.5f, 0.1f, 0.1f));
        SetAnchored(closeBtn, new Vector2(0.93f, 0.93f), Vector2.zero, new Vector2(70, 70));
        AddButtonCallback(closeBtn, uiMgrGO, closeMethod);

        return popup;
    }

    static GameObject BuildNodePrefab()
    {
        var nodeGO = new GameObject("NodePrefab");
        var sr     = nodeGO.AddComponent<SpriteRenderer>();
        sr.sprite  = GetCircleSprite();
        sr.color   = Color.white;

        var nc = nodeGO.AddComponent<NodeController>();

        // Glow child
        var glowGO = new GameObject("Glow");
        glowGO.transform.SetParent(nodeGO.transform, false);
        glowGO.transform.localScale = Vector3.one * 1.45f;
        var glowSR = glowGO.AddComponent<SpriteRenderer>();
        glowSR.sprite       = GetCircleSprite();
        glowSR.color        = new Color(1, 1, 1, 0.35f);
        glowSR.sortingOrder = -1;
        // Additive blend
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        glowSR.sharedMaterial = mat;

        // Colors for 6 node types
        nc.glowColors = new Color[]
        {
            new Color(1.00f, 0.23f, 0.23f),
            new Color(0.23f, 0.56f, 1.00f),
            new Color(0.23f, 1.00f, 0.48f),
            new Color(1.00f, 0.88f, 0.23f),
            new Color(0.69f, 0.23f, 1.00f),
            new Color(1.00f, 0.55f, 0.23f),
        };

        // Save prefab
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Nodes"))
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Nodes");

        string path = "Assets/_Project/Prefabs/Nodes/NodePrefab.prefab";
        var prefab  = PrefabUtility.SaveAsPrefabAsset(nodeGO, path);
        DestroyImmediate(nodeGO);
        return prefab;
    }

    static Sprite GetCircleSprite()
    {
        // Use Unity's built-in Knob texture as a circle
        return Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
    }
}
