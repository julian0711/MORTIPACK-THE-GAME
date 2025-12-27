using UnityEngine;

public class MobileGameCamera : MonoBehaviour
{
    [SerializeField] private float fieldOfView = 60f;
    [SerializeField] private float nearClipPlane = 0.3f;
    [SerializeField] private float farClipPlane = 1000f;
    [SerializeField] private bool useOrthographic = true;
    [SerializeField] private float orthographicSize = 5f;
    
    private Transform playerTransform;

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        
        // Ensure AudioListener exists for sound
        if (GetComponent<AudioListener>() == null)
        {
            gameObject.AddComponent<AudioListener>();
            Debug.Log("[MobileGameCamera] Auto-added AudioListener.");
        }

        cam.orthographic = useOrthographic;
        
        if (useOrthographic)
        {
            cam.orthographicSize = orthographicSize;
        }
        else
        {
            cam.fieldOfView = fieldOfView;
        }
        
        cam.nearClipPlane = nearClipPlane;
        cam.farClipPlane = farClipPlane;
        
        // プレイヤーを検索
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            // プレイヤーの位置にカメラを移動（Z軸は-10のまま）
            Vector3 playerPos = playerTransform.position;
            transform.position = new Vector3(playerPos.x, playerPos.y, -10f);
            Debug.Log($"Camera initialized at player position: {transform.position}");
        }
        else
        {
            Debug.LogWarning("Player not found! Camera will stay at default position.");
        }
    }

    void LateUpdate()
    {
        // プレイヤーを常に追従
        if (playerTransform != null)
        {
            Vector3 playerPos = playerTransform.position;
            // 即座に追従（Z軸は-10固定）
            transform.position = new Vector3(playerPos.x, playerPos.y, -10f);
        }
        else
        {
            // プレイヤーが見つからない場合は再検索
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }
}