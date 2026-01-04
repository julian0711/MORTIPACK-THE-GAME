using UnityEngine;

public class SpecialTile : MonoBehaviour
{
    private SpriteRenderer sr;
    [SerializeField] private bool isRevealed = false; // Kept for Inspector debug

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Default State: Invisible
        Hide();
    }

    public void Hide()
    {
        if (sr != null)
        {
            Color c = Color.white;
            c.a = 0f; // Transparent
            sr.color = c;
            sr.sortingOrder = 5; // Back to floor level
        }
        isRevealed = false;
    }

    public void Reveal()
    {
        Debug.Log($"[SpecialTile] Reveal called on {this.name} at {transform.position}");
        if (sr != null)
        {
            Color c = Color.white;
            c.a = 1f; // Visible
            sr.color = c;
            sr.sortingOrder = 101; // Above Fog (Order 100)
        }
        else
        {
            Debug.LogError($"[SpecialTile] SpriteRenderer is missing on {this.name}!");
        }
        isRevealed = true;
    }
    
    // Optional: Debug helper
    private bool loadingFromFixedStage = false; 
    private bool forceReveal = false;
}
