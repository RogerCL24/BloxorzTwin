using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de gestión de cuadrícula 2D para rastrear la posición lógica del jugador
/// y activar botones/mecánicas sin depender del sistema de colisiones físicas.
/// </summary>
public class GridManager : MonoBehaviour
{
    private static GridManager instance;
    public static GridManager Instance => instance;

    // Mapa 2D de tiles y botones: posición -> tipo de tile
    private Dictionary<Vector2Int, TileData> gridMap = new Dictionary<Vector2Int, TileData>();

    // Referencia al jugador principal
    private BlockMovement mainBlock;
    private SingleCubeMovement[] splitCubes;
    
    // Para evitar doble activación de botones en la misma posición
    private HashSet<Vector2Int> processedPositionsThisFrame = new HashSet<Vector2Int>();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public void RegisterTile(Vector2Int gridPos, TileType type, GameObject tileObject)
    {
        gridMap[gridPos] = new TileData { type = type, gameObject = tileObject };
    }

    public void UnregisterTile(Vector2Int gridPos)
    {
        gridMap.Remove(gridPos);
    }

    public void ClearGrid()
    {
        gridMap.Clear();
    }

    public void SetMainBlock(BlockMovement block)
    {
        mainBlock = block;
    }

    public void SetSplitCubes(SingleCubeMovement cubeA, SingleCubeMovement cubeB)
    {
        splitCubes = new SingleCubeMovement[] { cubeA, cubeB };
    }

    public void ClearSplitCubes()
    {
        splitCubes = null;
    }

    /// <summary>
    /// Verifica y activa botones basándose en la posición lógica del bloque principal.
    /// El bloque ocupa 1 tile si está de pie, o 2 tiles si está acostado.
    /// </summary>
    public void CheckBlockPosition(Vector2Int[] occupiedPositions, bool isUpright)
    {
        // Limpiar el registro de posiciones procesadas en este frame
        processedPositionsThisFrame.Clear();
        
        foreach (var pos in occupiedPositions)
        {
            if (gridMap.TryGetValue(pos, out TileData data))
            {
                switch (data.type)
                {
                    case TileType.Finish:
                        if (isUpright && occupiedPositions.Length == 1)
                        {
                            // Solo activa Finish si está de pie en una sola casilla
                            MapCreation map = FindFirstObjectByType<MapCreation>();
                            map?.StartLevelCompleteSequence();
                        }
                        break;

                    case TileType.BridgeButton:
                        // Bridge se activa con cualquier parte del bloque, pero solo una vez por movimiento
                        if (!processedPositionsThisFrame.Contains(pos))
                        {
                            if (data.gameObject != null)
                            {
                                ButtonController btn = data.gameObject.GetComponent<ButtonController>();
                                btn?.Activate();
                                processedPositionsThisFrame.Add(pos);
                            }
                        }
                        break;

                    case TileType.CrossButton:
                        // Cross solo se activa si está de pie (upright) en esa casilla
                        if (isUpright && occupiedPositions.Length == 1)
                        {
                            if (data.gameObject != null)
                            {
                                ButtonController btn = data.gameObject.GetComponent<ButtonController>();
                                btn?.Activate();
                            }
                        }
                        break;

                    case TileType.DivisorButton:
                        // Divisor solo se activa si está de pie
                        if (isUpright && occupiedPositions.Length == 1)
                        {
                            if (data.gameObject != null && mainBlock != null)
                            {
                                DivisorButtonData divisorData = data.gameObject.GetComponent<DivisorButtonData>();
                                SplitBlockController splitCtrl = mainBlock.GetComponent<SplitBlockController>();
                                if (divisorData != null && splitCtrl != null && !splitCtrl.IsSplit)
                                {
                                    splitCtrl.Split(divisorData.splitPositionA, divisorData.splitPositionB);
                                }
                            }
                        }
                        break;

                    case TileType.Orange:
                        // Breakable tile: solo se rompe si el bloque está de pie en esa casilla
                        if (isUpright && occupiedPositions.Length == 1)
                        {
                            if (data.gameObject != null && mainBlock != null)
                            {
                                mainBlock.StartCoroutine(mainBlock.BreakTileLogic(data.gameObject));
                            }
                        }
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Verifica y activa botones basándose en la posición lógica de un single cube.
    /// Los single cubes solo ocupan 1 tile y no pueden activar Cross/Finish/Divisor.
    /// </summary>
    public void CheckSingleCubePosition(Vector2Int position)
    {
        if (gridMap.TryGetValue(position, out TileData data))
        {
            switch (data.type)
            {
                case TileType.BridgeButton:
                    if (data.gameObject != null)
                    {
                        ButtonController btn = data.gameObject.GetComponent<ButtonController>();
                        btn?.Activate();
                    }
                    break;

                // Single cubes no activan: Finish, Cross, Divisor, Orange
            }
        }
    }

    private SingleCubeMovement FindActiveSingleCube(Vector2Int pos)
    {
        if (splitCubes != null)
        {
            foreach (var cube in splitCubes)
            {
                if (cube != null && cube.enabled && cube.GridPosition == pos)
                {
                    return cube;
                }
            }
        }
        return null;
    }

    public TileType GetTileType(Vector2Int pos)
    {
        if (gridMap.TryGetValue(pos, out TileData data))
        {
            return data.type;
        }
        return TileType.Empty;
    }

    public bool HasTileAt(Vector2Int pos)
    {
        return gridMap.ContainsKey(pos);
    }
}

public enum TileType
{
    Empty,
    Normal,
    Finish,
    BridgeButton,
    CrossButton,
    DivisorButton,
    Orange,
    BridgeTile
}

public struct TileData
{
    public TileType type;
    public GameObject gameObject;
}
