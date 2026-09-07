using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game State")]
    public string currentSceneName;
    public bool hasCreatedCharacter = false;
    
    [Header("Respawn System")]
    public Bonfire lastBonfire;
    public Vector3 respawnPosition;
    public Quaternion respawnRotation;
    [Tooltip("Optional: a transform to respawn at if the player has not rested at a bonfire yet. If left empty, the player's current position at first spawn is used as the fallback.")]
    public Transform defaultRespawnPoint;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // If the player has a saved character from a previous session, keep the flag set.
            hasCreatedCharacter = PlayerPrefs.HasKey("CharacterGender");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
        InitializeCurrentScene(currentSceneName);
    }

    // empty OnGUI stops unity from stripping old mouse input in builds with "Both" active
    void OnGUI() { }

    void InitializeCurrentScene(string sceneName)
    {
        switch (sceneName)
        {
            case "CharacterCreation":
                InitializeCharacterCreation();
                break;
            case "Fantasy":
                InitializeGameplay();
                break;
            case "Hub":
                InitializeHub();
                break;
        }
    }
    
    private void InitializeCharacterCreation()
    {
        Debug.Log("Initializing Character Creation scene");
    }
    
    private void InitializeGameplay()
    {
        Debug.Log("Initializing Fantasy scene - hasCreatedCharacter: " + hasCreatedCharacter);

        // load player if we have save data, or if one is already in the scene for testing
        if (hasCreatedCharacter || GameObject.FindWithTag("Player") != null)
        {
            LoadPlayerCharacter();
        }
        else
        {
            Debug.LogWarning("No character data found! Starting CharacterCreation first.");
            LoadCharacterCreation();
            return;
        }

        // make sure boss bar exists even when the singleton GM kills the saved one
        EnsureBossHealthBar();

        // make sure main cam exists so player/camera can work
        EnsureMainCamera();
    }
    
    private void InitializeHub()
    {
        Debug.Log("Initializing Hub scene");

        // Load player character in hub
        if (hasCreatedCharacter || GameObject.FindWithTag("Player") != null)
        {
            LoadPlayerCharacter();
            EnsureMainCamera();
        }
    }
    
    private void LoadPlayerCharacter()
    {
        // Try to find existing player in scene
        GameObject existingPlayer = GameObject.FindWithTag("Player");
        
        if (existingPlayer == null)
        {
            Debug.Log("No player found in scene, loading from CharacterLoader");
            
            // Use your existing CharacterLoader
            CharacterLoader loader = FindObjectOfType<CharacterLoader>();
            if (loader != null)
            {
                loader.LoadCharacter();
            }
            else
            {
                Debug.LogError("CharacterLoader not found in scene!");
                CreateDefaultPlayer();
            }
        }
        else
        {
            Debug.Log("Player already exists in scene");
        }
    }
    
    private void CreateDefaultPlayer()
    {
        Debug.LogWarning("Creating default player as fallback");

        // Pick a starting point if one exists
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (defaultRespawnPoint != null)
        {
            spawnPos = defaultRespawnPoint.position;
            spawnRot = defaultRespawnPoint.rotation;
        }
        else
        {
            Transform fallback = FindDefaultStartPoint();
            if (fallback != null)
            {
                spawnPos = fallback.position;
                spawnRot = fallback.rotation;
            }
        }

        // Create a basic player if all else fails
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = spawnPos;
        player.transform.rotation = spawnRot;

        // Add essential components
        player.AddComponent<CharacterController>();
        player.AddComponent<PlayerController>();

        // Add basic visual (capsule)
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.transform.parent = player.transform;
        capsule.transform.localPosition = Vector3.zero;
        capsule.name = "PlayerModel";
    }
    
    // Scene loading methods
    public void LoadCharacterCreation()
    {
        LoadingScreen.LoadScene("CharacterCreation", "Creating hero...");
    }

    void EnsureBossHealthBar()
    {
        if (FindAnyObjectByType<BossHealthBarUI>() == null)
        {
            GameObject barGO = new GameObject("BossHealthBarUI");
            barGO.AddComponent<BossHealthBarUI>();
            Debug.Log("[GameManager] Created runtime BossHealthBarUI.");
        }
    }

    void EnsureMainCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.tag = "MainCamera";
                Debug.Log("[GameManager] Tagged existing camera as MainCamera.");
            }
            else
            {
                GameObject camGO = new GameObject("MainCamera");
                camGO.tag = "MainCamera";
                cam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
                camGO.AddComponent<CameraFollow>();
                Debug.LogWarning("[GameManager] No camera found in scene. Created a fallback MainCamera.");
            }
        }

        // make sure the active camera can actually follow/rotate
        if (cam != null && cam.GetComponent<CameraFollow>() == null)
        {
            cam.gameObject.AddComponent<CameraFollow>();
            Debug.Log("[GameManager] Added CameraFollow to the main camera.");
        }
    }

    public void LoadFantasy()
    {
        LoadingScreen.LoadScene("Fantasy", "Entering the realm...");
    }

    public void LoadHub()
    {
        LoadingScreen.LoadScene("Hub", "Travelling...");
    }
    
    // Called by CharacterCreationController when character is created
    public void OnCharacterCreated()
    {
        hasCreatedCharacter = true;
        Debug.Log("Character creation completed, flag set");
    }
    
    public void SetRespawnPoint(Bonfire bonfire)
    {
        lastBonfire = bonfire;
        if (bonfire.respawnPoint != null)
        {
            respawnPosition = bonfire.respawnPoint.position;
            respawnRotation = bonfire.respawnPoint.rotation;
        }
        else
        {
            respawnPosition = bonfire.transform.position;
            respawnRotation = bonfire.transform.rotation;
        }
        
        Debug.Log("Respawn point set at: " + bonfire.bonfireName);
    }
    
    Transform FindDefaultStartPoint()
    {
        string[] names = { "PlayerStart", "StartPoint", "SpawnPoint", "Player Spawn" };
        foreach (string n in names)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) return go.transform;
        }

        GameObject tagGo = GameObject.FindWithTag("Respawn");
        if (tagGo != null) return tagGo.transform;

        return null;
    }

    public void RespawnPlayer(GameObject player)
    {
        ResetBoss();
        if (lastBonfire != null)
        {
            // Spawn slightly higher to avoid getting stuck in bonfire
            Vector3 spawnPos = respawnPosition + Vector3.up * 1.5f;
            player.transform.position = spawnPos;
            player.transform.rotation = respawnRotation;
        }
        else
        {
            // No bonfire visited yet — use the default start point if set.
            // If that is also missing, look for a "PlayerStart"/"StartPoint" object.'s current position so they
            // at least don't get stuck.
            if (defaultRespawnPoint != null)
            {
                player.transform.position = defaultRespawnPoint.position + Vector3.up * 1.5f;
                player.transform.rotation = defaultRespawnPoint.rotation;
                Debug.Log("Player respawned at default start point.");
            }
            else
            {
                Transform fallback = FindDefaultStartPoint();
                if (fallback != null)
                {
                    player.transform.position = fallback.position + Vector3.up * 1.5f;
                    player.transform.rotation = fallback.rotation;
                    Debug.Log("Player respawned at starting point: " + fallback.name);
                }
                else
                {
                    Debug.LogWarning("No bonfire or starting point found for respawn; respawning at current position.");
                }
            }
        }

        // Reset player state
        Entity playerEntity = player.GetComponent<Entity>();
        if (playerEntity != null)
        {
            playerEntity.isDead = false;
            playerEntity.currentHealth = playerEntity.maxHealth;
            playerEntity.currentStamina = playerEntity.maxStamina;
            playerEntity.currentMana = playerEntity.maxMana;
            playerEntity.RestorePotions();
            playerEntity.InvokeResourceEvents();
        }

        // Re-enable character controller
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = true;
        }
    }
    
    void ResetBoss()
    {
        BossController boss = FindAnyObjectByType<BossController>();
        if (boss != null)
        {
            boss.ResetToSpawn();
            Debug.Log("Boss reset to initial position.");
        }
    }

    public void RespawnEnemies()
    {
        // Respawn all non-boss enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            BossController boss = enemy.GetComponent<BossController>();
            if (boss == null) // Don't respawn bosses
            {
                enemy.SetActive(true);
                Entity enemyEntity = enemy.GetComponent<Entity>();
                if (enemyEntity != null)
                {
                    enemyEntity.currentHealth = enemyEntity.maxHealth;
                    enemyEntity.isDead = false;
                    enemyEntity.InvokeResourceEvents();
                }
            }
        }
        
        Debug.Log("Enemies respawned");
    }
    
    // Scene change events
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        Debug.Log("Scene loaded: " + currentSceneName);

        // This object is DontDestroyOnLoad, so Start only runs once.
        // Re-initialize the new scene here so the player/boss/etc are set up
        // when Fantasy is loaded from MainMenu/CharacterCreation.
        InitializeCurrentScene(currentSceneName);
    }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
