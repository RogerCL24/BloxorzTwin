using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BlockMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float duration = 0.3f;
    
    [Header("Audio")]
    public AudioClip[] moveSounds;
    public AudioClip fallSound;
    public AudioClip breakSound;
    
    [Header("Debug")]
    public bool showDebug = false;

    [Header("Physics Control")]
    [Tooltip("If false, movement uses only calculated placement; collisions are ignored except when enabled for falls/game over.")]
    public bool usePhysicsCollisions = false;
    
    Vector3 scale;
    // actual world-space size of the block (computed from colliders or renderers)
    private Vector3 worldSize;

    public bool isRotating = false;
    float directionX = 0;
    float directionZ = 0;

    float startAngleRad = 0;
    Vector3 startPos;
    float rotationTime = 0;
    float radius = 1;
    Quaternion preRotation;
    Quaternion postRotation;

    public bool isGrounded = false;

    // Track colliders that count as ground to avoid flicker
    private HashSet<Collider> groundColliders = new HashSet<Collider>();
    private HashSet<GameObject> breakingTiles = new HashSet<GameObject>();

    // New Input System (kept as optional fallback)
    private PlayerInput playerInput;
    private InputAction moveAction;

    // initial transform to reset to if falling too far
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    
    private MapCreation mapCreation;

    private LayerMask groundMask;

    /// <summary>
    /// Calcula las posiciones de cuadrícula ocupadas por el bloque según su orientación.
    /// Retorna 1 posición si está de pie, 2 si está acostado.
    /// </summary>
    public Vector2Int[] GetOccupiedGridPositions()
    {
        Vector3 pos = transform.position;
        
        float upDot = Mathf.Abs(Vector3.Dot(transform.up, Vector3.up));
        if (upDot > 0.9f)
        {
            // De pie: solo ocupa una casilla (la posición central)
            int baseX = Mathf.RoundToInt(pos.x);
            int baseZ = Mathf.RoundToInt(pos.z);
            return new Vector2Int[] { new Vector2Int(baseX, baseZ) };
        }
        else
        {
            // Acostado: ocupa dos casillas. La posición está centrada entre ambas (en x.5 o z.5)
            float rightUpDot = Mathf.Abs(Vector3.Dot(transform.right, Vector3.up));
            float forwardUpDot = Mathf.Abs(Vector3.Dot(transform.forward, Vector3.up));

            if (rightUpDot > 0.9f)
            {
                // Acostado a lo largo de X: ocupar dos casillas en X
                // pos.x está en x.5, las dos casillas son floor(x) y ceil(x)
                int x1 = Mathf.FloorToInt(pos.x);
                int x2 = Mathf.CeilToInt(pos.x);
                // Si pos.x es exactamente un entero (raro pero posible), ajustar
                if (x1 == x2) { x1 = x2 - 1; }
                int baseZ = Mathf.RoundToInt(pos.z);
                return new Vector2Int[] { new Vector2Int(x1, baseZ), new Vector2Int(x2, baseZ) };
            }
            else if (forwardUpDot > 0.9f)
            {
                // Acostado a lo largo de Z: ocupar dos casillas en Z
                // pos.z está en z.5, las dos casillas son floor(z) y ceil(z)
                int baseX = Mathf.RoundToInt(pos.x);
                int z1 = Mathf.FloorToInt(pos.z);
                int z2 = Mathf.CeilToInt(pos.z);
                // Si pos.z es exactamente un entero (raro pero posible), ajustar
                if (z1 == z2) { z1 = z2 - 1; }
                return new Vector2Int[] { new Vector2Int(baseX, z1), new Vector2Int(baseX, z2) };
            }
            else
            {
                // Fallback: solo una casilla
                int baseX = Mathf.RoundToInt(pos.x);
                int baseZ = Mathf.RoundToInt(pos.z);
                return new Vector2Int[] { new Vector2Int(baseX, baseZ) };
            }
        }
    }

    /// <summary>
    /// Verifica si el bloque está de pie (orientación vertical).
    /// </summary>
    public bool IsUpright()
    {
        float upDot = Mathf.Abs(Vector3.Dot(transform.up, Vector3.up));
        return upDot > 0.9f;
    }

    void Start()
    {
        mapCreation = FindFirstObjectByType<MapCreation>();
        groundMask = Physics.DefaultRaycastLayers;

        // compute world-space size: prefer colliders on children, then renderers, else lossyScale
        scale = transform.lossyScale;
        // compute combined bounds
        Bounds combined = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;
        Collider[] cols = GetComponentsInChildren<Collider>();
        if (cols != null && cols.Length > 0)
        {
            combined = cols[0].bounds;
            hasBounds = true;
            for (int i = 1; i < cols.Length; i++) combined.Encapsulate(cols[i].bounds);
        }
        else
        {
            Renderer[] rends = GetComponentsInChildren<Renderer>();
            if (rends != null && rends.Length > 0)
            {
                combined = rends[0].bounds;
                hasBounds = true;
                for (int i = 1; i < rends.Length; i++) combined.Encapsulate(rends[i].bounds);
            }
        }

        if (hasBounds)
        {
            worldSize = combined.size;
        }
        else
        {
            worldSize = new Vector3(scale.x, scale.y, scale.z);
        }
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        if (showDebug)
        {
            Debug.Log("lossyScale: " + transform.lossyScale);
            Debug.Log("Computed worldSize (bounds): " + worldSize);
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Debug.Log("Root collider bounds size: " + col.bounds.size);
            }
            else
            {
                Debug.LogWarning("BlockMovement: No root Collider found to report bounds.");
            }
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
        else
        {
            Debug.LogWarning("BlockMovement: No Rigidbody found on " + gameObject.name);
        }
        
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
            if (showDebug)
                Debug.Log("Using PlayerInput component for input");
        }
        else
        {
            // Fallback: Try to find the action directly from the Input System
            if (InputSystem.actions != null)
            {
                moveAction = InputSystem.actions.FindAction("Move");
                if (moveAction != null && showDebug)
                    Debug.Log("Using InputSystem.actions for input");
            }
            
            if (moveAction == null)
            {
                Debug.LogWarning("BlockMovement: No Move action found. Add a PlayerInput component or configure Input Actions.");
            }
        }
    }

    void Update()
    {
        // Cheat keys: load level 0-9
        if (!isRotating)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0)) mapCreation?.LoadLevel(0);
            else if (Input.GetKeyDown(KeyCode.Alpha1)) mapCreation?.LoadLevel(1);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) mapCreation?.LoadLevel(2);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) mapCreation?.LoadLevel(3);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) mapCreation?.LoadLevel(4);
            else if (Input.GetKeyDown(KeyCode.Alpha5)) mapCreation?.LoadLevel(5);
            else if (Input.GetKeyDown(KeyCode.Alpha6)) mapCreation?.LoadLevel(6);
            else if (Input.GetKeyDown(KeyCode.Alpha7)) mapCreation?.LoadLevel(7);
            else if (Input.GetKeyDown(KeyCode.Alpha8)) mapCreation?.LoadLevel(8);
            else if (Input.GetKeyDown(KeyCode.Alpha9)) mapCreation?.LoadLevel(9);
        }

        // Auto-reset if fallen too far
        if (transform.position.y < -8f)
        {
            if (showDebug) Debug.Log("Block fell below threshold, triggering fail sequence");
            TriggerFall();
            return; // skip input this frame
        }

        // Deterministic grounding check (no physics collisions needed)
        isGrounded = CheckGroundedByRaycast();

        if (!isRotating && isGrounded)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = 0;

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
                        y = moveInput.y;
                    else
                        y = 0;
                }
            }

            if (Mathf.Abs(x) > 0.1f || Mathf.Abs(y) > 0.1f)
            {
                float nx = x > 0.1f ? 1 : (x < -0.1f ? -1 : 0);
                float ny = y > 0.1f ? 1 : (y < -0.1f ? -1 : 0);

                directionX = -nx;
                directionZ = ny;

                startPos = transform.position;
                preRotation = transform.rotation;
                transform.Rotate(directionZ * 90, 0, directionX * 90, Space.World);
                postRotation = transform.rotation;
                transform.rotation = preRotation;
                SetRadius();
                rotationTime = 0;
                isRotating = true;
                
                // Disable colliders during movement to prevent getting stuck on edges (e.g. disabled bridges)
                Collider[] cols = GetComponentsInChildren<Collider>();
                foreach (var c in cols) c.enabled = false;
                
                PlayMoveSound();
                
                if (showDebug)
                    Debug.Log("Starting move: direction=[" + directionX + ", " + directionZ + "]");
            }
        }
    }

    void FixedUpdate()
    {
        if (isRotating)
        {
            rotationTime += Time.fixedDeltaTime;
            float ratio = Mathf.Lerp(0, 1, rotationTime / duration);

            float rotAng = Mathf.Lerp(0, Mathf.PI / 2f, ratio);
            float distanceX = -directionX * radius * (Mathf.Cos(startAngleRad) - Mathf.Cos(startAngleRad + rotAng));
            float distanceY = radius * (Mathf.Sin(startAngleRad + rotAng) - Mathf.Sin(startAngleRad));
            float distanceZ = directionZ * radius * (Mathf.Cos(startAngleRad) - Mathf.Cos(startAngleRad + rotAng));
            transform.position = new Vector3(startPos.x + distanceX, startPos.y + distanceY, startPos.z + distanceZ);

            transform.rotation = Quaternion.Lerp(preRotation, postRotation, ratio);

            if (ratio >= 1)
            {
                // Snap to grid & correct height to avoid sinking into tiles
                SnapToGrid();

                isRotating = false;
                directionX = 0;
                directionZ = 0;
                rotationTime = 0;
                
                // Re-enable colliders after movement
                Collider[] cols = GetComponentsInChildren<Collider>();
                foreach (var c in cols) c.enabled = true;
                
                // Re-evaluate grounding deterministically
                isGrounded = CheckGroundedByRaycast();

                if (showDebug)
                    Debug.Log("Move complete. Position: " + transform.position);
            }
        }
    }

    // this method sets the radius of the blocks center to the 4 pivot points, which allows for the motion to work
    void SetRadius()
    {
        Vector3 dirVec = new Vector3(0, 0, 0);
        Vector3 nomVec = Vector3.up;

        if (directionX != 0)
            dirVec = Vector3.right;
        else if (directionZ != 0)
            dirVec = Vector3.forward;

        if (Mathf.Abs(Vector3.Dot(transform.right, dirVec)) > 0.99)
        {                       // moving direction is the same as x of object
            if (Mathf.Abs(Vector3.Dot(transform.up, nomVec)) > 0.99)
            {                   // y axis of global is the same as y of object
                radius = Mathf.Sqrt(Mathf.Pow(worldSize.x / 2f, 2f) + Mathf.Pow(worldSize.y / 2f, 2f));
                startAngleRad = Mathf.Atan2(worldSize.y, worldSize.x);
            }
            else if (Mathf.Abs(Vector3.Dot(transform.forward, nomVec)) > 0.99)
            {       // y axis of global is the same as z of object
                radius = Mathf.Sqrt(Mathf.Pow(worldSize.x / 2f, 2f) + Mathf.Pow(worldSize.z / 2f, 2f));
                startAngleRad = Mathf.Atan2(worldSize.z, worldSize.x);
            }

        }
        else if (Mathf.Abs(Vector3.Dot(transform.up, dirVec)) > 0.99)
        {                   // moving direction is the same as y of object
            if (Mathf.Abs(Vector3.Dot(transform.right, nomVec)) > 0.99)
            {                   // y of global is the same as x of object
                radius = Mathf.Sqrt(Mathf.Pow(worldSize.y / 2f, 2f) + Mathf.Pow(worldSize.x / 2f, 2f));
                startAngleRad = Mathf.Atan2(worldSize.x, worldSize.y);
            }
            else if (Mathf.Abs(Vector3.Dot(transform.forward, nomVec)) > 0.99)
            {       // y axis of global is the same as z of object
                radius = Mathf.Sqrt(Mathf.Pow(worldSize.y / 2f, 2f) + Mathf.Pow(worldSize.z / 2f, 2f));
                startAngleRad = Mathf.Atan2(worldSize.z, worldSize.y);
            }
        }
        else if (Mathf.Abs(Vector3.Dot(transform.forward, dirVec)) > 0.99)
        {           // moving direction is the same as z of object
            if (Mathf.Abs(Vector3.Dot(transform.right, nomVec)) > 0.99)
            {                   // y of global is the same as x of object
                radius = Mathf.Sqrt(Mathf.Pow(worldSize.z / 2f, 2f) + Mathf.Pow(worldSize.x / 2f, 2f));
                startAngleRad = Mathf.Atan2(worldSize.x, worldSize.z);
            }
            else if (Mathf.Abs(Vector3.Dot(transform.up, nomVec)) > 0.99)
            {               // y axis of global is the same as y of object
                radius = Mathf.Sqrt(Mathf.Pow(worldSize.z / 2f, 2f) + Mathf.Pow(worldSize.y / 2f, 2f));
                startAngleRad = Mathf.Atan2(worldSize.y, worldSize.z);
            }
        }
        
        if (showDebug)
            Debug.Log("Radius calculated: " + radius + ", startAngle: " + startAngleRad);
    }

    // Snap position and rotation to grid-aligned values and correct height based on orientation
    public void SnapToGrid()
    {
        Vector3 pos = transform.position;
        // Tile top surface height
        float groundHeight = 0.25f + (0.07f / 2f);

        // Snap X/Z to half-units to avoid 0.5 drift (covers standing and lying)
        pos.x = Mathf.Round(pos.x * 2f) / 2f;
        pos.z = Mathf.Round(pos.z * 2f) / 2f;

        // Determine orientation by which local axis points up
        float upDot = Vector3.Dot(transform.up, Vector3.up);
        float rightUpDot = Mathf.Abs(Vector3.Dot(transform.right, Vector3.up));
        float forwardUpDot = Mathf.Abs(Vector3.Dot(transform.forward, Vector3.up));

        float halfVertical = worldSize.y * 0.5f;
        float halfThickness = Mathf.Min(worldSize.x, Mathf.Min(worldSize.y, worldSize.z)) * 0.5f; // block thickness when lying

        if (upDot > 0.9f)
        {
            pos.y = halfVertical + groundHeight;
        }
        else if (rightUpDot > 0.9f || forwardUpDot > 0.9f)
        {
            pos.y = halfThickness + groundHeight;
        }
        else
        {
            pos.y = halfVertical + groundHeight;
        }

        transform.position = pos;

        // Snap rotation to nearest 90 degrees to avoid floating errors
        Vector3 euler = transform.eulerAngles;
        euler.x = Mathf.Round(euler.x / 90f) * 90f;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        euler.z = Mathf.Round(euler.z / 90f) * 90f;
        transform.eulerAngles = euler;

        // Check for win condition
        float upDotCheck = Vector3.Dot(transform.up, Vector3.up);
        
        // Usar sistema lógico de cuadrícula en lugar de física
        Vector2Int[] occupied = GetOccupiedGridPositions();
        bool isUpright = IsUpright();
        GridManager.Instance?.CheckBlockPosition(occupied, isUpright);

        // Mantener HandleFootContacts como respaldo/compatibilidad si es necesario
        // HandleFootContacts(upDotCheck);

        if (showDebug) Debug.Log("Snapped to grid: " + transform.position + " rot=" + transform.eulerAngles);
    }

    public IEnumerator BreakTileLogic(GameObject tile)
    {
        if (showDebug) Debug.Log("Breaking tile...");
        
        // Optional: Play break sound
        // if (breakSound != null) AudioSource.PlayClipAtPoint(breakSound, transform.position);

        yield return new WaitForSeconds(0.2f); // Wait a bit before breaking
        
        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position);
        }

        BreakableTile breakable = tile.GetComponent<BreakableTile>();
        if (breakable != null)
        {
            breakable.Fracture();
        }
        else
        {
            Destroy(tile);
        }
        
        // Force fall
        isGrounded = false;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.WakeUp();
        }
        breakingTiles.Remove(tile);
    }

    // this method checks if the player hit the ground and enables the movement if it did
    void OnCollisionEnter(Collision theCollision)
    {
        if (!usePhysicsCollisions) return;
        if (showDebug)
            Debug.Log("Collision Enter: " + theCollision.gameObject.name + " (Tag: " + theCollision.gameObject.tag + ")");

        if (IsGroundCollision(theCollision))
        {
            groundColliders.Add(theCollision.collider);
            isGrounded = true;
            if (showDebug) Debug.Log("Block is now GROUNDED (Enter) with " + theCollision.collider.name);
            TryBreakTile(theCollision);
        }
    }
    private void TryBreakTile(Collision collision)
    {
        if (collision == null) return;
        GameObject tile = collision.gameObject;
        if (tile == null || !tile.CompareTag("Orange") || breakingTiles.Contains(tile))
            return;

        float upDot = Mathf.Abs(Vector3.Dot(transform.up, Vector3.up));
        if (upDot > 0.9f)
        {
            foreach (ContactPoint cp in collision.contacts)
            {
                if (Vector3.Dot(cp.normal, Vector3.up) > 0.5f)
                {
                    breakingTiles.Add(tile);
                    StartCoroutine(BreakTileLogic(tile));
                    break;
                }
            }
        }
    }

    void OnCollisionStay(Collision theCollision)
    {
        if (!usePhysicsCollisions) return;
        if (IsGroundCollision(theCollision))
        {
            groundColliders.Add(theCollision.collider);
            isGrounded = true;
            TryBreakTile(theCollision);
        }
    }

    void OnCollisionExit(Collision theCollision)
    {
        if (!usePhysicsCollisions) return;
        if (theCollision.gameObject.CompareTag("Tile") || theCollision.gameObject.CompareTag("Orange"))
        {
            groundColliders.Remove(theCollision.collider);
            isGrounded = groundColliders.Count > 0;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!usePhysicsCollisions) return;
        if (other.gameObject.name == "BorderBlock")
        {
            if (showDebug) Debug.Log("Hit BorderBlock - Forcing Fall");
            isGrounded = false;
            groundColliders.Clear();
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.WakeUp();
                rb.AddForce(Vector3.down * 2f, ForceMode.VelocityChange);
            }
        }
    }

    private bool IsGroundCollision(Collision collision)
    {
        if (collision == null || (!collision.gameObject.CompareTag("Tile") && !collision.gameObject.CompareTag("Orange")))
            return false;

        foreach (ContactPoint cp in collision.contacts)
        {
            if (Vector3.Dot(cp.normal, Vector3.up) > 0.5f)
                return true;
        }

        return false;
    }

    private bool CheckGroundedByRaycast()
    {
        RaycastHit hit;
        // Use a generous distance to cover lying orientations and map intro offsets
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 3f, groundMask, QueryTriggerInteraction.Collide))
        {
            string n = hit.collider.name;
            if (hit.collider.CompareTag("Tile") || hit.collider.CompareTag("Orange") || hit.collider.CompareTag("Finish") || n.Contains("Button"))
            {
                return true;
            }
        }
        return false;
    }


    /// <summary>
    /// Plays a random movement sound from the array
    /// </summary>
    private void PlayMoveSound()
    {
        MoveTracker.Instance?.RegisterMove();

        if (GlobalAudio.Instance != null)
        {
            GlobalAudio.Instance.PlayMove();
            return;
        }

        if (moveSounds != null && moveSounds.Length > 0)
        {
            int index = Random.Range(0, moveSounds.Length);
            AudioSource.PlayClipAtPoint(moveSounds[index], transform.position, 1.0f);
        }
    }

    /// <summary>
    /// Resets the block to its initial position and rotation, and clears velocity
    /// </summary>
    private void ResetToStart()
    {
        if (mapCreation != null)
        {
            mapCreation.LoadLevel(mapCreation.currentLevel);
        }
        else
        {
            // Fallback if no mapCreation found
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = true;
            }
            
            // Reinitialize states
            isRotating = false;
            directionX = 0;
            directionZ = 0;
            rotationTime = 0;
            isGrounded = false;
            
            // Ensure snapped correctly
            SnapToGrid();
        }
        
        if (showDebug) Debug.Log("Reset/Reload level triggered");
    }

    private void TriggerFall()
    {
        if (mapCreation != null)
        {
            mapCreation.StartFailSequence();
        }
        else
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }
}