using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates the splash screen logo and loading bar.
/// The actual redirect to Main Menu is handled by UIManager.
/// </summary>
public class SplashScreen : MonoBehaviour
{
    [Header("Animation")]
    public CanvasGroup logoGroup;
    public Slider      loadingBar;
    public float       fadeDuration = 0.8f;

    private void OnEnable()
    {
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        // Fade in logo
        if (logoGroup != null)
        {
            logoGroup.alpha = 0f;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                logoGroup.alpha = t / fadeDuration;
                yield return null;
            }
            logoGroup.alpha = 1f;
        }

        // Fill loading bar
        if (loadingBar != null)
        {
            loadingBar.value = 0f;
            float t = 0f, dur = 1.5f;
            while (t < dur)
            {
                t += Time.deltaTime;
                loadingBar.value = t / dur;
                yield return null;
            }
            loadingBar.value = 1f;
        }
    }
}
