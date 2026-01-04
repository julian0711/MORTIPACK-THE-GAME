using UnityEngine;
using UnityEditor;

public class StageBuilderTools : MonoBehaviour
{
    [MenuItem("Tools/Stage Builder/Create Normal Door")]
    public static void CreateNormalDoor()
    {
        // 1. Create GameObject
        GameObject door = new GameObject("Door_Normal");
        
        // 2. Add BoxCollider2D (Trigger)
        BoxCollider2D col = door.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1, 1); // Default tile size

        // 3. Add SpriteRenderer with Real Sprite
        SpriteRenderer sr = door.AddComponent<SpriteRenderer>();
        
        // Try to load the actual door sprite
        Sprite doorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/door.png");
        if (doorSprite != null)
        {
            sr.sprite = doorSprite;
            sr.color = Color.white; // Reset color so sprite is visible as-is
        }
        else
        {
            // Fallback if sprite not found
            Debug.LogWarning("door.png not found at Assets/Sprites/door.png, using placeholder.");
            Texture2D tex = Texture2D.whiteTexture;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f); 
            sr.color = new Color(0.6f, 0.3f, 0f, 1f);
        }
        
        sr.sortingOrder = 10; // Ensure it renders on top of floor

        // 4. Position (Fix Z to -1 for visibility)
        Vector3 pos = Vector3.zero;
        if (SceneView.lastActiveSceneView != null)
        {
            pos = SceneView.lastActiveSceneView.pivot;
        }
        pos.z = -1.0f; // Ensure it's in front of background
        door.transform.position = pos;

        // 5. Parent to Selected Object (The Stage Root)
        if (Selection.activeGameObject != null)
        {
            door.transform.SetParent(Selection.activeGameObject.transform, false);
            // Reset local position if needed, or keep world pos. 
            // Here we want to keep the position we calculated relative to scene view, 
            // but usually users select the Grid/Stage.
            // Let's rely on the SceneView pivot logic above setting the World Position, 
            // so SetParent with worldPositionStays=true (default) is fine.
        }

        // 6. Select it
        Selection.activeGameObject = door;
        Undo.RegisterCreatedObjectUndo(door, "Create Normal Door");
        
        Debug.Log("Created 'Door_Normal'. Please ensure it is inside your Stage Prefab!");
    }

    [MenuItem("Tools/Stage Builder/Create Shop Door")]
    public static void CreateShopDoor()
    {
        GameObject door = new GameObject("Door_Shop_Entrance");
        
        BoxCollider2D col = door.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1, 1);

        SpriteRenderer sr = door.AddComponent<SpriteRenderer>();
        
        // Try to load the actual shop door sprite
        Sprite shopDoorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/door_shop.png");
        if (shopDoorSprite != null)
        {
            sr.sprite = shopDoorSprite;
            sr.color = Color.white;
        }
        else
        {
            Debug.LogWarning("door_shop.png not found at Assets/Sprites/door_shop.png, using placeholder.");
            Texture2D tex = Texture2D.whiteTexture;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            sr.color = new Color(0f, 0f, 0.8f, 1f); // Blueish
        }
        
        sr.sortingOrder = 10;

        Vector3 pos = Vector3.zero;
        if (SceneView.lastActiveSceneView != null)
        {
            pos = SceneView.lastActiveSceneView.pivot;
        }
        pos.z = -1.0f; // Fix Z
        door.transform.position = pos;
        
        if (Selection.activeGameObject != null)
        {
            door.transform.SetParent(Selection.activeGameObject.transform, true);
        }

        Selection.activeGameObject = door;
        Undo.RegisterCreatedObjectUndo(door, "Create Shop Door");

        Debug.Log("Created 'Door_Shop_Entrance'. Please ensure it is inside your Stage Prefab!");
    }

    [MenuItem("Tools/Stage Builder/Create Shop Item Spot")]
    public static void CreateShopItemSpot()
    {
        GameObject spot = new GameObject("ShopItemSpot");
        
        // Add a visual marker (Sprite) so the user can see it
        SpriteRenderer sr = spot.AddComponent<SpriteRenderer>();
        // Use a simple circle or box if available, or just a colored rect
        Texture2D tex = Texture2D.whiteTexture;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        sr.color = new Color(1f, 0f, 0f, 0.5f); // Semi-transparent Red
        sr.sortingOrder = 5; // Below items (usually 10+, items 20?)

        Vector3 pos = Vector3.zero;
        if (SceneView.lastActiveSceneView != null)
        {
            pos = SceneView.lastActiveSceneView.pivot;
        }
        pos.z = -1.0f; // Standard object depth
        spot.transform.position = pos;
        
        if (Selection.activeGameObject != null)
        {
            spot.transform.SetParent(Selection.activeGameObject.transform, true);
        }

        Selection.activeGameObject = spot;
        Undo.RegisterCreatedObjectUndo(spot, "Create Shop Item Spot");

        Debug.Log("Created 'ShopItemSpot'. Place this where you want a shop item to spawn.");
    }
}
