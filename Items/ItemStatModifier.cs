using System;
using UnityEngine;

public enum StatModifierValueType
{
    Flat,
    Percent
}

[Serializable]
public class ItemStatModifier
{
    [Tooltip("Chỉ số bị thay đổi")]
    public PlayerStatType statType;

    [Tooltip("Giá trị cộng hoặc trừ vào stat")]
    public float value;

    [Tooltip("Flat = cộng/trừ trực tiếp. Percent = nhập 1 là 1%, 10 là 10%, 20 là 20%.")]
    public StatModifierValueType valueType = StatModifierValueType.Flat;

    [TextArea]
    public string note;
}
