using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the movement of a 1x2x1 block, replicating Bloxorz mechanics.
/// The script uses a state machine to track the block's orientation (Standing, LyingX, LyingZ)
/// and calculates movement by rotating around the correct bottom edge pivot.
/// After each move, the position and rotation are snapped to the grid to ensure perfect alignment.
/// </summary>
public class MoveCube : MonoBehaviour
{
    // --- Public Fields ---
    [Header("Movement Settings")]
    [Tooltip("Speed of the 90-degree rotation animation in degrees per second.")]
    public float rotationSpeed = 360f;
    [Tooltip("Speed of falling in units per second.")]
    public float fallSpeed = 10f;

    [Header("Audio")]
    [Tooltip("Sounds to play on movement.")]
    public AudioClip[] moveSounds;
    [Tooltip("Sound to play when falling.")]
    public AudioClip fallSound;

    // --- State ---
    private enum Orientation { Standing, LyingX, LyingZ }
    private Orientation currentOrientation = Orientation.Standing;

    private bool isMoving = false;
    private bool isFalling = false;
    private LayerMask groundMask;
    private InputAction moveAction;

    // --- Unity Methods ---
    void Start()
    {
        // Initialize Input and LayerMask
        moveAction = InputSystem.actions.FindAction("Move");
        groundMask = LayerMask.GetMask("Ground");

        // Snap to the nearest grid position on start to ensure alignment
        SnapToGrid();
    }

    void Update()
    {
        if (isFalling)
        {
            // If falling, just move down
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);
            return;
        }

        // Ignore input while a move is in progress
        if (isMoving)
        {
            return;
        }

        // Check if the block is grounded before allowing a move
        if (!IsGrounded())
        {
            isFalling = true;
            if (fallSound != null) AudioSource.PlayClipAtPoint(fallSound, transform.position, 1.5f);
            return;
        }

