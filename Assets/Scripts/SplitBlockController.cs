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

    void Update()
    {
        if (isSplit && Input.GetKeyDown(KeyCode.Space))
        {
            SwitchActiveBlock();
        }
        if (isSplit && blockA != null && blockB != null)
        {
            // Si los bloques están adyacentes, se unen
            if (Vector3.Distance(blockA.transform.position, blockB.transform.position) < 1.1f)
            {
                MergeBlocks();
            }
        }
    }

    public void Split(Vector3 posA, Vector3 posB)
    {
        if (isSplit) return;
        isSplit = true;
        splitPositionA = posA;
        splitPositionB = posB;
        // Oculta el bloque principal
        gameObject.SetActive(false);
        // Instancia los dos cubos individuales
        blockA = Instantiate(singleBlockPrefabA, posA, Quaternion.identity);
        blockB = Instantiate(singleBlockPrefabB, posB, Quaternion.identity);
        SetActiveBlock(0);
    }

    void SwitchActiveBlock()
    {
        activeBlock = 1 - activeBlock;
        SetActiveBlock(activeBlock);
    }

    void SetActiveBlock(int idx)
    {
        if (blockA != null && blockB != null)
        {
            blockA.GetComponent<BlockMovement>().enabled = (idx == 0);
            blockB.GetComponent<BlockMovement>().enabled = (idx == 1);
        }
    }

    void MergeBlocks()
    {
        // Destruye los cubos individuales
        Destroy(blockA);
        Destroy(blockB);
        // Reactiva el bloque principal en el punto medio
        Vector3 mid = (splitPositionA + splitPositionB) / 2f;
        gameObject.transform.position = mid;
        gameObject.SetActive(true);
        isSplit = false;
    }
}
