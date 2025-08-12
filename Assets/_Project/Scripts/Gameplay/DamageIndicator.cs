// DamageIndicator.cs
using System;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class DamageIndicator : MonoBehaviour
{
    [SerializeField] private Image icon = null!;
    [SerializeField] private float life = 1.2f;
    [SerializeField] private float fade = 0.5f;
    [SerializeField] private float radius = 100f;

    private RectTransform _rt;
    private CanvasGroup _cg;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    public async void Setup(Vector3 hitDirection, Action onFinished)
    {
        // Calculate screen-space position and rotation relative to forward (camera)
        float angle = Mathf.Atan2(hitDirection.x, hitDirection.z) * Mathf.Rad2Deg;
        _rt.localRotation = Quaternion.Euler(0, 0, -angle);
        _rt.anchoredPosition = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * radius;

        _cg.alpha = 1f;
        float elapsed = 0f;

        // Wait life seconds then fade
        await UniTask.Delay(Mathf.RoundToInt(life * 1000));
        elapsed = 0f;
        while (elapsed < fade)
        {
            await UniTask.Yield();
            elapsed += Time.unscaledDeltaTime;
            _cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fade);
        }

        onFinished?.Invoke();
    }
}
