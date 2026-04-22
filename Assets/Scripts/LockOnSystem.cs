using UnityEngine;

public class LockOnSystem : MonoBehaviour
{
    [Header("Settings")]
    public float lockOnRange = 20f;
    public string enemyTag = "Enemy";
    
    [Header("References")]
    public Transform player;
    [Tooltip("Assign the Cinemachine camera manager (preferred). Falls back to CameraFollow if null.")]
    public CinemachineLockOnCamera cinemachineCam;
    public CameraFollow cameraFollow;

    [HideInInspector] public Transform currentTarget;
    private bool isLockedOn = false;
    private int lockedEnemyInstanceId; // Store instance ID to track enemy through teleportation

    void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            if (!isLockedOn)
                LockOnToClosestEnemy();
            else
                ReleaseLockOn();
        }

        // Check if lock-on target still exists, try to re-acquire by instance ID if lost
        if (isLockedOn)
        {
            if (currentTarget == null || currentTarget.gameObject == null || !currentTarget.gameObject.activeInHierarchy)
            {
                // Try to find enemy by instance ID (handles teleportation GameObject recreation)
                GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
                foreach (GameObject enemy in enemies)
                {
                    if (enemy.GetInstanceID() == lockedEnemyInstanceId)
                    {
                        // Re-acquire lock-on point
                        Transform lockPoint = enemy.transform.Find("LockOnPoint");
                        currentTarget = lockPoint != null ? lockPoint : enemy.transform;

                        // Re-notify cameras
                        if (cinemachineCam != null)
                            cinemachineCam.SetLockOnTarget(currentTarget);
                        else if (cameraFollow != null)
                            cameraFollow.SetLockOnTarget(currentTarget);

                        return;
                    }
                }
                // Enemy truly gone
                ReleaseLockOn();
            }
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
            lockedEnemyInstanceId = closestEnemy.gameObject.GetInstanceID(); // Store instance ID for teleportation tracking

            if (cinemachineCam != null)
                cinemachineCam.SetLockOnTarget(currentTarget);
            else if (cameraFollow != null)
                cameraFollow.SetLockOnTarget(currentTarget);

            Debug.Log("Locked onto: " + closestEnemy.name);
        }
    }

    public void ReleaseLockOn()
    {
        isLockedOn = false;
        currentTarget = null;
        lockedEnemyInstanceId = 0;

        if (cinemachineCam != null)
            cinemachineCam.ClearLockOn();
        else if (cameraFollow != null)
            cameraFollow.SetLockOnTarget(null);

        Debug.Log("Lock-on released");
    }
}
