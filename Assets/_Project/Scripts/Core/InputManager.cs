using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reads mouse / touch input, builds a same-color chain across adjacent nodes,
/// and fires it to GameManager on release.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Chain Settings")]
    public int minimumChainLength = 2;

    [Header("Chain Line")]
    public LineRenderer chainLine;
    public Color validColor   = new Color(1f, 1f, 1f, 0.85f);
    public Color pendingColor = new Color(1f, 0.85f, 0f, 0.85f);
    public float lineWidth    = 0.08f;

    // ── Private ────────────────────────────────────────────────────────────────
    private readonly List<NodeController> chain = new List<NodeController>();
    private bool        isChaining;
    private Camera      cam;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (instance == null) instance = this;
        cam = Camera.main;

        if (chainLine != null)
        {
            chainLine.startWidth = lineWidth;
            chainLine.endWidth   = lineWidth;
            chainLine.positionCount = 0;
        }
    }

    private void Update()
    {
        if (GameManager.instance == null) return;
        if (GameManager.instance.CurrentState != GameManager.GameState.Gameplay) return;
        if (GameManager.instance.IsAnimating) return;

        HandleInput();
    }

    // ── Input routing ──────────────────────────────────────────────────────────

    private void HandleInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if      (Input.GetMouseButtonDown(0)) OnDown(ScreenToWorld(Input.mousePosition));
        else if (Input.GetMouseButton(0))     OnDrag(ScreenToWorld(Input.mousePosition));
        else if (Input.GetMouseButtonUp(0))   OnUp();
#else
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if      (t.phase == TouchPhase.Began)                      OnDown(ScreenToWorld(t.position));
            else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) OnDrag(ScreenToWorld(t.position));
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)   OnUp();
        }
#endif
    }

    private void OnDown(Vector2 worldPos)
    {
        NodeController node = BoardManager.instance.GetNodeAtWorldPos(worldPos);
        if (node == null) return;

        chain.Clear();
        AddNode(node);
        isChaining = true;
        RefreshLine();
        AudioManager.instance.PlaySound("tap");
    }

    private void OnDrag(Vector2 worldPos)
    {
        if (!isChaining || chain.Count == 0) return;

        NodeController node = BoardManager.instance.GetNodeAtWorldPos(worldPos);
        if (node == null) return;

        // Back-track: remove last node
        if (chain.Count >= 2 && node == chain[chain.Count - 2])
        {
            NodeController removed = chain[chain.Count - 1];
            removed.SetHighlighted(false);
            chain.RemoveAt(chain.Count - 1);
            RefreshLine();
            return;
        }

        // Extend chain
        if (!chain.Contains(node) && IsValidNext(node))
        {
            AddNode(node);
            RefreshLine();
            AudioManager.instance.PlaySound("tap");
        }
    }

    private void OnUp()
    {
        if (!isChaining) return;
        isChaining = false;
        ClearLine();

        if (chain.Count < minimumChainLength)
        {
            if (chain.Count > 0)
            {
                UIManager.instance.ShowInvalidMovePopup();
                foreach (NodeController n in chain) n.SetHighlighted(false);
            }
            chain.Clear();
            return;
        }

        // Snapshot and hand off to GameManager
        GameManager.instance.ExecuteChain(new List<NodeController>(chain));
        chain.Clear();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private bool IsValidNext(NodeController candidate)
    {
        NodeController last = chain[chain.Count - 1];
        if (candidate.nodeColor != last.nodeColor) return false;
        return BoardManager.instance.GetAdjacentNodes(last).Contains(candidate);
    }

    private void AddNode(NodeController node)
    {
        node.SetHighlighted(true);
        chain.Add(node);
    }

    private void RefreshLine()
    {
        if (chainLine == null) return;
        chainLine.positionCount = chain.Count;
        for (int i = 0; i < chain.Count; i++)
            chainLine.SetPosition(i, chain[i].transform.position + Vector3.back * 0.1f);

        Color c = chain.Count >= minimumChainLength ? validColor : pendingColor;
        chainLine.startColor = c;
        chainLine.endColor   = c;
    }

    private void ClearLine()
    {
        if (chainLine != null) chainLine.positionCount = 0;
    }

    public void CancelChain()
    {
        foreach (NodeController n in chain) if (n != null) n.SetHighlighted(false);
        chain.Clear();
        isChaining = false;
        ClearLine();
    }

    private Vector2 ScreenToWorld(Vector3 screenPos)
        => cam.ScreenToWorldPoint(screenPos);
}
