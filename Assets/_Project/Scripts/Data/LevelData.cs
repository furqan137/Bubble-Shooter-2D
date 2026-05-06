using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "LinkBurst/LevelData")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public int levelIndex;
    public string levelName;

    [Header("Grid")]
    public int gridWidth = 6;
    public int gridHeight = 6;

    [Header("Rules")]
    public int targetScore = 500;
    public int startingEnergy = 30;
    public int minimumChain = 2;

    [Header("Scoring Multipliers")]
    [Tooltip("Score = chainLength * (chainLength + bonusBase)")]
    public int bonusBase = 5;

    [Header("Power-ups")]
    public int boostCount = 3;
    public int bombCount = 3;
    public int energyCount = 3;
}
