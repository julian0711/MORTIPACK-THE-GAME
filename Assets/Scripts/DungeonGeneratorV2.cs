using UnityEngine;
using System.Collections.Generic;

public class DungeonGeneratorV2 : MonoBehaviour
{
    [Header("ダンジョン設定 (Grid & Size)")]
    [SerializeField] private int gridWidth = 25;
    [SerializeField] private int gridHeight = 25;
    [SerializeField] private int minRooms = 5;
    [SerializeField] private int maxRooms = 10;
    [SerializeField] private int minRoomSize = 4;
    [SerializeField] private int maxRoomSize = 6;
    [SerializeField] private float tileSize = 1f;
    
    [Header("プレハブ設定 (Map Objects)")]
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject doorPrefab1;
    [SerializeField] private GameObject doorPrefab2;

    [Header("装飾・生成率 (Decorations)")]
    [SerializeField] private GameObject bloodPrefab;
    [SerializeField] private float bloodSpawnChance = 0.05f;
    [SerializeField] private float shopDoorChance = 0.5f;
    
    [Header("エネミー設定 (Enemies)")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int minEnemies = 2;
    [SerializeField] private int maxEnemies = 5;
    
    [Header("棚・アイテム設定 (Shelves)")]
    [SerializeField] private GameObject shelfPrefab;
    [SerializeField] private int minShelvesPerRoom = 0;
    [SerializeField] private int maxShelvesPerRoom = 7;
    [SerializeField] private Sprite shelfSearchedSprite;

    [Header("参照 (References)")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private GameObject playerObject;
    public ItemDatabase itemManager; // Assign in Inspector or Find
    
    [Header("アニメーション・スプライト (Visuals)")]
    [SerializeField] private Sprite playerIdleSprite2;
    [SerializeField] private Sprite enemyIdleSprite2;
    [SerializeField] private Sprite enemyDanceSprite; // For Radio Stun Effect
    [SerializeField] private Sprite vendorSprite1;
    [SerializeField] private Sprite vendorSprite2;
    [SerializeField] private Sprite shoppanelSprite1;
    [SerializeField] private Sprite shoppanelSprite2;

    [Header("特殊マス設定")]
    [Range(0, 10)] public int specialTileCount = 0;
    public Sprite specialTileSprite; // Added for Inspector assignment

    [Header("ステージ生成バリエーション")]
    [SerializeField] private float defaultWeight = 50f;
    [SerializeField] private List<DungeonProfile> profiles = new List<DungeonProfile>();

    public Vector3 GetRandomWalkablePosition()
    {
        if (rooms == null || rooms.Count == 0) return Vector3.zero;

        // Try to find a valid spot
        for (int i = 0; i < 20; i++)
        {
            Room randomRoom = rooms[Random.Range(0, rooms.Count)];
            int x = Random.Range(randomRoom.x + 1, randomRoom.x + randomRoom.width - 1);
            int y = Random.Range(randomRoom.y + 1, randomRoom.y + randomRoom.height - 1);

            if (IsTileWalkable(x, y))
            {
                return new Vector3(x * tileSize, y * tileSize, -1f); // Player Z is -1
            }
        }
        
        // Fallback to start room center if random fails
        Vector2Int center = rooms[0].Center();
        return new Vector3(center.x * tileSize, center.y * tileSize, -1f);
    }
    

    
    private int[,] grid;
    private List<Room> rooms = new List<Room>();
    
    private class Room
    {
        public int x, y, width, height;
        public Room(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
        
        public Vector2Int Center()
        {
            return new Vector2Int(x + width / 2, y + height / 2);
        }
    }
    
    private void Awake()
    {
        // Check for Shop Flag from persistent UI
        if (GameUIManager.Instance != null && GameUIManager.Instance.NextStageIsShop)
        {
            Debug.Log("[DungeonGeneratorV2] NextStageIsShop flag detected. Generating Shop.");
            GameUIManager.Instance.NextStageIsShop = false; // Reset flag
            EnterShop();
        }
        else
        {
            // Normal Generation
            GenerateDungeon();
        }
    }

    private void Start()
    {
        TurnManager tm = FindFirstObjectByType<TurnManager>();
        if (tm != null)
        {
            tm.OnTurnCompleted += OnTurnCompleted;
        }
    }
    
    private int radarTurnsRemaining = 0;

    private void OnTurnCompleted()
    {
        if (radarTurnsRemaining > 0)
        {
            radarTurnsRemaining--;
            if (radarTurnsRemaining <= 0)
            {
                ResetEnemiesUnderFog();
                if (GameUIManager.Instance != null)
                {
                    GameUIManager.Instance.ShowMessage("探知機の効果が切れた。");
                }
            }
        }
    }
    
    [Header("Fog Settings")]
    [SerializeField] private bool useFogOfWar = true;
    private GameObject[,] fogGrid;

    [Header("固定ステージデバッグ (Fixed Stage Debug)")]
    public bool useFixedStageDebug = false;
    public GameObject debugFixedStagePrefab;
    
    [Header("ショップ設定 (Shop Settings)")]
    public GameObject shopStagePrefab;

    // Public Properties
    public bool IsFixedStage => isFixedStage;
    public Vector3 CurrentFixedSpawnPoint { get; private set; }

    private void ApplyRandomProfile()
    {
        int currentFloor = GameUIManager.Instance != null ? GameUIManager.Instance.CurrentFloor : 1;
        
        // 1. Valid Profiles (CurrentFloor >= startFloor)
        List<DungeonProfile> validProfiles = new List<DungeonProfile>();
        foreach(var p in profiles)
        {
            if (currentFloor >= p.startFloor)
            {
                validProfiles.Add(p);
            }
        }

        // 2. Weight Calculation
        float totalWeight = defaultWeight;
        foreach(var p in validProfiles) totalWeight += p.weight;

        // 3. Selection
        float randomPoint = Random.Range(0, totalWeight);
        
        // Check Default First
        if (randomPoint < defaultWeight)
        {
            Debug.Log($"[DungeonGenerator] Selected Profile: DEFAULT (Floor: {currentFloor})");
            return; // Use default inspector values
        }
        
        randomPoint -= defaultWeight;

        // Check Profiles
        foreach(var p in validProfiles)
        {
            if (randomPoint < p.weight)
            {
                ApplyProfile(p);
                return;
            }
            randomPoint -= p.weight;
        }
    }

    private void ApplyProfile(DungeonProfile p)
    {
        Debug.Log($"[DungeonGenerator] Selected Profile: {p.profileID}");
        
        // Only override if value > 0 (treating 0 as "Use Common Settings")
        if (p.gridWidth > 0) this.gridWidth = p.gridWidth;
        if (p.gridHeight > 0) this.gridHeight = p.gridHeight;
        if (p.minRooms > 0) this.minRooms = p.minRooms;
        if (p.maxRooms > 0) this.maxRooms = p.maxRooms;
        if (p.minRoomSize > 0) this.minRoomSize = p.minRoomSize;
        if (p.maxRoomSize > 0) this.maxRoomSize = p.maxRoomSize;
        
        if (p.minEnemies > 0) this.minEnemies = p.minEnemies;
        if (p.maxEnemies > 0) this.maxEnemies = p.maxEnemies;
        
        if (p.minShelvesPerRoom > 0) this.minShelvesPerRoom = p.minShelvesPerRoom;
        if (p.maxShelvesPerRoom > 0) this.maxShelvesPerRoom = p.maxShelvesPerRoom;
        
        if (p.specialTileCount > 0) this.specialTileCount = p.specialTileCount;
    }

    private void GenerateDungeon()
    {
        Debug.Log($"[DungeonGeneratorV2] GenerateDungeon Called. useFixedStageDebug: {useFixedStageDebug}, Prefab: {(debugFixedStagePrefab != null ? debugFixedStagePrefab.name : "null")}");

        // 0. Apply Random Profile (Variation)
        ApplyRandomProfile();

        // 1. Fixed Stage Debug Check
        if (useFixedStageDebug && debugFixedStagePrefab != null)
        {
            Debug.Log("[DungeonGeneratorV2] Switching to Fixed Stage Mode (Debug).");
            LoadFixedStage(debugFixedStagePrefab);
            return;
        }

        // 1b. Check nextStagePrefab from GameUIManager
        if (GameUIManager.Instance != null && GameUIManager.Instance.nextStagePrefab != null)
        {
             Debug.Log($"[DungeonGeneratorV2] Switching to Fixed Stage Mode (NextStagePrefab: {GameUIManager.Instance.nextStagePrefab.name}).");
             LoadFixedStage(GameUIManager.Instance.nextStagePrefab);
             return;
        }

        // 2. Normal Dungeon Generation
        isFixedStage = false;
        
        grid = new int[gridWidth, gridHeight];
        if (rooms == null) rooms = new List<Room>();
        rooms.Clear();
        
        // グリッド初期化
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = 1; // 1 = 壁
            }
        }
        
        // 部屋生成
        int targetRooms = Random.Range(minRooms, maxRooms + 1);
        int attempts = 0;
        int maxAttempts = 100; // 無限ループ防止

        while (rooms.Count < targetRooms && attempts < maxAttempts)
        {
            attempts++;
            
            int roomWidth = Random.Range(minRoomSize, maxRoomSize + 1);
            int roomHeight = Random.Range(minRoomSize, maxRoomSize + 1);
            int roomX = Random.Range(1, gridWidth - roomWidth - 1);
            int roomY = Random.Range(1, gridHeight - roomHeight - 1);
            
            Room newRoom = new Room(roomX, roomY, roomWidth, roomHeight);
            
            bool overlaps = false;
            foreach (Room other in rooms)
            {
                if (RoomsOverlap(newRoom, other))
                {
                    overlaps = true;
                    break;
                }
            }
            
            if (!overlaps)
            {
                CreateRoom(newRoom);
                
                if (rooms.Count > 0)
                {
                    ConnectRooms(rooms[rooms.Count - 1], newRoom);
                }
                
                rooms.Add(newRoom);
            }
        }
        
        // 安全策: 試行回数を超えても部屋が1つしかない場合、強制的に終了しないようにログを出すなど（通常発生しにくい）
        if (rooms.Count < 2)
        {
            Debug.LogWarning($"[DungeonGeneratorV2] Failed to generate multiple rooms after {maxAttempts} attempts. Rooms: {rooms.Count}");
        }
        
        // 部屋の数がある程度あれば、ランダムに追加の通路を作ってループさせる
        if (rooms.Count > 3)
        {
            // 全部屋数の約30%〜50%程度の追加パスを試みる
            int extraPaths = Random.Range(rooms.Count / 3, rooms.Count / 2 + 1);
            
            for (int i = 0; i < extraPaths; i++)
            {
                Room roomA = rooms[Random.Range(0, rooms.Count)];
                Room roomB = rooms[Random.Range(0, rooms.Count)];
                
                if (roomA != roomB)
                {
                    ConnectRooms(roomA, roomB);
                }
            }
        }
        
        // タイル生成
        GenerateTiles();
        
        // 血痕、棚、ドア配置
        SpawnBloodStains();
        SpawnShelves();
        SpawnDoors();
        
        // Fog生成
        GenerateFog();
        
        // プレイヤーとエネミー配置
        SpawnPlayer();
        SpawnEnemies();
        
        // Special Tiles (Collect walkable positions first)
        List<Vector2Int> walkablePositions = new List<Vector2Int>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == 0) // Floor
                {
                    walkablePositions.Add(new Vector2Int(x, y));
                }
            }
        }
        SpawnSpecialTiles(this.transform, walkablePositions);
    }
    
    // ... existing helper methods ...
    
    private void GenerateFog()
    {
        if (!useFogOfWar || floorPrefab == null) return;
        
        fogGrid = new GameObject[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 pos = new Vector3(x * tileSize, y * tileSize, -5f); // 手前に表示
                GameObject fog = Instantiate(floorPrefab, pos, Quaternion.identity, transform);
                fog.name = $"Fog_{x}_{y}";
                
                SpriteRenderer sr = fog.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = Color.black;
                    sr.sortingOrder = 100; // 最前面に表示
                }
                
                // コライダーがあれば無効化（移動の邪魔にならないように）
                Collider2D col = fog.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.enabled = false;
                }
                
                fogGrid[x, y] = fog;
            }
        }
    }
    
    public void RevealMap(Vector3 position, bool addScore = true)
    {
        if (!useFogOfWar || fogGrid == null) return;
        
        int cx = Mathf.RoundToInt(position.x / tileSize);
        int cy = Mathf.RoundToInt(position.y / tileSize);
        
        // 3x3の範囲をクリア
        for (int x = cx - 1; x <= cx + 1; x++)
        {
            for (int y = cy - 1; y <= cy + 1; y++)
            {
                if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                {
                    if (fogGrid[x, y] != null)
                    {
                        Destroy(fogGrid[x, y]);
                        fogGrid[x, y] = null;
                        if (addScore && GameUIManager.Instance != null) GameUIManager.Instance.AddScore(10);
                    }
                }
            }
        }
    }

    public void RevealRandomAreas(int areaCount)
    {
        if (!useFogOfWar || fogGrid == null) return;

        for (int i = 0; i < areaCount; i++)
        {
            Vector3 randomPos = GetRandomWalkablePosition();
            // RevealMap already reveals a 3x3 area around the position
            RevealMap(randomPos);
        }
        
        Debug.Log($"[DungeonGeneratorV2] Revealed {areaCount} random areas.");
    }
    
    public bool IsPositionRevealed(Vector3 position)
    {
        if (!useFogOfWar || fogGrid == null) return true; // Fogが無効なら常に「見えている」扱い
        
        int x = Mathf.RoundToInt(position.x / tileSize);
        int y = Mathf.RoundToInt(position.y / tileSize);
        
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return false;
        
        // fogGrid[x,y] が null なら霧が晴れている（Destroy済み）
        return fogGrid[x, y] == null;
    }
    
    private bool RoomsOverlap(Room a, Room b)
    {
        return !(a.x + a.width + 1 < b.x || b.x + b.width + 1 < a.x ||
                 a.y + a.height + 1 < b.y || b.y + b.height + 1 < a.y);
    }
    
    private void CreateRoom(Room room)
    {
        for (int x = room.x; x < room.x + room.width; x++)
        {
            for (int y = room.y; y < room.y + room.height; y++)
            {
                grid[x, y] = 0; // 0 = 床
            }
        }
    }
    
    private void ConnectRooms(Room a, Room b)
    {
        Vector2Int centerA = a.Center();
        Vector2Int centerB = b.Center();
        
        int x = centerA.x;
        int y = centerA.y;
        
        while (x != centerB.x)
        {
            SetFloor(x, y);
            x += (centerB.x > x) ? 1 : -1;
        }
        
        while (y != centerB.y)
        {
            SetFloor(x, y);
            y += (centerB.y > y) ? 1 : -1;
        }
    }

    private void SetFloor(int x, int y)
    {
        // 2x2のブラシサイズで床を塗る（通路を2マス幅にするため）
        for (int i = 0; i <= 1; i++)
        {
            for (int j = 0; j <= 1; j++)
            {
                int nx = x + i;
                int ny = y + j;
                
                // グリッド範囲内かチェック（外周の壁は残すため範囲を厳しくする）
                if (nx >= 1 && nx < gridWidth - 1 && ny >= 1 && ny < gridHeight - 1)
                {
                    grid[nx, ny] = 0;
                }
            }
        }
    }
    
    private void GenerateTiles()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 pos = new Vector3(x * tileSize, y * tileSize, 0);
                
                if (grid[x, y] == 0)
                {
                    if (floorPrefab != null)
                    {
                        GameObject floor = Instantiate(floorPrefab, pos, Quaternion.identity, transform);
                        floor.name = $"Floor_{x}_{y}";
                    }
                }
                else if (grid[x, y] == 1)
                {
                    if (wallPrefab != null && !IsInteriorWall(x, y))
                    {
                        GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity, transform);
                        wall.name = $"Wall_{x}_{y}";
                        wall.tag = "Wall";
                    }
                }
            }
        }
    }
    
    private bool IsInteriorWall(int x, int y)
    {
        int floorNeighbors = 0;
        
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                
                int nx = x + dx;
                int ny = y + dy;
                
                if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight)
                {
                    if (grid[nx, ny] == 0)
                    {
                        floorNeighbors++;
                    }
                }
            }
        }
        
        return floorNeighbors == 0;
    }
    
    private void SpawnBloodStains()
    {
        if (bloodPrefab == null) return;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == 0 && Random.value < bloodSpawnChance)
                {
                    Vector3 pos = new Vector3(x * tileSize, y * tileSize, -0.5f);
                    GameObject blood = Instantiate(bloodPrefab, pos, Quaternion.identity, transform);
                    blood.name = $"Blood_{x}_{y}";
                }
            }
        }
    }
    
    private void SpawnShelves()
    {
        if (shelfPrefab == null) return;
        
        List<InteractableShelf> allShelves = new List<InteractableShelf>();

        foreach (Room room in rooms)
        {
            List<Vector2Int> validPositions = new List<Vector2Int>();
            
            // 部屋の外周（壁際）の座標をリストアップ
            // 左端と右端の列
            for (int y = room.y; y < room.y + room.height; y++)
            {
                CheckAndAddShelfPosition(room.x, y, -1, 0, validPositions); // 左端
                CheckAndAddShelfPosition(room.x + room.width - 1, y, 1, 0, validPositions); // 右端
            }
            
            // 上端と下端の行（角は重複するがList.Containsなどで弾くか、ループ範囲を調整）
            // ここではループ範囲を調整して角重複を防ぐ（yは既に処理済みなので、xの範囲を狭める）
            for (int x = room.x + 1; x < room.x + room.width - 1; x++)
            {
                CheckAndAddShelfPosition(x, room.y, 0, -1, validPositions); // 下端
                CheckAndAddShelfPosition(x, room.y + room.height - 1, 0, 1, validPositions); // 上端
            }
            
            int shelfCount = Random.Range(minShelvesPerRoom, maxShelvesPerRoom + 1);
            
            // ランダムに選んで配置
            for (int i = 0; i < shelfCount && validPositions.Count > 0; i++)
            {
                int index = Random.Range(0, validPositions.Count);
                Vector2Int pos = validPositions[index];
                validPositions.RemoveAt(index);
                
                Vector3 worldPos = new Vector3(pos.x * tileSize, pos.y * tileSize, -0.5f);
                GameObject shelf = Instantiate(shelfPrefab, worldPos, Quaternion.identity, transform);
                shelf.name = $"Shelf_{pos.x}_{pos.y}";
                
                // インタラクション機能の追加
                if (shelfSearchedSprite != null)
                {
                    InteractableShelf interactable = shelf.AddComponent<InteractableShelf>();
                    interactable.Setup(shelfSearchedSprite);
                    
                    // 1. Try Singleton
                    if (itemManager == null) itemManager = ItemDatabase.Instance;
                    // 2. Try FindObject
                    if (itemManager == null) itemManager = Object.FindFirstObjectByType<ItemDatabase>();

                    if (itemManager != null)
                    {
                        interactable.SetDropTable(itemManager.shelfDropTable);
                        // Only log once to avoid spamming
                        if (i == 0) Debug.Log($"[DungeonGeneratorV2] Assigned DropTable to shelves. Item Count: {itemManager.shelfDropTable.Count}");
                    }
                    else
                    {
                         Debug.LogError("[DungeonGeneratorV2] CRITICAL: ItemDatabase not found! Using 'ItemDatabase' component?");
                    }
                    allShelves.Add(interactable);
                    
                    // コライダーがないと検知できないので追加
                    BoxCollider2D col = shelf.GetComponent<BoxCollider2D>();
                    if (col == null)
                    {
                        col = shelf.AddComponent<BoxCollider2D>();
                    }
                    col.isTrigger = true; // 通行は妨げないが検知はする
                }
            }
        }

        // Assign Key to one random shelf
        if (allShelves.Count > 0)
        {
            int keyIndex = Random.Range(0, allShelves.Count);
            InteractableShelf keyShelf = allShelves[keyIndex];
            keyShelf.SetFixedItem("key");
            Debug.Log($"[DungeonGeneratorV2] KEY hidden in shelf at {keyShelf.transform.position}");
        }
        else
        {
            Debug.LogError("[DungeonGeneratorV2] No shelves spawned! Key cannot be placed.");
        }
    }

    private void CheckAndAddShelfPosition(int x, int y, int dirX, int dirY, List<Vector2Int> validPositions)
    {
        // その場所自体が床であることを確認（念のため）
        if (grid[x, y] != 0) return;

        // 壁の「裏側」（部屋の外）を確認
        int checkX = x + dirX;
        int checkY = y + dirY;
        
        // グリッド範囲内かチェック
        if (checkX >= 0 && checkX < gridWidth && checkY >= 0 && checkY < gridHeight)
        {
            // 壁の裏が「壁(1)」なら配置OK。「床(0)」なら通路なのでNG
            if (grid[checkX, checkY] == 1)
            {
                validPositions.Add(new Vector2Int(x, y));
            }
        }
    }
    
    private void SpawnDoors()
    {
        if (doorPrefab1 == null && doorPrefab2 == null) return;
        
        // ドアを配置できる部屋の候補リスト（開始部屋[0]以外）
        List<int> availableRoomIndices = new List<int>();
        for (int i = 1; i < rooms.Count; i++)
        {
            availableRoomIndices.Add(i);
        }
        
        // 候補がない場合は終了
        if (availableRoomIndices.Count == 0) return;
        
        // 1. Shop Door (50% chance)
        if (doorPrefab2 != null && Random.value < shopDoorChance)
        {
            int randomIndex = Random.Range(0, availableRoomIndices.Count);
            int roomIndex = availableRoomIndices[randomIndex];
            
            SpawnDoorAtRoom(roomIndex, doorPrefab2, "DoorShop");
            
            // 使用した部屋をリストから削除
            availableRoomIndices.RemoveAt(randomIndex);
        }
        
        // 2. Normal Door (100% chance)
        if (doorPrefab1 != null && availableRoomIndices.Count > 0)
        {
            int randomIndex = Random.Range(0, availableRoomIndices.Count);
            int roomIndex = availableRoomIndices[randomIndex];
            
            SpawnDoorAtRoom(roomIndex, doorPrefab1, "DoorNormal");
        }
    }
    
    private void SpawnDoorAtRoom(int roomIndex, GameObject prefab, string nameSuffix)
    {
        Room room = rooms[roomIndex];
        Vector2Int center = room.Center();
        Vector3 pos = new Vector3(center.x * tileSize, center.y * tileSize, -0.5f);
        GameObject door = Instantiate(prefab, pos, Quaternion.identity, transform);
        door.name = $"Door_Room{roomIndex}_{nameSuffix}";

        // Ensure door has a trigger collider for detection
        BoxCollider2D col = door.GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = door.AddComponent<BoxCollider2D>();
        }
        col.isTrigger = true;
    }
    
    private void SpawnPlayer()
    {
        if (rooms.Count == 0) return;
        
        Room startRoom = rooms[0];
        Vector2Int center = startRoom.Center();
        Vector3 spawnPos = new Vector3(center.x * tileSize, center.y * tileSize, -1f);
        
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
        }
        
        if (playerObject != null)
        {
            PlayerMovement playerMovement = playerObject.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.InitializePosition(spawnPos);
            }
            else
            {
                playerObject.transform.position = spawnPos;
            }
            
            // アニメーションコンポーネントの追加/設定
            if (playerIdleSprite2 != null)
            {
                BreathingAnimation anim = playerObject.GetComponent<BreathingAnimation>();
                if (anim == null)
                {
                    anim = playerObject.AddComponent<BreathingAnimation>();
                }
                
                SpriteRenderer sr = playerObject.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 10; // プレイヤーは手前
                    anim.Setup(sr.sprite, playerIdleSprite2);
                }
            }
            
            Debug.Log($"Player spawned at tile: ({center.x}, {center.y}) position: {spawnPos}");
            
            // Ensure the starting area is revealed immediately
            RevealMap(spawnPos, false);
        }
    }
    
    private void SpawnEnemies()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("Enemy prefab not assigned in Inspector!");
            return;
        }
        
        int enemyCount = Random.Range(minEnemies, maxEnemies + 1);
        int spawned = 0;

        Vector2Int playerPos = (rooms.Count > 0) ? rooms[0].Center() : new Vector2Int(-1, -1);
        
        // Track spawn counts per room
        Dictionary<Room, int> roomSpawnCounts = new Dictionary<Room, int>();
        foreach(var r in rooms) roomSpawnCounts[r] = 0;

        int maxEnemiesPerRoom = 2; // Increased limit per user request
        int globalAttempts = 0;
        int maxGlobalAttempts = enemyCount * 20; // Safety breaker

        while (spawned < enemyCount && globalAttempts < maxGlobalAttempts)
        {
            globalAttempts++;
            
            // Pick a random room
            if (rooms.Count == 0) break;
            Room room = rooms[Random.Range(0, rooms.Count)];

            // Check room capacity
            if (roomSpawnCounts[room] >= maxEnemiesPerRoom)
            {
                continue; // Try another room
            }

            // Attempt to place in this room
            bool placed = false;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                int x = Random.Range(room.x + 1, room.x + room.width - 1);
                int y = Random.Range(room.y + 1, room.y + room.height - 1);

                // Skip if on player (only for first room/player pos check, though player could be anywhere)
                // Roughly check distance from player start
                if (Mathf.Abs(x - playerPos.x) < 2 && Mathf.Abs(y - playerPos.y) < 2)
                {
                    continue;
                }
                
                if (IsTileWalkable(x, y))
                {
                    Vector3 pos = new Vector3(x * tileSize, y * tileSize, -1f);
                    GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity, transform);
                    enemy.name = $"Mortipack_{spawned}";
                    
                    // アニメーション追加
                    if (enemyIdleSprite2 != null)
                    {
                        BreathingAnimation anim = enemy.AddComponent<BreathingAnimation>();
                        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sortingOrder = 20; // プレイヤー(10)や障害物より上に表示
                            anim.Setup(sr.sprite, enemyIdleSprite2);
                        }
                    }
                    
                    EnemyMovement enemyMovement = enemy.GetComponent<EnemyMovement>();
                    if (enemyMovement != null)
                    {
                        if (turnManager != null)
                        {
                            turnManager.RegisterEnemy(enemyMovement);
                        }
                        
                        // Setup Dance Sprite
                        enemyMovement.SetupDance(enemyDanceSprite);
                    }

                    // Ensure Enemy has a Collider for detection
                    BoxCollider2D col = enemy.GetComponent<BoxCollider2D>();
                    if (col == null)
                    {
                        col = enemy.AddComponent<BoxCollider2D>();
                    }
                    col.isTrigger = true; 
                    
                    Debug.Log($"Enemy spawned at: ({x}, {y}) in Room count: {roomSpawnCounts[room]}");
                    
                    spawned++;
                    roomSpawnCounts[room]++;
                    placed = true;
                    Debug.Log($"Enemy placed: {placed}"); // Silence warning
                    break;
                }
            }
        }
        
        Debug.Log($"Total enemies spawned: {spawned} (Requested: {enemyCount})");
    }
    
    private bool isFixedStage = false;

    public bool IsWorldPositionWalkable(Vector3 worldPosition)
    {
        if (isFixedStage)
        {
            // For Fixed Stages, we rely on Physics2D (Tilemap Colliders)
            // PlayerMovement checks Physics2D separately, but if it calls this, we should return true 
            // to defer to the Physics check, OR perform a BoxCast here.
            
            // Check for Wall Colliders
            Collider2D col = Physics2D.OverlapPoint(worldPosition);
            if (col != null && !col.isTrigger)
            {
                // Solid collider found (Wall)
                return false;
            }
            return true;
        }

        int x = Mathf.RoundToInt(worldPosition.x / tileSize);
        int y = Mathf.RoundToInt(worldPosition.y / tileSize);
        return IsTileWalkable(x, y);
    }

    public bool IsTileWalkable(int x, int y)
    {
        if (isFixedStage) return true; // Fixed stages don't use grid array

        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
        {
            return false;
        }
        
        return grid[x, y] == 0;
    }
    public void ActivateRadar(int turns)
    {
        radarTurnsRemaining = turns;
        RevealAllEnemiesOverFog();
        RevealAllSpecialTiles();
        Debug.Log($"[DungeonGeneratorV2] Radar activated for {turns} turns.");
    }
    
    private void RevealAllSpecialTiles()
    {
        SpecialTile[] tiles = FindObjectsByType<SpecialTile>(FindObjectsSortMode.None);
        foreach (var tile in tiles)
        {
            tile.Reveal();
        }
    }

    private void RevealAllEnemiesOverFog()
    {
        EnemyMovement[] enemies = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Fog is Order 100. Set enemy to 101 to be visible on top.
                sr.sortingOrder = 101; 
            }
        }
    }
    
    private void ResetEnemiesUnderFog()
    {
        EnemyMovement[] enemies = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Default Enemy Sorting Order is 20 (as per SpawnEnemies)
                sr.sortingOrder = 20; 
            }
        }
        
        ResetAllSpecialTiles();
        
        Debug.Log("[DungeonGeneratorV2] Radar effect expired. Enemies hidden under fog.");
    }

    private void ResetAllSpecialTiles()
    {
        SpecialTile[] tiles = FindObjectsByType<SpecialTile>(FindObjectsSortMode.None);
        foreach (var tile in tiles)
        {
            tile.Hide(); // Make invisible again
        }
    }
    public void SpawnShopDoorAt(Vector3 position)
    {
        if (doorPrefab2 == null || rooms == null || rooms.Count == 0) return;

        int x = Mathf.RoundToInt(position.x / tileSize);
        int y = Mathf.RoundToInt(position.y / tileSize);
        
        // Find which room contains this point (or closest)
        Room targetRoom = null;
        float minDistSq = float.MaxValue;

        foreach (var room in rooms)
        {
            // Center of room in grid coords
            Vector2Int center = room.Center();
            float distSq = (center.x - x) * (center.x - x) + (center.y - y) * (center.y - y);
            
            // Check if point is inside room
            if (x >= room.x && x < room.x + room.width && y >= room.y && y < room.y + room.height)
            {
                targetRoom = room;
                break;
            }
            
            // Track closest just in case we are in a corridor
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                targetRoom = room;
            }
        }
        
        if (targetRoom != null)
        {
            Vector2Int center = targetRoom.Center();
            Vector3 finalSpawnPos = Vector3.zero;
            bool foundValidPos = false;

            // Search priority: Center -> Up/Down/Left/Right -> Diagonals
            Vector2Int[] checkOffsets = new Vector2Int[] 
            { 
                Vector2Int.zero, 
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
            };

            foreach(var offset in checkOffsets)
            {
                int checkX = center.x + offset.x;
                int checkY = center.y + offset.y;
                
                // 1. Check if Tile is Walkable (Floor) using Grid
                if (!IsTileWalkable(checkX, checkY)) continue;

                Vector3 worldPos = new Vector3(checkX * tileSize, checkY * tileSize, -0.5f);

                // 2. Check for Overlaps (Existing Doors, Shelves)
                Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 0.3f);
                bool occupied = false;
                foreach(var hit in hits)
                {
                    // Check for Doors or Shelves
                    if (hit.name.Contains("Door") || hit.GetComponent<InteractableShelf>() != null)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                {
                    finalSpawnPos = worldPos;
                    foundValidPos = true;
                    // Debug.Log($"[DungeonGeneratorV2] Found valid spawn pos at {finalSpawnPos} (Offset: {offset})");
                    break;
                }
            }

            if (foundValidPos)
            {
                GameObject door = Instantiate(doorPrefab2, finalSpawnPos, Quaternion.identity, transform);
                door.name = $"Door_Shop_Summoned_{center.x}_{center.y}";
                
                BoxCollider2D col = door.GetComponent<BoxCollider2D>();
                if (col == null)
                {
                    col = door.AddComponent<BoxCollider2D>();
                }
                col.isTrigger = true;
                
                Debug.Log($"[DungeonGeneratorV2] Shop Door Summoned at {finalSpawnPos}");
            }
            else
            {
                Debug.LogWarning("[DungeonGeneratorV2] Could not find empty spot for Shop Door in room!");
            }
        }
    }

    public int BanishEnemies(int maxCount = 0)
    {
        EnemyMovement[] enemies = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        if (enemies.Length == 0) return 0;

        int countToRemove = 0;

        if (maxCount > 0)
        {
            // Use specified fixed count
            countToRemove = maxCount;
        }
        else
        {
            // Default Weight Logic: 1->50%, 2->30%, 3->20%
            float roll = Random.value;
            if (roll < 0.5f) countToRemove = 1;       // 0.0 - 0.5
            else if (roll < 0.8f) countToRemove = 2;  // 0.5 - 0.8
            else countToRemove = 3;                   // 0.8 - 1.0
        }

        int removedCount = 0;
        List<EnemyMovement> enemyList = new List<EnemyMovement>(enemies);
        
        while (countToRemove > 0 && enemyList.Count > 0)
        {
            int index = Random.Range(0, enemyList.Count);
            EnemyMovement target = enemyList[index];
            
            // Effect Implementation (Visuals could be added here if needed)
            Destroy(target.gameObject);
            
            enemyList.RemoveAt(index);
            countToRemove--;
            removedCount++;
        }
        
        Debug.Log($"[DungeonGeneratorV2] Banished {removedCount} enemies (Requested: {(maxCount > 0 ? maxCount.ToString() : "Random")}).");
        return removedCount;
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        if (enemyDanceSprite == null)
        {
             enemyDanceSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/mortipack03.png");
             if (enemyDanceSprite != null)
             {
                 Debug.Log("[DungeonGeneratorV2] Auto-assigned 'enemyDanceSprite' to mortipack03.png");
             }
        }
    }
