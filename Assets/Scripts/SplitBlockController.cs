using UnityEngine;


public class SplitBlockController : MonoBehaviour
{
    [Header("Prefabs de cubos individuales")]
    public GameObject singleBlockPrefabA;
    public GameObject singleBlockPrefabB;

    public Vector3 splitPositionA;
    public Vector3 splitPositionB;
    private GameObject blockA;
    private GameObject blockB;
    private bool isSplit = false;
    public bool IsSplit => isSplit;
    private int activeBlock = 0; // 0 = A, 1 = B
    private SingleCubeMovement movementA;
    private SingleCubeMovement movementB;
    private float mergeHeight;

    void Awake()
    {
        mergeHeight = transform.position.y;
    }

    void Update()
    {
        if (isSplit && Input.GetKeyDown(KeyCode.Space))
        {
            SwitchActiveBlock();
        }

        if (isSplit && movementA != null && movementB != null)
        {
            if (!movementA.IsMoving && !movementB.IsMoving)
            {
                Vector2 diff = (Vector2)movementA.GridPosition - (Vector2)movementB.GridPosition;
                if (Mathf.Abs(diff.x) + Mathf.Abs(diff.y) == 1f)
                {
                    MergeBlocks();
                }
            }
        }
    }

    public void Split(Vector3 posA, Vector3 posB)
    {
        if (isSplit) return;

        Debug.Log($"[SplitBlockController] Split() called. PrefabA: {(singleBlockPrefabA != null ? "Assigned" : "NULL")}, PrefabB: {(singleBlockPrefabB != null ? "Assigned" : "NULL")}");

        if (singleBlockPrefabA == null || singleBlockPrefabB == null)
        {
            Debug.LogError("[SplitBlockController] Cannot split: Missing split block prefabs!");
            return;
        }

        isSplit = true;
        splitPositionA = posA;
        splitPositionB = posB;
        mergeHeight = transform.position.y;
        HideMainBlock();

        blockA = Instantiate(singleBlockPrefabA, posA, Quaternion.identity);
        blockB = Instantiate(singleBlockPrefabB, posB, Quaternion.identity);

        if (blockA.GetComponent<SingleCubeMovement>() == null && blockA.GetComponent<BlockMovement>() == null)
        {
            blockA.AddComponent<SingleCubeMovement>();
        }
        if (blockB.GetComponent<SingleCubeMovement>() == null && blockB.GetComponent<BlockMovement>() == null)
        {
            blockB.AddComponent<SingleCubeMovement>();
        }

        movementA = blockA.GetComponent<SingleCubeMovement>();
        movementB = blockB.GetComponent<SingleCubeMovement>();
        SetActiveBlock(0);

        GridManager.Instance?.SetSplitCubes(movementA, movementB);

        Debug.Log($"[SplitBlockController] Split complete. BlockA: {blockA.name}, BlockB: {blockB.name}");
    }

    void SwitchActiveBlock()
    {
        activeBlock = 1 - activeBlock;
        SetActiveBlock(activeBlock);
    }

    void SetActiveBlock(int idx)
    {
        if (blockA == null || blockB == null) return;

        var bmA = blockA.GetComponent<BlockMovement>();
        var bmB = blockB.GetComponent<BlockMovement>();
        var smA = blockA.GetComponent<SingleCubeMovement>();
        var smB = blockB.GetComponent<SingleCubeMovement>();

        if (bmA != null) bmA.enabled = (idx == 0);
        if (bmB != null) bmB.enabled = (idx == 1);
        if (smA != null) smA.enabled = (idx == 0);
        if (smB != null) smB.enabled = (idx == 1);

        var flashA = blockA.GetComponent<SplitCubeFlash>() ?? blockA.AddComponent<SplitCubeFlash>();
        var flashB = blockB.GetComponent<SplitCubeFlash>() ?? blockB.AddComponent<SplitCubeFlash>();

        if (idx == 0)
        {
            flashA.Flash();
        }
        else
        {
            flashB.Flash();
        }
    }

    void MergeBlocks()
    {
        Vector2 centerGrid = Vector2.zero;
        Vector2 posA = Vector2.zero;
        Vector2 posB = Vector2.zero;
        
        if (movementA != null && movementB != null)
        {
            posA = movementA.GridPosition;
            posB = movementB.GridPosition;
            centerGrid = (posA + posB) / 2f;
        }
        else
        {
            posA = new Vector2(splitPositionA.x, splitPositionA.z);
            posB = new Vector2(splitPositionB.x, splitPositionB.z);
            centerGrid = (posA + posB) / 2f;
        }

        DestroySplitCubes();
        ShowMainBlock();
        
        // Posicionar el bloque en el centro
        Vector3 mergePos = new Vector3(centerGrid.x, transform.position.y, centerGrid.y);
        transform.position = mergePos;
        
        // Determinar orientación: el bloque debe estar tumbado en la dirección de los cubos
        Vector2 diff = posB - posA;
        if (Mathf.Abs(diff.x) > 0.1f)
        {
            // Horizontal en X (tumbado en eje X)
            transform.rotation = Quaternion.Euler(0, 0, 90);
        }
        else if (Mathf.Abs(diff.y) > 0.1f)
        {
            // Horizontal en Z (tumbado en eje Z)
            transform.rotation = Quaternion.Euler(90, 0, 0);
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        ResetSplitState();

        BlockMovement mainBM = GetComponent<BlockMovement>();
        if (mainBM != null)
        {
            mainBM.enabled = true;
            mainBM.SnapToGrid();
            GridManager.Instance?.SetMainBlock(mainBM);
        }
        
        GridManager.Instance?.ClearSplitCubes();
    }

    private void HideMainBlock()
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        var mainBM = GetComponent<BlockMovement>();
        if (mainBM != null) mainBM.enabled = false;
    }

    private void ShowMainBlock()
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = true;
        var mainBM = GetComponent<BlockMovement>();
        if (mainBM != null) mainBM.enabled = true;
    }

    private void DestroySplitCubes()
    {
        if (blockA != null) Destroy(blockA);
        if (blockB != null) Destroy(blockB);
        blockA = null;
        blockB = null;
        movementA = null;
        movementB = null;
    }

    private void ResetSplitState()
    {
        isSplit = false;
        activeBlock = 0;
    }

    public void ResetToMainBlock()
    {
        DestroySplitCubes();
        ShowMainBlock();
        ResetSplitState();
        Collider col = GetComponent<Collider>();
        float groundHeight = 0.25f + 0.07f * 0.5f;
        float halfHeight = col != null ? col.bounds.extents.y : 1f;
        mergeHeight = halfHeight + groundHeight;
        Vector3 pos = transform.position;
        pos.y = mergeHeight;
        transform.position = pos;
    }
}
