using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị máu hiện tại bằng Image loại Filled.
/// </summary>
public class HealthFilledImageUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Image healthFillImage;

    [Header("Display")]
    [SerializeField] private bool findPlayerByTag = true;
    [SerializeField] private bool smoothFill = true;
    [SerializeField] private float fillSpeed = 8f;

    private float targetFillAmount = 1f;

    private void Awake()
    {
        if (healthFillImage == null)
        {
            healthFillImage = GetComponent<Image>();
        }

        ResolvePlayerStats();
    }

    private void OnEnable()
    {
        ResolvePlayerStats();
        Subscribe();
        RefreshImmediately();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (healthFillImage == null || !smoothFill)
        {
            return;
        }

        healthFillImage.fillAmount = Mathf.MoveTowards(
            healthFillImage.fillAmount,
            targetFillAmount,
            fillSpeed * Time.deltaTime);
    }

    private void ResolvePlayerStats()
    {
        if (playerStats != null)
        {
            return;
        }

        if (findPlayerByTag)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerStats = player.GetComponent<PlayerStats>();
            }
        }
    }

    private void Subscribe()
    {
        if (playerStats != null)
        {
            playerStats.OnHPChanged -= HandleHPChanged;
            playerStats.OnHPChanged += HandleHPChanged;
        }
    }

    private void Unsubscribe()
    {
        if (playerStats != null)
        {
            playerStats.OnHPChanged -= HandleHPChanged;
        }
    }

    private void HandleHPChanged(int currentHp, int maxHp)
    {
        targetFillAmount = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;

        if (!smoothFill && healthFillImage != null)
        {
            healthFillImage.fillAmount = targetFillAmount;
        }
    }

    public void RefreshImmediately()
    {
        if (playerStats == null || healthFillImage == null)
        {
            return;
        }

        targetFillAmount = playerStats.GetHPProgress01();
        healthFillImage.fillAmount = targetFillAmount;
    }

    public void SetPlayerStats(PlayerStats stats)
    {
        Unsubscribe();
        playerStats = stats;
        Subscribe();
        RefreshImmediately();
    }
}