#endif
    public void LoadFixedStage(GameObject prefab)
    {
        Debug.Log("[DungeonGeneratorV2] Loading Fixed Stage...");
        isFixedStage = true; // Enable Fixed Stage Mode checks

        // 1. Cleanup old children (if any, though usually empty on start)
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // 2. Instantiate Prefab
        GameObject stageObj = Instantiate(prefab, transform);
        stageObj.name = "FixedStage";

        // 3. Setup Player Position
        // 3. Setup Player Position
        FixedMap fixedMap = stageObj.GetComponent<FixedMap>();
        if (fixedMap == null) fixedMap = stageObj.GetComponentInChildren<FixedMap>();

        Vector3 spawnPos = Vector3.zero;
        bool foundSpawn = false;

        if (fixedMap != null && fixedMap.playerSpawnPoint != null)
        {
            // Debug check: only use if flag is true (default true)
            if (fixedMap.debugUseThisSpawn)
            {
                spawnPos = fixedMap.playerSpawnPoint.position;
                foundSpawn = true;
                Debug.Log($"[DungeonGeneratorV2] Using FixedMap SpawnPoint: {spawnPos}");
            }
            else
            {
                Debug.Log("[DungeonGeneratorV2] FixedMap found but 'debugUseThisSpawn' is false. Searching fallback...");
            }
        }
        
        if (!foundSpawn)
        {
            // Fallback: Search by name
            Transform fallbackTransform = RecursiveSearch(stageObj.transform, "SpawnPoint");
            if (fallbackTransform != null)
            {
                spawnPos = fallbackTransform.position;
                foundSpawn = true;
                Debug.Log("[DungeonGeneratorV2] Found 'SpawnPoint' by name (FixedMap missing or unassigned).");
            }
            else
            {
                Debug.LogWarning("[DungeonGeneratorV2] Critical: Player Spawn Point NOT found! Defaulting to (0,0).");
            }
        }
        
        // CORRECTION: Ensure Z is -1 for Player plane (standard for this project)
        spawnPos.z = -1f;

        // Save for Warpcoin usage
        CurrentFixedSpawnPoint = spawnPos; 
        Debug.Log($"[DungeonGeneratorV2] CurrentFixedSpawnPoint set to: {CurrentFixedSpawnPoint}");

        // 4. Move Player
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
        }

        if (playerObject != null)
        {
            Debug.Log($"[DungeonGeneratorV2] Player Found: {playerObject.name}. Moving to {spawnPos}");
            
            PlayerMovement playerMovement = playerObject.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.InitializePosition(spawnPos);
                Debug.Log("[DungeonGeneratorV2] Called InitializePosition on PlayerMovement.");
            }
            else
            {
                playerObject.transform.position = spawnPos;
                Debug.Log("[DungeonGeneratorV2] Moved Player Transform directly.");
            }
            
            // Allow Start implementation to sync target if needed
             // アニメーションコンポーネントの追加/設定 (Copy from SpawnPlayer)
            if (playerIdleSprite2 != null)
            {
                BreathingAnimation anim = playerObject.GetComponent<BreathingAnimation>();
                if (anim == null) anim = playerObject.AddComponent<BreathingAnimation>();
                
                SpriteRenderer sr = playerObject.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 10; 
                    anim.Setup(sr.sprite, playerIdleSprite2);
                }
            }
        }

        // 5. Fog Management?
        // Pour fixed stages, we might want to disable Fog or Reveal All.
        if (useFogOfWar)
        {
            // Disable Fog for fixed stages to avoid black screen.
            Debug.Log("[DungeonGeneratorV2] Fixed Stage loaded. Fog of War disabled for this stage.");
        }
        // 6. Spawn Shop Items (if any spots exist)
        SpawnShopItems(stageObj.transform);
        
        // 7. Spawn Shop Vendor (if any spots exist)
        SpawnShopVendor(stageObj.transform);
        
        // 8. Spawn Special Tiles
        // Note: GetWalkablePositions(grid) is not applicable here as fixed stages don't use a generated grid.
        // Assuming SpawnSpecialTiles will find spots or generate based on stageObj.transform.
        SpawnSpecialTiles(stageObj.transform);

        // 9. Setup Shoppanel Animation
        SetupShopPanelAnimation(stageObj.transform);
    }
    
    private void SpawnShopItems(Transform stageRoot)
    {
        // Find all ShopItemSpots recursively or by name
        List<Transform> spots = new List<Transform>();
        foreach (Transform t in stageRoot.GetComponentsInChildren<Transform>())
        {
            // Allow "ShopItemSpot", "ShopItemSpot (1)", etc.
            if (t.name.StartsWith("ShopItemSpot"))
            {
                spots.Add(t);
            }
        }
        
        if (spots.Count == 0) return;
        
        Debug.Log($"[DungeonGeneratorV2] Found {spots.Count} ShopItemSpots. Spawning items...");
        
        if (itemManager == null) itemManager = ItemDatabase.Instance;
        if (itemManager == null) return;

        // Valid items list
        List<InteractableShelf.DropItem> validItems = new List<InteractableShelf.DropItem>();
        foreach(var item in itemManager.shelfDropTable)
        {
            if (item.key != "nothing" && item.key != "key" && item.key != "report" && item.key != "map")
            {
                validItems.Add(item);
            }
        }
        
        if (validItems.Count == 0) return;

        foreach (Transform spot in spots)
        {
            // Pick random item
            var randomItem = validItems[Random.Range(0, validItems.Count)];
            
            // Create Item Object
            GameObject itemObj = new GameObject($"ShopItem_{randomItem.key}");
            itemObj.transform.position = spot.position;
            itemObj.transform.SetParent(stageRoot);
            
            // Add Sprite
            SpriteRenderer sr = itemObj.AddComponent<SpriteRenderer>();
            Sprite itemSprite = Resources.Load<Sprite>($"item/item_{randomItem.key}");
            if (itemSprite == null) 
            {
                Debug.LogWarning($"[DungeonGeneratorV2] Sprite not found for: item/item_{randomItem.key}. Using fallback.");
                itemSprite = Resources.Load<Sprite>("item/item_doc"); // Fallback
            }
            sr.sprite = itemSprite;
            sr.sortingOrder = 20; // Above floor/spots
            
            // Add Collider for Interaction
            BoxCollider2D col = itemObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.8f); // Slightly smaller than tile

            // Add ShopItem Script
            ShopItem shopItem = itemObj.AddComponent<ShopItem>();
            shopItem.Setup(randomItem.key, randomItem.price);
            
            // Destroy the spot marker visual (optional, or just destroy the whole spot object)
            Destroy(spot.gameObject);
        }
    }

    public void EnterShop()
    {
        if (shopStagePrefab != null)
        {
            Debug.Log("[DungeonGeneratorV2] Entering Shop...");
            LoadFixedStage(shopStagePrefab);
            
            // Delay message to ensure UI is initialized (waits for Start/Update frame)
            StartCoroutine(ShowShopWelcomeMessage());
            
            // Note: Key grant removed as per request.
        }
        else
        {
            Debug.LogError("[DungeonGeneratorV2] Shop Stage Prefab is not assigned in Inspector!");
            if (GameUIManager.Instance != null) GameUIManager.Instance.ShowMessage("ショップデータが見つからない！");
        }
    }

    private System.Collections.IEnumerator ShowShopWelcomeMessage()
    {
        // Wait for one frame to allow MobileInputController.Start() to create the UI
        yield return null;
        
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowMessage(ItemDatabase.Instance.vendorMessageConfig.welcome, "vendor");
        }

        // Setup Trash Can
        GameObject trashObj = GameObject.Find("trash");
        
        if (trashObj == null)
        {
            // Use recursive search 
            Transform trashTrans = RecursiveSearch(transform, "trash");
            if (trashTrans != null) 
            {
                trashObj = trashTrans.gameObject;
            }
            else
            {
                Debug.LogWarning("[DungeonGeneratorV2] Trash object NOT found by RecursiveSearch.");
            }
        }
        else
        {
             Debug.Log($"[DungeonGeneratorV2] Trash object found by GameObject.Find: {trashObj.name}");
        }
        
        if (trashObj != null)
        {
            // Add Collider if missing
            BoxCollider2D col = trashObj.GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = trashObj.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(0.1f, 0.1f); // Extremely reduced size
            }
            else
            {
                 col.isTrigger = true;
                 col.size = new Vector2(0.1f, 0.1f); // Ensure size is small even if existing
            }

            // Add TrashCan Script if missing
            TrashCan trashCan = trashObj.GetComponent<TrashCan>();
            if (trashCan == null)
            {
                trashCan = trashObj.AddComponent<TrashCan>();
            }
        }
    }

    private Transform RecursiveSearch(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = RecursiveSearch(child, name);
            if (result != null) return result;
        }
        return null;
    }
    private void SpawnShopVendor(Transform stageRoot)
    {
        // Find VendorSpot
        Transform vendorSpot = null;
        foreach (Transform t in stageRoot.GetComponentsInChildren<Transform>())
        {
            if (t.name.StartsWith("VendorSpot"))
            {
                vendorSpot = t;
                break;
            }
        }
        
        if (vendorSpot != null)
        {
            Debug.Log("[DungeonGeneratorV2] Found VendorSpot. Spawning Vendor...");
            GameObject vendorObj = new GameObject("ShopVendor");
            vendorObj.transform.position = vendorSpot.position;
            vendorObj.transform.SetParent(stageRoot);
            
            // Sprite Renderer
            SpriteRenderer sr = vendorObj.AddComponent<SpriteRenderer>();
            sr.sprite = vendorSprite1;
            sr.sortingOrder = 20;
            
            // Animation
            if (vendorSprite1 != null && vendorSprite2 != null)
            {
                BreathingAnimation anim = vendorObj.AddComponent<BreathingAnimation>();
                anim.Setup(vendorSprite1, vendorSprite2, 0.5f);
            }
            else
            {
                 Debug.LogWarning("[DungeonGeneratorV2] Vendor Sprites are NOT assigned! Animation will not play.");
            }
            
            // Collider (Trigger)
            BoxCollider2D col = vendorObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.8f);
            
            // Script
            vendorObj.AddComponent<ShopVendor>();
            
            // Clean up spot if needed, or leave it
            // Destroy(vendorSpot.gameObject); 
        }
        else
        {
            // If no explicit spot, maybe spawn at fixed relative pos?
            // Or just do nothing and rely on User placing "VendorSpot"
            Debug.Log("[DungeonGeneratorV2] No VendorSpot found.");
        }
    }

    public void RefreshShopItems()
    {
        Debug.Log("[DungeonGeneratorV2] Refreshing Shop Items...");

        if (itemManager == null) itemManager = ItemDatabase.Instance;
        if (itemManager == null) return;

        // Valid items list (Logic shared with SpawnShopItems)
        List<InteractableShelf.DropItem> validItems = new List<InteractableShelf.DropItem>();
        foreach(var item in itemManager.shelfDropTable)
        {
            if (item.key != "nothing" && item.key != "key" && item.key != "report" && item.key != "map")
            {
                validItems.Add(item);
            }
        }
        
        if (validItems.Count == 0) return;

        // Find all active ShopItem components in the current stage (usually children of stageRoot)
        // If stageRoot is not easily accessible here (local var in Spawn/Load), we search generally or assume this script is on a manager
        // that persists? No, DungeonGeneratorV2 persists?
        // ShopItems are children of the stage instantiated.
        // We can use FindObjectsByType because in the shop scene these are the only ones unique enough.
        
        ShopItem[] currentShopItems = Object.FindObjectsByType<ShopItem>(FindObjectsSortMode.None);
        
        foreach (ShopItem shopItem in currentShopItems)
        {
            // Pick new random item
            var randomItem = validItems[Random.Range(0, validItems.Count)];
            
            // Update Data
            shopItem.Setup(randomItem.key, randomItem.price);
            
            // Update Visuals
            // Name
            shopItem.name = $"ShopItem_{randomItem.key}";
            
            // Sprite
            SpriteRenderer sr = shopItem.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Sprite itemSprite = Resources.Load<Sprite>($"item/item_{randomItem.key}");
                if (itemSprite == null) itemSprite = Resources.Load<Sprite>("item/item_doc");
                sr.sprite = itemSprite;
            }
            
            // Trigger Animation
            StartCoroutine(AnimateItemPop(shopItem.transform));
        }
    }

    private System.Collections.IEnumerator AnimateItemPop(Transform target)
    {
        Vector3 originalScale = Vector3.one; // Assuming default is one
        // If default is not one, we should probably read it, but these are newly spawned or existing.
        // Let's assume 1.0f base.
        
        float duration = 0.2f;
        float elapsed = 0f;
        
        // Scale Up
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Ease Out Back for "Gyun" feel? Or just simple ping pong.
            float scale = Mathf.Lerp(1.0f, 1.5f, t);
            target.localScale = new Vector3(scale, scale, 1.0f);
            yield return null;
        }
        
        // Scale Down
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1.5f, 1.0f, t);
            target.localScale = new Vector3(scale, scale, 1.0f);
            yield return null;
        }
        
        target.localScale = originalScale;
    }

    private void SetupShopPanelAnimation(Transform stageRoot)
    {
        if (shoppanelSprite1 == null || shoppanelSprite2 == null) 
        {
            Debug.LogWarning("[DungeonGeneratorV2] Shoppanel Sprites are NOT assigned! Animation will not play.");
            return;
        }

        int foundCount = 0;
        foreach (Transform t in stageRoot.GetComponentsInChildren<Transform>(true))
        {
            // Match "Shoppanel" case-insensitive
            if (t.name.IndexOf("Shoppanel", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.Log($"[DungeonGeneratorV2] Found Shoppanel: {t.name}. Attaching animation.");
                
                BreathingAnimation anim = t.GetComponent<BreathingAnimation>();
                if (anim == null) anim = t.gameObject.AddComponent<BreathingAnimation>();
                
                // Force visibility
                SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                if (sr == null) sr = t.gameObject.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 20;

                anim.Setup(shoppanelSprite1, shoppanelSprite2, 0.5f);
                foundCount++;
            }
        }
        
        if (foundCount == 0)
        {
             Debug.LogWarning("[DungeonGeneratorV2] No object with name containing 'Shoppanel' found in stage hierarchy!");
        }
    }
    private void SpawnSpecialTiles(Transform stageRoot)
    {
        Debug.Log($"[DungeonGeneratorV2] (FixedStage) SpawnSpecialTiles called. Count: {specialTileCount}");
        // Overload for Fixed Stages
        if (specialTileCount <= 0)
        {
             Debug.Log($"[DungeonGeneratorV2] (FixedStage) SpecialTileCount is {specialTileCount}. Skipping.");
             return;
        }

        // Find potential spawn spots (Floor objects)
        List<Vector3> floorPositions = new List<Vector3>();
        foreach (Transform t in stageRoot.GetComponentsInChildren<Transform>())
        {
            if (t.name.Contains("Floor") || t.tag == "Floor") // Robust check
            {
                floorPositions.Add(t.position);
            }
        }
        
        Debug.Log($"[DungeonGeneratorV2] (FixedStage) Found {floorPositions.Count} floor spots. Spawning {specialTileCount} Special Tiles.");

        if (floorPositions.Count == 0) return;

        for (int i = 0; i < specialTileCount; i++)
        {
            if (floorPositions.Count == 0) break;
            
            int idx = Random.Range(0, floorPositions.Count);
            Vector3 pos = floorPositions[idx];
            floorPositions.RemoveAt(idx);
            
            // Adjust Z to match SpecialTile visibility (usually 0 or slightly above floor)
            // Floors are at Z=0 based on GenerateTiles
            Vector3 spawnPos = new Vector3(pos.x, pos.y, 0f);
            
            GameObject tileObj = new GameObject($"SpecialTile_Fixed_{i}");
            tileObj.transform.position = spawnPos;
            tileObj.transform.SetParent(stageRoot);
            
            // Setup Sprite
            SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
            Sprite sp = specialTileSprite; 
            if (sp == null)
            {
                // Fallback if not assigned in Inspector
                sp = Resources.Load<Sprite>("item/item_report"); // Legacy fallback
                if (sp == null) sp = Resources.Load<Sprite>("item/key");
            }
            sr.sprite = sp;
            sr.sortingOrder = 5; 
            
            tileObj.AddComponent<SpecialTile>();
            
            Debug.Log($"[DungeonGeneratorV2] (FixedStage) Spawned SpecialTile_{i} at {spawnPos}");
        }
    }

    private void SpawnSpecialTiles(Transform stageRoot, List<Vector2Int> walkablePositions)
    {
        if (specialTileCount <= 0) 
        {
            Debug.Log($"[DungeonGeneratorV2] SpecialTileCount is {specialTileCount}. No tiles spawned.");
            return;
        }
        if (walkablePositions == null || walkablePositions.Count == 0) return;
        
        Debug.Log($"[DungeonGeneratorV2] Attempting to spawn {specialTileCount} Special Tiles from {walkablePositions.Count} available spots.");
        
        List<Vector2Int> available = new List<Vector2Int>(walkablePositions);
        
        for (int i = 0; i < specialTileCount; i++)
        {
            if (available.Count == 0) break;
            
            int idx = Random.Range(0, available.Count);
            Vector2Int pos = available[idx];
            available.RemoveAt(idx);
            
            Vector3 worldPos = new Vector3(pos.x * tileSize, pos.y * tileSize, 0);
            
            GameObject tileObj = new GameObject($"SpecialTile_{i}");
            tileObj.transform.position = worldPos;
            tileObj.transform.SetParent(stageRoot);
            
            // Setup Sprite
            SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();

            // Use assigned sprite
            Sprite sp = specialTileSprite;
            if (sp == null)
            {
                // Fallback
                sp = Resources.Load<Sprite>("item/item_report"); 
                if (sp == null) sp = Resources.Load<Sprite>("item/key"); 
                Debug.LogWarning($"[DungeonGeneratorV2] 'specialTileSprite' not assigned in Inspector. Using fallback: {(sp!=null?sp.name:"null")}");
            }
            sr.sprite = sp;
            sr.sortingOrder = 5; // Low order
            
            tileObj.AddComponent<SpecialTile>();
            
            Debug.Log($"[DungeonGeneratorV2] Spawned SpecialTile_{i} at {pos} (World: {worldPos}) with sprite: {(sp != null ? sp.name : "NULL")}");
        }
    }
}

[System.Serializable]
public class DungeonProfile
{
    [Header("Profile Settings")]
    public string profileID;
    public int weight = 10;
    public int startFloor = 1; // この階層以降で出現

    [Header("Dungeon Settings")]
    public int gridWidth = 25;
    public int gridHeight = 25;
    public int minRooms = 5;
    public int maxRooms = 10;
    public int minRoomSize = 4;
    public int maxRoomSize = 6;

    [Header("Enemy Settings")]
    public int minEnemies = 2;
    public int maxEnemies = 5;

    [Header("Shelf Settings")]
    public int minShelvesPerRoom = 0;
    public int maxShelvesPerRoom = 7;

    [Header("Special Tile Settings")]
    public int specialTileCount = 0;
}


