using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class NodeController : MonoBehaviour
{
    public enum NodeColor { Red, Blue, Green, Yellow, Purple, Orange }

    // ── Grid position ──────────────────────────────────────────────────────────
    [HideInInspector] public int gridX;
    [HideInInspector] public int gridY;

    // ── Runtime state ──────────────────────────────────────────────────────────
    [HideInInspector] public NodeColor nodeColor;
    [HideInInspector] public bool isInChain = false;

    // ── Inspector refs (assigned by BoardManager) ──────────────────────────────
    [HideInInspector] public Sprite[] colorSprites;   // index = (int)NodeColor
    [HideInInspector] public Color[]  glowColors;     // index = (int)NodeColor

    // ── Private components ─────────────────────────────────────────────────────
    private SpriteRenderer  spriteRenderer;
    private SpriteRenderer  glowRenderer;       // child named "Glow"
    private Vector3         originalScale;
    private Coroutine       pulseCoroutine;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale  = transform.localScale;

        Transform glowChild = transform.Find("Glow");
        if (glowChild != null)
            glowRenderer = glowChild.GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        StartPulse();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void SetColor(NodeColor color)
    {
        nodeColor = color;
        int idx = (int)color;

        if (colorSprites != null && idx < colorSprites.Length && colorSprites[idx] != null)
            spriteRenderer.sprite = colorSprites[idx];

        if (glowRenderer != null && glowColors != null && idx < glowColors.Length)
        {
            Color gc = glowColors[idx];
            gc.a = 0.35f;
            glowRenderer.color = gc;
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        isInChain = highlighted;

        if (highlighted)
        {
            StopPulse();
            transform.localScale = originalScale * 1.18f;
            SetGlowAlpha(1f);
        }
        else
        {
            transform.localScale = originalScale;
            SetGlowAlpha(0.35f);
            StartPulse();
        }
    }

    // Animate out then self-destruct; optional stagger delay.
    public IEnumerator ExplodeAndDestroy(float delay = 0f)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        StopPulse();

        float elapsed   = 0f;
        float duration  = 0.28f;
        Vector3 startScale = transform.localScale;
        Color   startColor = spriteRenderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / duration;

            transform.localScale      = startScale * (1f + t * 0.6f);
            spriteRenderer.color      = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

            if (glowRenderer != null)
            {
                Color gc = glowRenderer.color;
                gc.a = (1f - t);
                glowRenderer.color = gc;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private void SetGlowAlpha(float a)
    {
        if (glowRenderer == null) return;
        Color c = glowRenderer.color;
        c.a = a;
        glowRenderer.color = c;
    }

    private void StartPulse()
    {
        StopPulse();
        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    private void StopPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        transform.localScale = originalScale;
    }

    private IEnumerator PulseRoutine()
    {
        // Each node starts at a random phase so they don't all pulse together.
        float t = Random.Range(0f, Mathf.PI * 2f);
        while (true)
        {
            t += Time.deltaTime * 1.8f;
            float scale = 1f + Mathf.Sin(t) * 0.045f;
            transform.localScale = originalScale * scale;
            yield return null;
        }
    }
}
