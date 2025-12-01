using UnityEngine;

public class LockOnSystem : MonoBehaviour
{
    [Header("Settings")]
    public float lockOnRange = 20f;
    public string enemyTag = "Enemy"; // Use tags instead of layers
    
    [Header("References")]
    public Transform player;
    public CameraFollow cameraFollow;
    
    [HideInInspector] public Transform currentTarget;
    private bool isLockedOn = false;

    void Update()
    {
        // Middle mouse button = Fire2 in old input system
        if (Input.GetMouseButtonDown(2))
        {
            Debug.Log("Middle mouse pressed!");
            
            if (!isLockedOn)
            {
                LockOnToClosestEnemy();
            }
            else
            {
                ReleaseLockOn();
            }
        }
        
        // Auto-release if target too far or destroyed
        if (isLockedOn && currentTarget != null)
        {
            float distance = Vector3.Distance(player.position, currentTarget.position);
            if (distance > lockOnRange)
            {
                ReleaseLockOn();
            }
        }
        
        if (isLockedOn && currentTarget == null)
        {
            ReleaseLockOn();
        }
    }

    void LockOnToClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        
        if (enemies.Length == 0)
        {
            Debug.Log("No enemies found!");
            return;
        }
        
        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;
        
        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(player.position, enemy.transform.position);
            
            if (distance <= lockOnRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }
        
        if (closestEnemy != null)
        {
            // Find lock-on point (child) or use enemy center
            Transform lockPoint = closestEnemy.Find("LockOnPoint");
            currentTarget = lockPoint != null ? lockPoint : closestEnemy;
            
            isLockedOn = true;
            cameraFollow.SetLockOnTarget(currentTarget);
            
            Debug.Log("Locked onto: " + closestEnemy.name);
        }
        else
        {
            Debug.Log("No enemies in range!");
        }
    }

    void ReleaseLockOn()
    {
        isLockedOn = false;
        currentTarget = null;
        cameraFollow.SetLockOnTarget(null);
        
        Debug.Log("Lock-on released");
    }
}
