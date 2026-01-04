using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float jumpHeight = 0.5f;
    [SerializeField] private float moveDuration = 0.2f;
    [Header("Debug Settings")]
    [SerializeField] private bool isInvincible = false;
    

    private Vector3 targetPosition;
    private Vector3 startPosition;
    private float moveTimer;
    private bool isMoving = false;
    private TurnManager turnManager;
    private MobileInputController mobileInput;
    private DungeonGeneratorV2 dungeonGenerator;
    private SpriteRenderer spriteRenderer; // Added missing field
    
    // 向き保存ロジックは削除 (足元を調べるため不要)
    
    private void Start()
    {
        targetPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>(); // Added initialization

        turnManager = Object.FindFirstObjectByType<TurnManager>();
        if (turnManager == null)
        {
            Debug.LogWarning("TurnManager not found in scene!");
        }
        
        mobileInput = Object.FindFirstObjectByType<MobileInputController>();
        if (mobileInput == null)
        {
            GameObject inputObj = new GameObject("MobileInputController");
            mobileInput = inputObj.AddComponent<MobileInputController>();
            Debug.Log("[PlayerMovement] Auto-created MobileInputController");
        }

        if (GameUIManager.Instance == null)
        {
            GameUIManager existingUI = Object.FindFirstObjectByType<GameUIManager>();
            if (existingUI == null)
            {
                GameObject uiObj = new GameObject("GameUIManager");
                uiObj.AddComponent<GameUIManager>();
                Debug.Log("[PlayerMovement] Auto-created GameUIManager");
            }
        }

        if (InventoryManager.Instance == null)
        {
            InventoryManager existingInv = Object.FindFirstObjectByType<InventoryManager>();
            if (existingInv == null)
            {
                GameObject invObj = new GameObject("InventoryManager");
                invObj.AddComponent<InventoryManager>();
                Debug.Log("[PlayerMovement] Auto-created InventoryManager");
            }
        }

        dungeonGenerator = Object.FindFirstObjectByType<DungeonGeneratorV2>();
        if (dungeonGenerator == null)
        {
            Debug.LogError("DungeonGeneratorV2 not found! Movement restriction might fail.");
        }
    }
    
    public void InitializePosition(Vector3 position)
    {
        transform.position = position;
        targetPosition = position;
        isMoving = false;
        
        if (dungeonGenerator != null)
        {
            dungeonGenerator.RevealMap(position);
        }
        
        Debug.Log($"Player initialized at position: {position}");
    }
    
    private bool inputEnabled = true;

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (!enabled) isMoving = false; // Stop immediately
    }

    private float inputCooldownTimer = 0f;

    private void Update()
    {
        if (inputCooldownTimer > 0f)
        {
            inputCooldownTimer -= Time.deltaTime;
            return;
        }

        if (isShootingMode)
        {
            UpdateShootingMode();
            return;
        }

        if (!inputEnabled) return;

        if (isMoving)
        {
            UpdateMovement();
            return;
        }
        
        // ... (rest of update)
        // Interaction is now fully handled by HandleSearchButtonInteraction() for both Key and Mobile Button
        
        Vector2 input = GetMovementInput();
        if (input != Vector2.zero)
        {
            // ... (existing movement logic)
            // Update sprite direction based on horizontal input
            if (spriteRenderer != null)
            {
                if (input.x < 0)
                {
                    spriteRenderer.flipX = true; // Flip left
                }
                else if (input.x > 0)
                {
                    spriteRenderer.flipX = false; // Unflip right
                }
            }
            TryMove(input);
        }
        
        // Shop Interaction Logic (Search Button Hold)
        HandleSearchButtonInteraction();
    }
    
    // Shop Interaction Variables
    private float searchButtonHoldDuration = 0f;
    private bool shopActionExecuted = false;

    private void HandleSearchButtonInteraction()
    {
        // 1. Check if Search Button is Held
        bool isHeld = Input.GetKey(KeyCode.E);
        if (mobileInput != null && mobileInput.GetInteractHeld()) isHeld = true;

        if (isHeld)
        {
            searchButtonHoldDuration += Time.deltaTime;
            
            // 2. Check overlap with ShopItem
            ShopItem shopItem = GetShopItemAtFeet();
            if (shopItem != null)
            {
                // Long Press (Buy)
                if (searchButtonHoldDuration >= 0.5f && !shopActionExecuted)
                {
                    ExecuteShopPurchase(shopItem);
                    shopActionExecuted = true; // Prevent multiple buys in one press
                }
            }
            
            // 3. Check overlap with Vendor (Interactive) - Long Press for Reroll
            ShopVendor vendor = GetVendorAtFeet();
            if (vendor != null)
            {
                if (searchButtonHoldDuration >= 0.5f && !shopActionExecuted)
                {
                    // Reroll Logic
                    if (GameUIManager.Instance != null && GameUIManager.Instance.TotalPoint >= 5000)
                    {
                        if (GameUIManager.Instance.TrySpendPoint(5000))
                        {
                            DungeonGeneratorV2 dGen = Object.FindFirstObjectByType<DungeonGeneratorV2>();
                            if (dGen != null)
                            {
                                dGen.RefreshShopItems();
                                GameUIManager.Instance.ShowMessage("商品を入れ替えました！", "vendor");
                                
                                // Play Sound
                                if (GlobalSoundManager.Instance != null && GlobalSoundManager.Instance.shopResetSE != null)
                                {
                                    GlobalSoundManager.Instance.PlaySE(GlobalSoundManager.Instance.shopResetSE);
                                } 
                            }
                        }
                    }
                    else
                    {
                         GameUIManager.Instance.ShowMessage("ポイントが足りないようだね… (必要: 5000Pt)", "vendor");
                    }
                    
                    shopActionExecuted = true;
                }
            }
        }
        else
        {
            // Button Released
            if (searchButtonHoldDuration > 0f)
            {
                // Verify if it was a short press
                if (searchButtonHoldDuration < 0.5f && !shopActionExecuted)
                {
                    // Short Press Logic
                    
                    // A. Shop Item (Show Price)
                    ShopItem shopItem = GetShopItemAtFeet();
                    if (shopItem != null)
                    {
                        GameUIManager.Instance.ShowMessage($"{shopItem.price}Ptです！ご購入は長押しで！", shopItem.itemId);
                    }
                    else
                    {
                        // B. Vendor Interaction
                        ShopVendor vendor = GetVendorAtFeet();
                        if (vendor != null)
                        {
                             GameUIManager.Instance.ShowMessage("5000pointで商品を入れ替えますか？？(長押し)", "vendor");
                        }
                        else
                        {
                            // C. Standard Interaction (Shelf, etc.)
                            CheckForInteraction();
                        }
                    }
                }
                
                // Reset
                searchButtonHoldDuration = 0f;
                shopActionExecuted = false;
            }
        }
    }

    private ShopItem GetShopItemAtFeet()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.4f);
        foreach (var hit in hits)
        {
            ShopItem item = hit.GetComponent<ShopItem>();
            if (item != null) return item;
        }
        return null;
    }

    private ShopVendor GetVendorAtFeet()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.4f);
        foreach (var hit in hits)
        {
            ShopVendor vendor = hit.GetComponent<ShopVendor>();
            if (vendor != null) return vendor;
        }
        return null;
    }
    
    // Removed old Raycast logic
    /* 
    private void HandleShopInteraction() { ... } 
    */
    
    private void ExecuteShopPurchase(ShopItem shopItem)
    {
        if (shopItem == null) return;
        
        if (GameUIManager.Instance != null)
        {
            // Use TotalPoint (Currency) instead of StageScore
            if (GameUIManager.Instance.TrySpendPoint(shopItem.price))
            {
                // Purchase Success (Point deduction handled in TrySpendPoint)
                InventoryManager.Instance.AddItem(shopItem.itemId);
                
                GameUIManager.Instance.ShowMessage($"ありがとうございました～！", shopItem.itemId);
                GameUIManager.Instance.ShowFloatingItem(shopItem.itemId, shopItem.transform.position);
                
                if (GlobalSoundManager.Instance != null && GlobalSoundManager.Instance.getSE != null)
                {
                    GlobalSoundManager.Instance.PlaySE(GlobalSoundManager.Instance.getSE);
                }
                
                Destroy(shopItem.gameObject);
            }
            else
            {
                GameUIManager.Instance.ShowMessage($"おや、pointが足りないね ({shopItem.price}pt必要)");
            }
        }
    }

    // ... (rest of methods)

    private void ShootBullet(Vector2 dir)
    {
        isShootingMode = false;
        SetInputEnabled(true); // Resume normal control, but Cooldown will block immediate move
        inputCooldownTimer = 0.5f; // Block input for 0.5s after shooting
        
        // 弾の生成 (Prefabがないのでコード生成)
        GameObject bullet = new GameObject("WarpBullet");
        bullet.transform.position = transform.position;
        bullet.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        
        // Sprite
        SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
        // Placeholder sprite: reuse "item_coin" (warpcoin) image for the bullet
        Sprite bulletSprite = Resources.Load<Sprite>("item/item_warpcoin"); 
        if (bulletSprite == null) 
        {
            // Fallback try simple item_
            bulletSprite = Resources.Load<Sprite>("item/item_doc");
        }
        sr.sprite = bulletSprite;
        sr.color = Color.cyan; // Tint it to look distinct
        sr.sortingOrder = 50; 

        // Collider
        BoxCollider2D col = bullet.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.3f, 0.3f); // slightly smaller
        
        // Rigidbody2D (Essential for Trigger events with static enemies)
        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.bodyType = RigidbodyType2D.Dynamic; // Dynamic needed to trigger against Static
        rb.mass = 0.0001f; // Lightweight
        
        // Script
        BulletController bc = bullet.AddComponent<BulletController>();
        bc.Initialize(dir);
        
        Debug.Log($"[PlayerMovement] Fired bullet in direction {dir}");
        
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem("warp_gun");
        }
        
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowMessage("転送ビーム発射！");
        }
    }
    
    private void CheckForInteraction()
    {
        // 足元のタイルをチェック
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.4f);
        
        foreach(var hit in hits)
        {
            // 1. Check for Shelf
            InteractableShelf shelf = hit.GetComponent<InteractableShelf>();
            if (shelf != null)
            {
                shelf.Interact(transform.position);
                continue; // Shelf interaction done
            }
            
            // 2. Check for ShopItem (Removed - Moved to Touch Interaction)
            /*
            ShopItem shopItem = hit.GetComponent<ShopItem>();
            if (shopItem != null)
            {
                 // ... Old Logic ...
                 return;
            }
            */
        }
    }
    
    private void TryMove(Vector2 direction)
    {
        Vector3 moveDirection = Vector3.zero;
        float horizontal = direction.x;
        float vertical = direction.y;

        if (Mathf.Abs(horizontal) > Mathf.Abs(vertical))
        {
            vertical = 0;
        }
        else
        {
            horizontal = 0;
        }

        if (horizontal != 0)
        {
            moveDirection = new Vector3(Mathf.Sign(horizontal), 0, 0);
        }
        else if (vertical != 0)
        {
            moveDirection = new Vector3(0, Mathf.Sign(vertical), 0);
        }
        else
        {
            return;
        }

        Vector3 potentialPos = transform.position + moveDirection;
        
        if (IsPositionWalkable(potentialPos))
        {
            // Check if moving into an Enemy
            Collider2D[] hits = Physics2D.OverlapCircleAll(potentialPos, 0.4f);
            if (hits.Length > 0)
            {
                 Debug.Log($"[PlayerMovement] OverlapCircleAll found {hits.Length} hits at {potentialPos}");
            }
            foreach (var hit in hits)
            {
                EnemyMovement enemy = hit.GetComponent<EnemyMovement>();
                if (enemy != null)
                {
                    enemy.SkipNextTurn();
                    Debug.Log($"[PlayerMovement] Locking Enemy at {hit.transform.position}. Name: {hit.name}");
                }
            }

            startPosition = transform.position;
            targetPosition = potentialPos;
            moveTimer = 0f;
            isMoving = true;
            
            if (dungeonGenerator != null)
            {
                dungeonGenerator.RevealMap(potentialPos);
            }
            
            if (turnManager != null)
            {
                turnManager.OnPlayerMoved();
            }
            
            // Decrement confusion
            if (confusedTurns > 0)
            {
                confusedTurns--;
                if (confusedTurns == 0)
                {
                    Debug.Log("[PlayerMovement] Confusion wore off.");
                    GameUIManager.Instance.ShowMessage("意識がはっきりしてきた。");
                }
            }
        }
    }
    
    private int confusedTurns = 0;

    public void Confuse(int turns)
    {
        confusedTurns += turns;
        Debug.Log($"[PlayerMovement] Confused for {confusedTurns} turns!");
    }

    private Vector2 GetMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        if (mobileInput != null)
        {
            Vector2 mobileDir = mobileInput.GetMovementDirection();
            if (mobileDir != Vector2.zero)
            {
                horizontal = mobileDir.x;
                vertical = mobileDir.y;
            }
        }
        
        // Confusion Logic: Reverse Input
        if (confusedTurns > 0)
        {
            horizontal *= -1;
            vertical *= -1;
        }
        
        if (Mathf.Abs(horizontal) > Mathf.Abs(vertical))
        {
            vertical = 0;
        }
        else
        {
            horizontal = 0;
        }

        if (Mathf.Abs(horizontal) < 0.1f && Mathf.Abs(vertical) < 0.1f)
        {
            return Vector2.zero;
        }
        
        return new Vector2(horizontal, vertical);
    }
    
    private bool IsPositionWalkable(Vector3 position)
    {
        if (dungeonGenerator != null)
        {
            if (!dungeonGenerator.IsWorldPositionWalkable(position))
            {
                return false;
            }
        }

        Collider2D hit = Physics2D.OverlapCircle(position, 0.4f);
        
        if (hit != null)
        {
            if (hit.CompareTag("Wall"))
            {
                return false;
            }

            // Allow walking on Triggers, Player, and Enemies
            if (hit.isTrigger || hit.gameObject == gameObject || hit.GetComponent<EnemyMovement>() != null) 
            {
                // It's walkable
            }
            else
            {
                // Otherwise, it's an obstacle
                return false;
            }
        }

        return true;
    }
    
    private void UpdateMovement()
    {
        moveTimer += Time.deltaTime;
        float percent = Mathf.Clamp01(moveTimer / moveDuration);
        
        Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, percent);
        
        float height = Mathf.Sin(percent * Mathf.PI) * jumpHeight;
        currentPos.y += height;
        
        transform.position = currentPos;
        
        if (percent >= 1f)
        {
            transform.position = targetPosition;
            isMoving = false;
            CheckForDoor();
            CheckForCollision(); // Add Collision Check
        }
    }

    private void CheckForCollision()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.1f);
        foreach (var hit in hits)
        {
            EnemyMovement enemy = hit.GetComponent<EnemyMovement>();
            if (enemy != null)
            {
                if (isInvincible)
                {
                    Debug.Log("[Debug] Player collided with Enemy, but Invincibility is ON.");
                    return;
                }

                if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem("migawari"))
                {
                    Debug.Log("[PlayerMovement] Migawari activated! Preventing Game Over.");
                    StartCoroutine(ActivateDollSequence(enemy));
                    return; // Stop processing collision (prevent Game Over)
                }

                if (GlobalSoundManager.Instance != null && GlobalSoundManager.Instance.deadSE != null)
                {
                    GlobalSoundManager.Instance.PlaySE(GlobalSoundManager.Instance.deadSE);
                }
                Debug.Log("Player collided with Enemy! Game Over.");
                GameUIManager.Instance.ShowGameOverScreen();
                // ShowGameOverScreen handles input disabling
            }
        }
    }

    private System.Collections.IEnumerator ActivateDollSequence(EnemyMovement enemy)
    {
        // 1. Disable Input
        SetInputEnabled(false);
        isMoving = false;

        // 2. Message & Visuals
        GameUIManager.Instance.ShowMessage("身代わり人形が身代わりになった！", "migawari");
        if (GlobalSoundManager.Instance != null && GlobalSoundManager.Instance.migawariSE != null)
        {
            GlobalSoundManager.Instance.PlaySE(GlobalSoundManager.Instance.migawariSE);
        }
        // Optional: Show floating doll icon?
        GameUIManager.Instance.ShowFloatingItem("migawari", transform.position);

        // 3. Wait 0.5s
        yield return new WaitForSeconds(0.5f);

        // 4. Consume Item & Apply Effect
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem("migawari");
        }
        
        if (enemy != null)
        {
            enemy.Stun(5);
            Debug.Log($"[PlayerMovement] Enemy {enemy.name} stunned for 5 turns by Doll.");
        }

        // 5. Re-enable Input
        SetInputEnabled(true);
    }

    private void CheckForDoor()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.4f);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                Debug.Log($"[PlayerMovement] Overlap Detected: {hit.name}");
                // Case-insensitive check to cover "Door", "door", "door_shop", etc.
                if (hit.name.IndexOf("door", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Special Case: Shop Door
                    if (hit.name.IndexOf("Shop", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (InventoryManager.Instance.HasItem("key"))
                        {
                            Debug.Log("[PlayerMovement] Shop Door reached with Key. Entering Shop...");
                            if (GlobalSoundManager.Instance != null && GlobalSoundManager.Instance.doorSE != null)
                            {
                                GlobalSoundManager.Instance.PlaySE(GlobalSoundManager.Instance.doorSE);
                            }
                            
                            if (dungeonGenerator != null)
                            {
                                // Flag next stage as Shop
                                GameUIManager.Instance.NextStageIsShop = true;
                                
                                // Trigger Result Screen (which handles Point Add -> Next Button -> Load Scene)
                                Debug.Log("[PlayerMovement] Shop Door reached. Flagging Shop and showing Result Screen.");
                                GameUIManager.Instance.ShowResultScreen();
                            }
                        }
                        else
                        {
                            Debug.Log("[PlayerMovement] Shop Door reached but no Key.");
                            GameUIManager.Instance.ShowMessage("鍵が必要だ。");
                        }
                        return; // Exit immediately
                    }

                    if (InventoryManager.Instance.HasItem("key"))
                    {
                        if (GlobalSoundManager.Instance != null && GlobalSoundManager.Instance.doorSE != null)
                        {
                            GlobalSoundManager.Instance.PlaySE(GlobalSoundManager.Instance.doorSE);
                        }
                        Debug.Log("[PlayerMovement] Door reached with Key! Showing Result Screen.");
                        GameUIManager.Instance.ShowResultScreen();
                        return; // Found correct door, exit loop
                    }
                    else
                    {
                        Debug.Log("[PlayerMovement] Door reached but no Key (or key missing in InventoryManager).");
                        GameUIManager.Instance.ShowMessage("鍵が必要だ。");
                    }
                }
            }
        }
    }

    public void StartWarpSequence(Vector3 targetPos)
    {
        StartCoroutine(WarpSequence(targetPos));
    }

    public System.Collections.IEnumerator WarpSequence(Vector3 targetPos)
    {
        SetInputEnabled(false);
        isMoving = false; // Ensure normal movement assumes stop

        float duration = 0.25f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 upPos = startPos + Vector3.up * 1.5f;
        
        // Phase 1: Float Up & Fade Out
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            transform.position = Vector3.Lerp(startPos, upPos, t);
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = c;
            }
            yield return null;
        }

        // Warp
        if (dungeonGenerator != null)
        {
             dungeonGenerator.RevealMap(targetPos);
        }
        
        // Phase 2: Warp to Target (High) & Float Down & Fade In
        elapsed = 0f;
        Vector3 landPos = targetPos;
        Vector3 highPos = targetPos + Vector3.up * 1.5f;
        
        transform.position = highPos;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            transform.position = Vector3.Lerp(highPos, landPos, t);
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(0f, 1f, t);
                spriteRenderer.color = c;
            }
            yield return null;
        }
        
        // Ensure final state
        transform.position = landPos;
        targetPosition = landPos;
        if (spriteRenderer != null)
        {
             Color c = spriteRenderer.color;
             c.a = 1f;
             spriteRenderer.color = c;
        }
        
        SetInputEnabled(true);
        // Warp does not consume a turn, so we do NOT call turnManager.OnPlayerMoved()
    }

    private bool isShootingMode = false;

    public void EnterShootingMode()
    {
        if (isShootingMode) return;
        
        isShootingMode = true;
        isMoving = false; // Stop movement
        SetInputEnabled(false); // Disable normal movement processing
        
        Debug.Log("[PlayerMovement] Entered Shooting Mode. Waiting for input...");
    }

    private void UpdateShootingMode()
    {
        // Check for Directional Input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        // Also check Mobile Input
        if (mobileInput != null)
        {
            Vector2 mDir = mobileInput.GetMovementDirection();
            if (mDir != Vector2.zero)
            {
                h = mDir.x;
                v = mDir.y;
            }
        }

        if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
        {
             Vector2 dir = Vector2.zero;
             if (Mathf.Abs(h) > Mathf.Abs(v)) dir = new Vector2(Mathf.Sign(h), 0);
             else dir = new Vector2(0, Mathf.Sign(v));
             
             ShootWarpBullet(dir);
        }
    }

    private void ShootWarpBullet(Vector2 dir)
    {
        if (GlobalSoundManager.Instance != null && GlobalSoundManager.Instance.warpSE != null)
        {
            GlobalSoundManager.Instance.PlaySE(GlobalSoundManager.Instance.warpSE);
        }

        isShootingMode = false;
        SetInputEnabled(true); 
        inputCooldownTimer = 0.5f; 
        
        GameObject bullet = new GameObject("WarpBullet");
        bullet.transform.position = transform.position;
        // Correct Size: 1.0f
        bullet.transform.localScale = new Vector3(1.0f, 1.0f, 1f);
        
        SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
        // Updated to use the requested sprite
        Sprite bulletSprite = Resources.Load<Sprite>("item/item_warp_gun_shot"); 
        if (bulletSprite == null) 
        {
            // Fallback
            bulletSprite = Resources.Load<Sprite>("item/item_warpcoin");
        }
        sr.sprite = bulletSprite;
        sr.color = Color.white; // Correct Color: White
        sr.sortingOrder = 50; 

        BoxCollider2D col = bullet.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.3f, 0.3f); 
        
        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.bodyType = RigidbodyType2D.Dynamic; 
        rb.mass = 0.0001f; 
        
        BulletController bc = bullet.AddComponent<BulletController>();
        bc.Initialize(dir);
        
        Debug.Log($"[PlayerMovement] Fired warp bullet in direction {dir}");
        
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem("warp_gun");
        }
        
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowMessage("転送ビーム発射！");
        }
    }
    




    public bool IsMoving()
    {
        return isMoving;
    }
}
