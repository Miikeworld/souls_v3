using UnityEngine;

public class CharacterGround : MonoBehaviour
{
    [Header("Settings")]
    public float groundOffset = 0f; // Adjust this to fine-tune
    public LayerMask groundLayer; // Set to "Ground" layer
    public bool autoAlign = true;
    
    void Start()
    {
        if (autoAlign)
        {
            AlignToGround();
        }
    }
    
    void AlignToGround()
    {
        // Raycast down to find ground
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out hit, 10f, groundLayer))
        {
            // Position character on ground
            Vector3 newPos = transform.localPosition;
            newPos.y = hit.point.y - transform.parent.position.y + groundOffset;
            transform.localPosition = newPos;
            
            Debug.Log("Character aligned to ground at Y: " + newPos.y);
        }
        else
        {
            // No ground found, use offset
            Vector3 newPos = transform.localPosition;
            newPos.y = groundOffset;
            transform.localPosition = newPos;
            
            Debug.LogWarning("No ground found, using offset: " + groundOffset);
        }
    }
}
