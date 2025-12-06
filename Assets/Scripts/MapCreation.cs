using System;
using UnityEngine;


// MapCreation instances multiple copies of a tile prefab to build a level
// following the contents of a map file


public class MapCreation : MonoBehaviour
{
    public TextAsset[] maps; 		// Text files containing the maps
    public int currentLevel = 0;
    public GameObject tile, bridge, cross, divisor, breakable, final; 	// Tile prefab used to instance and build the level
    public GameObject bridgeTile; // New bridge tile prefab
    //cross, divisor, breakable
    
    private PlayerSpawner playerSpawner; // Reference to the PlayerSpawner component

    // Start is called once after the MonoBehaviour is created
    void Start()
    {
        // Get the PlayerSpawner component from this GameObject
        playerSpawner = GetComponent<PlayerSpawner>();
        LoadLevel(currentLevel);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Reloading level...");
            LoadLevel(currentLevel);
        }
    }

    public void LoadLevel(int levelIndex)
    {
        if (maps == null || maps.Length == 0) return;
        if (levelIndex < 0 || levelIndex >= maps.Length)
        {
            Debug.Log("Level index out of bounds or all levels completed.");
            return;
        }

        // Clear existing level
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        TextAsset map = maps[levelIndex];
        string[] lines = map.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        int sizeZ = lines.Length;
        int sizeX = 0;
        foreach (string line in lines) if (line.Length > sizeX) sizeX = line.Length;

        char[,] mapGrid = new char[sizeX, sizeZ];
        for(int x=0; x<sizeX; x++) for(int z=0; z<sizeZ; z++) mapGrid[x,z] = 'E';

        Vector3 playerSpawnPos = Vector3.zero;
        
        for (int i = 0; i < lines.Length; i++)
        {
            int z = sizeZ - 1 - i;
            string line = lines[i];
            for (int x = 0; x < line.Length; x++)
            {
                char tileChar = line[x];
                mapGrid[x, z] = tileChar;
                
                GameObject tilePrefab = null;
                switch (tileChar)
                {
                    case 'S': // Player spawn point + Normal Tile
                        playerSpawnPos = new Vector3(x, 2, z);
                        tilePrefab = tile;
                        break;
                    case 'T': // Normal tile
                        tilePrefab = tile;
                        break;
                    case 'V': // Bridge Tile (New)
                        tilePrefab = bridgeTile;
                        break;
                    case 'B': // Breakable tile
                        tilePrefab = breakable;
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
                    if (tileChar == 'F')
                    {
                        instance.tag = "Finish";
                    }
                    else if (tileChar == 'H')
                    {
                        instance.name = "BridgeButton";
                    }
                    else if (tileChar == 'X')
                    {
                        instance.name = "CrossButton";
                    }
                }
            }
        }
        
        // Spawn the player after the level is created
        if (playerSpawner != null)
        {
            playerSpawner.SpawnPlayer(playerSpawnPos);
        }
        else
        {
            Debug.LogWarning("MapCreation: PlayerSpawner component not found on this GameObject!");
        }

        // Create border blocks for empty spaces (E) and surroundings
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
                    // Check if it is a valid block type (not E, not space, not null)
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
        // CreateBackground(sizeX, sizeZ);
    }

    // void CreateBackground(int sizeX, int sizeZ)
    // {
    //     GameObject bgObj = new GameObject("OceanBackground");
    //     bgObj.transform.parent = this.transform;
        
    //     // Center the background on the map
    //     float centerX = (sizeX - 1) / 2.0f;
    //     float centerZ = (sizeZ - 1) / 2.0f;
    //     bgObj.transform.position = new Vector3(centerX, -4f, centerZ); // Position below the map

    //     OceanGenerator ocean = bgObj.AddComponent<OceanGenerator>();
        
    //     // Configure ocean size to be large enough
    //     // Map size is roughly sizeX by sizeZ. 
    //     // We want the ocean to extend far beyond
    //     ocean.xSize = 150;
    //     ocean.zSize = 150;
    //     ocean.gridSize = 1.5f;
    // }

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
