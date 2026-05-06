using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    // ── State ──────────────────────────────────────────────────────────────────
    private int currentScore;
    private int comboMultiplier = 1;
    private int consecutiveChains;
    private int bonusBase = 5;   // overridden by LevelData

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void SetBonusBase(int b) => bonusBase = b;

    // Returns score for a chain of given length.
    // Formula: n*(n+bonusBase) * comboMultiplier
    // Example (bonusBase=5): 5 nodes → 50 pts, 10 nodes → 150 pts
    public int CalculateChainScore(int chainLength)
        => chainLength * (chainLength + bonusBase) * comboMultiplier;

    public int AddChainScore(int chainLength)
    {
        int gained = CalculateChainScore(chainLength);
        currentScore += gained;

        consecutiveChains++;
        comboMultiplier = consecutiveChains >= 6 ? 3
                        : consecutiveChains >= 3 ? 2
                        : 1;

        int hs = PlayerPrefs.GetInt("LB_HighScore", 0);
        if (currentScore > hs)
            PlayerPrefs.SetInt("LB_HighScore", currentScore);

        UIManager.instance.UpdateScore(currentScore, gained, comboMultiplier);
        return gained;
    }

    public void ResetCombo()
    {
        consecutiveChains = 0;
        comboMultiplier   = 1;
    }

    public void Reset()
    {
        currentScore      = 0;
        comboMultiplier   = 1;
        consecutiveChains = 0;
    }

    public int GetScore()       => currentScore;
    public int GetCombo()       => comboMultiplier;
    public int GetHighScore()   => PlayerPrefs.GetInt("LB_HighScore", 0);

    // Star rating: 1 = reached target, 2 = 120%, 3 = 150%
    public int GetStars(int targetScore)
    {
        if (currentScore >= targetScore * 1.5f) return 3;
        if (currentScore >= targetScore * 1.2f) return 2;
        if (currentScore >= targetScore)        return 1;
        return 0;
    }
}
