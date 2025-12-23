using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SingleCubeMovement : MonoBehaviour
{
    [Header("Movement")]
    public float duration = 0.18f;
    public float fallThreshold = -8f;

    [Header("Audio")]
    public AudioClip[] moveSounds;
    public AudioClip fallSound;
    public AudioClip breakSound;

    [Header("Debug")]
    public bool showDebug = false;

    private bool isMoving;
    private bool isGrounded = true;
    private bool hasFallen;
    private Vector2Int gridPosition;
    private Vector2Int initialGridPosition;
    private Vector3 initialWorldPosition;
    private Quaternion initialRotation;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private MapCreation mapCreation;

    public Vector2Int GridPosition => gridPosition;
    public bool IsMoving => isMoving;

    void Start()
    {
        mapCreation = FindFirstObjectByType<MapCreation>();

        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
        }
        else if (InputSystem.actions != null)
        {
            moveAction = InputSystem.actions.FindAction("Move");
        }

        Vector3 start = transform.position;
        initialWorldPosition = start;
        initialRotation = transform.rotation;
        gridPosition = new Vector2Int(Mathf.RoundToInt(start.x), Mathf.RoundToInt(start.z));
        initialGridPosition = gridPosition;
        SnapToGridImmediate();
    }

    void Update()
    {
        if (transform.position.y < fallThreshold && !hasFallen)
        {
            hasFallen = true;
            StartCoroutine(HandleFall());
            return;
        }

        if (isMoving || !isGrounded)
        {
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float y = 0f;

        if (Mathf.Abs(x) < 0.1f)
        {
            y = Input.GetAxisRaw("Vertical");
        }

        if (moveAction != null)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            if (Mathf.Abs(moveInput.x) > 0.1f || Mathf.Abs(moveInput.y) > 0.1f)
            {
                x = moveInput.x;
                if (Mathf.Abs(x) < 0.1f)
                {
                    y = moveInput.y;
                }
                else
                {
                    y = 0f;
                }
            }
        }

        Vector3 direction = Vector3.zero;
        if (Mathf.Abs(x) > 0.1f)
        {
            direction = new Vector3(Mathf.Sign(x), 0f, 0f);
        }
        else if (Mathf.Abs(y) > 0.1f)
        {
            direction = new Vector3(0f, 0f, Mathf.Sign(y));
        }

        if (direction != Vector3.zero)
        {
            StartCoroutine(MoveStep(direction));
        }
    }

    private IEnumerator MoveStep(Vector3 dir)
    {
        isMoving = true;
        isGrounded = false;

        MoveTracker.Instance?.RegisterMove();

        if (moveSounds != null && moveSounds.Length > 0)
        {
            int index = Random.Range(0, moveSounds.Length);
            AudioSource.PlayClipAtPoint(moveSounds[index], transform.position);
        }

        Vector2Int delta = new Vector2Int(Mathf.RoundToInt(dir.x), Mathf.RoundToInt(dir.z));
        Vector2Int targetGrid = gridPosition + delta;
        Vector3 start = transform.position;
        Vector3 end = new Vector3(targetGrid.x, initialWorldPosition.y, targetGrid.y);

        Vector3 axis = Vector3.Cross(Vector3.up, dir);
        if (axis.sqrMagnitude < 0.0001f)
        {
            axis = Vector3.right;
        }
        axis.Normalize();

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = startRotation * Quaternion.AngleAxis(90f, axis);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            transform.position = Vector3.Lerp(start, end, progress);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, progress);
            yield return null;
        }

        transform.position = end;
        transform.rotation = targetRotation;
        gridPosition = targetGrid;
        SnapToGridAndCheck();
        isMoving = false;
    }

    private void SnapToGridImmediate()
    {
        Vector3 target = new Vector3(gridPosition.x, initialWorldPosition.y, gridPosition.y);
        transform.position = target;
        transform.rotation = initialRotation;
        isGrounded = true;
    }

    private void SnapToGridAndCheck()
    {
        gridPosition = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
        Vector3 pos = new Vector3(gridPosition.x, initialWorldPosition.y, gridPosition.y);
        transform.position = pos;
        isGrounded = true;
        hasFallen = false;
        ProcessTileUnderneath();
    }

    private void ProcessTileUnderneath()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2f))
        {
            if (hit.collider.CompareTag("Finish"))
            {
                if (showDebug) Debug.Log("Single cube reached Finish — ignored (only full block triggers level complete).");
            }
            else if (hit.collider.name == "BridgeButton")
            {
                if (showDebug) Debug.Log("Bridge Button Activated (single cube)");
                hit.collider.GetComponent<ButtonController>()?.Activate();
            }
            else if (hit.collider.name == "CrossButton")
            {
                if (showDebug) Debug.Log("Split cube cannot activate Cross Button");
            }
            else if (hit.collider.name == "DivisorButton")
            {
                if (showDebug) Debug.Log("Single cube standing on divisor: no split triggered");
            }
            else if (hit.collider.CompareTag("Orange"))
            {
                StartCoroutine(BreakTile(hit.collider.gameObject));
            }
        }
    }

    private IEnumerator HandleFall()
    {
        if (fallSound != null)
        {
            AudioSource.PlayClipAtPoint(fallSound, transform.position);
        }

        if (showDebug) Debug.Log("Single cube fell below threshold, triggering fail.");
        mapCreation?.StartFailSequence();
        ResetToInitialPosition();
        yield return null;
    }

    private IEnumerator BreakTile(GameObject tile)
    {
        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position);
        }

        yield return new WaitForSeconds(0.2f);
        BreakableTile breakable = tile.GetComponent<BreakableTile>();
        if (breakable != null)
        {
            breakable.Fracture();
        }
        else
        {
            Destroy(tile);
        }

        isGrounded = false;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.WakeUp();
        }
    }

    public void ResetToInitialPosition()
    {
        StopAllCoroutines();
        isMoving = false;
        isGrounded = true;
        hasFallen = false;
        gridPosition = initialGridPosition;
        Vector3 reset = new Vector3(initialGridPosition.x, initialWorldPosition.y, initialGridPosition.y);
        transform.position = reset;
        transform.rotation = initialRotation;
    }
}
