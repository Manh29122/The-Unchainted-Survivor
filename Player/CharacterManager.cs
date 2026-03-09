using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Load tất cả CharacterData từ Resources, quản lý bằng List + Dictionary.
/// Gắn vào GameManager hoặc CharacterSelectManager.
/// 
/// Yêu cầu: Đặt tất cả CharacterData.asset vào folder Resources/Characters/
/// </summary>
[DefaultExecutionOrder(-100)]
public class CharacterManager : MonoBehaviour
{
    //public static CharacterManager Instance { get; private set; }

    // ─────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────
    [Header("Resources Path")]
    [Tooltip("Folder trong Resources chứa tất cả CharacterData")]
    public string resourcesPath = "Characters";

    [Header("Spawn")]
    [Tooltip("Vị trí spawn player khi chọn nhân vật")]
    public Transform spawnPoint;
    [Tooltip("Tên object spawn trong scene mới nếu reference cũ bị mất")]
    public string spawnPointName = "SpawnPoint";
    [Tooltip("Tag của spawn point trong scene mới")]
    public string spawnPointTag = "PlayerSpawnPoint";
    [Tooltip("Tự tạo lại prefab character đã chọn khi sang scene mới")]
    public bool respawnSelectedCharacterOnSceneLoad = true;
    public float offsetY = 2f;
    public Vector3 selectionScale = new Vector3(4f, 4f, 4f);
    public Vector3 sceneSpawnScale = Vector3.one;
    // ─────────────────────────────────────────
    //  DATA
    // ─────────────────────────────────────────

    /// <summary>Danh sách tất cả nhân vật (theo thứ tự load)</summary>
    public List<CharacterData> CharacterList = new List<CharacterData>();

    /// <summary>Tra cứu nhanh theo tên nhân vật</summary>
    public Dictionary<string, CharacterData> CharacterDict { get; private set; } = new Dictionary<string, CharacterData>();

    /// <summary>Nhân vật đang được chọn</summary>
    public CharacterData SelectedCharacter;

    /// <summary>GameObject player hiện tại trong scene</summary>
    public GameObject CurrentPlayerObject;

    // ── Events ───────────────────────────────
    public event System.Action<CharacterData> OnCharacterApplied;

    /// <summary>Lisst BookCharacterSelector để hiển thị và chọn </summary>
    public List<BookCharacterSelector> _listCharacterSelector = new List<BookCharacterSelector>();

    public Transform _content;
    public GameObject _childContent;

    [Header("Character Info UI")]
    public TextMeshProUGUI characterNameTxt;
    public TextMeshProUGUI descriptionTxt;
    public TextMeshProUGUI hpValueTxt;
    public TextMeshProUGUI attackValueTxt;
    public TextMeshProUGUI armorValueTxt;
    public TextMeshProUGUI speedValueTxt;
    public TextMeshProUGUI recovery;
    public TextMeshProUGUI magnet;


