using UnityEngine;
using UnityEngine.UI;

public class MobileInputController : MonoBehaviour
{
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button interactButton; // Added Interact Button
    
    private Vector2 movementDirection = Vector2.zero;
    private bool isInteractPressed = false;
    private PlayerMovement playerMovement;
    
    [Header("UI Settings")]
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private float buttonSize = 100f;
    [SerializeField] private float spacing = 10f;
    [SerializeField] private Vector2 dpadPosition = new Vector2(100, 100);
    
    private RectTransform dpadRect;

    private void Start()
    {
        playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
        
        SetupMovementButton(upButton, Vector2.up);
        SetupMovementButton(downButton, Vector2.down);
        SetupMovementButton(leftButton, Vector2.left);
        SetupMovementButton(rightButton, Vector2.right);
        
        if (interactButton != null)
        {
            interactButton.onClick.RemoveAllListeners();
            interactButton.onClick.AddListener(() => {
                isInteractPressed = true;
                StartCoroutine(FlashButton(interactButton.GetComponent<Image>()));
            });
        }
    }
    
    private void SetupMovementButton(Button btn, Vector2 dir)
    {
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                SetDirection(dir);
                StartCoroutine(FlashButton(btn.GetComponent<Image>()));
            });
        }
    }
    
    // UI生成ロジック (ContextMenuから実行)
    [ContextMenu("Generate Complete UI")]
    public void GenerateCompleteUI()
    {
        // 1. EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
             GameObject eventSystem = new GameObject("EventSystem");
             eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
             eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#if UNITY_EDITOR
             UnityEditor.Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
#endif
        }

        // 2. Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MobileControlsCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
#endif
        }

        // 3. D-Pad (左下)
        if (dpadRect == null)
        {
            GameObject dpadObj = new GameObject("D-Pad");
            dpadObj.transform.SetParent(canvas.transform, false);
            dpadRect = dpadObj.AddComponent<RectTransform>();
            dpadRect.anchorMin = Vector2.zero;
            dpadRect.anchorMax = Vector2.zero;
            dpadRect.pivot = Vector2.zero;
            dpadRect.anchoredPosition = dpadPosition;
            
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(dpadObj, "Create D-Pad");
#endif
            
            float size = 100f;
            float space = 10f;
            
            upButton = CreateButton("Up", dpadObj.transform, new Vector2(size + space, (size + space) * 2), size);
            downButton = CreateButton("Down", dpadObj.transform, new Vector2(size + space, 0), size);
            leftButton = CreateButton("Left", dpadObj.transform, new Vector2(0, size + space), size);
            rightButton = CreateButton("Right", dpadObj.transform, new Vector2((size + space) * 2, size + space), size);
            
            dpadRect.sizeDelta = new Vector2((size + space) * 3, (size + space) * 3);
        }

        // 4. Action Buttons (右下)
        GameObject actionsObj = new GameObject("ActionButtons");
        actionsObj.transform.SetParent(canvas.transform, false);
        RectTransform actionsRect = actionsObj.AddComponent<RectTransform>();
        actionsRect.anchorMin = new Vector2(1, 0); // 右下
        actionsRect.anchorMax = new Vector2(1, 0);
        actionsRect.pivot = new Vector2(1, 0);
        actionsRect.anchoredPosition = new Vector2(-100, 100);
        actionsRect.sizeDelta = new Vector2(300, 300);
#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(actionsObj, "Create Actions");
#endif

        // Sprite自動設定 (Editor only)
#if UNITY_EDITOR
        if (buttonSprite == null)
        {
            string spritePath = "Assets/Sprites/button_control.png";
            // マルチスプライト対応: 全アセットを読み込んでSprite型を探す
            Object[] allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(spritePath);
            foreach (var asset in allAssets)
            {
                if (asset is Sprite sp)
                {
                    buttonSprite = sp;
                    break; // 最初に見つけたスプライトを使用
                }
            }
            
            if (buttonSprite != null)
            {
                 UnityEditor.Undo.RecordObject(this, "Auto-assign Button Sprite");
                 Debug.Log($"[MobileInput] Auto-assigned sprite: {buttonSprite.name}");
            }
            else
            {
                 // パスで見つからない場合、名前で検索 (t:Spriteで検索すればサブアセットもヒットする)
                 string[] guids = UnityEditor.AssetDatabase.FindAssets("button_control t:Sprite");
                 if (guids.Length > 0)
                 {
                     string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                     // もしテクスチャパスが返ってきたら再度サブアセット検索が必要だが、
                     // FindAssets t:Sprite は通常メインアセットを返してくることがあるので注意。
                     // ここではシンプルにそのパスから再度Spriteを探す
                     allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                     foreach (var asset in allAssets)
                     {
                        if (asset is Sprite sp)
                        {
                            buttonSprite = sp;
                            break;
                        }
                     }
                     
                     if (buttonSprite != null)
                     {
                         UnityEditor.Undo.RecordObject(this, "Auto-assign Button Sprite");
                         Debug.Log($"[MobileInput] Found and assigned sprite from search: {buttonSprite.name}");
                     }
                 }
            }
        }
#endif

        interactButton = CreateButton("Search", actionsObj.transform, new Vector2(-150, 0), 120f);
        CreateButton("Menu", actionsObj.transform, new Vector2(0, 150), 100f);
        
        Debug.Log("Mobile UI Generated Successfully!");
    }

    private Button CreateButton(string name, Transform parent, Vector2 pos, float size)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        Image img = btnObj.AddComponent<Image>();
        if (buttonSprite != null) img.sprite = buttonSprite;
        img.color = new Color(1f, 1f, 1f, 0.5f);
        
        Button btn = btnObj.AddComponent<Button>();
        
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = pos;
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = name;
        
        Font font = null;
#if UNITY_EDITOR
        string fontPath = "Assets/Fonts/PixelMplus10-Regular.ttf";
        font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(fontPath);
#endif
        
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null) font = Resources.FindObjectsOfTypeAll<Font>()[0];
        
        text.font = font;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = 40;
        
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        return btn;
    }
    
    private void SetDirection(Vector2 direction)
    {
        movementDirection = direction;
    }
    
    public Vector2 GetMovementDirection()
    {
        Vector2 dir = movementDirection;
        movementDirection = Vector2.zero;
        return dir;
    }
    
    public bool GetInteractDown()
    {
        if (isInteractPressed)
        {
            isInteractPressed = false;
            return true;
        }
        return false;
    }
    
    private System.Collections.IEnumerator FlashButton(Image img)
    {
        if (img == null) yield break;
        Color original = img.color;
        img.color = Color.green;
        yield return new WaitForSeconds(0.1f);
        img.color = original;
    }
}
