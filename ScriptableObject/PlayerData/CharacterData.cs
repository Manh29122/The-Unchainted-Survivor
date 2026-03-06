using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "ScriptableObject/CharacterData")]
public class CharacterData : ScriptableObject
{
    public GameObject characterPrefab;
    public RectTransform characterIcon;

    public string characterName;
    public float Health;
    public float Attack;
    public float Armor;
    public float Speed;
    public float Recovery;
    public float CooldownSkill;
    public float Magnet;


    public GameObject skillPlayer;

    public string ActiveskillName;
    public string ActiveDescription;
    public string DeactiveskillName;
    public string DeactiveDescription;
}
