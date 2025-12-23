using UnityEngine;

public class MoveTracker : MonoBehaviour
{
    public static MoveTracker Instance { get; private set; }

    private int currentLevelMoves = 0;
    private int totalCompletedMoves = 0;
    private int totalBeforeCurrentLevel = 0;

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

    public void BeginLevelAttempt()
    {
        totalBeforeCurrentLevel = totalCompletedMoves;
        currentLevelMoves = 0;
    }

    public void CompleteLevel()
    {
        totalCompletedMoves += currentLevelMoves;
        currentLevelMoves = 0;
        totalBeforeCurrentLevel = totalCompletedMoves;
    }

    public void FailLevel()
    {
        totalCompletedMoves = totalBeforeCurrentLevel;
        currentLevelMoves = 0;
    }

    public int TotalCompletedMoves => totalCompletedMoves;

    public int DisplayTotalMoves => totalBeforeCurrentLevel + currentLevelMoves;
}
