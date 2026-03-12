using UnityEngine;

public class ImpactScaleFadeAutoReturn : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float lifetime = 1f;

    [Header("Scale")]
    [SerializeField] private Vector3 startScaleMultiplier = Vector3.one;
    [SerializeField] private Vector3 endScaleMultiplier = new Vector3(1.5f, 1.5f, 1f);
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Fade")]
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    [SerializeField] private bool includeInactiveChildren = true;

    private SpriteRenderer[] spriteRenderers = System.Array.Empty<SpriteRenderer>();
    private Color[] originalColors = System.Array.Empty<Color>();
    private Vector3 baseScale = Vector3.one;
    private float timer;

    private void Awake()
    {
        CacheRenderers();
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            CacheRenderers();
        }

        baseScale = transform.localScale;
        timer = Mathf.Max(0.01f, lifetime);
        ApplyVisuals(0f);
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        float normalizedTime = 1f - Mathf.Clamp01(timer / Mathf.Max(0.01f, lifetime));
        ApplyVisuals(normalizedTime);

        if (timer > 0f)
        {
            return;
        }

        Cleanup();
    }

    public void SetLifetime(float value)
    {
        lifetime = Mathf.Max(0.01f, value);
        timer = lifetime;
        ApplyVisuals(0f);
    }

    private void CacheRenderers()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactiveChildren);
        originalColors = new Color[spriteRenderers.Length];

        for (int index = 0; index < spriteRenderers.Length; index++)
        {
            originalColors[index] = spriteRenderers[index] != null ? spriteRenderers[index].color : Color.white;
        }
    }

    private void ApplyVisuals(float normalizedTime)
    {
        float scaleProgress = scaleCurve != null ? scaleCurve.Evaluate(normalizedTime) : normalizedTime;
        Vector3 startScale = Vector3.Scale(baseScale, startScaleMultiplier);
        Vector3 endScale = Vector3.Scale(baseScale, endScaleMultiplier);
        transform.localScale = Vector3.LerpUnclamped(startScale, endScale, scaleProgress);

        float alphaProgress = alphaCurve != null ? alphaCurve.Evaluate(normalizedTime) : 1f - normalizedTime;
        for (int index = 0; index < spriteRenderers.Length; index++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[index];
            if (spriteRenderer == null)
            {
                continue;
            }

            Color color = originalColors[index];
            color.a = Mathf.Clamp01(alphaProgress * originalColors[index].a);
            spriteRenderer.color = color;
        }
    }

    private void Cleanup()
    {
        RestoreOriginalState();

        if (!PoolManager.Return(gameObject))
        {
            Destroy(gameObject);
        }
    }

    private void RestoreOriginalState()
    {
        transform.localScale = baseScale;

        for (int index = 0; index < spriteRenderers.Length; index++)
        {
            if (spriteRenderers[index] != null)
            {
                spriteRenderers[index].color = originalColors[index];
            }
        }
    }
}