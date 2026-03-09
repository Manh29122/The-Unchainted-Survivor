using UnityEngine;

/// <summary>
/// Tính toán level và exp của player bằng AnimationCurve.
/// Curve trả về số exp cần để hoàn thành 1 level cụ thể.
/// Ví dụ: level 1 -> 100 exp, level 2 -> 150 exp, level 3 -> 230 exp...
/// </summary>
public class PlayerLevelCurveCalculator : MonoBehaviour
{
    [Header("Level Curve")]
    [SerializeField] private int minLevel = 1;
    [SerializeField] private int maxLevel = 99;
    [SerializeField] private float expMultiplier = 1f;
    [SerializeField] private AnimationCurve expRequiredPerLevel = new AnimationCurve(
        new Keyframe(1f, 100f),
        new Keyframe(10f, 250f),
        new Keyframe(25f, 700f),
        new Keyframe(50f, 1800f)
    );

    public int MinLevel => minLevel;
    public int MaxLevel => maxLevel;

    /// <summary>
    /// Exp cần để hoàn thành level hiện tại và lên level kế tiếp.
    /// </summary>
    public int GetExpRequiredForLevel(int level)
    {
        int clampedLevel = Mathf.Clamp(level, minLevel, maxLevel);
        float evaluatedValue = expRequiredPerLevel.Evaluate(clampedLevel) * Mathf.Max(0.01f, expMultiplier);
        return Mathf.Max(1, Mathf.RoundToInt(evaluatedValue));
    }

    /// <summary>
    /// Tổng exp cần để đạt tới 1 level.
    /// Level 1 => 0 exp, Level 2 => exp của level 1, ...
    /// </summary>
    public int GetTotalExpToReachLevel(int targetLevel)
    {
        int clampedLevel = Mathf.Clamp(targetLevel, minLevel, maxLevel);
        int totalExp = 0;

        for (int level = minLevel; level < clampedLevel; level++)
        {
            totalExp += GetExpRequiredForLevel(level);
        }

        return totalExp;
    }

    /// <summary>
    /// Tính level hiện tại từ tổng exp.
    /// </summary>
    public int GetLevelFromTotalExp(int totalExp)
    {
        int safeExp = Mathf.Max(0, totalExp);
        int currentLevel = minLevel;
        int accumulatedExp = 0;

        while (currentLevel < maxLevel)
        {
            int requiredExp = GetExpRequiredForLevel(currentLevel);
            if (safeExp < accumulatedExp + requiredExp)
            {
                break;
            }

            accumulatedExp += requiredExp;
            currentLevel++;
        }

        return currentLevel;
    }

    /// <summary>
    /// Exp hiện có trong level hiện tại.
    /// </summary>
    public int GetCurrentLevelExp(int totalExp)
    {
        int currentLevel = GetLevelFromTotalExp(totalExp);
        int levelStartExp = GetTotalExpToReachLevel(currentLevel);
        return Mathf.Max(0, totalExp - levelStartExp);
    }

    /// <summary>
    /// Exp cần để lên level kế tiếp từ level hiện tại.
    /// </summary>
    public int GetExpToNextLevel(int totalExp)
    {
        int currentLevel = GetLevelFromTotalExp(totalExp);
        if (currentLevel >= maxLevel)
        {
            return 0;
        }

        return GetExpRequiredForLevel(currentLevel);
    }

    /// <summary>
    /// Phần trăm tiến độ trong level hiện tại, từ 0 đến 1.
    /// </summary>
    public float GetLevelProgress01(int totalExp)
    {
        int expToNextLevel = GetExpToNextLevel(totalExp);
        if (expToNextLevel <= 0)
        {
            return 1f;
        }

        int currentLevelExp = GetCurrentLevelExp(totalExp);
        return Mathf.Clamp01((float)currentLevelExp / expToNextLevel);
    }

    /// <summary>
    /// Cộng thêm exp và trả ra level mới.
    /// </summary>
    public int AddExp(ref int totalExp, int amount, out int currentLevelExp, out int expToNextLevel)
    {
        totalExp = Mathf.Max(0, totalExp + Mathf.Max(0, amount));
        int newLevel = GetLevelFromTotalExp(totalExp);
        currentLevelExp = GetCurrentLevelExp(totalExp);
        expToNextLevel = GetExpToNextLevel(totalExp);
        return newLevel;
    }
}
