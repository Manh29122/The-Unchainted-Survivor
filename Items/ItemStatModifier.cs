using System;
using UnityEngine;

[Serializable]
public class ItemStatModifier
{
    [Tooltip("Chỉ số bị thay đổi")]
    public PlayerStatType statType;

    [Tooltip("Giá trị cộng hoặc trừ vào stat")]
    public float value;

    [TextArea]
    public string note;
}
