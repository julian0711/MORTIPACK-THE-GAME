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
    [SerializeField] private Text itemNameText; // Added for explicit item name display
    [SerializeField] private Text floorText;
    [SerializeField] private GameObject resultScreenPanel;
    [SerializeField] private Text resultTotalScoreText;
    [SerializeField] private Text resultStageScoreText;
    [SerializeField] private GameObject inventoryScreenPanel; // New Inventory Screen
    [SerializeField] private Text inventoryTotalScoreText; // Added for Total Score in Inventory
    [SerializeField] private Button nextFloorButton;
    [SerializeField] private RectTransform uiBoxItem;
    [SerializeField] private RectTransform uiBoxKey;
    [SerializeField] private Image loadingOverlay; // Reverted to Image for compatibility

    [SerializeField] private Canvas canvas;
    [SerializeField] private Font customFont;
    // private Image loadingOverlay; // This is now serialized as a GameObject above

    // [Removed unused CreateLoadingOverlay method]

    public int CurrentFloor { get; private set; } = 1;
    
    // Score Variables
    public int TotalScore { get; private set; } = 0;
    public int StageScore { get; private set; } = 0;

    private void Awake()
    {
        Debug.Log($"[GameUIManager] Awake() called. _instance is null: {_instance == null}, this: {gameObject.name}");
        
        if (_instance == null)
        {
            _instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("[GameUIManager] Initialized singleton.");
            HandlePersistentUI();
        }
        else if (_instance != this)
        {
            Debug.Log("[GameUIManager] Duplicate instance detected, destroying this one.");
            Destroy(gameObject);
            return;
        }
    }

    private static GameObject persistentUIRoot; // Static reference to the keeper

    private void HandlePersistentUI()
    {
        // Robustly find ALL MobileControllers to handle duplicates
        // Note: GameObject.Find only returns one. We need to check everything.
        // Since MobileController is usually a root object:
        
        List<GameObject> roots = new List<GameObject>();
        SceneManager.GetActiveScene().GetRootGameObjects(roots);
        
        GameObject foundKeeper = null;
        
        foreach (GameObject root in roots)
        {
            if (root.name == "MobileController")
            {
                if (persistentUIRoot == null)
                {
                    // This is our first one, keep it
                    persistentUIRoot = root;
                    DontDestroyOnLoad(root);
                    Debug.Log($"[GameUIManager] Set {root.name} as Persistent Root.");
                    foundKeeper = root;
                }
                else if (root != persistentUIRoot)
                {
                    // This is a duplicate (newly loaded scene version), destroy it
                    Debug.Log($"[GameUIManager] Destroying duplicate MobileController in new scene: {root.name}");
                    Destroy(root);
                }
                else
                {
                    foundKeeper = root;
                }
            }
        }
        
        // If we didn't find the keeper in this scene's roots (because it's in DDOL scene), that's fine.
        // But we must ensure foundKeeper is set if we are capable of finding it.
        // Actually, DDOL objects are not in the Active Scene's root list usually. 
        // So the loop above mainly finds duplication in the NEW scene.
        
        // Special case: If we are logic-only GameUIManager (Separate), check self persistence
        if (persistentUIRoot != null && !transform.IsChildOf(persistentUIRoot.transform) && transform.parent == null)
        {
             DontDestroyOnLoad(gameObject);
        }
    }
    
    private void AssignCameraToCanvas()
    {
        if (canvas == null) return;
        
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
        {
            if (canvas.worldCamera == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    canvas.worldCamera = mainCam;
                    canvas.planeDistance = 10; // Ensure visible
                    Debug.Log($"[GameUIManager] Assigned Camera {mainCam.name} to persistent Canvas.");
                }
                else
                {
                    // Fallback search
                    GameObject camObj = GameObject.Find("MobileGameCamera"); // Check your camera name
                    if (camObj != null) 
                    {
                        canvas.worldCamera = camObj.GetComponent<Camera>();
                        Debug.Log("[GameUIManager] Assigned MobileGameCamera found by name.");
                    }
                }
            }
        }
    }
    
    // ... (Existing code) ...

    private void UpdateFloorText()
    {
        if (floorText == null) InitializeUI();

        if (floorText != null)
        {
            // Format: Score:100 B1 (Shows Stage Score only per user request)
            floorText.text = $"Score:{StageScore} B{CurrentFloor}";
            // Ensure visible
            if (!floorText.gameObject.activeInHierarchy) floorText.gameObject.SetActive(true);
        }
    }
    
    public void AddScore(int points)
    {
        // Only add to Stage Score
        // TotalScore += points;
        StageScore += points;
        UpdateFloorText();
        Debug.Log($"[Score] Added {points}. Total: {TotalScore}, Stage: {StageScore}");
    }

    // ... (Transition Logic) ...

    private Coroutine pulseCoroutine;
    private Vector3 initialTextScale = Vector3.one; // Default fallback

    public void ProceedToNextFloor()
    {
        Debug.Log($"[GameUIManager] ProceedToNextFloor Clicked. Flag: {isTransitioningToNextFloor}, Button: {(nextFloorButton != null ? nextFloorButton.interactable : "NULL")}");
        
        // 1. Double check flag
        if (isTransitioningToNextFloor) 
        {
            Debug.Log("[GameUIManager] Blocked ProceedToNextFloor because isTransitioningToNextFloor is TRUE.");
            return;
        }

        // LOCK IMMEDIATELY to prevent double clicks during delay
        isTransitioningToNextFloor = true;
        
        // 2. Disable button immediately
        if (nextFloorButton != null) nextFloorButton.interactable = false;

        // 3. Stop Pulse & Reset
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        if (nextFloorButton != null)
        {
             Transform textObj = nextFloorButton.transform.Find("Text");
             if (textObj != null) textObj.localScale = initialTextScale; 
        }

        StartCoroutine(LoadNextFloorRoutine());
    }

    private System.Collections.IEnumerator LoadNextFloorRoutine()
    {
        // Bonus moved to ShowResultScreen
        // AddScore(300);
        
        // Show Loading Overlay (Fake fade out)
        if (loadingOverlay != null)
        {
             loadingOverlay.gameObject.SetActive(true);
             loadingOverlay.canvasRenderer.SetAlpha(1.0f);
        }

        // Wait for the specified delay - allow player to see score update briefly if needed, 
        // OR start fading immediately. For now, just wait.
        yield return new WaitForSeconds(sceneLoadDelay);

        CurrentFloor++;
        StageScore = 0; // Reset Stage Score for new floor
        UpdateFloorText(); // Refresh UI before load/during fade
        
        if (InventoryManager.Instance != null) InventoryManager.Instance.RemoveItem("key");
        
        // Hide ResultScreen before scene transition? NO, keep it to cover the map!
        // if (resultScreenPanel != null) resultScreenPanel.SetActive(false);
        // Hide ResultScreen before scene transition? NO, keep it to cover the map!
        // if (resultScreenPanel != null) resultScreenPanel.SetActive(false);
        // isTransitioningToNextFloor = true; // Moved to ProceedToNextFloor for immediate lock 
        
        // Re-enable player input for the new scene
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) pm.SetInputEnabled(true);
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator FadeInRoutine()
    {
        if (loadingOverlay == null) yield break;
        
        loadingOverlay.gameObject.SetActive(true);
        loadingOverlay.canvasRenderer.SetAlpha(1.0f);
        
        // Wait for generation (Black Screen Duration extended)
        // User requested to keep it black longer to hide map generation/fog glitches
        yield return new WaitForSeconds(1.5f);

        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1.0f - Mathf.Clamp01(elapsed / duration);
            if (loadingOverlay != null) loadingOverlay.canvasRenderer.SetAlpha(alpha);
            yield return null;
        }
        
        if (loadingOverlay != null) loadingOverlay.gameObject.SetActive(false);
        isTransitioningToNextFloor = false; // Fix: Reset transition flag
    }

    private IEnumerator FadeOutResultScreen()
    {
        if (resultScreenPanel == null) yield break;
        
        // Ensure CanvasGroup
        CanvasGroup cg = resultScreenPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = resultScreenPanel.AddComponent<CanvasGroup>();
        
        cg.alpha = 1.0f;
        resultScreenPanel.SetActive(true);

        // Wait to ensure map/fog is ready
        yield return new WaitForSeconds(0.5f);

        float duration = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
             elapsed += Time.deltaTime;
             float t = elapsed / duration;
             cg.alpha = Mathf.Lerp(1.0f, 0f, t);
             yield return null;
        }

        cg.alpha = 1.0f; // Reset for next time
        resultScreenPanel.SetActive(false);
        isTransitioningToNextFloor = false; // Reset flag
    }
    
    // Flag to track transition state
    private bool isTransitioningToNextFloor = false;




    private bool isInventoryConnected = false;

    private void Start()
    {
        Debug.Log("[GameUIManager] Start() called. Initializing UI...");
        InitializeUI();
        Debug.Log($"[GameUIManager] After InitializeUI - canvas: {(canvas != null ? canvas.name : "NULL")}, floorText: {(floorText != null ? "OK" : "NULL")}");
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
        Debug.Log($"[GameUIManager] OnSceneLoaded: {scene.name}");
        
        // Cleanup duplicates from new scene
        HandlePersistentUI();
        
        // Reset connection flag to ensure event is re-subscribed if needed
        // (InventoryManager is also DontDestroyOnLoad, so the event might be stale)
        if (InventoryManager.Instance != null && !isInventoryConnected)
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateInventoryUI;
            isInventoryConnected = true;
            Debug.Log("[GameUIManager] Re-connected to InventoryManager OnSceneLoaded.");
        }
        
        InitializeUI();
        
        // Ensure all UI elements are active
        EnsureUIActive();
        
        UpdateInventoryUI(); // Force refresh to prevent empty inventory flicker
        
        UpdateInventoryUI(); // Force refresh to prevent empty inventory flicker
        
        AssignCameraToCanvas(); // Ensure camera is hooked up
        
        // Match transition timing with LoadingOverlay
        if (isTransitioningToNextFloor)
        {
            // Use ResultScreen as cover? NO, User requested instant black screen instead of fade out.
            // We disable ResultScreen logic for transition cover.
            if (resultScreenPanel != null)
            {
                resultScreenPanel.SetActive(false); // Ensure hidden
            }
            StartCoroutine(FadeInRoutine());
        }
        else
        {
            StartCoroutine(FadeInRoutine());
        }
    }
    
    private void EnsureUIActive()
    {
        // Canvas check and recovery
        if (canvas == null)
        {
            InitializeUI(); // Re-run initialization to find canvas
        }

        // Ensure Canvas is active
        if (canvas != null && !canvas.gameObject.activeInHierarchy)
        {
            canvas.gameObject.SetActive(true);
            Debug.Log("[GameUIManager] Reactivated Canvas.");
        }
        
        // Ensure FloorText is active
        if (floorText != null && !floorText.gameObject.activeInHierarchy)
        {
            floorText.gameObject.SetActive(true);
            Debug.Log("[GameUIManager] Reactivated FloorText.");
        }
        
        // Ensure UI Boxes are active
        if (uiBoxItem != null && !uiBoxItem.gameObject.activeInHierarchy)
        {
            uiBoxItem.gameObject.SetActive(true);
            Debug.Log("[GameUIManager] Reactivated uiBoxItem.");
        }
        
        if (uiBoxKey != null && !uiBoxKey.gameObject.activeInHierarchy)
        {
            uiBoxKey.gameObject.SetActive(true);
            Debug.Log("[GameUIManager] Reactivated uiBoxKey.");
        }
        
        // Hide MessageText on load (User request: Start hidden)
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
            messageText.text = ""; // Clear text too
        }
        
        // Ensure ResultScreen is hidden (should not persist across scene loads UNLESS transitioning)
        if (!isTransitioningToNextFloor && resultScreenPanel != null && resultScreenPanel.activeInHierarchy)
        {
            resultScreenPanel.SetActive(false);
            Debug.Log("[GameUIManager] Hid ResultScreen on scene load.");
        }
    }

    private void InitializeUI()
    {
        Debug.Log("[GameUIManager] InitializeUI() started.");

        // 1. MobileUI (Canvas)
        if (canvas == null)
        {
            GameObject mobileUIObj = GameObject.Find("MobileUI");
            if (mobileUIObj != null)
            {
                // Verify it is part of MobileController
                if (mobileUIObj.transform.parent != null && mobileUIObj.transform.parent.name == "MobileController")
                {
                   canvas = mobileUIObj.GetComponent<Canvas>();
                }
            }
        }

        if (canvas != null)
        {
            // 2. Bind FloorText (Created in TopBar)
            // 2. Bind FloorText (Created in TopBar)
            if (floorText == null)
            {
                Transform t = RecursiveFind(canvas.transform, "FloorText");
                if (t != null) floorText = t.GetComponent<Text>();
            }

            // 3. Bind MessageText
            if (messageText == null)
            {
                Transform t = RecursiveFind(canvas.transform, "MessageText");
                if (t != null) messageText = t.GetComponent<Text>();
            }

            // 3b. Bind ItemNameText
            if (itemNameText == null)
            {
                Transform t = RecursiveFind(canvas.transform, "ItemNameText");
                if (t != null) itemNameText = t.GetComponent<Text>();
            }

            // 4. Bind Boxes
            if (uiBoxItem == null)
            {
                Transform t = RecursiveFind(canvas.transform, "item_box");
                if (t != null) uiBoxItem = t.GetComponent<RectTransform>();
            }
             if (uiBoxKey == null)
            {
                Transform t = RecursiveFind(canvas.transform, "key_box");
                if (t != null) uiBoxKey = t.GetComponent<RectTransform>();
            }
            
            // 5. ResultScreen (Player/UI_Root/ResultScreen or MobileUI?)
            // If ResultScreen is in Player/UI_Root, we need to find Player first, OR we assume it's in Canvas.
            // Since we reverted, original code looked in Canvas.
            // If user has not created ResultScreen in MobileUI, this might fail unless it's in UI_Root.
            // For now, let's look in Canvas, and if not found, look in Player/UI_Root just in case.
            
            if (resultScreenPanel == null)
            {
                Transform t = RecursiveFind(canvas.transform, "ResultScreen");
                if (t == null)
                {
                     // Fallback check in Player
                     GameObject player = GameObject.FindGameObjectWithTag("Player");
                     if (player != null) t = RecursiveFind(player.transform, "ResultScreen");
                }
                
                if (t != null)
                {
                    resultScreenPanel = t.gameObject;
                    
                    // Re-bind Button
                    Transform btn = RecursiveFind(t, "NextButton");
                    if (btn != null) nextFloorButton = btn.GetComponent<Button>();
                    
                    resultScreenPanel.SetActive(false);
                }
            }
            
            // 6. InventoryScreen
            if (inventoryScreenPanel == null)
            {
                Transform t = RecursiveFind(canvas.transform, "InventoryScreen");
                if (t != null)
                {
                    inventoryScreenPanel = t.gameObject;
                    inventoryScreenPanel.SetActive(false); // Ensure hidden by default
                    
                    // Optional: Find Close Button inside if needed in future
                    // Transform closeBtn = RecursiveFind(t, "CloseButton");
                    // if (closeBtn != null) ...
                }
            }
        }
        else
        {
            Debug.LogError("[GameUIManager] MobileUI Canvas NOT found. UI will not work.");
        }

        if (nextFloorButton != null)
        {
            nextFloorButton.onClick.RemoveAllListeners();
            nextFloorButton.onClick.RemoveAllListeners();
            nextFloorButton.onClick.AddListener(ProceedToNextFloor);
            nextFloorButton.onClick.AddListener(ProceedToNextFloor);
            nextFloorButton.interactable = true; // Insurance: Ensure enabled by default
            Debug.Log("[GameUIManager] InitializeUI: NextFloorButton found and listener added.");
            
            Transform textObj = nextFloorButton.transform.Find("Text");
            if (textObj != null) initialTextScale = textObj.localScale;
        }

        UpdateFloorText();
    }

    private void EnsureLayoutGroup(RectTransform rt, bool isLeftAlign)
    {
        HorizontalLayoutGroup layout = rt.GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        
        layout.childAlignment = isLeftAlign ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
        if (layout.childForceExpandWidth) layout.childForceExpandWidth = false;
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

            // Delegate to shared method (ALLOW USE = true)
            CreateItemSlot(targetParent, key, count, true);
        }
    }

    public void ToggleInventoryScreen()
    {
        if (inventoryScreenPanel == null) InitializeUI();
        
        if (inventoryScreenPanel != null)
        {
            bool isActive = !inventoryScreenPanel.activeSelf;
            inventoryScreenPanel.SetActive(isActive);
            
            if (isActive)
            {
                Time.timeScale = 0f; // Pause Game
                RefreshInventoryScreen(); // Show items in the big screen
                Debug.Log("[GameUIManager] Inventory Screen Opened.");
            }
            else
            {
                Time.timeScale = 1f; // Resume Game
                Debug.Log("[GameUIManager] Inventory Screen Closed.");
            }
        }
    }
    
    // Logic to populate the big Inventory Screen (separate from bottom bar)
    // REMOVED: User uses main UI item box for inventory display now.
    private void RefreshInventoryScreen()
    {
        // Update Total Score Text if assigned
        if (inventoryTotalScoreText != null)
        {
            inventoryTotalScoreText.text = $"Total Score: {TotalScore}";
        }
    }

    /// <summary>
    /// Creates a standardized Item Slot using consistent style (Size 80x80).
    /// Used by both Main UI and Inventory Screen.
    /// </summary>
    private void CreateItemSlot(Transform parent, string key, int count, bool allowUse)
    {
        // 1. Slot (Container & Interaction)
        GameObject slot = new GameObject($"ItemSlot_{key}");
        slot.transform.SetParent(parent, false);
        
        RectTransform slotRT = slot.AddComponent<RectTransform>();
        slotRT.sizeDelta = new Vector2(80, 80); // UNIFIED SIZE

        // Add transparent Image for Raycast target
        Image slotImage = slot.AddComponent<Image>();
        slotImage.color = Color.clear;
        slotImage.raycastTarget = true;

        // Add ItemSlotHandler for interaction
        ItemSlotHandler handler = slot.AddComponent<ItemSlotHandler>();
        
        handler.OnClick = () => {
            ItemDatabase itemMgr = ItemDatabase.Instance;
            if (itemMgr == null) itemMgr = FindObjectOfType<ItemDatabase>();

            if (itemMgr != null)
            {
                string desc = itemMgr.GetItemDescription(key);
                ShowMessage(desc ?? $"{key}", key);
            }
        };
        
        if (allowUse)
        {
            handler.OnLongPress = () => {
                // If Inventory Screen is active, DO NOT use item (View Only mode for all slots)
                if (inventoryScreenPanel != null && inventoryScreenPanel.activeSelf)
                {
                    return;
                }

                UseItem(key); 
                
                // If we are in Inventory Screen mode, we might want to refresh it.
                // Using a simple check: if the parent is our inventory container.
                if (inventoryScreenPanel != null && inventoryScreenPanel.activeSelf)
                {
                    // Simple delayed refresh or immediate if safe
                    RefreshInventoryScreen();
                }
            };
        }

        // 2. Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slot.transform, false);
        Image icon = iconObj.AddComponent<Image>();
        icon.sprite = LoadItemSprite(key);
        icon.preserveAspect = true;
        RectTransform iconRT = icon.GetComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.sizeDelta = Vector2.zero;
        icon.raycastTarget = false; 

        // 3. Count Text
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
            textRT.offsetMax = new Vector2(-5, 5); 
        }
    }

    public void ShowResultScreen()
    {
        Debug.Log("[GameUIManager] ShowResultScreen called. Forcing isTransitioningToNextFloor = false.");
        isTransitioningToNextFloor = false; // ★ FORCE RESET
        
        if (resultScreenPanel == null) InitializeUI();
        
        resultScreenPanel.SetActive(true);
        
        // Disable Player Control
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null)
        {
            pm.SetInputEnabled(false);
        }

        // Bonus for Stage Clear (Moved here so it's counted in animation)
        AddScore(300);

        // Start Score Animation
        StartCoroutine(AnimateScoreTally());
    }

    private System.Collections.IEnumerator AnimateScoreTally()
    {
        // Initial setup
        int startStage = StageScore;
        int startTotal = TotalScore; // This is the total BEFORE this stage (since AddScore doesn't update it anymore)
        int targetTotal = startTotal + startStage; // Target total
        
        int currentStageScore = startStage;
        int currentTotalScore = startTotal;
        
        // Set initial text
        if (resultStageScoreText != null) resultStageScoreText.text = $"Stage Score: {currentStageScore}";
        if (resultTotalScoreText != null) resultTotalScoreText.text = $"Total Score: {currentTotalScore}";
        
        // Disable Next Button while animating
        if (nextFloorButton != null) 
        {
            nextFloorButton.interactable = false;
            Transform textObj = nextFloorButton.transform.Find("Text");
            if (textObj != null) textObj.gameObject.SetActive(false); // Hide text during calculation
        }

        yield return new WaitForSeconds(0.5f); // Small delay before start

        float duration = 2.0f; // Animation duration
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Ease out
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            int movedScore = Mathf.RoundToInt(Mathf.Lerp(0, startStage, t));
            
            currentStageScore = startStage - movedScore;
            currentTotalScore = startTotal + movedScore;

            if (resultStageScoreText != null) resultStageScoreText.text = $"Stage Score: {currentStageScore}";
            if (resultTotalScoreText != null) resultTotalScoreText.text = $"Total Score: {currentTotalScore}";
            
            yield return null;
        }

        // Ensure final values
        if (resultStageScoreText != null) resultStageScoreText.text = $"Stage Score: 0";
        if (resultTotalScoreText != null) resultTotalScoreText.text = $"Total Score: {targetTotal}";

        // Commit logic: Update actual Total Score
        TotalScore = targetTotal;
        // StageScore remains as is until reset in LoadNextFloorRoutine, or we can visually set it to 0?
        // Let's leave StageScore variable alone (it will be reset on load), but visual is 0.

        if (nextFloorButton != null) 
        {
            nextFloorButton.interactable = true;
            Transform textObj = nextFloorButton.transform.Find("Text");
            if (textObj != null) textObj.gameObject.SetActive(true); // Show text
            
            // Start Pulse Animation
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseButtonText());
        }
    }

    private System.Collections.IEnumerator PulseButtonText()
    {
        if (nextFloorButton == null) yield break;
        Transform textObj = nextFloorButton.transform.Find("Text");
        if (textObj == null) yield break;

        Vector3 originalScale = initialTextScale; 
        
        while (true)
        {
            // Pulse between 0.985 and 1.015 (Amplitude 0.015)
            float scale = 1.0f + Mathf.Sin(Time.time * 5.0f) * 0.015f;
            textObj.localScale = originalScale * scale;
            yield return null;
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
                        // Fix: Consume item
                        if (InventoryManager.Instance != null) InventoryManager.Instance.RemoveItem(key);
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
                    if (GlobalSoundManager.Instance != null && GlobalSoundManager.Instance.radarSE != null)
                    {
                        GlobalSoundManager.Instance.PlaySE(GlobalSoundManager.Instance.radarSE);
                    }
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
                    if (GlobalSoundManager.Instance != null && GlobalSoundManager.Instance.talismanSE != null)
                    {
                        GlobalSoundManager.Instance.PlaySE(GlobalSoundManager.Instance.talismanSE);
                    }
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
                    if (GlobalSoundManager.Instance != null && GlobalSoundManager.Instance.blueboxSE != null)
                    {
                        GlobalSoundManager.Instance.PlaySE(GlobalSoundManager.Instance.blueboxSE);
                    }
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

    private Transform RecursiveFind(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = RecursiveFind(child, name);
            if (result != null) return result;
        }
        return null;
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
