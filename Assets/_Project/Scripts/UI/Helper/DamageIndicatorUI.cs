using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Laboratory.Gameplay.UI;
// FIXME: tidyup after 8/29
public class DamageIndicatorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform indicatorsParent = null!;
    [SerializeField] private DamageIndicator indicatorPrefab = null!;
    [SerializeField] private AudioSource audioSource = null!; // Optional, for sounds
    [SerializeField] private AudioClip damageSound = null!;
    [SerializeField] private AudioClip criticalDamageSound = null!;

    [Header("Settings")]
    [SerializeField] private float indicatorDuration = 1.5f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float distanceFromCenter = 100f; // How far from screen center indicators appear

    [SerializeField] private UIShakeEffect shakeEffect = null!;

    private readonly Queue<DamageIndicator> _indicatorPool = new();
    private readonly List<DamageIndicator> _activeIndicators = new();
    private Camera _mainCamera = null!;


    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        MessageBus.OnDamage += OnDamageEvent;
    }
    private void OnDisable()
    {
        MessageBus.OnDamage -= OnDamageEvent;
    }

    private void OnDamageEvent(DamageEvent damageEvent)
    {
        if (damageEvent.TargetId != NetworkManager.Singleton.LocalClientId) return;

        var indicator = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(indicatorPrefab, _container);
        indicator.Setup(damageEvent.HitDirection);
        indicator.OnFinished += () => _pool.Enqueue(indicator);
    }

    /// <summary>
    /// Spawns a damage indicator with optional damage amount, damage type, and sound/vibration effects.
    /// </summary>
    /// <param name="sourcePosition">World position where damage came from</param>
    /// <param name="damageAmount">Optional damage amount to display. Pass null to hide.</param>
    /// <param name="damageType">Damage type for style and effects</param>
    /// <param name="playSound">Whether to play sound effect (default true)</param>
    /// <param name="vibrate">Whether to trigger vibration (default false)</param>
    public void SpawnIndicator(
        Vector3 sourcePosition,
        int? damageAmount = null,
        DamageType damageType = DamageType.Normal,
        bool playSound = true,
        bool vibrate = false)
    {
        var indicator = GetIndicatorFromPool();
        indicator.RectTransform.gameObject.SetActive(true);

        // Calculate rotation & position same as before
        Vector3 playerForward = _mainCamera.transform.forward;
        Vector3 toSource = (sourcePosition - _mainCamera.transform.position).normalized;

        Vector3 flatForward = new Vector3(playerForward.x, 0, playerForward.z).normalized;
        Vector3 flatToSource = new Vector3(toSource.x, 0, toSource.z).normalized;
        float angle = Vector3.SignedAngle(flatForward, flatToSource, Vector3.up);

        indicator.RectTransform.localRotation = Quaternion.Euler(0, 0, -angle);

        float radius = 100f;
        Vector2 pos = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * radius;
        indicator.RectTransform.anchoredPosition = pos;

        // Set damage amount text if any
        if (damageAmount.HasValue)
        {
            indicator.DamageText.gameObject.SetActive(true);
            indicator.DamageText.text = damageAmount.Value.ToString();

            // Optionally style text by damage type
            switch (damageType)
            {
                case DamageType.Critical:
                    indicator.DamageText.color = Color.red;
                    indicator.DamageText.fontStyle = FontStyle.Bold;
                    break;
                case DamageType.Fire:
                    indicator.DamageText.color = new Color(1f, 0.5f, 0f); // orange
                    indicator.DamageText.fontStyle = FontStyle.Normal;
                    break;
                case DamageType.Ice:
                    indicator.DamageText.color = Color.cyan;
                    indicator.DamageText.fontStyle = FontStyle.Normal;
                    break;
                default:
                    indicator.DamageText.color = Color.white;
                    indicator.DamageText.fontStyle = FontStyle.Normal;
                    break;
            }
        }
        else
        {
            indicator.DamageText.gameObject.SetActive(false);
        }

        // Set indicator icon style by damage type (if you want to swap sprites/colors)
        switch (damageType)
        {
            case DamageType.Critical:
                indicator.Image.color = Color.red;
                break;
            case DamageType.Fire:
                indicator.Image.color = new Color(1f, 0.5f, 0f);
                break;
            case DamageType.Ice:
                indicator.Image.color = Color.cyan;
                break;
            default:
                indicator.Image.color = Color.white;
                break;
        }

        indicator.StartLife(indicatorDuration, fadeDuration);

        _activeIndicators.Add(indicator);

        if (playSound && audioSource != null)
        {
            AudioClip clipToPlay = damageType == DamageType.Critical && criticalDamageSound != null
                ? criticalDamageSound
                : damageSound;

            if (clipToPlay != null)
                audioSource.PlayOneShot(clipToPlay);
        }

        if (vibrate && shakeEffect != null)
        {
            shakeEffect.Shake();
        }
    }

    private DamageIndicator GetIndicatorFromPool()
    {
        if (_indicatorPool.Count > 0)
        {
            return _indicatorPool.Dequeue();
        }
        else
        {
            var go = Instantiate(indicatorPrefab, indicatorsParent);
            var indicator = new DamageIndicator(go);
            return indicator;
        }
    }

    private void RecycleIndicator(DamageIndicator indicator)
    {
        indicator.RectTransform.gameObject.SetActive(false);
        _indicatorPool.Enqueue(indicator);
    }

    public void StartLife(float lifeDuration, float fadeDuration)
    {
        lifeTime = lifeDuration;
        fadeTime = fadeDuration;
        timer = 0f;
        fadingOut = false;
        CanvasGroup.alpha = 1f;
    }

    public void UpdateIndicator(float delta)
    {
        timer += delta;

        if (!fadingOut && timer >= lifeTime)
        {
            fadingOut = true;
            timer = 0f;
        }

        if (fadingOut)
        {
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeTime);
            CanvasGroup.alpha = alpha;
        }
    }

    private void Update()
    {
        float delta = Time.unscaledDeltaTime;
        for (int i = _activeIndicators.Count - 1; i >= 0; i--)
        {
            var indicator = _activeIndicators[i];
            indicator.UpdateIndicator(delta);
            if (indicator.IsExpired)
            {
                RecycleIndicator(indicator);
                _activeIndicators.RemoveAt(i);
            }
        }
    }
}
