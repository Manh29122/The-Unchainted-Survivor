using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {            
            return _instance;
        }
        set
        {
            _instance = value;
        }
    }

    public CharacterManager _characterManager;
    void Start()
    {
        if (_instance != null)
        {
            Debug.LogWarning("Multiple GameManager instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        _instance = this;
        if (_characterManager == null)
        {
            Debug.LogError("GameManager: CharacterManager reference is not set in the inspector!");
            Debug.LogError("Please assign the CharacterManager reference in the GameManager inspector.");
            Debug.LogError("Without a reference to CharacterManager, the game will not function correctly.");
        }
    } 
}
