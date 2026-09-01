using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 10f;
    public Entity owner;
    
    void OnCollisionEnter(Collision collision)
    {
        Entity target = collision.gameObject.GetComponent<Entity>();
        
        if (target != null && target != owner)
        {
            target.TakeDamage(damage, owner);
        }
        
        Destroy(gameObject);
    }
}
