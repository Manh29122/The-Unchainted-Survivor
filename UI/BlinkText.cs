using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlinkText : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] private Graphic uiGraphic;

    [Header("Blink Settings")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool useFade = true;
    [SerializeField] private bool loop = true;
    [SerializeField] private float blinkInterval = 0.5f;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float minAlpha = 0f;
    [SerializeField] private float maxAlpha = 1f;

    private Coroutine blinkCoroutine;

    private void Reset()
    {
        AutoAssign();
    }

    private void Awake()
    {
        AutoAssign();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            StartBlink();
        }
    }

    private void OnDisable()
    {
        StopBlink();
    }

    private void AutoAssign()
    {
        if (tmpText == null)
        {
            tmpText = GetComponent<TMP_Text>();
        }

        if (uiGraphic == null)
        {
            uiGraphic = GetComponent<Graphic>();
        }
    }

    public void StartBlink()
    {
        StopBlink();
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    public void StopBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        SetAlpha(maxAlpha);
    }

    public void SetBlinkInterval(float value)
    {
        blinkInterval = Mathf.Max(0.01f, value);
    }

    public void SetFadeDuration(float value)
    {
        fadeDuration = Mathf.Max(0.01f, value);
    }

    public void SetAlphaRange(float min, float max)
    {
        minAlpha = Mathf.Clamp01(min);
        maxAlpha = Mathf.Clamp01(max);
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            if (useFade)
            {
                yield return FadeTo(minAlpha);
                yield return new WaitForSeconds(blinkInterval);
                yield return FadeTo(maxAlpha);
                yield return new WaitForSeconds(blinkInterval);
            }
            else
            {
                SetAlpha(minAlpha);
                yield return new WaitForSeconds(blinkInterval);
                SetAlpha(maxAlpha);
                yield return new WaitForSeconds(blinkInterval);
            }

            if (!loop)
            {
                break;
            }
        }

        blinkCoroutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = GetCurrentAlpha();
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private float GetCurrentAlpha()
    {
        if (tmpText != null)
        {
            return tmpText.color.a;
        }

        if (uiGraphic != null)
        {
            return uiGraphic.color.a;
        }

        return 1f;
    }

    private void SetAlpha(float alpha)
    {
        if (tmpText != null)
        {
            Color color = tmpText.color;
            color.a = alpha;
            tmpText.color = color;
        }

        if (uiGraphic != null)
        {
            Color color = uiGraphic.color;
            color.a = alpha;
            uiGraphic.color = color;
        }
    }
}
