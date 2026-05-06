using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the three Link Burst power-ups: Boost (area clear), Bomb (3x3 clear), Energy (+5 moves).
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager instance;

    // ── Runtime counts ─────────────────────────────────────────────────────────
    [Header("Default Counts (overridden by LevelData)")]
    public int boostCount  = 3;
    public int bombCount   = 3;
    public int energyCount = 3;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void ResetPowerUps(LevelData data)
    {
        boostCount  = data != null ? data.boostCount  : 3;
        bombCount   = data != null ? data.bombCount   : 3;
        energyCount = data != null ? data.energyCount : 3;
        BroadcastCounts();
    }

    // Boost: clears a 3×3 area centred on the board middle.
    public void UseBoost()
    {
        if (boostCount <= 0) return;
        if (GameManager.instance.IsAnimating) return;

        boostCount--;
        BroadcastCounts();
        StartCoroutine(ApplyAreaClear(
            BoardManager.instance.gridWidth  / 2,
            BoardManager.instance.gridHeight / 2,
            1));   // radius 1 → 3×3
    }

    // Bomb: clears a 3×3 area at a random non-edge position.
    public void UseBomb()
    {
        if (bombCount <= 0) return;
        if (GameManager.instance.IsAnimating) return;

        bombCount--;
        BroadcastCounts();
        int rx = Random.Range(1, BoardManager.instance.gridWidth  - 1);
        int ry = Random.Range(1, BoardManager.instance.gridHeight - 1);
        StartCoroutine(ApplyAreaClear(rx, ry, 1));
    }

    // Energy: adds 5 energy to the active game.
    public void UseEnergy()
    {
        if (energyCount <= 0) return;

        energyCount--;
        BroadcastCounts();
        GameManager.instance.AddEnergy(5);
        AudioManager.instance.PlaySound("energy");
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private IEnumerator ApplyAreaClear(int cx, int cy, int radius)
    {
        var nodes = new List<NodeController>();
        for (int x = cx - radius; x <= cx + radius; x++)
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            NodeController n = BoardManager.instance.GetNode(x, y);
            if (n != null) nodes.Add(n);
        }

        if (nodes.Count == 0) yield break;

        AudioManager.instance.PlaySound("explosion");
        ScoreManager.instance.AddChainScore(nodes.Count);
        yield return BoardManager.instance.RemoveNodesAndFill(nodes);
    }

    private void BroadcastCounts()
    {
        UIManager.instance.UpdatePowerUpCounts(boostCount, bombCount, energyCount);
    }
}
