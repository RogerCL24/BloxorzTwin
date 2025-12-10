using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// MapCreation instances multiple copies of a tile prefab to build a level
// following the contents of a map file


public class MapCreation : MonoBehaviour
{
    public TextAsset[] maps; 		// Text files containing the maps
    public int currentLevel = 0;
    public GameObject tile, bridge, cross, divisor, final; 	// Tile prefab used to instance and build the level
    [Header("Divisor Prefabs")]
    public GameObject singleBlockPrefabA;
    public GameObject singleBlockPrefabB;
    public GameObject[] breakables; // Array of breakable tile prefabs
    public GameObject bridgeTile; // New bridge tile prefab
    public GlobalAudio globalAudio; // Reference to global audio (optional, auto-found)
    public ScreenFader screenFader; // Optional fade controller (auto-created if null)
    [Header("Physics")]
    public bool overrideGravity = true;
    public Vector3 gravityOverride = new Vector3(0, -8f, 0);
    //cross, divisor, breakable
    
    private PlayerSpawner playerSpawner; // Reference to the PlayerSpawner component
    private Dictionary<Vector2Int, GameObject> levelTiles = new Dictionary<Vector2Int, GameObject>();

    private int mapHeight;
    private Vector3 startPlayerPos;
    private bool levelTransitionRunning = false;
    private bool introInProgress = false;
    private bool introCompleted = false;
    private bool isLoadingMap = false;
    private float playerLiftHeight = 30f;
    private float playerDropHeight = 5f;

    // Start is called once after the MonoBehaviour is created
    void Start()
    {
        // Get the PlayerSpawner component from this GameObject
        playerSpawner = GetComponent<PlayerSpawner>();

        if (globalAudio == null)
            globalAudio = FindFirstObjectByType<GlobalAudio>();
        if (globalAudio == null)
        {
            // Auto-create a global audio manager if none exists
            GameObject audioObj = new GameObject("GlobalAudio");
            globalAudio = audioObj.AddComponent<GlobalAudio>();
        }

        if (screenFader == null)
            screenFader = FindFirstObjectByType<ScreenFader>();
        if (screenFader == null)
        {
            GameObject fadeObj = new GameObject("ScreenFader");
            screenFader = fadeObj.AddComponent<ScreenFader>();
        }

        if (overrideGravity)
        {
            Physics.gravity = gravityOverride;
        }
        EnsureMoveSystems();
        LoadLevel(currentLevel);
    }

