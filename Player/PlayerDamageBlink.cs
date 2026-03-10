using System.Collections;
using UnityEngine;

public class PlayerDamageBlink : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private bool includeInactiveChildren = true;

    [Header("Blink Settings")]
    [SerializeField] private Color blinkColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float blinkInterval = 0.08f;
    [SerializeField] private int blinkCount = 4;

    private SpriteRenderer[] spriteRenderers = System.Array.Empty<SpriteRenderer>();
    private Color[] originalColors = System.Array.Empty<Color>();
    private Coroutine blinkCoroutine;

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }

        CacheSpriteRenderers();
    }

    private void OnEnable()
    {
        CacheSpriteRenderers();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        RestoreOriginalColors();
    }

    public void RefreshRenderers()
    {
        CacheSpriteRenderers();
        RestoreOriginalColors();
    }

    private void Subscribe()
    {
        if (playerStats != null)
        {
            playerStats.OnDamageTaken -= HandleDamageTaken;
            playerStats.OnDamageTaken += HandleDamageTaken;
        }
    }

    private void Unsubscribe()
    {
        if (playerStats != null)
        {
            playerStats.OnDamageTaken -= HandleDamageTaken;
        }
    }

    private void HandleDamageTaken(int damage)
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            RestoreOriginalColors();
        }

        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        int flashes = Mathf.Max(1, blinkCount);
        float interval = Mathf.Max(0.01f, blinkInterval);

        for (int index = 0; index < flashes; index++)
        {
            SetAllColors(blinkColor);
            yield return new WaitForSeconds(interval);

            RestoreOriginalColors();
            yield return new WaitForSeconds(interval);
        }

        blinkCoroutine = null;
    }

    private void CacheSpriteRenderers()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactiveChildren);
        originalColors = new Color[spriteRenderers.Length];

        for (int index = 0; index < spriteRenderers.Length; index++)
        {
            originalColors[index] = spriteRenderers[index] != null ? spriteRenderers[index].color : Color.white;
        }
    }

    private void SetAllColors(Color targetColor)
    {
        for (int index = 0; index < spriteRenderers.Length; index++)
        {
            if (spriteRenderers[index] != null)
            {
                spriteRenderers[index].color = targetColor;
            }
        }
    }

    private void RestoreOriginalColors()
    {
        for (int index = 0; index < spriteRenderers.Length; index++)
        {
            if (spriteRenderers[index] != null)
            {
                spriteRenderers[index].color = originalColors[index];
            }
        }
    }
}