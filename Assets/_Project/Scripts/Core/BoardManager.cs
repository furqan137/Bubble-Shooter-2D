using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager instance;

    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Grid")]
    public int gridWidth  = 6;
    public int gridHeight = 6;
    public float cellSize    = 1.1f;
    public float cellSpacing = 0.05f;

    [Header("Node Prefab")]
    public GameObject nodePrefab;

    [Header("Visuals (assigned to each node at spawn)")]
    public Sprite[] nodeSprites;     // 6 entries – one per NodeColor
    public Color[]  nodeGlowColors;  // 6 entries – one per NodeColor

    [Header("Board Root")]
    public Transform boardParent;

    // ── Runtime ────────────────────────────────────────────────────────────────
    private NodeController[,] grid;
    private Vector2 boardOrigin;   // world-space bottom-left cell centre

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void InitializeBoard(int width = -1, int height = -1)
    {
        if (width  > 0) gridWidth  = width;
        if (height > 0) gridHeight = height;

        grid = new NodeController[gridWidth, gridHeight];
        RecalcOrigin();
        SpawnAllNodes();
    }

    public void ClearBoard()
    {
        if (grid == null) return;
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                if (grid[x, y] != null)
                {
                    Destroy(grid[x, y].gameObject);
                    grid[x, y] = null;
                }
    }

    public NodeController GetNode(int x, int y)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return null;
        return grid[x, y];
    }

    public List<NodeController> GetAdjacentNodes(NodeController node)
    {
        var list = new List<NodeController>(4);
        int x = node.gridX, y = node.gridY;
        TryAdd(list, x,     y + 1);
        TryAdd(list, x,     y - 1);
        TryAdd(list, x - 1, y    );
        TryAdd(list, x + 1, y    );
        return list;
    }

    // Raycasts to nearest node within half a cell-size.
    public NodeController GetNodeAtWorldPos(Vector2 worldPos)
    {
        float threshold = cellSize * 0.52f;
        float best      = float.MaxValue;
        NodeController found = null;

        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == null) continue;
                float d = Vector2.Distance(grid[x, y].transform.position, worldPos);
                if (d < threshold && d < best)
                {
                    best  = d;
                    found = grid[x, y];
                }
            }
        return found;
    }

    // Explode nodes, apply gravity, then refill from top.
    public IEnumerator RemoveNodesAndFill(List<NodeController> nodes)
    {
        // Explode with staggered delay
        float delay = 0f;
        foreach (NodeController node in nodes)
        {
            if (node == null) continue;
            grid[node.gridX, node.gridY] = null;
            StartCoroutine(node.ExplodeAndDestroy(delay));
            delay += 0.06f;
        }

        yield return new WaitForSeconds(delay + 0.32f);

        yield return StartCoroutine(ApplyGravity());
        yield return StartCoroutine(FillEmptyCells());
    }

    // ── Grid helpers ───────────────────────────────────────────────────────────

    private void SpawnAllNodes()
    {
        for (int x = 0; x < gridWidth;  x++)
        for (int y = 0; y < gridHeight; y++)
            SpawnNode(x, y, RandomColor());
    }

    private NodeController SpawnNode(int x, int y, NodeController.NodeColor color)
    {
        Vector2 pos = WorldPos(x, y);
        GameObject obj = Instantiate(nodePrefab, pos, Quaternion.identity, boardParent);
        NodeController node = obj.GetComponent<NodeController>();
        node.gridX      = x;
        node.gridY      = y;
        node.colorSprites = nodeSprites;
        node.glowColors   = nodeGlowColors;
        node.SetColor(color);
        grid[x, y] = node;
        return node;
    }

    // Gravity: nodes fall into empty cells below them.
    private IEnumerator ApplyGravity()
    {
        bool moved = true;
        while (moved)
        {
            moved = false;
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 1; y < gridHeight; y++)
                {
                    if (grid[x, y] == null) continue;

                    int targetY = y;
                    while (targetY > 0 && grid[x, targetY - 1] == null)
                        targetY--;

                    if (targetY == y) continue;

                    NodeController node = grid[x, y];
                    grid[x, targetY] = node;
                    grid[x, y]       = null;
                    node.gridY       = targetY;
                    StartCoroutine(SlideTo(node, WorldPos(x, targetY)));
                    moved = true;
                }
            }
            if (moved) yield return new WaitForSeconds(0.17f);
        }
    }

    // Fill every remaining empty cell from above the board.
    private IEnumerator FillEmptyCells()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            int spawnRow = gridHeight;   // spawn above visible board
            for (int y = gridHeight - 1; y >= 0; y--)
            {
                if (grid[x, y] != null) continue;

                NodeController node = SpawnNode(x, y, RandomColor());
                // Teleport to spawn position above board, then slide down
                node.transform.position = new Vector3(WorldPos(x, spawnRow).x,
                                                       WorldPos(x, spawnRow).y, 0f);
                StartCoroutine(SlideTo(node, WorldPos(x, y)));
                spawnRow++;
                yield return new WaitForSeconds(0.04f);
            }
        }
        yield return new WaitForSeconds(0.28f);
    }

    private IEnumerator SlideTo(NodeController node, Vector2 target)
    {
        float duration = 0.22f;
        float elapsed  = 0f;
        Vector3 start  = node.transform.position;
        Vector3 end    = new Vector3(target.x, target.y, start.z);

        while (elapsed < duration)
        {
            if (node == null) yield break;
            elapsed += Time.deltaTime;
            node.transform.position = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        if (node != null) node.transform.position = end;
    }

    // ── Utility ────────────────────────────────────────────────────────────────

    private void RecalcOrigin()
    {
        float step  = cellSize + cellSpacing;
        float totalW = step * gridWidth  - cellSpacing;
        float totalH = step * gridHeight - cellSpacing;
        boardOrigin  = new Vector2(-totalW / 2f + cellSize / 2f,
                                   -totalH / 2f + cellSize / 2f);
    }

    public Vector2 WorldPos(int x, int y)
    {
        float step = cellSize + cellSpacing;
        return boardOrigin + new Vector2(x * step, y * step);
    }

    private static NodeController.NodeColor RandomColor()
    {
        int count = System.Enum.GetValues(typeof(NodeController.NodeColor)).Length;
        return (NodeController.NodeColor)Random.Range(0, count);
    }

    private void TryAdd(List<NodeController> list, int x, int y)
    {
        NodeController n = GetNode(x, y);
        if (n != null) list.Add(n);
    }

    // Draw board outline in editor
    private void OnDrawGizmosSelected()
    {
        if (grid == null) RecalcOrigin();
        Gizmos.color = Color.cyan;
        float step = cellSize + cellSpacing;
        for (int x = 0; x < gridWidth;  x++)
        for (int y = 0; y < gridHeight; y++)
        {
            Vector2 c = boardOrigin + new Vector2(x * step, y * step);
            Gizmos.DrawWireCube(c, Vector2.one * cellSize);
        }
    }
}
