using TMPro;
using UnityEngine;

/// <summary>
/// Hiển thị lượng vàng hiện tại của player trên TextMeshPro.
/// </summary>
public class GoldTextUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private TMP_Text goldText;

    [Header("Display")]
    [SerializeField] private string prefix = "Gold: ";
    [SerializeField] private bool findPlayerByTag = true;

    private void Awake()
    {
        if (goldText == null)
        {
            goldText = GetComponent<TMP_Text>();
        }

        ResolvePlayerStats();
    }

    private void OnEnable()
    {
        ResolvePlayerStats();
        Subscribe();
        RefreshNow();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void ResolvePlayerStats()
    {
        if (playerStats != null)
        {
            return;
        }

        if (!findPlayerByTag)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
        }
    }

    private void Subscribe()
    {
        if (playerStats != null)
        {
            playerStats.OnGoldChanged -= HandleGoldChanged;
            playerStats.OnGoldChanged += HandleGoldChanged;
        }
    }

    private void Unsubscribe()
    {
        if (playerStats != null)
        {
            playerStats.OnGoldChanged -= HandleGoldChanged;
        }
    }

    private void HandleGoldChanged(int currentGold)
    {
        UpdateText(currentGold);
    }

    public void RefreshNow()
    {
        if (playerStats == null)
        {
            UpdateText(0);
            return;
        }

        UpdateText(playerStats.gold);
    }

    public void SetPlayerStats(PlayerStats stats)
    {
        Unsubscribe();
        playerStats = stats;
        Subscribe();
        RefreshNow();
    }

    private void UpdateText(int currentGold)
    {
        if (goldText == null)
        {
            return;
        }

        goldText.text = $"{prefix}{currentGold}";
    }
}
