using UnityEngine;

/// <summary>
/// Creates an invisible arena boundary that prevents the player and boss from leaving
/// the fight area until the boss is defeated. The boundary consists of colliders that
/// act as walls. When the boss dies, the boundary is automatically disabled.
/// </summary>
public class ArenaBoundary : MonoBehaviour
{
    [Header("Boss Reference")]
    [Tooltip("The boss entity. When this boss dies, the boundary will be disabled.")]
    public Entity boss;

    [Header("Boundary Settings")]
    [Tooltip("Size of the arena boundary (x = width, z = depth, y = wall height).")]
    public Vector3 arenaSize = new Vector3(30f, 10f, 30f);
    
    [Tooltip("If true, automatically creates box colliders to form the boundary walls.")]
    public bool autoCreateColliders = true;

    [Header("Manual Colliders (optional)")]
    [Tooltip("If autoCreateColliders is false, assign these pre-made wall colliders.")]
    public Collider[] boundaryColliders;

    private Collider[] createdColliders;
    private bool wasAlive = true;

    void Start()
    {
        if (boss != null)
        {
            wasAlive = boss.currentHealth > 0f;
        }

        if (autoCreateColliders)
        {
            CreateBoundaryColliders();
        }
        else if (boundaryColliders != null && boundaryColliders.Length > 0)
        {
            createdColliders = boundaryColliders;
        }
    }

    void Update()
    {
        if (boss == null) return;

        bool isAlive = boss.currentHealth > 0f;

        // Detect when boss dies
        if (wasAlive && !isAlive)
        {
            DisableBoundary();
        }

        wasAlive = isAlive;
    }

    /// <summary>
    /// Automatically creates 4 box colliders to form walls around the arena.
    /// Walls are positioned at the edges of the arenaSize.
    /// </summary>
    void CreateBoundaryColliders()
    {
        createdColliders = new Collider[4];
        string[] wallNames = { "Wall_North", "Wall_South", "Wall_East", "Wall_West" };

        for (int i = 0; i < 4; i++)
        {
            GameObject wall = new GameObject(wallNames[i]);
            wall.transform.SetParent(transform);
            wall.layer = gameObject.layer;

            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.isTrigger = false;

            // Position walls at arena edges
            Vector3 pos = Vector3.zero;
            Vector3 size = Vector3.zero;

            switch (i)
            {
                case 0: // North (positive Z)
                    pos = new Vector3(0f, arenaSize.y / 2f, arenaSize.z / 2f);
                    size = new Vector3(arenaSize.x, arenaSize.y, 1f);
                    break;
                case 1: // South (negative Z)
                    pos = new Vector3(0f, arenaSize.y / 2f, -arenaSize.z / 2f);
                    size = new Vector3(arenaSize.x, arenaSize.y, 1f);
                    break;
                case 2: // East (positive X)
                    pos = new Vector3(arenaSize.x / 2f, arenaSize.y / 2f, 0f);
                    size = new Vector3(1f, arenaSize.y, arenaSize.z);
                    break;
                case 3: // West (negative X)
                    pos = new Vector3(-arenaSize.x / 2f, arenaSize.y / 2f, 0f);
                    size = new Vector3(1f, arenaSize.y, arenaSize.z);
                    break;
            }

            wall.transform.localPosition = pos;
            collider.size = size;
            createdColliders[i] = collider;
        }

        Debug.Log($"[ArenaBoundary] Created {createdColliders.Length} boundary walls");
    }

    /// <summary>
    /// Disables all boundary colliders, allowing the player to leave the arena.
    /// Called automatically when the boss dies.
    /// </summary>
    public void DisableBoundary()
    {
        if (createdColliders == null || createdColliders.Length == 0) return;

        foreach (Collider col in createdColliders)
        {
            if (col != null)
                col.enabled = false;
        }

        Debug.Log("[ArenaBoundary] Boundary disabled (boss defeated)");
    }

    /// <summary>
    /// Manually enable the boundary (e.g., for boss phase transitions).
    /// </summary>
    public void EnableBoundary()
    {
        if (createdColliders == null || createdColliders.Length == 0) return;

        foreach (Collider col in createdColliders)
        {
            if (col != null)
                col.enabled = true;
        }

        Debug.Log("[ArenaBoundary] Boundary enabled");
    }

    void OnDrawGizmosSelected()
    {
        // Visualize the arena boundary in the editor
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawCube(transform.position, arenaSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, arenaSize);
    }
}