    // ─────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────
    void Awake()
    {
        //if (Instance != null) { Destroy(gameObject); return; }
        //Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadAllCharacters();

        if (_content != null && _childContent != null)
        {
            SpawnUIChooseCharacter();
        }

        RefreshSpawnPoint();

    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ─────────────────────────────────────────
    //  LOAD
    // ─────────────────────────────────────────

    /// <summary>
    /// Load tất cả CharacterData từ Resources/Characters/
    /// Tự động điền vào CharacterList và CharacterDict.
    /// </summary>
    public void LoadAllCharacters()
    {
        CharacterList.Clear();
        CharacterDict.Clear();

        CharacterData[] loaded = Resources.LoadAll<CharacterData>(resourcesPath);

        if (loaded.Length == 0)
        {
            Debug.LogWarning($"[CharacterManager] Không tìm thấy CharacterData nào trong Resources/{resourcesPath}/");
            return;
        }

        foreach (var data in loaded)
        {
            // Thêm vào List
            CharacterList.Add(data);

            // Thêm vào Dictionary (key = characterName)
            if (!CharacterDict.ContainsKey(data.characterName))
            {
                CharacterDict.Add(data.characterName, data);
            }
            else
            {
                Debug.LogWarning($"[CharacterManager] Tên nhân vật trùng: '{data.characterName}' — bỏ qua bản thứ 2!");
            }
        }

        Debug.Log($"[CharacterManager] ✅ Đã load {CharacterList.Count} nhân vật: " +
                  string.Join(", ", CharacterDict.Keys));
    }
    void SpawnUIChooseCharacter()
    {
        foreach (var cl in CharacterList)
        {
            if (cl != null)
            {
                var tempObj = Instantiate(_childContent.gameObject, _content.transform);
                tempObj.GetComponent<BookCharacterSelector>().characterData = cl;
                var headset = cl.characterIcon;
                var headsetIns = Instantiate(headset, tempObj.transform);
                headsetIns.gameObject.GetComponent<RectTransform>().SetParent(tempObj.gameObject.GetComponent<RectTransform>());
                headsetIns.gameObject.GetComponent<RectTransform>().anchoredPosition = tempObj.gameObject.GetComponent<RectTransform>().anchoredPosition;
                headsetIns.gameObject.GetComponent<RectTransform>().localScale = new Vector3(80, 80, 80);
                headsetIns.gameObject.SetActive(true);
            }
        }
    }
    Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
    // ─────────────────────────────────────────
    //  APPLY CHARACTER
    // ─────────────────────────────────────────

    /// <summary>
    /// Chọn và spawn nhân vật theo CharacterData.
    /// Xoá player cũ nếu có, spawn prefab mới, áp dụng stats.
    /// </summary>
    public void ApplyCharacter(CharacterData data)
    {
        ApplyCharacter(data, false);
    }

    private void ApplyCharacter(CharacterData data, bool spawnForGameplayScene)
    {
        if (data == null)
        {
            Debug.LogError("[CharacterManager] ApplyCharacter: data là null!");
            return;
        }

        if (data.characterPrefab == null)
        {
            Debug.LogError($"[CharacterManager] '{data.characterName}' thiếu characterPrefab!");
            return;
        }

        SelectedCharacter = data;

        bool hasSpawnPoint = RefreshSpawnPoint();

        if (!hasSpawnPoint)
        {
            Debug.LogWarning("[CharacterManager] Không tìm thấy spawn point, sẽ tạo character tại (0,0,0).");
        }

        // Xoá player cũ
        if (CurrentPlayerObject != null)
            Destroy(CurrentPlayerObject);

        // Spawn player mới
        Vector3 spawnPos = hasSpawnPoint
            ? new Vector3(spawnPoint.position.x, spawnPoint.position.y - offsetY, spawnPoint.position.z)
            : Vector3.zero;

        GameObject tempObj = Instantiate(data.characterPrefab, spawnPos, Quaternion.identity);
        CurrentPlayerObject = tempObj;

        if (hasSpawnPoint)
        {
            CurrentPlayerObject.transform.SetParent(spawnPoint);
        }

        CurrentPlayerObject.transform.localScale = spawnForGameplayScene ? sceneSpawnScale : selectionScale;

        CurrentPlayerObject.name = $"Player_{data.characterName}";

        // Áp dụng stats vào các component của player
        ApplyStatsToPlayer(CurrentPlayerObject, data);
        Display(data);
        Debug.Log($"[CharacterManager] ✅ Đã chọn nhân vật: {data.characterName}");
        OnCharacterApplied?.Invoke(data);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (!respawnSelectedCharacterOnSceneLoad)
        {
            return;
        }

        RefreshSpawnPoint();

        if (SelectedCharacter == null)
        {
            SelectedCharacter = GetRandomCharacter();

            if (SelectedCharacter == null)
            {
                Debug.LogWarning("[CharacterManager] Không có character nào để spawn ngẫu nhiên.");
                return;
            }

            Debug.Log($"[CharacterManager] Chưa chọn character, dùng ngẫu nhiên: {SelectedCharacter.characterName}");
        }

        ApplyCharacter(SelectedCharacter, true);
    }

    private CharacterData GetRandomCharacter()
    {
        if (CharacterList == null || CharacterList.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, CharacterList.Count);
        return CharacterList[randomIndex];
    }

    private bool RefreshSpawnPoint()
    {
        if (spawnPoint != null && spawnPoint.gameObject.scene.IsValid())
        {
            return true;
        }

        spawnPoint = null;

        if (!string.IsNullOrWhiteSpace(spawnPointTag))
        {
            GameObject taggedSpawn = null;

            try
            {
                taggedSpawn = GameObject.FindGameObjectWithTag(spawnPointTag);
            }
            catch (UnityException)
            {
                Debug.LogWarning($"[CharacterManager] Tag '{spawnPointTag}' chưa được tạo trong Tag Manager, bỏ qua tìm bằng tag.");
            }

            if (taggedSpawn != null)
            {
                spawnPoint = taggedSpawn.transform;
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(spawnPointName))
        {
            GameObject namedSpawn = GameObject.Find(spawnPointName);
            if (namedSpawn != null)
            {
                spawnPoint = namedSpawn.transform;
                return true;
            }
        }

        return false;
    }

    /// <summary>Overload: chọn theo tên</summary>
    public void ApplyCharacter(string characterName)
    {
        if (CharacterDict.TryGetValue(characterName, out CharacterData data))
            ApplyCharacter(data);
        else
            Debug.LogError($"[CharacterManager] Không tìm thấy nhân vật: '{characterName}'");
    }

    /// <summary>Overload: chọn theo index trong list</summary>
    public void ApplyCharacter(int index)
    {
        if (index < 0 || index >= CharacterList.Count)
        {
            Debug.LogError($"[CharacterManager] Index {index} ngoài phạm vi (0 - {CharacterList.Count - 1})");
            return;
        }
        ApplyCharacter(CharacterList[index]);
    }

    // ─────────────────────────────────────────
    //  APPLY STATS
    // ─────────────────────────────────────────

    void ApplyStatsToPlayer(GameObject player, CharacterData data)
    {
        // ── PlayerHealth ──────────────────────
        //var health = player.GetComponent<PlayerHealth>();
        //if (health != null)
        //    health.SetHP((int)data.Health, (int)data.Health);

        // ── PlayerStats ───────────────────────
        var stats = player.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.maxHP = (int)data.Health;
            stats.currentHP = (int)data.Health;
            stats.magnetMultiplier = data.Magnet;
        }

        // ── PlayerMovement ────────────────────
        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
            movement.moveSpeed = data.Speed;

        // ── SkillSpawner ──────────────────────
        // Spawn skill riêng của nhân vật nếu có
        if (data.skillPlayer != null)
        {
            var skillObj = Instantiate(data.skillPlayer, player.transform);
            skillObj.name = $"Skill_{data.characterName}";

            var spawner = skillObj.GetComponent<SkillSpawner>();
            if (spawner != null)
                spawner.player = player.transform;
        }

        Debug.Log($"[CharacterManager] Stats applied → HP:{data.Health} | ATK:{data.Attack} | " +
                  $"SPD:{data.Speed} | ARM:{data.Armor} | MAG:{data.Magnet}");
    }

    public void DisplayCharacterInSpellBook(int currentPage)
    {
        //var page = currentPage * 2;

        //GameObject PRCS1 = _listCharacterSelector[0]?.gameObject;

        //BookCharacterSelector cs1 = PRCS1.GetComponent<BookCharacterSelector>();
        //cs1.characterData = CharacterList[page];
        //Destroy(PRCS1.transform.GetChild(0).transform.GetChild(0).gameObject);
        //GameObject newCh1 = Instantiate(CharacterList[page].characterPrefab);
        //newCh1.GetComponent<RectTransform>().SetParent(PRCS1.transform.GetChild(0).transform);
        //newCh1.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        //BookPageDisplay pageDisplay1 = PRCS1.GetComponent<BookPageDisplay>();
        //pageDisplay1.Display(CharacterList[page]);
        //if ((page +1) >= CharacterList.Count)
        //{
        //    return;
        //}

        //GameObject PRCS2 = _listCharacterSelector[1]?.gameObject;
        //BookCharacterSelector cs2 = PRCS2.GetComponent<BookCharacterSelector>();
        //Destroy(PRCS2.transform.GetChild(0).transform.GetChild(0).gameObject);
        //_listCharacterSelector[1].characterData = CharacterList[page + 1];
        //GameObject newCh2 = Instantiate(CharacterList[page + 1].characterPrefab);
        //newCh2.GetComponent<RectTransform>().SetParent(PRCS2.transform.GetChild(0).transform);
        //newCh2.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        //BookPageDisplay pageDisplay2 = PRCS2.GetComponent<BookPageDisplay>();
        //pageDisplay2.Display(CharacterList[page+1]);

    }

    // ─────────────────────────────────────────
    //  QUERY HELPERS
    // ─────────────────────────────────────────

    /// <summary>Lấy CharacterData theo tên, trả null nếu không có</summary>
    public CharacterData GetByName(string characterName)
    {
        CharacterDict.TryGetValue(characterName, out CharacterData data);
        return data;
    }

    /// <summary>Kiểm tra nhân vật có tồn tại không</summary>
    public bool HasCharacter(string characterName)
        => CharacterDict.ContainsKey(characterName);

    /// <summary>Tổng số nhân vật đã load</summary>
    public int CharacterCount => CharacterList.Count;


    public void Display(CharacterData data)
    {
        if (data == null) return;
        // Text
        characterNameTxt.text = data.characterName;
        //descriptionTxt.text = data.ActiveDescription;
        hpValueTxt.text = data.Health.ToString("F0");
        attackValueTxt.text = data.Attack.ToString("F0");
        armorValueTxt.text = data.Armor.ToString("F0");
        speedValueTxt.text = data.Speed.ToString("F0");
        recovery.text = data.Recovery.ToString();
    }
}