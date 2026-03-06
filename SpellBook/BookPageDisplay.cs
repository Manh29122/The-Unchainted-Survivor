using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào mỗi trang sách để hiển thị thông tin CharacterData.
/// Tự động fill data khi SpellBookController mở trang.
/// </summary>
public class BookPageDisplay : MonoBehaviour
{
    [Header("Character Info UI")]
    public TextMeshProUGUI characterNameTxt;
    public TextMeshProUGUI descriptionTxt;
    public TextMeshProUGUI hpValueTxt;
    public TextMeshProUGUI attackValueTxt;
    public TextMeshProUGUI armorValueTxt;
    public TextMeshProUGUI speedValueTxt;
    public TextMeshProUGUI recovery;
    public TextMeshProUGUI magnet;

   

    // Max values để tính % thanh stat
    private const float MAX_STAT = 200f;

    // ─────────────────────────────────────────

    /// <summary>Gọi từ SpellBookController khi lật đến trang này</summary>
    


    void SetStat(Slider bar, TextMeshProUGUI label, float value)
    {
        if (bar != null) bar.value = value / MAX_STAT;
        if (label != null) label.text = value.ToString("F0");
    }
}