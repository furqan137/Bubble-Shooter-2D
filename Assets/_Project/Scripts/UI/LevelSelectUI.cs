using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Populates the level-select grid at runtime.
/// Attach to the LevelSelect screen root.
/// </summary>
public class LevelSelectUI : MonoBehaviour
{
    [Header("Level Button Template")]
    public GameObject levelButtonPrefab;
    public Transform  buttonContainer;

    private void OnEnable()
    {
        PopulateButtons();
    }

    private void PopulateButtons()
    {
        // Clear old buttons
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        if (GameManager.instance == null || GameManager.instance.levels == null) return;

        for (int i = 0; i < GameManager.instance.levels.Length; i++)
        {
            int index = i; // capture for lambda
            GameObject btn   = Instantiate(levelButtonPrefab, buttonContainer);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = $"Level {i + 1}";

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.instance.PlaySound("ui_click");
                GameManager.instance.SelectLevel(index);
            });
        }
    }
}
