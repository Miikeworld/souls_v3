using UnityEngine;

public class BossSetupHelper : MonoBehaviour
{
    [Header("Auto Setup")]
    public bool autoSetupOnStart = true;
    public bool createPhaseModels = true;
    public bool createSpawnPoints = true;
    
    void Start()
    {
        if (!autoSetupOnStart) return;
        
        SetupBossComponents();
        SetupPhaseModels();
        SetupSpawnPoints();
        
        Debug.Log("Boss setup complete!");
    }
    
    void SetupBossComponents()
    {
        // Add required components if missing
        if (GetComponent<BossController>() == null)
            gameObject.AddComponent<BossController>();
            
        if (GetComponent<CharacterController>() == null)
            gameObject.AddComponent<CharacterController>();
            
        if (GetComponent<Animator>() == null)
            gameObject.AddComponent<Animator>();
            
        if (GetComponent<AudioSource>() == null)
            gameObject.AddComponent<AudioSource>();
            
        if (GetComponent<Collider>() == null)
        {
            SphereCollider col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;
        }
    }
    
    void SetupPhaseModels()
    {
        if (!createPhaseModels) return;
        
        // Create phase model placeholders
        string[] phaseNames = {"Phase1_Mage", "Phase2_Blade", "Phase3_Samurai", "Phase4_LastResort"};
        
        for (int i = 0; i < phaseNames.Length; i++)
        {
            Transform existingPhase = transform.Find(phaseNames[i]);
            if (existingPhase == null)
            {
                GameObject phaseModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                phaseModel.name = phaseNames[i];
                phaseModel.transform.SetParent(transform);
                phaseModel.transform.localPosition = Vector3.zero;
                phaseModel.SetActive(false); // Start with all disabled
            }
        }
    }
    
    void SetupSpawnPoints()
    {
        if (!createSpawnPoints) return;
        
        // Create spawn point for projectiles
        Transform existingSpawnPoint = transform.Find("ProjectileSpawnPoint");
        if (existingSpawnPoint == null)
        {
            GameObject spawnPoint = new GameObject("ProjectileSpawnPoint");
            spawnPoint.transform.SetParent(transform);
            spawnPoint.transform.localPosition = new Vector3(0, 1f, 1f);
        }
    }
}
