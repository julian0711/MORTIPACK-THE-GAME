using UnityEngine;

public class BreathingAnimation : MonoBehaviour
{
    [SerializeField] private float interval = 0.5f;
    
    private Sprite sprite1;
    private Sprite sprite2;
    private SpriteRenderer spriteRenderer;
    private float timer;
    private bool isSprite1 = true;
    
    public void Setup(Sprite s1, Sprite s2, float animationInterval = 0.5f)
    {
        sprite1 = s1;
        sprite2 = s2;
        interval = animationInterval;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        if (sprite1 != null)
        {
            spriteRenderer.sprite = sprite1;
        }
        
        Debug.Log($"[BreathingAnimation] Setup complete for {gameObject.name}. S1: {sprite1?.name}, S2: {sprite2?.name}, Interval: {interval}");
    }
    
    private void Update()
    {
        if (sprite1 == null || sprite2 == null || spriteRenderer == null) return;
        
        timer += Time.deltaTime;
        
        if (timer >= interval)
        {
            timer = 0f;
            isSprite1 = !isSprite1;
            
            spriteRenderer.sprite = isSprite1 ? sprite1 : sprite2;
            // Debug.Log($"[BreathingAnimation] Swapped sprite for {gameObject.name}. Current: {(isSprite1 ? "Sprite1" : "Sprite2")}");
        }
    }
}