    private void EnsureMoveSystems()
    {
        if (MoveTracker.Instance == null)
        {
            GameObject trackerObj = new GameObject("MoveTracker");
            trackerObj.AddComponent<MoveTracker>();
        }

        if (FindFirstObjectByType<MoveDisplay>() == null)
        {
            GameObject displayObj = new GameObject("MoveDisplayUI");
            displayObj.AddComponent<MoveDisplay>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Reloading level...");
            LoadLevel(currentLevel);
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 pos = hit.transform.position;
                int x = Mathf.RoundToInt(pos.x);
                int z = Mathf.RoundToInt(pos.z);
                int row = mapHeight - 1 - z;
                Vector3 relative = new Vector3(x, 0, z) - new Vector3(startPlayerPos.x, 0, startPlayerPos.z);

                Debug.Log($"[DEBUG] Object: {hit.transform.name} | Unity Pos: ({x}, {z}) | Map Coord (Col, Row): ({x}, {row}) | Rel to Start: ({relative.x}, {relative.z})");
            }
        }
    }

    public void LoadLevel(int levelIndex, bool skipFade = false)
    {
        if (isLoadingMap)
        {
            Debug.LogWarning("MapCreation: Level load already running.");
            return;
        }

        StartCoroutine(LoadLevelRoutine(levelIndex, skipFade));
    }

    private IEnumerator LoadLevelRoutine(int levelIndex, bool skipFade)
    {
        if (maps == null || maps.Length == 0)
        {
            yield break;
        }

        if (levelIndex < 0 || levelIndex >= maps.Length)
        {
            Debug.Log("Level index out of bounds or all levels completed.");
            yield break;
        }

        isLoadingMap = true;
        currentLevel = levelIndex;
        MoveTracker.Instance?.ResetCurrentLevel();
        levelTransitionRunning = false;
        introInProgress = false;
        introCompleted = false;

        if (!skipFade && screenFader != null)
        {
            yield return screenFader.FadeTo(1f, 0.4f);
        }

        PreparePlayerForLevelLoad();

        List<(Transform t, Vector3 target)> introTiles = new List<(Transform, Vector3)>();
        Vector3 playerSpawnPos = BuildLevel(levelIndex, introTiles);

        introInProgress = true;
        introCompleted = false;
        yield return StartCoroutine(LevelIntroRoutine(introTiles));

        CompletePlayerPlacement(playerSpawnPos);

        if (!skipFade && screenFader != null)
        {
            yield return screenFader.FadeTo(0f, 0.6f);
        }

        isLoadingMap = false;
    }

    private Vector3 BuildLevel(int levelIndex, List<(Transform t, Vector3 target)> introTiles)
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        levelTiles.Clear();

        TextAsset map = maps[levelIndex];
        string[] allLines = map.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        List<string> mapLines = new List<string>();
        List<string> configLines = new List<string>();
        
        foreach (var line in allLines)
        {
            if (line.Contains(";")) configLines.Add(line);
            else mapLines.Add(line);
        }
        
        int sizeZ = mapLines.Count;
        mapHeight = sizeZ;
        int sizeX = 0;
        foreach (string line in mapLines) if (line.Length > sizeX) sizeX = line.Length;

        char[,] mapGrid = new char[sizeX, sizeZ];
        for(int x=0; x<sizeX; x++) for(int z=0; z<sizeZ; z++) mapGrid[x,z] = 'E';

        Vector3 playerSpawnPos = Vector3.zero;

        for (int i = 0; i < mapLines.Count; i++)
        {
            int z = sizeZ - 1 - i;
            string line = mapLines[i];
            for (int x = 0; x < line.Length; x++)
            {
                char tileChar = line[x];
                mapGrid[x, z] = tileChar;
                
                GameObject tilePrefab = null;
                switch (tileChar)
                {
                    case 'S': // Player spawn point + Normal Tile
                        playerSpawnPos = new Vector3(x, 2, z);
                        startPlayerPos = playerSpawnPos;
                        tilePrefab = tile;
                        break;
                    case 'T': // Normal tile
                        tilePrefab = tile;
                        break;
                    case 'V': // Bridge Tile (New)
                        tilePrefab = bridgeTile;
                        break;
                    case 'B': // Breakable tile
                        if (breakables != null && breakables.Length > 0)
                        {
                            tilePrefab = breakables[UnityEngine.Random.Range(0, breakables.Length)];
                        }
                        break;
                    case 'D': // Divisor tile
                        tilePrefab = divisor;
                        break;
                    case 'H': // Bridge button
                        tilePrefab = bridge;
                        break;
                    case 'X': // Cross tile
                        tilePrefab = cross;
                        break;
                    case 'F': // Final tile
                        tilePrefab = final;
                        break;
                }

                if (tilePrefab != null)
                {
                    Vector3 pos = new Vector3(x, 0, z);
                    Quaternion rotation = Quaternion.identity;
                    if (tileChar == 'X') //hotfix for cross tile rotation
                    {
                        rotation = Quaternion.Euler(0, -90, 0);
                    }
                    GameObject instance = Instantiate(tilePrefab, pos, rotation, this.transform);
                    levelTiles[new Vector2Int(x, z)] = instance;

                    introTiles.Add((instance.transform, pos));

                    if (tileChar == 'F')
                    {
                        instance.tag = "Finish";
                    }
                    else if (tileChar == 'B')
                    {
                        instance.tag = "Orange";
                        if (instance.GetComponent<BreakableTile>() == null)
                            instance.AddComponent<BreakableTile>();
                    }
                    else if (tileChar == 'H')
                    {
                        instance.name = "BridgeButton";
                        if (instance.GetComponent<ButtonController>() == null)
                            instance.AddComponent<ButtonController>();
                    }
                    else if (tileChar == 'X')
                    {
                        instance.name = "CrossButton";
                        if (instance.GetComponent<ButtonController>() == null)
                            instance.AddComponent<ButtonController>();
                    }
                    else if (tileChar == 'D')
                    {
                        instance.name = "DivisorButton";
                        // Aquí podrías agregar un componente específico si lo necesitas
                    }
                    else if (tileChar == 'V')
                    {
                        if (instance.GetComponent<BridgeTile>() == null)
                            instance.AddComponent<BridgeTile>();
                        // Default disabled
                        instance.GetComponent<BridgeTile>().SetState(false, true);
                    }
                }
            }
        }
        
        // Parse Config Lines
        foreach (var line in configLines)
        {
            string[] parts = line.Split(';');
            if (parts.Length < 2) continue;

            string[] btnCoords = parts[0].Split(',');
            if (btnCoords.Length != 2) continue;
            int bx = int.Parse(btnCoords[0].Trim());
            int bRow = int.Parse(btnCoords[1].Trim());
            int bz = sizeZ - 1 - bRow;
            
            if (levelTiles.TryGetValue(new Vector2Int(bx, bz), out GameObject btnObj))
            {
                // Check if it's a DivisorButton or a BridgeButton/CrossButton
                if (btnObj.name == "DivisorButton")
                {
                    // For divisor buttons, expect exactly 2 coordinate pairs for split positions
                    if (parts.Length >= 3)
                    {
                        string[] posACoords = parts[1].Split(',');
                        string[] posBCoords = parts[2].Split(',');
                        
                        if (posACoords.Length == 2 && posBCoords.Length == 2)
                        {
                            int axPos = int.Parse(posACoords[0].Trim());
                            int aRowPos = int.Parse(posACoords[1].Trim());
                            int azPos = sizeZ - 1 - aRowPos;
                            
                            int bxPos = int.Parse(posBCoords[0].Trim());
                            int bRowPos = int.Parse(posBCoords[1].Trim());
                            int bzPos = sizeZ - 1 - bRowPos;
                            
                            // Store split positions in the button GameObject for later retrieval
                            DivisorButtonData dbData = btnObj.GetComponent<DivisorButtonData>();
                            if (dbData == null)
                                dbData = btnObj.AddComponent<DivisorButtonData>();
                            
                            dbData.splitPositionA = new Vector3(axPos, 2, azPos);
                            dbData.splitPositionB = new Vector3(bxPos, 2, bzPos);
                            
                            Debug.Log($"Linked DivisorButton at ({bx},{bRow}) to split positions ({axPos},{aRowPos}) and ({bxPos},{bRowPos})");
                        }
                    }
                }
                else
                {
                    // BridgeButton or CrossButton
                    ButtonController ctrl = btnObj.GetComponent<ButtonController>();
                    if (ctrl != null)
                    {
                        // Detect optional mode token immediately after button coords
                        int startIndex = 1; // default: parts[1] is a tile coord
                        if (parts.Length > 1)
                        {
                            string token = parts[1].Trim();
                            if (string.Equals(token, "O", StringComparison.OrdinalIgnoreCase))
                            {
                                ctrl.mode = ButtonController.Mode.OpenOnly;
                                startIndex = 2;
                            }
                            else if (string.Equals(token, "C", StringComparison.OrdinalIgnoreCase))
                            {
                                ctrl.mode = ButtonController.Mode.CloseOnly;
                                startIndex = 2;
                            }
                        }

                        for (int k = startIndex; k < parts.Length; k++)
                        {
                            string[] tileCoords = parts[k].Split(',');
                            if (tileCoords.Length != 2) continue;
                            int tx = int.Parse(tileCoords[0].Trim());
                            int tRow = int.Parse(tileCoords[1].Trim());
                            int tz = sizeZ - 1 - tRow;
                            
                            if (levelTiles.TryGetValue(new Vector2Int(tx, tz), out GameObject tileObj))
                            {
                                BridgeTile bt = tileObj.GetComponent<BridgeTile>();
                                if (bt != null)
                                {
                                    ctrl.controlledTiles.Add(bt);
                                    Debug.Log($"Linked Button at ({bx},{bRow}) to Bridge at ({tx},{tRow})");
                                }
                                else
                                {
                                    Debug.LogWarning($"Object at ({tx},{tRow}) is not a BridgeTile");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"No tile found at ({tx},{tRow}) [Z={tz}]");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Object at ({bx},{bRow}) is not a Button");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"No button found at ({bx},{bRow}) [Z={bz}]");
            }
        }

        GameObject borderParent = new GameObject("Border");
        borderParent.transform.parent = this.transform;
        borderParent.transform.localPosition = Vector3.zero;

        for (int z = -2; z < sizeZ + 2; z++)
        {
            for (int x = -2; x < sizeX + 2; x++)
            {
                bool isBlock = false;
                if (x >= 0 && x < sizeX && z >= 0 && z < sizeZ)
                {
                    char c = mapGrid[x, z];
                    if (c != 'E' && c != ' ' && c != 0)
                    {
                        isBlock = true;
                    }
                }

                if (!isBlock)
                {
                    GameObject block = new GameObject("BorderBlock");
                    block.transform.position = new Vector3(x, 0, z);
                    block.transform.parent = borderParent.transform;
                    BoxCollider bc = block.AddComponent<BoxCollider>();
                    bc.isTrigger = true;
                    bc.size = new Vector3(1, 1, 1);
                }
            }
        }

        CenterCamera(sizeX, sizeZ);

        return playerSpawnPos;
    }

    private void PreparePlayerForLevelLoad()
    {
        if (playerSpawner == null) return;

        GameObject player = playerSpawner.CurrentPlayer;
        if (player == null)
        {
            player = playerSpawner.SpawnPlayer(new Vector3(0, playerLiftHeight, 0));
        }

        if (player == null) return;

        player.transform.position = new Vector3(0, playerLiftHeight, 0);
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    private void CompletePlayerPlacement(Vector3 spawnPos)
    {
        if (playerSpawner == null) return;

        Vector3 startPos = spawnPos + Vector3.up * playerDropHeight;
        GameObject player = playerSpawner.SpawnPlayer(startPos);
        if (player == null) return;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
        }
    }

    private IEnumerator LevelIntroRoutine(List<(Transform t, Vector3 target)> tiles)
    {
        float drop = 8f;
        float duration = 0.6f;
        float maxDelay = 0.25f;

        List<float> delays = new List<float>(tiles.Count);
        foreach (var entry in tiles)
        {
            entry.t.position = entry.target - Vector3.up * drop;
            delays.Add(UnityEngine.Random.Range(0f, maxDelay));
        }

        float time = 0f;
        while (time < duration + maxDelay)
        {
            time += Time.deltaTime;
            for (int i = 0; i < tiles.Count; i++)
            {
                float t = Mathf.Clamp01((time - delays[i]) / duration);
                t = Mathf.Sin(t * Mathf.PI * 0.5f);
                if (t > 0f)
                {
                    tiles[i].t.position = Vector3.Lerp(tiles[i].target - Vector3.up * drop, tiles[i].target, t);
                }
            }
            yield return null;
        }

        foreach (var entry in tiles)
        {
            entry.t.position = entry.target;
        }

        introInProgress = false;
        introCompleted = true;
    }

    public void StartLevelCompleteSequence()
    {
        if (!levelTransitionRunning)
        {
            StartCoroutine(LevelCompleteRoutine());
        }
    }

    public void StartFailSequence()
    {
        if (levelTransitionRunning || isLoadingMap)
        {
            return;
        }

        StartCoroutine(LevelFailRoutine());
    }

    private IEnumerator LevelCompleteRoutine()
    {
        levelTransitionRunning = true;

        MoveTracker.Instance?.CompleteLevel();

        // Play map change sound
        if (globalAudio != null)
            globalAudio.PlayMapChange();

        // Rise and spin tiles for a dramatic exit
        RiseTilesUpSpin();

        // Give some time for the drop to be visible
        yield return new WaitForSeconds(0.8f);

        // Fade to black
        if (screenFader != null)
            yield return screenFader.FadeTo(1f, 0.6f);

        // Small pause in black
        yield return new WaitForSeconds(0.3f);

        // Advance level
        currentLevel++;
        LoadLevel(currentLevel);

        // Fade back in
        if (screenFader != null)
            yield return screenFader.FadeTo(0f, 0.6f);

        levelTransitionRunning = false;
    }

    private IEnumerator LevelFailRoutine()
    {
        levelTransitionRunning = true;

        // Drop tiles downward to show failure
        DropTilesDown();

        // short delay to show fall
        yield return new WaitForSeconds(0.8f);

        // Fade to black (quick)
        if (screenFader != null)
            yield return screenFader.FadeTo(1f, 0.4f);

        // Give player a breath before level reload
        yield return new WaitForSeconds(2f);

        // Reload current level without duplicating the fade that already ran above
        LoadLevel(currentLevel, true);

        // Wait until the new map is fully built before fading back in
        yield return new WaitUntil(() => introCompleted);

        // Fade back in
        if (screenFader != null)
            yield return screenFader.FadeTo(0f, 0.4f);

        levelTransitionRunning = false;
    }

    private void DropTilesDown()
    {
        foreach (Transform child in transform)
        {
            if (child == null) continue;

            Rigidbody rb = child.GetComponent<Rigidbody>();
            if (rb == null) rb = child.gameObject.AddComponent<Rigidbody>();

            rb.useGravity = true;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.down * UnityEngine.Random.Range(3f, 7f) + UnityEngine.Random.insideUnitSphere * 3f;
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 4f;
        }
    }

    private void RiseTilesUpSpin()
    {
        foreach (Transform child in transform)
        {
            if (child == null) continue;

            Rigidbody rb = child.GetComponent<Rigidbody>();
            if (rb == null) rb = child.gameObject.AddComponent<Rigidbody>();

            rb.useGravity = false;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.up * UnityEngine.Random.Range(3f, 6f) + UnityEngine.Random.insideUnitSphere * 2f;
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 5f;
        }
    }

    void CenterCamera(int sizeX, int sizeZ)
    {
        if (Camera.main == null) return;

        // Calculate the center of the map
        Vector3 mapCenter = new Vector3((sizeX - 1) / 2.0f, 0, (sizeZ - 1) / 2.0f);

        // Find the point on the ground (y=0) that the camera is currently looking at
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 currentLookAt = ray.GetPoint(distance);
            Vector3 offset = mapCenter - currentLookAt;
            
            // Move the camera by the offset to center it on the map
            Camera.main.transform.position += offset;
        }
    }

    public void LoadNextLevel()
    {
        currentLevel++;
        LoadLevel(currentLevel);
    }
}
