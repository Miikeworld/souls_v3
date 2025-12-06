using UnityEngine;

public class LockOnSystem : MonoBehaviour
{
    [Header("Settings")]
    public float lockOnRange = 20f;
    public string enemyTag = "Enemy";
    
    [Header("References")]
    public Transform player;
    public CameraFollow cameraFollow;
    
    [HideInInspector] public Transform currentTarget;
    private bool isLockedOn = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            if (!isLockedOn)
            {
                LockOnToClosestEnemy();
            }
            else
            {
                ReleaseLockOn();
            }
        }
        
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
        
        if (enemies.Length == 0) return;
        
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
            Transform lockPoint = closestEnemy.Find("LockOnPoint");
            currentTarget = lockPoint != null ? lockPoint : closestEnemy;
            
            isLockedOn = true;
            cameraFollow.SetLockOnTarget(currentTarget);
            
            Debug.Log("Locked onto: " + closestEnemy.name);
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
