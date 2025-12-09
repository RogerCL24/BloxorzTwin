using UnityEngine;

/// <summary>
/// Manages the instantiation of the player block at the start of a level.
/// This script replaces the need to have the player pre-loaded in the scene.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject playerPrefab;
    
    [SerializeField]
    private Vector3 spawnPosition = new Vector3(3, 2, 5);

    private GameObject currentPlayer;

    /// <summary>
    /// Instantiates a clone of the player prefab at the spawn position.
    /// </summary>
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
            player.name = "Player"; // Give it a meaningful name in the hierarchy
            currentPlayer = player;
        }
        else
        {
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
