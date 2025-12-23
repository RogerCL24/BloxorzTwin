using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SingleCubeMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float duration = 0.3f;
    public float fallSpeed = 6f;
    
    [Header("Audio")]
    public AudioClip fallSound;
    public AudioClip breakSound;
    
    [Header("Debug")]
    public bool showDebug = false;

    // Actual world-space size of the block (computed from colliders or renderers)
    private Vector3 worldSize;

    private bool isRotating = false;
    private bool isFalling = false;
    private Vector3 rotationAxis;
    private Vector3 rotationPoint;
    private float rotationRemaining = 0f;
    private float rotationDirection = 1f;
    private float rotationSpeedDeg = 300f;
    
    // Rastrear la dirección del movimiento y la posición inicial para calcular la nueva posición
    private int currentMoveX = 0;
    private int currentMoveZ = 0;
    private Vector3 moveStartPosition;

    private bool isGrounded = false;

    // New Input System (kept as optional fallback)
    private PlayerInput playerInput;
    private InputAction moveAction;

    // Initial transform to reset to if falling too far
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector2Int gridPosition;
    
    private MapCreation mapCreation;
    private LayerMask groundMask;
    
    public Vector2Int GridPosition => gridPosition;
    public bool IsMoving => isRotating;

    void Start()
    {
        mapCreation = FindFirstObjectByType<MapCreation>();
        groundMask = ~0;

        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
        }
        else if (InputSystem.actions != null)
        {
            moveAction = InputSystem.actions.FindAction("Move");
        }

        initialPosition = transform.position;
        initialRotation = transform.rotation;
        gridPosition = new Vector2Int(Mathf.RoundToInt(initialPosition.x), Mathf.RoundToInt(initialPosition.z));

        // Compute world-space size: prefer colliders on children, then renderers, else lossyScale
        Vector3 scale = transform.lossyScale;
        // Compute combined bounds
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

        SnapToGrid();
        gridPosition = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
    }

    void Update()
    {
        if (isFalling)
        {
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);
            return;
        }

        if (isRotating)
        {
            float step = rotationSpeedDeg * Time.deltaTime;
            if (step > rotationRemaining)
            {
                step = rotationRemaining;
            }

            transform.RotateAround(rotationPoint, rotationAxis, step * rotationDirection);
            rotationRemaining -= step;

            if (rotationRemaining <= 0.001f)
            {
                CompleteMove();
            }
            return;
        }

        // Cheat keys: load level 0-9
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

        // Auto-reset if fallen too far - SOLO check de limite inferior
        if (transform.position.y < -8f)
        {
            if (showDebug) Debug.Log("Single cube fell below threshold");
            TriggerFall();
            return;
        }

        // Deterministic grounding check - SOLO cuando no está rotando
        if (!isRotating)
        {
            isGrounded = CheckGroundedByRaycast();
            // NO llamar a TriggerFall aqui - permitir que el cubo caiga naturalmente
            // Solo hacer gamefail cuando Y < -8 (controlado arriba)
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        if (moveAction != null)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
            {
                x = moveInput.x;
                z = 0f;
            }
            else if (Mathf.Abs(moveInput.y) > 0.1f)
            {
                z = moveInput.y;
                x = 0f;
            }
        }

        int moveX = 0;
        int moveZ = 0;

        if (Mathf.Abs(x) > 0.1f)
        {
            moveX = x > 0f ? 1 : -1;
        }
        else if (Mathf.Abs(z) > 0.1f)
        {
            moveZ = z > 0f ? 1 : -1;
        }

        if (moveX != 0 || moveZ != 0)
        {
            BeginMove(moveX, moveZ);
        }
    }

    private void BeginMove(int moveX, int moveZ)
    {
        if (moveX != 0 && moveZ != 0)
        {
            return; // ignore diagonal inputs
        }

        // Guardar la posición inicial ANTES de comenzar la rotación
        moveStartPosition = transform.position;
        
        // Guardar la dirección del movimiento para usarla en CompleteMove
        currentMoveX = moveX;
        currentMoveZ = moveZ;

        rotationSpeedDeg = 90f / Mathf.Max(0.01f, duration);
        rotationRemaining = 90f;
        rotationDirection = 1f;

<<<<<<< HEAD
=======
        // Para un cubo 1x1x1, el punto de pivote está en la arista inferior
        // hacia la dirección del movimiento.
        // El centro del cubo está a 0.5 del suelo, y el pivote está en el suelo (y = groundHeight)
        float groundHeight = 0.25f + (0.07f / 2f); // misma altura que en SnapToGrid
        float pivotY = transform.position.y - 0.5f; // el pivote está en el suelo, no en -0.5 relativo al centro
>>>>>>> 077e0d90bf67bc83022425ef91f24c6beec0c7ab
        Vector3 pos = transform.position;
        float pivotY = pos.y; // pivote en la superficie del tile, no en la mitad del cubo

        if (moveX > 0)
        {
            rotationDirection = -1f;
            rotationAxis = Vector3.forward;
            rotationPoint = new Vector3(pos.x + 0.5f, pivotY, pos.z);
        }
        else if (moveX < 0)
        {
            rotationDirection = 1f;
            rotationAxis = Vector3.forward;
            rotationPoint = new Vector3(pos.x - 0.5f, pivotY, pos.z);
        }
        else if (moveZ > 0)
        {
            rotationDirection = 1f;
            rotationAxis = Vector3.right;
            rotationPoint = new Vector3(pos.x, pivotY, pos.z + 0.5f);
        }
        else if (moveZ < 0)
        {
            rotationDirection = -1f;
            rotationAxis = Vector3.right;
            rotationPoint = new Vector3(pos.x, pivotY, pos.z - 0.5f);
        }
        else
        {
            return;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // "Modo Fantasma": Se mueve pero no choca ni empuja
        }

        isRotating = true;
        isFalling = false;

        // No desactivar colliders durante la rotación - dejar que la física sea más robusta
        // Collider[] cols = GetComponentsInChildren<Collider>();
        // foreach (var c in cols) c.enabled = false;

        PlayMoveSound();

        if (showDebug)
            Debug.Log($"Single cube begin move dir=({moveX},{moveZ})");
    }

    private void CompleteMove()
    {

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false; // "Modo Real": Vuelve a tener peso y gravedad
        }
        isRotating = false;
        rotationRemaining = 0f;

        // Calcular la nueva posición correcta basándose en la posición INICIAL y el movimiento realizado
        // El cubo se mueve 1 unidad en la dirección especificada
        Vector3 newPos = moveStartPosition;
        newPos.x = Mathf.Round(newPos.x + currentMoveX); // mover 1 unidad en X si es necesario
        newPos.z = Mathf.Round(newPos.z + currentMoveZ); // mover 1 unidad en Z si es necesario
        
        // Establecer la nueva posición (manteniendo la Y que resultó de RotateAround)
        transform.position = new Vector3(newPos.x, transform.position.y, newPos.z);
        
        // SnapToGrid SOLO ajustará Y - X/Z ya están correctos
        SnapToGrid();
        
        // Limpiar variables
        currentMoveX = 0;
        currentMoveZ = 0;
        moveStartPosition = Vector3.zero;

        gridPosition = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
        GridManager.Instance?.CheckSingleCubePosition(gridPosition);

