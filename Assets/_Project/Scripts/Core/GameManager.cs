using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central game-state machine for Link Burst.
/// Orchestrates: board, scoring, energy, power-ups, and UI.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    // ── Game State ─────────────────────────────────────────────────────────────
    public enum GameState { Splash, MainMenu, LevelSelect, Gameplay, Paused, Win, Lose }

    public GameState CurrentState { get; private set; }
    public bool      IsAnimating  { get; private set; }

    // ── Level settings (defaults; overridden by LevelData) ─────────────────────
    [Header("Defaults (overridden by LevelData)")]
    public int defaultTargetScore  = 500;
    public int defaultStartEnergy  = 30;
    public int defaultGridWidth    = 6;
    public int defaultGridHeight   = 6;

    [Header("Level Data")]
    public LevelData[] levels;        // assign 10 LevelData assets in Inspector
    public int currentLevelIndex = 0;

    // ── Runtime ────────────────────────────────────────────────────────────────
    private int currentEnergy;
    private int targetScore;
    private LevelData activeLevel;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ChangeState(GameState.Splash);
    }

    // ── State machine ──────────────────────────────────────────────────────────

    public void ChangeState(GameState next)
    {
        CurrentState = next;

        // Let UI respond first
        if (UIManager.instance != null)
            UIManager.instance.OnStateChanged(next);

        switch (next)
        {
            case GameState.Gameplay: StartGameplay(); break;
        }
    }

    private void StartGameplay()
    {
        Time.timeScale = 1f;

        // Resolve active level data
        activeLevel = (levels != null && currentLevelIndex < levels.Length)
                      ? levels[currentLevelIndex]
                      : null;

        targetScore   = activeLevel != null ? activeLevel.targetScore   : defaultTargetScore;
        currentEnergy = activeLevel != null ? activeLevel.startingEnergy : defaultStartEnergy;

        int w = activeLevel != null ? activeLevel.gridWidth  : defaultGridWidth;
        int h = activeLevel != null ? activeLevel.gridHeight : defaultGridHeight;

        ScoreManager.instance.Reset();
        if (activeLevel != null)
            ScoreManager.instance.SetBonusBase(activeLevel.bonusBase);

        PowerUpManager.instance.ResetPowerUps(activeLevel);

        BoardManager.instance.ClearBoard();
        BoardManager.instance.InitializeBoard(w, h);

        UIManager.instance.UpdateEnergy(currentEnergy, currentEnergy);
        UIManager.instance.UpdateScore(0, 0, 1);
        UIManager.instance.UpdateTargetScore(targetScore);
    }

    // ── Chain execution ────────────────────────────────────────────────────────

    public void ExecuteChain(List<NodeController> chain)
    {
        if (IsAnimating) return;
        StartCoroutine(ProcessChain(chain));
    }

    private IEnumerator ProcessChain(List<NodeController> chain)
    {
        IsAnimating = true;

        // Consume 1 energy
        currentEnergy--;
        UIManager.instance.UpdateEnergy(currentEnergy,
            activeLevel != null ? activeLevel.startingEnergy : defaultStartEnergy);

        // Score
        ScoreManager.instance.AddChainScore(chain.Count);

        // Sound
        AudioManager.instance.PlaySound("explosion");

        // Board update
        yield return BoardManager.instance.RemoveNodesAndFill(chain);

        IsAnimating = false;

        CheckGameEnd();
    }

    private void CheckGameEnd()
    {
        if (ScoreManager.instance.GetScore() >= targetScore)
            ChangeState(GameState.Win);
        else if (currentEnergy <= 0)
            ChangeState(GameState.Lose);
    }

    // ── Public actions (called by UI buttons / power-ups) ──────────────────────

    public void AddEnergy(int amount)
    {
        int max = activeLevel != null ? activeLevel.startingEnergy + 10 : defaultStartEnergy + 10;
        currentEnergy = Mathf.Min(currentEnergy + amount, max);
        UIManager.instance.UpdateEnergy(currentEnergy, max);
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Gameplay) return;
        CurrentState = GameState.Paused;
        Time.timeScale = 0f;
        UIManager.instance.OnStateChanged(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        CurrentState = GameState.Gameplay;
        Time.timeScale = 1f;
        UIManager.instance.OnStateChanged(GameState.Gameplay);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        ChangeState(GameState.Gameplay);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (InputManager.instance != null) InputManager.instance.CancelChain();
        BoardManager.instance.ClearBoard();
        ChangeState(GameState.MainMenu);
    }

    public void GoToNextLevel()
    {
        currentLevelIndex = (levels != null)
            ? (currentLevelIndex + 1) % levels.Length
            : 0;
        ChangeState(GameState.Gameplay);
    }

    public void SelectLevel(int index)
    {
        currentLevelIndex = index;
        ChangeState(GameState.Gameplay);
    }

    // ── Accessors ──────────────────────────────────────────────────────────────
    public int GetTargetScore()   => targetScore;
    public int GetCurrentEnergy() => currentEnergy;
    public int GetCurrentLevel()  => currentLevelIndex + 1;
}
