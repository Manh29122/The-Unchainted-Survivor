using UnityEngine;

/// <summary>
/// Module tăng máu tối đa cho player.
/// Gắn component này lên player hoặc prefab module skill/buff rồi gọi ApplyModule().
/// </summary>
public class MaxHealthModule : MonoBehaviour
{
    [Header("Max HP Bonus")]
    [SerializeField] private int bonusMaxHP = 20;
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool healByIncrease = true;
    [SerializeField] private bool applyOnlyOnce = true;

    private PlayerStats playerStats;
    private bool hasApplied;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();

        if (playerStats == null)
        {
            playerStats = GetComponentInParent<PlayerStats>();
        }
    }

    private void OnEnable()
    {
        if (applyOnEnable)
        {
            ApplyModule();
        }
    }

    public void ApplyModule()
    {
        if (playerStats == null)
        {
            Debug.LogWarning("[MaxHealthModule] PlayerStats not found.");
            return;
        }

        if (applyOnlyOnce && hasApplied)
        {
            return;
        }

        playerStats.IncreaseMaxHP(bonusMaxHP, healByIncrease);
        hasApplied = true;
    }

    public void SetBonusMaxHP(int value)
    {
        bonusMaxHP = Mathf.Max(0, value);
    }
}