<<<<<<< HEAD
        // Después de SnapToGrid, el cubo debería estar definitivamente grounded
        isGrounded = true; // forzar grounded después de snap
=======
        isGrounded = CheckGroundedByRaycast();
        if (!isGrounded)
        {
            TriggerFall();
        }
>>>>>>> 077e0d90bf67bc83022425ef91f24c6beec0c7ab
        isFalling = false;

        if (showDebug)
            Debug.Log("Single cube move complete. Position: " + transform.position);
    }

    private void SnapToGrid()
    {
        Vector3 pos = transform.position;
        
        // Calcular la altura correcta basándose en la orientación actual del cubo
        float tileHeight = 0.25f; // altura del tile
        float cubeHalfSize = 0.5f; // mitad del cubo
        float targetY = tileHeight + cubeHalfSize; // superficie del tile + mitad del cubo
        
        // Evitar saltitos: interpolar suavemente hacia la altura correcta
        // si no está ya cerca de ella
        if (Mathf.Abs(pos.y - targetY) > 0.01f)
        {
            // Hacer una transición suave en lugar de un cambio abrupto
            pos.y = targetY;
        }
        // Si ya está muy cerca, dejar como está para evitar jitter

        transform.position = pos;

<<<<<<< HEAD
        // NO resetear rotación - dejar que tenga la rotación que resultó de RotateAround()
        // La rotación visual es parte de la animación y debe ser preservada
=======
        // Snap rotation to nearest 90 degrees AND normalize to identity
        // Un cubo 1x1x1 debería verse igual en cualquier rotación múltiplo de 90
        // pero para evitar acumulación de errores, normalizar a identidad
        //transform.rotation = Quaternion.identity;

        Vector3 euler = transform.eulerAngles;
        euler.x = Mathf.Round(euler.x / 90f) * 90f;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        euler.z = Mathf.Round(euler.z / 90f) * 90f;
        transform.eulerAngles = euler;

        // Alternativa: snap a 90 grados si quieres mantener la rotación visual
        // Vector3 euler = transform.eulerAngles;
        // euler.x = Mathf.Round(euler.x / 90f) * 90f;
        // euler.y = Mathf.Round(euler.y / 90f) * 90f;
        // euler.z = Mathf.Round(euler.z / 90f) * 90f;
        // transform.eulerAngles = euler;
>>>>>>> 077e0d90bf67bc83022425ef91f24c6beec0c7ab
    }

    private bool CheckGroundedByRaycast()
    {
        RaycastHit hit;
        // Lanzamos un rayo hacia abajo
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 3f, groundMask, QueryTriggerInteraction.Ignore))
        {
            // TRUCO: Si el objeto que tocamos NO es un Trigger (es solido), es suelo valido.
            if (!hit.collider.isTrigger)
            {
                return true;
            }
        }
        return false;
    }

    private void TriggerFall()
    {
        if (isFalling) return;

        // NO activar fall si estamos rotando - el movimiento es válido
        if (isRotating)
        {
            if (showDebug) Debug.Log("Ignoring fall trigger during rotation");
            return;
        }

        // Solo activar gamefail si está REALMENTE muy bajo
        if (transform.position.y >= -8f)
        {
            // El cubo está cayendo pero aún dentro del mapa - dejar que caiga
            // No llamar a StartFailSequence aún
            return;
        }

        isFalling = true;
        isRotating = false;

        // 1. IMPORTANTE: Activa la gravedad para que caiga a plomo
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false; // Deja que la gravedad actúe
            rb.useGravity = true;
        }

        // 2. Desactiva los colliders para que no se choque al caer
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;

        if (fallSound != null) AudioSource.PlayClipAtPoint(fallSound, transform.position);

        // 3. Avisa al mapa (si tienes música de derrota, etc)
        mapCreation?.StartFailSequence();

        // ❌ BORRA ESTA LÍNEA DE AQUÍ:
        // ResetToInitialPosition();  <--- ¡ESTO ES LO QUE TE IMPIDE VER LA CAÍDA!
    }

    public void ResetToInitialPosition()
    {
        StopAllCoroutines();
        isRotating = false;
        isFalling = false;
        rotationRemaining = 0f;
        isGrounded = true;
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        gridPosition = new Vector2Int(Mathf.RoundToInt(initialPosition.x), Mathf.RoundToInt(initialPosition.z));
        SnapToGrid();
        
        // Re-enable colliders
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = true;
    }

    private void PlayMoveSound()
    {
        MoveTracker.Instance?.RegisterMove();
        
        if (GlobalAudio.Instance != null)
        {
            GlobalAudio.Instance.PlayMove();
        }
    }

    /*private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (other.name.Contains("BorderBlock") || other.CompareTag("GameOver"))
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 away = (transform.position - other.bounds.center).normalized;
                Vector3 push = (away + Vector3.down).normalized * 3f;
                rb.AddForce(push, ForceMode.Impulse);
            }
        }
    }*/
}