        // Process player input
        HandleInput();
    }

    // --- Core Logic ---

    /// <summary>
    /// Reads input and triggers the movement coroutine.
    /// </summary>
    private void HandleInput()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 direction = Vector3.zero;

        if (Mathf.Abs(input.x) > 0.9f)
        {
            direction = input.x > 0 ? Vector3.right : Vector3.left;
        }
        else if (Mathf.Abs(input.y) > 0.9f)
        {
            direction = input.y > 0 ? Vector3.forward : Vector3.back;
        }

        if (direction != Vector3.zero)
        {
            StartCoroutine(MoveStep(direction));
        }
    }

    /// <summary>
    /// Coroutine that animates the block's movement over one step.
    /// </summary>
    private IEnumerator MoveStep(Vector3 direction)
    {
        isMoving = true;
        MoveTracker.Instance?.RegisterMove();
        PlayMoveSound();

        Vector3 pivot, axis;
        Orientation nextOrientation = CalculateMovement(direction, out pivot, out axis);

        // Animate the 90-degree rotation
        float angleRemaining = 90f;
        while (angleRemaining > 0)
        {
            float rotationAmount = rotationSpeed * Time.deltaTime;
            if (rotationAmount > angleRemaining)
            {
                rotationAmount = angleRemaining;
            }
            transform.RotateAround(pivot, axis, rotationAmount);
            angleRemaining -= rotationAmount;
            yield return null;
        }

        // Update state and snap to grid
        currentOrientation = nextOrientation;
        SnapToGrid();

        isMoving = false;
    }

    /// <summary>
    /// Calculates the pivot point, rotation axis, and next orientation for a given move.
    /// </summary>
    private Orientation CalculateMovement(Vector3 direction, out Vector3 pivot, out Vector3 axis)
    {
        pivot = Vector3.zero;
        axis = Vector3.zero;
        Orientation nextOrientation = currentOrientation;

        float halfWidth = 0.5f;
        float halfHeight = 1.0f;

        switch (currentOrientation)
        {
            case Orientation.Standing:
                pivot = transform.position + (direction * halfWidth) + (Vector3.down * halfHeight);
                if (direction.x != 0) // Moving along X
                {
                    axis = Vector3.Cross(Vector3.up, direction);
                    nextOrientation = Orientation.LyingX;
                }
                else // Moving along Z
                {
                    axis = Vector3.Cross(Vector3.up, direction);
                    nextOrientation = Orientation.LyingZ;
                }
                break;

            case Orientation.LyingX:
                float pivotXFactor = (direction.x != 0) ? halfHeight : halfWidth;
                pivot = transform.position + (direction * pivotXFactor) + (Vector3.down * halfWidth);
                axis = Vector3.Cross(Vector3.up, direction);
                nextOrientation = (direction.x != 0) ? Orientation.Standing : Orientation.LyingX;
                break;

            case Orientation.LyingZ:
                float pivotZFactor = (direction.z != 0) ? halfHeight : halfWidth;
                pivot = transform.position + (direction * pivotZFactor) + (Vector3.down * halfWidth);
                axis = Vector3.Cross(Vector3.up, direction);
                nextOrientation = (direction.z != 0) ? Orientation.Standing : Orientation.LyingZ;
                break;
        }
        return nextOrientation;
    }

    /// <summary>
    /// Checks if the block is currently on the ground.
    /// Uses one or two raycasts depending on the orientation.
    /// </summary>
    private bool IsGrounded()
    {
        float raycastDistance = 0.1f;
        switch (currentOrientation)
        {
            case Orientation.Standing:
                return Physics.Raycast(transform.position, Vector3.down, transform.localScale.y / 2f + raycastDistance, groundMask);
            case Orientation.LyingX:
                bool ground1 = Physics.Raycast(transform.position + Vector3.right * 0.5f, Vector3.down, transform.localScale.y / 2f + raycastDistance, groundMask);
                bool ground2 = Physics.Raycast(transform.position + Vector3.left * 0.5f, Vector3.down, transform.localScale.y / 2f + raycastDistance, groundMask);
                return ground1 || ground2;
            case Orientation.LyingZ:
                bool ground3 = Physics.Raycast(transform.position + Vector3.forward * 0.5f, Vector3.down, transform.localScale.y / 2f + raycastDistance, groundMask);
                bool ground4 = Physics.Raycast(transform.position + Vector3.back * 0.5f, Vector3.down, transform.localScale.y / 2f + raycastDistance, groundMask);
                return ground3 || ground4;
        }
        return false;
    }

    /// <summary>
    /// Snaps the block's position and rotation to the nearest grid-aligned values.
    /// This is crucial for preventing floating-point inaccuracies.
    /// </summary>
    private void SnapToGrid()
    {
        Vector3 snappedPosition = transform.position;
        switch (currentOrientation)
        {
            case Orientation.Standing:
                snappedPosition.x = Mathf.Round(snappedPosition.x);
                snappedPosition.y = 1.0f;
                snappedPosition.z = Mathf.Round(snappedPosition.z);
                break;
            case Orientation.LyingX:
                snappedPosition.x = Mathf.Round(snappedPosition.x * 2) / 2.0f; // Snap to halves
                snappedPosition.y = 0.5f;
                snappedPosition.z = Mathf.Round(snappedPosition.z);
                break;
            case Orientation.LyingZ:
                snappedPosition.x = Mathf.Round(snappedPosition.x);
                snappedPosition.y = 0.5f;
                snappedPosition.z = Mathf.Round(snappedPosition.z * 2) / 2.0f; // Snap to halves
                break;
        }
        transform.position = snappedPosition;

        // Snap rotation to the nearest 90-degree angle
        Vector3 euler = transform.eulerAngles;
        euler.x = Mathf.Round(euler.x / 90) * 90;
        euler.y = Mathf.Round(euler.y / 90) * 90;
        euler.z = Mathf.Round(euler.z / 90) * 90;
        transform.eulerAngles = euler;
    }

    /// <summary>
    /// Plays a random movement sound from the array.
    /// </summary>
    private void PlayMoveSound()
    {
        if (moveSounds != null && moveSounds.Length > 0)
        {
            int index = Random.Range(0, moveSounds.Length);
            AudioSource.PlayClipAtPoint(moveSounds[index], transform.position, 1.0f);
        }
    }
}