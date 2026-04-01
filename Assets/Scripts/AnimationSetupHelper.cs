using UnityEngine;

public class AnimationSetupHelper : MonoBehaviour
{
    [Header("Auto Setup")]
    public bool setupPlayer = true;
    public bool setupEnemy = true;
    
    void Start()
    {
        if (setupPlayer)
            SetupPlayerAnimator();
            
        if (setupEnemy)
            SetupEnemyAnimator();
    }
    
    void SetupPlayerAnimator()
    {
        Animator animator = GetComponent<Animator>();
        if (animator == null) return;
        
        // Add required parameters if they don't exist
        // Note: This would need to be done in Unity Editor
        Debug.Log("Player Animator Setup - Add these parameters:");
        Debug.Log("Speed (float), IsGrounded (bool), IsJumping (bool)");
        Debug.Log("IsAttacking (bool), IsDodging (bool), AttackType (int)");
        Debug.Log("Hurt (trigger), Die (trigger), IsParrying (bool), IsBlocking (bool)");
    }
    
    void SetupEnemyAnimator()
    {
        Animator animator = GetComponent<Animator>();
        if (animator == null) return;
        
        Debug.Log("Enemy Animator Setup - Add these parameters:");
        Debug.Log("Speed (float), IsAlerted (bool), IsAttacking (bool)");
        Debug.Log("AttackType (int), Hurt (trigger), Alert (trigger)");
        Debug.Log("IsDead (bool)");
    }
}
