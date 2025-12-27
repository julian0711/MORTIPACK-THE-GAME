using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameUIManager : MonoBehaviour // Forced Refresh 2
{
    private static GameUIManager _instance;
    public static GameUIManager Instance 
    { 
        get 
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameUIManager>();
                if (_instance == null)
                {
                     // Lazy instantiation: Create if missing
                     GameObject go = new GameObject("GameUIManager");
                     _instance = go.AddComponent<GameUIManager>();
                     Debug.Log("[GameUIManager] Auto-created instance via Lazy Instantiation.");
                }
            }
            return _instance;
        }
    }

    [SerializeField] private Text messageText;

    [SerializeField] private Canvas canvas;
    [SerializeField] private Font customFont;
    [SerializeField] private Text floorText;
    [SerializeField] private GameObject resultScreenPanel;
    [SerializeField] private Button nextFloorButton;

    public int CurrentFloor { get; private set; } = 1;
    
    // Score Variables
    public int TotalScore { get; private set; } = 0;
    public int StageScore { get; private set; } = 0;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    // ... (Existing code) ...

    private void UpdateFloorText()
    {
        if (floorText != null)
        {
            // Format: Score:100 (Stage:100) B1
            floorText.text = $"Score:{TotalScore} (Stage:{StageScore}) B{CurrentFloor}";
            // Reduce font size slightly if text gets too long, or rely on AutoSizing if set (currently using fixed 48)
            // For now, assume it fits or text wrap handles it (though right aligned).
        }
    }
    
    public void AddScore(int points)
    {
        TotalScore += points;
        StageScore += points;
        UpdateFloorText();
        Debug.Log($"[Score] Added {points}. Total: {TotalScore}, Stage: {StageScore}");
    }

    // ... (Transition Logic) ...

    public void ProceedToNextFloor()
    {
        StartCoroutine(LoadNextFloorRoutine());
    }

    private System.Collections.IEnumerator LoadNextFloorRoutine()
    {
        // Bonus for Stage Clear
        AddScore(300);
        
        // Wait for the specified delay
        yield return new WaitForSeconds(sceneLoadDelay);

        CurrentFloor++;
        StageScore = 0; // Reset Stage Score for new floor
        UpdateFloorText(); // Refresh UI before load/during fade
        
        if (InventoryManager.Instance != null) InventoryManager.Instance.RemoveItem("key");
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    [SerializeField] private RectTransform uiBoxItem;
    [SerializeField] private RectTransform uiBoxKey; // 鍵専用のボックス

    private bool isInventoryConnected = false;

    private void Start()
    {
        InitializeUI();
    }

    private void Update()
    {
        // Retry connection until successful
        if (!isInventoryConnected)
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged += UpdateInventoryUI;
                isInventoryConnected = true;
                UpdateInventoryUI(); // Force initial sync
                Debug.Log("[GameUIManager] Connected to InventoryManager and updated UI");
            }
        }
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateInventoryUI;
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeUI();
        UpdateInventoryUI(); // Force refresh to prevent empty inventory flicker
    }

    private void InitializeUI()
    {
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                CreateCanvas();
            }
        }

        // Ensure EventSystem exists for UI interaction
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[GameUIManager] Auto-created EventSystem.");
        }

        CreateLetterbox(); // Add Black Bars

        // Validate assigned uiBoxItem (Ignore Prefab Assets)
        if (uiBoxItem != null && !uiBoxItem.gameObject.scene.IsValid())
        {
            Debug.LogWarning("[GameUIManager] Assigned uiBoxItem is a Prefab Asset (not in scene). Ignoring and finding/creating new one.");
            uiBoxItem = null;
        }

        // Find or Create UI_box_item
        if (uiBoxItem == null)
        {
            uiBoxItem = FindOrCreateLoopBox("UI_box_item", new Vector2(0.5f, 0.8f), new Vector2(0.95f, 0.95f));
        }

        // Validate assigned uiBoxKey
        if (uiBoxKey != null && !uiBoxKey.gameObject.scene.IsValid())
        {
             uiBoxKey = null;
        }

        // Find or Create UI_box_key
        if (uiBoxKey == null)
        {
            // UI_box_keyの探索・作成
            // 配置はユーザーの指定場所があるかもしれないので探索優先
            uiBoxKey = FindOrCreateLoopBox("UI_box_key", new Vector2(0.05f, 0.8f), new Vector2(0.3f, 0.95f), true);
        }

        if (messageText == null)
        {
            Transform textObj = canvas.transform.Find("MessageText");
            if (textObj != null)
            {
                messageText = textObj.GetComponent<Text>();
            }
            else
            {
                CreateMessageText();
            }
        }
        


        if (floorText == null)
        {
             Transform floorObj = canvas.transform.Find("FloorText");
             if (floorObj != null)
             {
                 floorText = floorObj.GetComponent<Text>();
             }
             else
             {
                 CreateFloorText();
             }
        }
        UpdateFloorText();

        if (resultScreenPanel == null)
        {
            Transform resultObj = canvas.transform.Find("ResultScreen");
            if (resultObj != null)
            {
                resultScreenPanel = resultObj.gameObject;
                Transform btnObj = resultScreenPanel.transform.Find("NextButton");
                if (btnObj != null) nextFloorButton = btnObj.GetComponent<Button>();
            }
            else
            {
                CreateResultScreen();
            }
        }

        // Ensure button functionality regardless of how it was created/assigned
        if (nextFloorButton != null)
        {
            nextFloorButton.onClick.RemoveAllListeners();
            nextFloorButton.onClick.AddListener(ProceedToNextFloor);
        }
    }

    // Helper to reduce code duplication
    private RectTransform FindOrCreateLoopBox(string boxName, Vector2 anchorMin, Vector2 anchorMax, bool isLeftAlign = false)
    {
        Transform box = canvas.transform.Find(boxName);
        if (box == null)
        {
            foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == boxName)
                {
                    box = t;
                    break;
                }
            }
        }

        if (box != null)
        {
            RectTransform rt = box.GetComponent<RectTransform>();
            EnsureLayoutGroup(rt, isLeftAlign);
            return rt;
        }
        else
        {
            return CreateBox(boxName, anchorMin, anchorMax, isLeftAlign);
        }
    }

    private void EnsureLayoutGroup(RectTransform rt, bool isLeftAlign)
    {
        HorizontalLayoutGroup layout = rt.GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        
        layout.childAlignment = isLeftAlign ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
        if (layout.childForceExpandWidth) layout.childForceExpandWidth = false;
    }

    private RectTransform CreateBox(string boxName, Vector2 anchorMin, Vector2 anchorMax, bool isLeftAlign)
    {
        if (canvas == null) return null;
        GameObject boxObj = new GameObject(boxName, typeof(RectTransform));
        boxObj.transform.SetParent(canvas.transform, false);
        RectTransform rt = boxObj.GetComponent<RectTransform>();
        
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = isLeftAlign ? new Vector2(0, 1) : new Vector2(1, 1);
        rt.sizeDelta = Vector2.zero;

        HorizontalLayoutGroup layout = boxObj.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = isLeftAlign ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 20;

        return rt;
    }

    [SerializeField] private RectTransform topBarPanel;
    [SerializeField] private RectTransform bottomBarPanel;

    private void CreateLetterbox()
    {
        if (canvas == null) return;

        // Top Bar
        if (topBarPanel == null)
        {
            // Try to find existing first
            Transform existing = canvas.transform.Find("TopBar");
            if (existing != null)
            {
                topBarPanel = existing.GetComponent<RectTransform>();
            }
            else
            {
                // Create new
                GameObject topObj = new GameObject("TopBar");
                topObj.transform.SetParent(canvas.transform, false);
                Image topImage = topObj.AddComponent<Image>();
                topImage.color = Color.black;
                topImage.raycastTarget = false;

                topBarPanel = topObj.GetComponent<RectTransform>();
                topBarPanel.anchorMin = new Vector2(0, 1);
                topBarPanel.anchorMax = new Vector2(1, 1);
                topBarPanel.pivot = new Vector2(0.5f, 1);
                topBarPanel.sizeDelta = new Vector2(0, 120); // Height 120
                topBarPanel.anchoredPosition = Vector2.zero;
                
                // Only push to back if we created it (assume manual placement is correct otherwise)
                topObj.transform.SetSiblingIndex(0);
            }
        }

        // Bottom Bar
        if (bottomBarPanel == null)
        {
            // Try to find existing first
            Transform existing = canvas.transform.Find("BottomBar");
            if (existing != null)
            {
                bottomBarPanel = existing.GetComponent<RectTransform>();
            }
            else
            {
                GameObject bottomObj = new GameObject("BottomBar");
                bottomObj.transform.SetParent(canvas.transform, false);
                Image bottomImage = bottomObj.AddComponent<Image>();
                bottomImage.color = Color.black;
                bottomImage.raycastTarget = false;

                bottomBarPanel = bottomObj.GetComponent<RectTransform>();
                bottomBarPanel.anchorMin = new Vector2(0, 0);
                bottomBarPanel.anchorMax = new Vector2(1, 0);
                bottomBarPanel.pivot = new Vector2(0.5f, 0);
                bottomBarPanel.sizeDelta = new Vector2(0, 120); // Height 120
                bottomBarPanel.anchoredPosition = Vector2.zero;

                // Only push to back if we created it
                bottomObj.transform.SetSiblingIndex(0);
            }
        }
    }

    // Deprecated CreateUIBoxItem replaced by generic helper, keeping method signature if needed or removing internal call
    // Removed specific CreateUIBoxItem to use helper logic inside InitializeUI

    private void UpdateInventoryUI()
    {
        // Self-repair: Ensure UI is initialized before updating
        if (uiBoxItem == null || uiBoxKey == null) InitializeUI();
        
        // Clear existing children
        if (uiBoxItem != null)
        {
            foreach (Transform child in uiBoxItem) Destroy(child.gameObject);
        }
        if (uiBoxKey != null)
        {
            foreach (Transform child in uiBoxKey) Destroy(child.gameObject);
        }

        if (uiBoxItem == null && uiBoxKey == null) return;

        if (InventoryManager.Instance == null)
        {
             return;
        }

        Dictionary<string, int> inventory = InventoryManager.Instance.GetInventory();

        foreach (var kvp in inventory)
        {
            string key = kvp.Key;
            int count = kvp.Value;
            
            Debug.Log($"[GameUIManager] Processing item: {key}, Count: {count}");

            // Decide parent based on item key
            RectTransform targetParent = uiBoxItem;
            if (key == "key")
            {
                targetParent = uiBoxKey;
            }

            if (targetParent == null) continue;

            // Create Item Slot
            GameObject slot = new GameObject($"ItemSlot_{key}");
            slot.transform.SetParent(targetParent, false);
            
            RectTransform slotRT = slot.AddComponent<RectTransform>();
            slotRT.sizeDelta = new Vector2(80, 80); // Fixed slot size

            // Add transparent Image for Raycast target
            Image slotImage = slot.AddComponent<Image>();
            slotImage.color = Color.clear;
            slotImage.raycastTarget = true; // Explicitly enable

            // Add ItemSlotHandler for interaction (Click & LongPress)
            ItemSlotHandler handler = slot.AddComponent<ItemSlotHandler>();
            
            handler.OnClick = () => {
                ItemDatabase itemMgr = ItemDatabase.Instance;
                if (itemMgr == null) itemMgr = FindObjectOfType<ItemDatabase>();

                if (itemMgr != null)
                {
                    string desc = itemMgr.GetItemDescription(key);
                    if (!string.IsNullOrEmpty(desc))
                    {
                        ShowMessage(desc, key);
                    }
                    else
                    {
                        ShowMessage($"{key} (No description)", key);
                    }
                }
            };
            
            // OnLongPress: Use Item
            handler.OnLongPress = () => {
                UseItem(key);
            };

            // Image
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slot.transform, false);
            Image icon = iconObj.AddComponent<Image>();
            icon.sprite = LoadItemSprite(key);
            icon.preserveAspect = true;
            RectTransform iconRT = icon.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.sizeDelta = Vector2.zero;
            
            // Allow click to pass through image if needed, but Button is on parent
            icon.raycastTarget = false; 

            // Count Text (if > 1)
            if (count > 1)
            {
                GameObject textObj = new GameObject("CountText");
                textObj.transform.SetParent(slot.transform, false);
                Text countText = textObj.AddComponent<Text>();
                countText.text = $"x{count}";
                countText.font = customFont != null ? customFont : Font.CreateDynamicFontFromOSFont("Arial", 24);
                countText.fontSize = 24;
                countText.alignment = TextAnchor.LowerRight;
                countText.color = Color.white;
                countText.raycastTarget = false;
                
                Outline ol = textObj.AddComponent<Outline>();
                ol.effectColor = Color.black;
                
                RectTransform textRT = countText.rectTransform;
                textRT.anchorMin = new Vector2(0.5f, 0);
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = new Vector2(-5, 5); // Padding
            }
        }
    }


    private void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("UICanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
    }

    private void CreateMessageText()
    {
        if (canvas == null) return;

        GameObject textObj = new GameObject("MessageText");
        textObj.transform.SetParent(canvas.transform, false);
        
        messageText = textObj.AddComponent<Text>();
        messageText.font = customFont != null ? customFont : Font.CreateDynamicFontFromOSFont("Arial", 48);
        messageText.fontSize = 48;
        messageText.alignment = TextAnchor.LowerCenter;
        messageText.color = Color.white;
        
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        RectTransform rectTransform = messageText.rectTransform;
        rectTransform.anchorMin = new Vector2(0.1f, 0.05f);
        rectTransform.anchorMax = new Vector2(0.9f, 0.2f);
        rectTransform.pivot = new Vector2(0.5f, 0);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        
        messageText.raycastTarget = false; // Disable blocking clicks
        messageText.text = "";
    }
    


    private void CreateFloorText()
    {
        if (canvas == null) return;

        GameObject textObj = new GameObject("FloorText");
        textObj.transform.SetParent(canvas.transform, false);
        
        floorText = textObj.AddComponent<Text>();
        floorText.font = customFont != null ? customFont : Font.CreateDynamicFontFromOSFont("Arial", 48);
        floorText.fontSize = 48;
        floorText.alignment = TextAnchor.UpperRight;
        floorText.color = Color.white;
        floorText.horizontalOverflow = HorizontalWrapMode.Overflow;
        
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        RectTransform rectTransform = floorText.rectTransform;
        // Top Right
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(1, 1);
        rectTransform.anchoredPosition = new Vector2(-20, -20); // Padding
        rectTransform.sizeDelta = new Vector2(1000, 100); // Expanded width for Score text
        
        UpdateFloorText();
    }

    // [Removed duplicate UpdateFloorText]

    private void GenerateResultScreenInEditor()
    {
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) CreateCanvas();
        CreateResultScreen();
        // Ensure the reference is saved in Editor
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    private void CreateResultScreen()
    {
        if (canvas == null) return;

        // Check if already exists to avoid duplicates when clicking multiple times
        Transform existing = canvas.transform.Find("ResultScreen");
        if (existing != null)
        {
            resultScreenPanel = existing.gameObject;
            return;
        }

        // Panel
        resultScreenPanel = new GameObject("ResultScreen", typeof(RectTransform));
        resultScreenPanel.transform.SetParent(canvas.transform, false);
        Image panelImage = resultScreenPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f); // Dark semi-transparent
        
        RectTransform rt = resultScreenPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        // Button
        GameObject btnObj = new GameObject("NextButton", typeof(RectTransform));
        btnObj.transform.SetParent(resultScreenPanel.transform, false);
        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = Color.white;
        
        nextFloorButton = btnObj.AddComponent<Button>();
        // Note: In Editor mode, AddListener doesn't persist consistently for runtime events if not serialized, 
        // but CreateResultScreen is also called at runtime. 
        // For Editor-generated objects, it's better if the user assigns the OnClick event or we do it at runtime start.
        // We will keep the runtime hook in InitializeUI or Awake if possible, but here we add it for runtime generation.
        nextFloorButton.onClick.AddListener(ProceedToNextFloor);
        
        RectTransform btnRT = btnObj.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRT.sizeDelta = new Vector2(1500, 500); // 5x size
        btnRT.anchoredPosition = Vector2.zero;

        // Button Text
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(btnObj.transform, false);
        Text btnText = textObj.AddComponent<Text>();
        btnText.text = "NEXT FLOOR";
        btnText.font = customFont != null ? customFont : Font.CreateDynamicFontFromOSFont("Arial", 160); // 5x size
        
        // Force Point filtering for sharp edges (pixel perfect)
        if (btnText.font != null && btnText.font.material != null && btnText.font.material.mainTexture != null)
        {
            btnText.font.material.mainTexture.filterMode = FilterMode.Point;
        }

        btnText.fontSize = 160; // 5x size
        btnText.color = Color.black; // Text color
        btnText.alignment = TextAnchor.MiddleCenter;

        // Add Outline for "crisp" look (White text with black outline is typical, but here text is black. 
        // If user wants outline, maybe they want white text? keeping black for now as per previous code, 
        // but Adding Outline component often helps visibility if valid.)
        // Actually, if text is black, Outline should be white? Or maybe no outline? 
        // The user complained about "blurry outline". If there WAS NO outline, they wouldn't say "outline is blurry".
        // They might mean the character edges. 
        // But for "Next Floor", let's add a white outline to make it "pop" if it's black text? 
        // Or keep it simple. Let's add the code to force FilterMode.Point, that's the main "crispness" fix.
        // And add an Outline component just in case they want that style, with large distance.
        
        Outline ol = textObj.AddComponent<Outline>();
        ol.effectColor = Color.white;
        ol.effectDistance = new Vector2(4, -4); // Moderate outline
        
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        resultScreenPanel.SetActive(false);
    }

    public void ShowResultScreen()
    {
        if (resultScreenPanel == null) InitializeUI();
        
        resultScreenPanel.SetActive(true);
        
        // Disable Player Control
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null)
        {
            pm.SetInputEnabled(false);
        }
    }

    [SerializeField] private float sceneLoadDelay = 1.0f; // Editable in Inspector

    // [Removed duplicate UpdateFloorText, ProceedToNextFloor, LoadNextFloorRoutine]
    
    public void ShowGameOverScreen()
    {
        if (resultScreenPanel == null) InitializeUI();
        
        resultScreenPanel.SetActive(true);
        
        // Change text to GAME OVER
        Transform btnObj = resultScreenPanel.transform.Find("NextButton");
        if (btnObj != null)
        {
            Transform textObj = btnObj.Find("Text");
            if (textObj != null)
            {
                Text btnText = textObj.GetComponent<Text>();
                if (btnText != null) btnText.text = "RETRY";
            }
            
            // Re-bind button to Restart
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(RestartGame);
            }
        }
        
        // Game Over Message on screen
        ShowMessage("GAME OVER", "death");

        // Disable Player Control
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null)
        {
            pm.SetInputEnabled(false);
        }
    }

    public void RestartGame()
    {
        // Reset Logic
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ClearInventory();
        }
        
        // Reset Floor? (User said "Start from B1")
        // CurrentFloor property has a private setter, we might need to reset it or reloading scene logic handles it if it's not static.
        // CurrentFloor is an instance property, so reloading scene resets it unless GameUIManager is DontDestroyOnLoad.
        // It IS DontDestroyOnLoad. So we must reset it manually.
        // But setter is private. I need to change it or add a reset method.
        // Since I can't easily change the property in this Replace block without targeting the top, I'll use reflection or just assume CurrentFloor = 1 works if I change the property definition or just edit logic.
        // Wait, I can't edit the property definition here easily.
        // Actually, I can just destroy the GameUIManager instance so it gets recreated? No, that's risky.
        // I will use a dirty hack or separate edit to reset floor if needed. 
        // Let's check line 37: public int CurrentFloor { get; private set; } = 1;
        // I'll assume for now I can figure out how to reset it, or I'll just load the scene and GameUIManager persists... 
        // If I can't set CurrentFloor, detailed floor display might be wrong.
        // I'll add a separate edit to change the setter to public or add a method later if needed.
        // For now:
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
        // But wait, if GameUIManager persists, CurrentFloor persists.
        
        // I will implement the method now and deal with CurrentFloor separately or in another edit.
        // Actually, let's try to reset it via a new method or assume I'll fix the property later.
        
        StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        Debug.Log("[GameUIManager] Restarting Game...");
        yield return new WaitForSeconds(0.5f);
        
        // Reset Floor Number directly (internal access allowed)
        CurrentFloor = 1;
        
        // We do NOT destroy GameUIManager because it holds the code running this coroutine!
        // Instead, we just reload the scene. The state reset (Inventory clear + Floor reset) is sufficient.
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ShowMessage(string text, string itemKey = null)
    {
        // Debug.Log($"[GameUIManager] Show: '{text}', Item: {itemKey}");
        
        if (messageText == null) 
        {
            InitializeUI();
        }
        
        if (messageText != null)
        {
            messageText.gameObject.SetActive(true); // Ensure active
            messageText.text = text;
            
            // Validate Font
            if (messageText.font == null) 
            {
                 messageText.font = customFont != null ? customFont : Font.CreateDynamicFontFromOSFont("Arial", 48);
            }
        }
    }
    

    public void ShowFloatingItem(string itemKey, Vector3 playerPosition)
    {
        if (string.IsNullOrEmpty(itemKey) || itemKey == "nothing") return;

        Sprite sprite = LoadItemSprite(itemKey);
        if (sprite == null) return;

        // Create simplistic object in world space (not UI canvas)
        GameObject floatObj = new GameObject($"FloatingItem_{itemKey}");
        
        // Initial position: Slightly above player
        floatObj.transform.position = playerPosition + new Vector3(0, 1.0f, 0);
        floatObj.transform.localScale = new Vector3(3.0f, 3.0f, 1f); // Increased size from 1.5 to 3.0

        SpriteRenderer sr = floatObj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 32767; // Ensure it renders on top of most things

        floatObj.AddComponent<FloatingItemAnimation>();
    }

    private Sprite LoadItemSprite(string key)
    {
        // Default naming: "item_" + key
        // Special case: "key" -> "key"
        // Special case: "warpcoin" -> "item_coin"
        
        string filename = "";
        
        switch (key)
        {
            case "key": filename = "key"; break;
            case "warpcoin": filename = "item_coin"; break;
            case "doll": filename = "item_migawari"; break;
            case "radar": filename = "item_radar"; break;
            case "hallucinogen": filename = "item_radar"; break; // User specified to use radar image
            case "report": filename = "item_report"; break;
            case "radio": filename = "item_radio"; break;
            default: filename = "item_" + key; break;
        }
        
        // Load from Assets/Resources/item/
        string path = "item/" + filename;
        Sprite sprite = Resources.Load<Sprite>(path);
        
        if (sprite == null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0)
            {
                sprite = sprites[0];
            }
        }

        if (sprite == null)
        {
             Debug.LogWarning($"[GameUIManager] Failed to load sprite for key: {key} at path: {path}");
        }

        return sprite;
    }

    private void UseItem(string key)
    {
        Debug.Log($"[GameUIManager] Attempting to use item: {key}");
        
        switch (key)
        {
            case "warpcoin":
                DungeonGeneratorV2 dungeon = FindObjectOfType<DungeonGeneratorV2>();
                PlayerMovement player = FindObjectOfType<PlayerMovement>();
                
                if (dungeon != null && player != null)
                {
                    Vector3 bestPos = Vector3.zero;
                    bool foundSafePos = false;

                    // 安全なワープ先（エネミーがいない場所）を探す
                    // 最大20回試行する
                    for (int i = 0; i < 20; i++)
                    {
                        Vector3 candidatePos = dungeon.GetRandomWalkablePosition();
                        Collider2D[] hits = Physics2D.OverlapCircleAll(candidatePos, 0.4f);
                        bool hasEnemy = false;
                        foreach (var hit in hits)
                        {
                            if (hit.GetComponent<EnemyMovement>() != null)
                            {
                                hasEnemy = true;
                                break;
                            }
                        }

                        if (!hasEnemy)
                        {
                            bestPos = candidatePos;
                            foundSafePos = true;
                            break;
                        }
                    }

                    if (foundSafePos)
                    {
                        if (GlobalSoundManager.Instance != null && GlobalSoundManager.Instance.warpSE != null)
                        {
                            GlobalSoundManager.Instance.PlaySE(GlobalSoundManager.Instance.warpSE);
                        }
                        StartCoroutine(player.WarpSequence(bestPos));
                        string msg = ItemDatabase.Instance.GetItemUsageMessage(key);
                        if (string.IsNullOrEmpty(msg)) msg = "ワープした！";
                        ShowMessage(msg, key);
                    }
                    else
                    {
                        ShowMessage("ワープに失敗した。", key);
                    }
                }
                break;

            case "warp_gun":
                PlayerMovement pm = FindObjectOfType<PlayerMovement>();
                if (pm != null)
                {
                    pm.EnterShootingMode();
                    string msg = ItemDatabase.Instance.GetItemUsageMessage(key);
                    if (string.IsNullOrEmpty(msg)) msg = "転送銃を構えた。方向キーで発射！";
                    ShowMessage(msg, key);
                }
                break;

            case "radio":
                TurnManager turnManager = FindObjectOfType<TurnManager>();
                if (turnManager != null)
                {
                    if (GlobalSoundManager.Instance != null && GlobalSoundManager.Instance.radioSE != null)
                    {
                        GlobalSoundManager.Instance.PlaySE(GlobalSoundManager.Instance.radioSE);
                    }
                    turnManager.StunAllEnemies(10);
                    string msg = ItemDatabase.Instance.GetItemUsageMessage(key);
                    if (string.IsNullOrEmpty(msg)) msg = "怪音波を流した！全敵10ターン停止！";
                    ShowMessage(msg, key);
                    InventoryManager.Instance.RemoveItem(key);
                }
                break;

            case "map": 
                DungeonGeneratorV2 dungeonMap = FindObjectOfType<DungeonGeneratorV2>();
                if (dungeonMap != null)
                {
                    dungeonMap.RevealRandomAreas(3);
                    string msg = ItemDatabase.Instance.GetItemUsageMessage(key);
                    if (string.IsNullOrEmpty(msg)) msg = "周辺の地図が書き込まれた！";
                    ShowMessage(msg, key);
                }
                break;
                
            case "radar":
                DungeonGeneratorV2 dGenRadar = FindObjectOfType<DungeonGeneratorV2>();
                if (dGenRadar != null)
                {
                    dGenRadar.ActivateRadar(10); // 10 Turns
                    string msg = ItemDatabase.Instance.GetItemUsageMessage(key);
                    if (string.IsNullOrEmpty(msg)) msg = "エネミーの気配が可視化された！（10ターン）";
                    ShowMessage(msg, key);
                    if (InventoryManager.Instance != null) InventoryManager.Instance.RemoveItem(key);
                }
                break;

            case "talisman":
                DungeonGeneratorV2 dGenTalisman = FindObjectOfType<DungeonGeneratorV2>();
                if (dGenTalisman != null)
                {
                    int banished = dGenTalisman.BanishEnemies();
                    if (banished > 0)
                    {
                        string msg = ItemDatabase.Instance.GetItemUsageMessage(key);
                        if (string.IsNullOrEmpty(msg)) msg = $"不吉な気配が消えた！（{banished}体消滅）";
                        ShowMessage(msg, key);
                    }
                    else
                    {
                        ShowMessage("しかし何も起きなかった…（エネミー不在）", key);
                    }
                    if (InventoryManager.Instance != null) InventoryManager.Instance.RemoveItem(key);
                }
                break;
                
            case "bluebox":
                PlayerMovement pmBlue = FindObjectOfType<PlayerMovement>();
                DungeonGeneratorV2 dGenBlue = FindObjectOfType<DungeonGeneratorV2>();
                
                if (pmBlue != null && dGenBlue != null)
                {
                    dGenBlue.SpawnShopDoorAt(pmBlue.transform.position);
                    string msg = ItemDatabase.Instance.GetItemUsageMessage(key);
                    if (string.IsNullOrEmpty(msg)) msg = "謎の扉が現れた…";
                    ShowMessage(msg, key);
                    if (InventoryManager.Instance != null) InventoryManager.Instance.RemoveItem(key);
                }
                break;

            default:
                ShowMessage("今は使えないようだ。", key);
                break;
        }


    }
}

public class FloatingItemAnimation : MonoBehaviour
{
    private float moveSpeed = 1.0f;
    private float fadeSpeed = 1.0f;
    private float lifeTime = 1.5f;
    private SpriteRenderer spriteRenderer;
    private Color color;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            color = spriteRenderer.color;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        if (spriteRenderer != null)
        {
            color.a -= fadeSpeed * Time.deltaTime;
            spriteRenderer.color = color;
        }

        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0 || color.a <= 0)
        {
            Destroy(gameObject);
        }
    }
}
