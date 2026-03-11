using TMPro;
using UnityEngine;

public class WaveTimerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyWaveSpawner waveSpawner;
    [SerializeField] private TMP_Text timerText;

    [Header("Display")]
    [SerializeField] private string prefix = "Wave Time: ";
    [SerializeField] private string idleText = string.Empty;
    [SerializeField] private bool useCeilForSeconds = true;

    private void Awake()
    {
        if (waveSpawner == null)
        {
            waveSpawner = FindFirstObjectByType<EnemyWaveSpawner>();
        }

        if (timerText == null)
        {
            timerText = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        if (waveSpawner == null)
        {
            waveSpawner = FindFirstObjectByType<EnemyWaveSpawner>();
        }

        if (waveSpawner == null)
        {
            RefreshIdleState();
            return;
        }

        waveSpawner.OnWaveStarted += HandleWaveStarted;
        waveSpawner.OnWaveCompleted += HandleWaveCompleted;
        waveSpawner.OnWaveTimerUpdated += HandleWaveTimerUpdated;

        if (waveSpawner.IsWaveActive)
        {
            UpdateTimerText(waveSpawner.CurrentWaveRemainingTime);
        }
        else
        {
            RefreshIdleState();
        }
    }

    private void OnDisable()
    {
        if (waveSpawner == null)
        {
            return;
        }

        waveSpawner.OnWaveStarted -= HandleWaveStarted;
        waveSpawner.OnWaveCompleted -= HandleWaveCompleted;
        waveSpawner.OnWaveTimerUpdated -= HandleWaveTimerUpdated;
    }

    private void HandleWaveStarted(int waveIndex)
    {
        if (waveSpawner == null)
        {
            RefreshIdleState();
            return;
        }

        UpdateTimerText(waveSpawner.CurrentWaveRemainingTime);
    }

    private void HandleWaveCompleted(int waveIndex)
    {
        RefreshIdleState();
    }

    private void HandleWaveTimerUpdated(int waveIndex, float remainingTime, float duration)
    {
        UpdateTimerText(remainingTime);
    }

    private void UpdateTimerText(float remainingTime)
    {
        if (timerText == null)
        {
            return;
        }

        int totalSeconds = useCeilForSeconds ? Mathf.CeilToInt(Mathf.Max(0f, remainingTime)) : Mathf.FloorToInt(Mathf.Max(0f, remainingTime));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{prefix}{minutes:00}:{seconds:00}";
    }

    private void RefreshIdleState()
    {
        if (timerText == null)
        {
            return;
        }

        timerText.text = idleText;
    }
}
