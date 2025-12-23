using UnityEngine;

/// <summary>
/// Controlador para dividir el bloque principal en dos cubos individuales y alternar el control entre ellos.
/// </summary>
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
        if (movementA != null && movementB != null)
        {
            centerGrid = ((Vector2)movementA.GridPosition + (Vector2)movementB.GridPosition) / 2f;
        }
        else
        {
            centerGrid = new Vector2((splitPositionA.x + splitPositionB.x) / 2f, (splitPositionA.z + splitPositionB.z) / 2f);
        }

        DestroySplitCubes();

        Vector3 mergePosition = new Vector3(centerGrid.x, mergeHeight, centerGrid.y);
        transform.position = mergePosition;

        ShowMainBlock();
        ResetSplitState();
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
        mergeHeight = transform.position.y;
    }
}
