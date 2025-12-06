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
    
    [Header("Debug")]
    public bool showDebug = false;
    
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
    
    // New Input System (kept as optional fallback)
    private PlayerInput playerInput;
    private InputAction moveAction;

    // initial transform to reset to if falling too far
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    
    private MapCreation mapCreation;

    void Start()
    {
        mapCreation = FindFirstObjectByType<MapCreation>();

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
            // fallback to transform.lossyScale assuming a 1-unit cube scaled
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

        // Apply initial gravity
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0, -50, 0);
        }
        else
        {
            Debug.LogWarning("BlockMovement: No Rigidbody found on " + gameObject.name);
        }
        
        // Setup New Input System
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
        // Auto-reset if fallen too far
        if (transform.position.y < -8f)
        {
            if (showDebug) Debug.Log("Block fell below threshold, resetting to initial position");
            ResetToStart();
            return; // skip input this frame
        }

        if (!isRotating && isGrounded)
        {
            float x = 0;
            // Classic Input system
            x = Input.GetAxisRaw("Horizontal");
            float y = 0;

            // Read input from New Input System if available
            if (moveAction != null)
            {
                Vector2 moveInput = moveAction.ReadValue<Vector2>();
                // prefer new input if non-zero
                if (Mathf.Abs(moveInput.x) > 0.1f || Mathf.Abs(moveInput.y) > 0.1f)
                {
                    x = moveInput.x;
                    // Only process vertical if no horizontal input
                    if (Mathf.Abs(x) < 0.1f)
                        y = moveInput.y;
                }
                else
                {
                    if (x == 0)
                        y = Input.GetAxisRaw("Vertical");
                }
            }
            else
            {
                if (x == 0)
                    y = Input.GetAxisRaw("Vertical");
            }

            if ((Mathf.Abs(x) > 0.1f || Mathf.Abs(y) > 0.1f) && !isRotating)
            {
                // Normalize to -1, 0, or 1 for trig movement
                float nx = x > 0.1f ? 1 : (x < -0.1f ? -1 : 0);
                float ny = y > 0.1f ? 1 : (y < -0.1f ? -1 : 0);

                // map inputs: horizontal -> X movement, vertical -> Z movement
                // invert horizontal sign so A -> left
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
                
                PlayMoveSound();
                
                if (showDebug)
                    Debug.Log("Starting move: direction=[" + directionX + ", " + directionZ + "]");
            }
        }
        else if (!isGrounded)
        {
            // Small position adjustment to keep physics active
            this.transform.position += new Vector3(0, 0.1f, 0);
            this.transform.position -= new Vector3(0, 0.1f, 0);
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
    private void SnapToGrid()
    {
        Vector3 pos = transform.position;
        // Determine orientation by which local axis points up
        float upDot = Vector3.Dot(transform.up, Vector3.up);
        float rightUpDot = Mathf.Abs(Vector3.Dot(transform.right, Vector3.up));
        float forwardUpDot = Mathf.Abs(Vector3.Dot(transform.forward, Vector3.up));

        // Tile center is at y=0.25, size.y is 0.07.
        // Top surface is at center.y + size.y/2 = 0.25 + 0.035 = 0.285
        float groundHeight = 0.25f + (0.07f / 2f);

        if (upDot > 0.9f)
        {
            // Standing (vertical)
            pos.x = Mathf.Round(pos.x);
            pos.z = Mathf.Round(pos.z);
            pos.y = worldSize.y / 2f + groundHeight; // half height + ground
        }
        else if (rightUpDot > 0.9f)
        {
            // Lying along X (block height equals scale.x)
            // If block is 2 units long (worldSize.y > 1.5), snap to .5, else integer
            if (worldSize.y > 1.5f)
                pos.x = Mathf.Floor(pos.x) + 0.5f;
            else
                pos.x = Mathf.Round(pos.x);

            pos.z = Mathf.Round(pos.z);
            pos.y = worldSize.x / 2f + groundHeight;
        }
        else if (forwardUpDot > 0.9f)
        {
            // Lying along Z
            pos.x = Mathf.Round(pos.x);
            
            if (worldSize.y > 1.5f)
                pos.z = Mathf.Floor(pos.z) + 0.5f;
            else
                pos.z = Mathf.Round(pos.z);

            pos.y = worldSize.z / 2f + groundHeight;
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
        
        // Check what we are standing on
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2.0f))
        {
             if (hit.collider.CompareTag("Finish"))
             {
                 if (Mathf.Abs(upDotCheck) > 0.9f)
                 {
                     if (mapCreation != null)
                     {
                         mapCreation.LoadNextLevel();
                     }
                 }
             }
             else if (hit.collider.name == "BridgeButton")
             {
                 if (showDebug) Debug.Log("Bridge Button Activated");
                 // TODO: Implement bridge activation
             }
             else if (hit.collider.name == "CrossButton")
             {
                 if (Mathf.Abs(upDotCheck) > 0.9f)
                 {
                     if (showDebug) Debug.Log("Cross Button Activated");
                     // TODO: Implement cross bridge activation
                 }
             }
        }

        if (showDebug) Debug.Log("Snapped to grid: " + transform.position + " rot=" + transform.eulerAngles);
    }

    // this method checks if the player hit the ground and enables the movement if it did
    void OnCollisionEnter(Collision theCollision)
    {
        if (showDebug)
            Debug.Log("Collision Enter: " + theCollision.gameObject.name + " (Tag: " + theCollision.gameObject.tag + ")");

        if (theCollision.gameObject.tag == "Tile" || theCollision.gameObject.CompareTag("Tile"))
        // consider only tile collisions
        if (theCollision.gameObject.CompareTag("Tile"))
        {
            // check contact normals to ensure collision is from below
            foreach (ContactPoint cp in theCollision.contacts)
            {
                if (Vector3.Dot(cp.normal, Vector3.up) > 0.5f)
                {
                    groundColliders.Add(theCollision.collider);
                    isGrounded = true;
            if (showDebug)
                Debug.Log("Block is now GROUNDED");
                    if (showDebug) Debug.Log("Block is now GROUNDED (Enter) with " + theCollision.collider.name);
                    break;
                }
            }
        }
    }
    
    void OnCollisionStay(Collision theCollision)
    {
        if (theCollision.gameObject.CompareTag("Tile"))
        {
            bool anyBelow = false;
            foreach (ContactPoint cp in theCollision.contacts)
            {
                if (Vector3.Dot(cp.normal, Vector3.up) > 0.5f)
                {
                    anyBelow = true;
                    break;
                }
            }
            if (anyBelow)
            {
                groundColliders.Add(theCollision.collider);
                isGrounded = true;
                if (showDebug) Debug.Log("Block is GROUNDED (Stay) with " + theCollision.collider.name);
            }
            else
            {
                // if no suitable contact, ensure collider removed
                if (groundColliders.Remove(theCollision.collider))
                {
                    isGrounded = groundColliders.Count > 0;
                    if (showDebug) Debug.Log("Removed collider in Stay; groundedCount=" + groundColliders.Count);
                }
            }
        }
    }
    
    void OnCollisionExit(Collision theCollision)
    {
        if (theCollision.gameObject.CompareTag("Tile"))
        {
            if (groundColliders.Remove(theCollision.collider))
            {
                isGrounded = groundColliders.Count > 0;
                if (showDebug) Debug.Log("Removed collider (Exit); groundedCount=" + groundColliders.Count);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "BorderBlock")
        {
            if (showDebug) Debug.Log("Hit BorderBlock - Forcing Fall");
            isGrounded = false;
            groundColliders.Clear();
            
            // Force physics to take over immediately
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.WakeUp();
                // Optional: Apply a small downward impulse to ensure it doesn't stick
                rb.AddForce(Vector3.down * 2f, ForceMode.VelocityChange);
            }
        }
    }
    
    /// <summary>
    /// Plays a random movement sound from the array
    /// </summary>
    private void PlayMoveSound()
    {
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
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Reinitialize states
        isRotating = false;
        directionX = 0;
        directionZ = 0;
        rotationTime = 0;
        isGrounded = false;
        groundColliders.Clear();
        
        // Ensure snapped correctly
        SnapToGrid();
        
        if (showDebug) Debug.Log("Reset to initial position/rotation");
    }
}