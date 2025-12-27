using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float jumpHeight = 0.5f;
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private Sprite danceSprite; // For Stun Animation
    
    private Transform player;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float moveTimer;
    private bool isMoving = false;
    private bool wasRevealed = false;
    private DungeonGeneratorV2 dungeonGenerator;

    private Sprite originalSprite;
    private bool isDancing = false;
    private float danceTimer = 0f;
    private SpriteRenderer sr;

    public void SetupDance(Sprite sprite)
    {
        danceSprite = sprite;
    }

    private void Start()
    {
        targetPosition = transform.position;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        dungeonGenerator = Object.FindFirstObjectByType<DungeonGeneratorV2>();
        if (dungeonGenerator == null)
        {
            Debug.LogError("DungeonGeneratorV2 not found for EnemyMovement!");
        }
        
        sr = GetComponent<SpriteRenderer>();
    }

    private bool IsPositionWalkable(Vector3 position)
    {
        // 1. ダンジョンデータに基づいて床があるかチェック
        if (dungeonGenerator != null)
        {
            if (!dungeonGenerator.IsWorldPositionWalkable(position))
            {
                return false;
            }
        }

        // 2. 物理的な衝突判定 (他エネミーやプレイヤーとの重なり防止)
        Collider2D hit = Physics2D.OverlapCircle(position, 0.4f);
        if (hit != null)
        {
             // "Wall"タグ, "Enemy"タグ, "Player"タグなどは通行不可
             // ただし、自分自身は除外する必要があるがOverlapCircleは自分も拾う？
             // Physics2D.queriesStartInColliders = false設定によるが、
             // 安全のため自分以外の物体があるかチェック
             
             if (hit.gameObject != gameObject && !hit.isTrigger)
             {
                 return false;
             }
        }

        return true;
    }
    
    public void TakeTurn()
    {
        if (isMoving || player == null) return;
        
        // Stun check (Multi-turn stop) - Priority 1: Decrement even in fog
        if (stunTurns > 0)
        {
            stunTurns--;
            Debug.Log($"[EnemyMovement] {name} is stunned. Remaining turns: {stunTurns}");
            return;
        }
        
        // 霧の中にいる場合は行動しない
        if (dungeonGenerator != null && !dungeonGenerator.IsPositionRevealed(transform.position))
        {
            wasRevealed = false;
            return;
        }

        // プレイヤーによる足止め（衝突用 - One turn skip）
        if (shouldSkipTurn)
        {
            shouldSkipTurn = false;
            return; 
        }

        // 霧から出た瞬間のターンは行動しない（理不尽な急襲を防ぐ）
        if (!wasRevealed)
        {
            wasRevealed = true;
            return; // 待機ターン
        }
        
        // 以下、通常行動
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // プレイヤーまでの距離が3マス以内の場合のみ行動する
        if (distanceToPlayer <= 3f)
        {
            MoveTowardsPlayer();
        }
        // 3マスより遠い場合は何もしない（待機）
    }
    
    private void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        
        // 確率的移動
        float randomFactor = Random.Range(0f, 1f);
        Vector3 moveDirection;
        
        if (randomFactor < 0.7f)
        {
            // 70%の確率でプレイヤーに近づく
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                moveDirection = new Vector3(Mathf.Sign(direction.x), 0, 0);
            }
            else
            {
                moveDirection = new Vector3(0, Mathf.Sign(direction.y), 0);
            }
        }
        else
        {
            // 30%の確率でランダム移動
            moveDirection = GetRandomDirection();
        }
        
        TryStartMove(transform.position + moveDirection);
    }
    
    private void MoveRandomly()
    {
        Vector3 moveDirection = GetRandomDirection();
        TryStartMove(transform.position + moveDirection);
    }
    
    private void TryStartMove(Vector3 destination)
    {
        if (IsPositionWalkable(destination))
        {
            startPosition = transform.position;
            targetPosition = destination;
            moveTimer = 0f;
            isMoving = true;
        }
    }
    
    private Vector3 GetRandomDirection()
    {
        int randomDir = Random.Range(0, 4);
        switch (randomDir)
        {
            case 0: return Vector3.up;
            case 1: return Vector3.down;
            case 2: return Vector3.left;
            case 3: return Vector3.right;
            default: return Vector3.zero;
        }
    }
    
    private void Update()
    {
        // Dance Logic (Stun Animation)
        if (stunTurns > 0)
        {
            if (!isDancing)
            {
                // Enter Dance State
                isDancing = true;
                if (sr != null)
                {
                    originalSprite = sr.sprite;
                    if (danceSprite != null) sr.sprite = danceSprite;
                }
                
                BreathingAnimation anim = GetComponent<BreathingAnimation>();
                if (anim != null) anim.enabled = false;
            }

            danceTimer += Time.deltaTime;
            if (danceTimer >= 0.2f)
            {
                danceTimer = 0f;
                if (sr != null) sr.flipX = !sr.flipX; // Toggle Flip
            }
        }
        else
        {
             if (isDancing)
             {
                 // Exit Dance State
                 isDancing = false;
                 if (sr != null)
                 {
                     // Only restore if we have an original, though BreathingAnim might overwrite anyway
                     if (originalSprite != null) sr.sprite = originalSprite;
                     sr.flipX = false;
                 }
                 
                 BreathingAnimation anim = GetComponent<BreathingAnimation>();
                 if (anim != null) anim.enabled = true;
             }
        }

        if (isMoving)
        {
            moveTimer += Time.deltaTime;
            float percent = Mathf.Clamp01(moveTimer / moveDuration);
            
            // 直線補間
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, percent);
            
            // 垂直方向のオフセット（放物線）
            float height = Mathf.Sin(percent * Mathf.PI) * jumpHeight;
            currentPos.y += height;
            
            transform.position = currentPos;
            
            if (percent >= 1f)
            {
                transform.position = targetPosition; // 位置ズレ補正
                isMoving = false;
            }
        }
    }


    private bool shouldSkipTurn = false;



    public void SkipNextTurn()
    {
        shouldSkipTurn = true;
    }

    private int stunTurns = 0;

    public void Stun(int turns)
    {
        stunTurns = turns;
        Debug.Log($"[EnemyMovement] {name} stunned for {turns} turns.");
    }

    public void WarpToRandomPosition()
    {
        if (dungeonGenerator != null)
        {
            // Find safe position (reuse logic from GameUIManager mostly, but simpler here)
            Vector3 bestPos = transform.position;
            bool found = false;
            
            for (int i = 0; i < 20; i++)
            {
                Vector3 candidate = dungeonGenerator.GetRandomWalkablePosition();
                // Check valid
                Collider2D[] hits = Physics2D.OverlapCircleAll(candidate, 0.4f);
                bool occupied = false;
                foreach(var hit in hits)
                {
                    // Avoid other enemies and player
                    if (hit.GetComponent<EnemyMovement>() != null || hit.CompareTag("Player"))
                    {
                        occupied = true;
                        break;
                    }
                }
                
                if (!occupied)
                {
                    bestPos = candidate;
                    found = true;
                    break;
                }
            }
            
            if (!found) bestPos = dungeonGenerator.GetRandomWalkablePosition(); // Fallback
            
            StartCoroutine(WarpSequence(bestPos));
        }
    }

    private System.Collections.IEnumerator WarpSequence(Vector3 targetPos)
    {
        isMoving = true; // Block normal movement
        
        float duration = 0.5f; // Fast warp for enemies? User said "Fast is ok". match player?
        // User said "Start: Fast is ok" for BULLET speed.
        // For Warp animation, Player was 0.25f. Let's use 0.25f for Enemy too.
        duration = 0.25f;

        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 upPos = startPos + Vector3.up * 1.5f;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = (sr != null) ? sr.color : Color.white;

        // Phase 1: Float Up & Fade Out
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            transform.position = Vector3.Lerp(startPos, upPos, t);
            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(originalColor.a, 0f, t);
                sr.color = c;
            }
            yield return null;
        }

        // Warp
        transform.position = targetPos + Vector3.up * 1.5f; // High pos at target
        // if (dungeonGenerator != null) dungeonGenerator.RevealMap(targetPos); // Disable map reveal for enemy warp

        // Phase 2: Float Down & Fade In
        elapsed = 0f;
        Vector3 landPos = targetPos;
        Vector3 highPos = transform.position;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            transform.position = Vector3.Lerp(highPos, landPos, t);
            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0f, originalColor.a, t);
                sr.color = c;
            }
            yield return null;
        }
        
        transform.position = landPos;
        if (sr != null) sr.color = originalColor;
        
        targetPosition = landPos; // Synch movement target
        startPosition = landPos;
        isMoving = false;
    }
}
