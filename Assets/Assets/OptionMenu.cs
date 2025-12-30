using UnityEngine;

public class OptionMenu : MonoBehaviour
{
    // Start is called before the first frame update
    private bool isMenuOpen = false;
    private MobileGameCamera gameCamera;
    
    // Configurable offset distance
    [SerializeField] private float slideDistance = 20f;

    void Start()
    {
        gameCamera = FindObjectOfType<MobileGameCamera>();
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        
        if (gameCamera != null)
        {
            if (isMenuOpen)
            {
                gameCamera.SetCameraOffset(new Vector3(slideDistance, 0, 0));
                Debug.Log("[OptionMenu] Menu Opened. Sliding Camera.");
            }
            else
            {
                gameCamera.SetCameraOffset(Vector3.zero);
                Debug.Log("[OptionMenu] Menu Closed. Resetting Camera.");
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Visualize the target position relative to the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 playerPos = player.transform.position;
            // Target center position (Z is 0 for Gizmo drawing on 2D plane)
            Vector3 targetPos = new Vector3(playerPos.x + slideDistance, playerPos.y, 0f);

            Gizmos.color = Color.yellow;
            
            Camera cam = Camera.main;
            if (cam != null)
            {
                // Draw camera frame
                float height = cam.orthographicSize * 2;
                float width = height * cam.aspect;
                Gizmos.DrawWireCube(targetPos, new Vector3(width, height, 1));
                
                // Draw connection line
                Gizmos.DrawLine(playerPos, targetPos);
            }
        }
    }
}
