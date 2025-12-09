using UnityEngine;

public class MoveTracker : MonoBehaviour
{
    public static MoveTracker Instance { get; private set; }

    private int currentLevelMoves = 0;
    private int totalCompletedMoves = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterMove()
    {
        currentLevelMoves++;
    }

    public void ResetCurrentLevel()
    {
        currentLevelMoves = 0;
    }

    public void CompleteLevel()
    {
        totalCompletedMoves += currentLevelMoves;
        ResetCurrentLevel();
    }

    public int TotalCompletedMoves => totalCompletedMoves;
}
