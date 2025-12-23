using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject playerPrefab;
    
    [SerializeField]
    private Vector3 spawnPosition = new Vector3(3, 2, 5);

    private GameObject currentPlayer;

    public GameObject CurrentPlayer => currentPlayer;

    public GameObject SpawnPlayer(Vector3 position)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("PlayerSpawner: Player prefab is not assigned!");
            return null;
        }

        if (currentPlayer == null)
        {
            GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);
            player.name = "Player"; 
            
            if (player.GetComponent<SplitBlockController>() == null)
            {
                player.AddComponent<SplitBlockController>();
            }
            
            currentPlayer = player;
        }
        else
        {
            SplitBlockController splitCtrl = currentPlayer.GetComponent<SplitBlockController>();
            splitCtrl?.ResetToMainBlock();
            currentPlayer.transform.position = position;
            currentPlayer.transform.rotation = Quaternion.identity;
            Rigidbody rb = currentPlayer.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        return currentPlayer;
    }
}
