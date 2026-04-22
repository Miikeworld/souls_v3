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
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        
        // Handle scene-specific initialization
        switch (currentSceneName)
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
        
        // Load player character if data exists
        if (hasCreatedCharacter)
        {
            LoadPlayerCharacter();
        }
        else
        {
            Debug.LogWarning("No character data found! Starting CharacterCreation first.");
            LoadCharacterCreation();
        }
    }
    
    private void InitializeHub()
    {
        Debug.Log("Initializing Hub scene");
        
        // Load player character in hub
        if (hasCreatedCharacter)
        {
            LoadPlayerCharacter();
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
        
        // Create a basic player if all else fails
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        
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
        SceneManager.LoadScene("CharacterCreation");
    }
    
    public void LoadFantasy()
    {
        SceneManager.LoadScene("Fantasy");
    }
    
    public void LoadHub()
    {
        SceneManager.LoadScene("Hub");
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
    
    public void RespawnPlayer(GameObject player)
    {
        if (lastBonfire != null)
        {
            // Spawn slightly higher to avoid getting stuck in bonfire
            Vector3 spawnPos = respawnPosition + Vector3.up * 1.5f;
            player.transform.position = spawnPos;
            player.transform.rotation = respawnRotation;
            
            // Reset player state
            Entity playerEntity = player.GetComponent<Entity>();
            PlayerController playerController = player.GetComponent<PlayerController>();
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
            if (playerController != null)
            {
                CharacterController controller = player.GetComponent<CharacterController>();
                if (controller != null)
                    controller.enabled = true;
            }
            
            Debug.Log("Player respawned at bonfire: " + lastBonfire.bonfireName);
        }
        else
        {
            Debug.LogWarning("No bonfire set for respawn!");
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
