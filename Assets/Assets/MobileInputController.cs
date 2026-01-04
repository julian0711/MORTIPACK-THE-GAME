using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class MobileInputController : MonoBehaviour
{
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button interactButton; // Added Interact Button
    [FormerlySerializedAs("menuButton")]
    [SerializeField] private Button inventoryButton; // Renamed from menuButton
    [SerializeField] private Button closeInventoryButton; // Added Close Inventory Button
    
    private Vector2 movementDirection = Vector2.zero;
    private bool isInteractPressed = false;
    private PlayerMovement playerMovement;
    
    [Header("UI Settings")]
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private float buttonSize = 100f;
    [SerializeField] private float spacing = 10f;
    [SerializeField] private Vector2 dpadPosition = new Vector2(100, 100);
    
    [Header("ボタン発光設定")]
    [SerializeField] private Color buttonFlashColor = Color.green;
    [SerializeField] private float flashDuration = 0.1f;
    
    private RectTransform dpadRect;

    private void Start()
    {
        // 0. Ensure EventSystem exists (Critical for UI interaction)
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
             GameObject eventSystem = new GameObject("EventSystem");
             eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
             eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
             Debug.Log("[MobileInputController] Auto-created EventSystem (was missing).");
        }

        // 1. Dynamic Rebinding: Find UI elements if ANY references are missing
        if (upButton == null || downButton == null || leftButton == null || rightButton == null || 
            interactButton == null || inventoryButton == null)
        {
            RebindUIElements();
        }

        playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
        
        SetupMovementButton(upButton, Vector2.up);
        SetupMovementButton(downButton, Vector2.down);
        SetupMovementButton(leftButton, Vector2.left);
        SetupMovementButton(rightButton, Vector2.right);
        
        if (interactButton != null)
        {
            // Use EventTrigger for Hold Detection
            UnityEngine.EventSystems.EventTrigger trigger = interactButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null) trigger = interactButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            trigger.triggers.Clear();

            // Down
            UnityEngine.EventSystems.EventTrigger.Entry entryDown = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            entryDown.callback.AddListener((data) => { 
                isInteractHeld = true; 
                StartCoroutine(FlashButton(interactButton.GetComponent<Image>()));
            });
            trigger.triggers.Add(entryDown);

            // Up
            UnityEngine.EventSystems.EventTrigger.Entry entryUp = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
            entryUp.callback.AddListener((data) => { isInteractHeld = false; });
            trigger.triggers.Add(entryUp);
            
            // Also keep visual flash safely? Or remove Button component's control?
            // Usually Button interferes with EventTrigger if not careful, but PointerDown works.
        }

        SetupInventoryButton(inventoryButton);
        SetupInventoryButton(inventoryButton);
        SetupInventoryButton(closeInventoryButton);
        
        // Disable built-in transitions to prevent conflict with FlashButton
        DisableButtonTransition(upButton);
        DisableButtonTransition(downButton);
        DisableButtonTransition(leftButton);
        DisableButtonTransition(rightButton);
        DisableButtonTransition(interactButton);
        DisableButtonTransition(inventoryButton);
        DisableButtonTransition(closeInventoryButton);
    }
    
    private void DisableButtonTransition(Button btn)
    {
        if (btn != null)
        {
            btn.transition = Selectable.Transition.None;
        }
    }

    private void RebindUIElements()
    {
        Debug.Log("[MobileInputController] Rebinding UI elements...");
        GameObject canvasObj = GameObject.Find("MobileControlsCanvas");
        if (canvasObj != null)
        {
            Transform canvasTr = canvasObj.transform;
            
            // D-Pad
            Transform dpad = canvasTr.Find("D-Pad");
            if (dpad != null)
            {
                if (upButton == null) upButton = dpad.Find("Up")?.GetComponent<Button>();
                if (downButton == null) downButton = dpad.Find("Down")?.GetComponent<Button>();
                if (leftButton == null) leftButton = dpad.Find("Left")?.GetComponent<Button>();
                if (rightButton == null) rightButton = dpad.Find("Right")?.GetComponent<Button>();
            }
            
            // Action Buttons
            Transform actions = canvasTr.Find("ActionButtons");
            if (actions != null)
            {
                if (interactButton == null) interactButton = actions.Find("Search")?.GetComponent<Button>();
                if (inventoryButton == null) inventoryButton = actions.Find("Inventory")?.GetComponent<Button>();
                
                // Try finding CloseButton if it exists (might be in InventoryPanel inside GameUIManager? Or separate?)
                // Assuming it's not critical for Search button bug.
            }
            
            Debug.Log($"[MobileInputController] Rebound: Search={interactButton!=null}, Up={upButton!=null}");
        }
        else
        {
            Debug.LogWarning("[MobileInputController] MobileControlsCanvas not found during rebind!");
        }
    }

    private void SetupInventoryButton(Button btn)
    {
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                // Directly toggle inventory via GameUIManager
                if (GameUIManager.Instance != null)
                {
                    GameUIManager.Instance.ToggleInventoryScreen();
                    StartCoroutine(FlashButton(btn.GetComponent<Image>()));
                }
                else
                {
                    Debug.LogWarning("[MobileInput] GameUIManager not found!");
                }
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
    // UI生成ロジック (ContextMenuから実行)
    // Refactored to be non-destructive: Finds existing elements first.
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
        Transform dpadTransform = canvas.transform.Find("D-Pad");
        if (dpadTransform == null)
        {
            GameObject dpadObj = new GameObject("D-Pad");
            dpadObj.transform.SetParent(canvas.transform, false);
            dpadRect = dpadObj.AddComponent<RectTransform>();
            dpadRect.anchorMin = Vector2.zero;
            dpadRect.anchorMax = Vector2.zero;
            dpadRect.pivot = Vector2.zero;
            dpadRect.anchoredPosition = dpadPosition;
            dpadTransform = dpadObj.transform;

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(dpadObj, "Create D-Pad");
#endif
            dpadRect.sizeDelta = new Vector2(330, 330); // 100*3 + 10*3 approx
        }
        else
        {
             dpadRect = dpadTransform.GetComponent<RectTransform>();
        }

        float size = buttonSize;
        float space = spacing;

        upButton = CreateOrGetButton("Up", dpadTransform, new Vector2(size + space, (size + space) * 2), size);
        downButton = CreateOrGetButton("Down", dpadTransform, new Vector2(size + space, 0), size);
        leftButton = CreateOrGetButton("Left", dpadTransform, new Vector2(0, size + space), size);
        rightButton = CreateOrGetButton("Right", dpadTransform, new Vector2((size + space) * 2, size + space), size);


        // 4. Action Buttons (右下)
        Transform actionsTransform = canvas.transform.Find("ActionButtons");
        RectTransform actionsRect;
        if (actionsTransform == null)
        {
            GameObject actionsObj = new GameObject("ActionButtons");
            actionsObj.transform.SetParent(canvas.transform, false);
            actionsRect = actionsObj.AddComponent<RectTransform>();
            actionsRect.anchorMin = new Vector2(1, 0); // 右下
            actionsRect.anchorMax = new Vector2(1, 0);
            actionsRect.pivot = new Vector2(1, 0);
            actionsRect.anchoredPosition = new Vector2(-100, 100);
            actionsRect.sizeDelta = new Vector2(300, 300);
            actionsTransform = actionsObj.transform;
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(actionsObj, "Create Actions");
#endif
        }
        else
        {
            actionsRect = actionsTransform.GetComponent<RectTransform>();
        }

        // Sprite自動設定 (Editor only) - Only if not set
#if UNITY_EDITOR
        if (buttonSprite == null)
        {
            string spritePath = "Assets/Sprites/button_control.png";
            Object[] allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(spritePath);
            foreach (var asset in allAssets)
            {
                if (asset is Sprite sp)
                {
                    buttonSprite = sp;
                    break; 
                }
            }
            if (buttonSprite == null)
            {
                 // Fallback search
                 string[] guids = UnityEditor.AssetDatabase.FindAssets("button_control t:Sprite");
                 if (guids.Length > 0)
                 {
                     string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                     allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                     foreach (var asset in allAssets)
                     {
                        if (asset is Sprite sp)
                        {
                            buttonSprite = sp;
                            break;
                        }
                     }
                 }
            }
            if (buttonSprite != null)
            {
                 UnityEditor.Undo.RecordObject(this, "Auto-assign Button Sprite");
                 Debug.Log($"[MobileInput] Auto-assigned sprite: {buttonSprite.name}");
            }
        }
#endif

        interactButton = CreateOrGetButton("Search", actionsTransform, new Vector2(-150, 0), 120f);
        interactButton = CreateOrGetButton("Search", actionsTransform, new Vector2(-150, 0), 120f);
        
        // Handling Legacy "Option" button if exists
        Transform oldOption = actionsTransform.Find("Option");
        if (oldOption != null)
        {
             oldOption.name = "Inventory";
             Text t = oldOption.GetComponentInChildren<Text>();
             if (t != null && t.text == "Option") t.text = "Inventory";
             Debug.Log("[MobileInput] Renamed legacy 'Option' button to 'Inventory'");
        }

        inventoryButton = CreateOrGetButton("Inventory", actionsTransform, new Vector2(0, 150), 100f);
        SetupInventoryButton(inventoryButton);
        
        Debug.Log("Mobile UI Updated (Existing elements preserved, new ones added)!");
    }

    private Button CreateOrGetButton(string name, Transform parent, Vector2 pos, float size)
    {
        // Try find existing first
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing.GetComponent<Button>();
        }

        // Create new if not found
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

    private bool isInteractHeld = false;
    public bool GetInteractHeld()
    {
        return isInteractHeld;
    }
    
    private System.Collections.IEnumerator FlashButton(Image img)
    {
        if (img == null) yield break;
        Color original = img.color;
        img.color = buttonFlashColor;
        yield return new WaitForSeconds(flashDuration);
        img.color = original;
    }
}
