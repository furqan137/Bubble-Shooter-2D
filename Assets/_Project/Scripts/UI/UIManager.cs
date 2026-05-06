using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all UI screens and popups for Link Burst.
/// Screens: Splash → MainMenu → (LevelSelect) → Gameplay ↔ Paused → Win / Lose
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    // ── Screens ────────────────────────────────────────────────────────────────
    [Header("Screens")]
    public GameObject splashScreen;
    public GameObject mainMenuScreen;
    public GameObject levelSelectScreen;
    public GameObject gameplayScreen;

    // ── Gameplay HUD ───────────────────────────────────────────────────────────
    [Header("Gameplay HUD")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI targetScoreText;
    public TextMeshProUGUI energyText;
    public Slider          energySlider;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI comboText;        // floating pop-up "+150"
    public TextMeshProUGUI comboMultiText;   // "x2" banner

    // ── Power-up HUD ───────────────────────────────────────────────────────────
    [Header("Power-up Buttons (count labels)")]
    public TextMeshProUGUI boostCountText;
    public TextMeshProUGUI bombCountText;
    public TextMeshProUGUI energyCountText;

    // ── Popups ─────────────────────────────────────────────────────────────────
    [Header("Popups")]
    public GameObject pauseMenu;
    public GameObject winScreen;
    public GameObject loseScreen;
    public GameObject invalidMovePopup;
    public GameObject boostPopup;
    public GameObject bombPopup;
    public GameObject energyPopup;
    public GameObject settingsPanel;

    // ── Win Screen ─────────────────────────────────────────────────────────────
    [Header("Win Screen")]
    public TextMeshProUGUI winScoreText;
    public TextMeshProUGUI winHighScoreText;
    public Image[]         starImages;
    public Sprite          starFilled;
    public Sprite          starEmpty;

    // ── Settings ───────────────────────────────────────────────────────────────
    [Header("Settings")]
    public Image soundToggleIcon;
    public Image musicToggleIcon;

    // ── Private ────────────────────────────────────────────────────────────────
    private Coroutine invalidMoveRoutine;
    private Coroutine comboRoutine;
    private Vector3   comboTextOrigin;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        if (comboText != null)
        {
            comboTextOrigin = comboText.rectTransform.anchoredPosition;
            comboText.gameObject.SetActive(false);
        }
        if (comboMultiText != null)
            comboMultiText.gameObject.SetActive(false);
    }

    // ── State handler ──────────────────────────────────────────────────────────

    public void OnStateChanged(GameManager.GameState state)
    {
        HideAllScreens();
        HideAllPopups();

        switch (state)
        {
            case GameManager.GameState.Splash:
                Show(splashScreen);
                StartCoroutine(SplashCountdown());
                break;

            case GameManager.GameState.MainMenu:
                Show(mainMenuScreen);
                break;

            case GameManager.GameState.LevelSelect:
                Show(levelSelectScreen);
                break;

            case GameManager.GameState.Gameplay:
                Show(gameplayScreen);
                if (levelText != null)
                    levelText.text = $"Level {GameManager.instance.GetCurrentLevel()}";
                break;

            case GameManager.GameState.Paused:
                Show(gameplayScreen);
                Show(pauseMenu);
                break;

            case GameManager.GameState.Win:
                Show(gameplayScreen);
                PopulateWinScreen();
                Show(winScreen);
                break;

            case GameManager.GameState.Lose:
                Show(gameplayScreen);
                Show(loseScreen);
                break;
        }
    }

    // ── HUD updates ────────────────────────────────────────────────────────────

    public void UpdateScore(int total, int gained, int combo)
    {
        if (scoreText != null) scoreText.text = total.ToString("N0");

        if (gained > 0)
        {
            ShowFloatingScore(gained, combo);
            ShowComboMultiplier(combo);
        }
    }

    public void UpdateTargetScore(int target)
    {
        if (targetScoreText != null)
            targetScoreText.text = $"Goal: {target:N0}";
    }

    public void UpdateEnergy(int current, int max)
    {
        if (energyText   != null) energyText.text = $"{current}/{max}";
        if (energySlider != null) energySlider.value = max > 0 ? (float)current / max : 0f;
    }

    public void UpdatePowerUpCounts(int boost, int bomb, int energy)
    {
        if (boostCountText  != null) boostCountText.text  = boost.ToString();
        if (bombCountText   != null) bombCountText.text   = bomb.ToString();
        if (energyCountText != null) energyCountText.text = energy.ToString();
    }

    // ── Popups ─────────────────────────────────────────────────────────────────

    public void ShowInvalidMovePopup()
    {
        if (invalidMoveRoutine != null) StopCoroutine(invalidMoveRoutine);
        invalidMoveRoutine = StartCoroutine(InvalidMoveRoutine());
    }

    private IEnumerator InvalidMoveRoutine()
    {
        if (invalidMovePopup == null) yield break;
        invalidMovePopup.SetActive(true);

        Vector3 origin = invalidMovePopup.transform.localPosition;
        float elapsed  = 0f, duration = 0.45f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Mathf.Sin(elapsed * 48f) * 9f * (1f - elapsed / duration);
            invalidMovePopup.transform.localPosition = origin + Vector3.right * x;
            yield return null;
        }
        invalidMovePopup.transform.localPosition = origin;
        invalidMovePopup.SetActive(false);
    }

    public void ShowBoostPopup()   { Show(boostPopup);  }
    public void ShowBombPopup()    { Show(bombPopup);   }
    public void ShowEnergyPopup()  { Show(energyPopup); }
    public void HideBoostPopup()   { Hide(boostPopup);  }
    public void HideBombPopup()    { Hide(bombPopup);   }
    public void HideEnergyPopup()  { Hide(energyPopup); }

    public void ToggleSettings()
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(!settingsPanel.activeSelf);
        RefreshAudioIcons();
    }

    public void ToggleSound()
    {
        AudioManager.instance.ToggleMute();
        RefreshAudioIcons();
    }

    // ── Button callbacks ────────────────────────────────────────────────────────

    public void OnPlayButton()       => GameManager.instance.ChangeState(GameManager.GameState.Gameplay);
    public void OnLevelSelectButton()=> GameManager.instance.ChangeState(GameManager.GameState.LevelSelect);
    public void OnPauseButton()      => GameManager.instance.PauseGame();
    public void OnResumeButton()     => GameManager.instance.ResumeGame();
    public void OnRestartButton()    => GameManager.instance.RestartGame();
    public void OnNextLevelButton()  => GameManager.instance.GoToNextLevel();
    public void OnHomeButton()       => GameManager.instance.GoToMainMenu();

    // Power-ups
    public void OnBoostButton()
    {
        Show(boostPopup);
    }
    public void OnConfirmBoost()
    {
        Hide(boostPopup);
        PowerUpManager.instance.UseBoost();
    }
    public void OnBombButton()
    {
        Show(bombPopup);
    }
    public void OnConfirmBomb()
    {
        Hide(bombPopup);
        PowerUpManager.instance.UseBomb();
    }
    public void OnEnergyButton()
    {
        PowerUpManager.instance.UseEnergy();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private IEnumerator SplashCountdown()
    {
        yield return new WaitForSeconds(2.5f);
        GameManager.instance.ChangeState(GameManager.GameState.MainMenu);
    }

    private void PopulateWinScreen()
    {
        int score  = ScoreManager.instance.GetScore();
        int target = GameManager.instance.GetTargetScore();
        int stars  = ScoreManager.instance.GetStars(target);

        if (winScoreText     != null) winScoreText.text     = score.ToString("N0");
        if (winHighScoreText != null) winHighScoreText.text = $"Best: {ScoreManager.instance.GetHighScore():N0}";

        if (starImages != null)
            for (int i = 0; i < starImages.Length; i++)
                starImages[i].sprite = i < stars ? starFilled : starEmpty;
    }

    private void ShowFloatingScore(int gained, int combo)
    {
        if (comboText == null) return;
        if (comboRoutine != null) StopCoroutine(comboRoutine);
        comboRoutine = StartCoroutine(FloatingScoreRoutine(gained, combo));
    }

    private IEnumerator FloatingScoreRoutine(int gained, int combo)
    {
        comboText.gameObject.SetActive(true);
        comboText.text = combo > 1 ? $"+{gained}  x{combo}!" : $"+{gained}";
        comboText.rectTransform.anchoredPosition = comboTextOrigin;

        Color start = comboText.color;
        start.a = 1f;
        comboText.color = start;

        float t = 0f, dur = 1.1f;
        while (t < dur)
        {
            t += Time.deltaTime;
            comboText.rectTransform.anchoredPosition =
                comboTextOrigin + Vector3.up * (t / dur) * 70f;
            Color c = comboText.color;
            c.a = 1f - t / dur;
            comboText.color = c;
            yield return null;
        }
        comboText.gameObject.SetActive(false);
    }

    private void ShowComboMultiplier(int combo)
    {
        if (comboMultiText == null) return;
        if (combo > 1)
        {
            comboMultiText.gameObject.SetActive(true);
            comboMultiText.text = $"x{combo} COMBO!";
        }
        else
        {
            comboMultiText.gameObject.SetActive(false);
        }
    }

    private void RefreshAudioIcons()
    {
        bool muted = AudioManager.instance.mute;
        if (soundToggleIcon != null)
            soundToggleIcon.color = muted ? new Color(1,1,1,0.35f) : Color.white;
        if (musicToggleIcon != null)
            musicToggleIcon.color = muted ? new Color(1,1,1,0.35f) : Color.white;
    }

    private void HideAllScreens()
    {
        Hide(splashScreen);
        Hide(mainMenuScreen);
        Hide(levelSelectScreen);
        Hide(gameplayScreen);
    }

    private void HideAllPopups()
    {
        Hide(pauseMenu);
        Hide(winScreen);
        Hide(loseScreen);
        Hide(invalidMovePopup);
        Hide(boostPopup);
        Hide(bombPopup);
        Hide(energyPopup);
        Hide(settingsPanel);
    }

    private static void Show(GameObject go) { if (go) go.SetActive(true);  }
    private static void Hide(GameObject go) { if (go) go.SetActive(false); }
}
