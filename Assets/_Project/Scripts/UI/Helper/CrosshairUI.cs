using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image crosshairImage;

    [Header("Settings")]
    [SerializeField] private float defaultSize = 40f;
    [SerializeField] private float expandedSize = 60f;
    [SerializeField] private float expandDuration = 0.2f;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.15f;

    private Coroutine currentExpandCoroutine;
    private Coroutine currentHitCoroutine;

    private void Awake()
    {
        if (crosshairImage == null)
        {
            Debug.LogError("Crosshair Image not assigned!");
            enabled = false;
            return;
        }
        ResetCrosshair();
    }

    /// <summary>
    /// Resets crosshair to default appearance.
    /// </summary>
    public void ResetCrosshair()
    {
        crosshairImage.rectTransform.sizeDelta = new Vector2(defaultSize, defaultSize);
        crosshairImage.color = defaultColor;
    }

    /// <summary>
    /// Expands crosshair size temporarily (e.g., when shooting or moving).
    /// </summary>
    public void ExpandCrosshair()
    {
        if (currentExpandCoroutine != null)
            StopCoroutine(currentExpandCoroutine);
        currentExpandCoroutine = StartCoroutine(ExpandRoutine());
    }

    private IEnumerator ExpandRoutine()
    {
        // Expand
        float elapsed = 0f;
        Vector2 startSize = crosshairImage.rectTransform.sizeDelta;
        Vector2 targetSize = new Vector2(expandedSize, expandedSize);

        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            crosshairImage.rectTransform.sizeDelta = Vector2.Lerp(startSize, targetSize, elapsed / expandDuration);
            yield return null;
        }

        // Shrink back
        elapsed = 0f;
        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            crosshairImage.rectTransform.sizeDelta = Vector2.Lerp(targetSize, new Vector2(defaultSize, defaultSize), elapsed / expandDuration);
            yield return null;
        }

        crosshairImage.rectTransform.sizeDelta = new Vector2(defaultSize, defaultSize);
    }

    /// <summary>
    /// Flashes the crosshair color to indicate a hit.
    /// </summary>
    public void ShowHitFeedback()
    {
        if (currentHitCoroutine != null)
            StopCoroutine(currentHitCoroutine);
        currentHitCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        crosshairImage.color = hitColor;

        yield return new WaitForSeconds(hitFlashDuration);

        crosshairImage.color = defaultColor;
    }

    /// <summary>
    /// Changes crosshair sprite.
    /// </summary>
    public void SetCrosshairSprite(Sprite newSprite)
    {
        crosshairImage.sprite = newSprite;
    }
}
