using UnityEngine;

public class ShopVendor : MonoBehaviour
{
    private bool messageShown = false;
    private float resetTimer = 0f;
    private const float RESET_TIME = 2.0f; // Time before message can be shown again

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Proximity message removed. Handled by PlayerMovement interaction.
    }
    
    // Optional: Logic to re-enable message if player stays or leaves?
    // User Requirement: "Player overlaps -> Message". 
    // Implementation: Trigger Enter. 
    
    private void Update()
    {
        if (messageShown)
        {
            resetTimer -= Time.deltaTime;
            if (resetTimer <= 0)
            {
                messageShown = false;
            }
        }
    }
}
