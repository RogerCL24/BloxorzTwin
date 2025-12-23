using System.Collections;
using UnityEngine;

/// <summary>
/// Reimplementation of block movement with explicit grid snapping to avoid half-block drift on the long axis.
/// Handles standing, lying on X, and lying on Z with correct vertical placement based on collider bounds.
/// </summary>
public class BlockMove2 : MonoBehaviour
{
    public float moveDuration = 0.25f;
    public float fallThreshold = -8f;
    [Tooltip("Height of the tile top surface above world origin. Default matches existing tiles (0.25 + 0.07/2).")]
    public float groundHeight = 0.285f;
    public AudioClip[] moveSounds;
    public AudioClip fallSound;
    public bool showDebug;

    private enum Orientation { Standing, LyingX, LyingZ }

    private Orientation orientation = Orientation.Standing;
    private bool isMoving;
    private bool isFalling;
    private Vector3 worldSize;

    private MapCreation mapCreation;

    private void Awake()
    {
        mapCreation = FindFirstObjectByType<MapCreation>();
        CacheWorldSize();
        SnapToGrid(true);
    }

    private void Update()
    {
        if (isFalling)
        {
            transform.Translate(Vector3.down * 10f * Time.deltaTime, Space.World);
            return;
        }

        if (transform.position.y < fallThreshold)
        {
            TriggerFall();
            return;
        }

        if (isMoving) return;

        Vector3 dir = ReadInput();
        if (dir != Vector3.zero)
        {
            StartCoroutine(MoveStep(dir));
        }
    }

    private void CacheWorldSize()
    {
        Bounds combined = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;
        Collider[] cols = GetComponentsInChildren<Collider>();
        if (cols.Length > 0)
        {
            combined = cols[0].bounds;
            hasBounds = true;
            for (int i = 1; i < cols.Length; i++) combined.Encapsulate(cols[i].bounds);
        }
        else
        {
            Renderer[] rends = GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                combined = rends[0].bounds;
                hasBounds = true;
                for (int i = 1; i < rends.Length; i++) combined.Encapsulate(rends[i].bounds);
            }
        }

        worldSize = hasBounds ? combined.size : transform.lossyScale;
    }

    private Vector3 ReadInput()
    {
        float x = 0f, z = 0f;

        float rawX = Input.GetAxisRaw("Horizontal");
        float rawZ = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(rawX) > 0.1f) x = Mathf.Sign(rawX);
        if (Mathf.Abs(rawZ) > 0.1f) z = Mathf.Sign(rawZ);

        if (Mathf.Abs(x) > 0.1f)
        {
            return x > 0 ? Vector3.right : Vector3.left;
        }
        if (Mathf.Abs(z) > 0.1f)
        {
            return z > 0 ? Vector3.forward : Vector3.back;
        }
        return Vector3.zero;
    }

    private IEnumerator MoveStep(Vector3 dir)
    {
        isMoving = true;
        MoveTracker.Instance?.RegisterMove();
        PlayMoveSound();

        Orientation nextOrientation;
        Vector3 pivot;
        Vector3 axis;
        ComputePivotAndOrientation(dir, out pivot, out axis, out nextOrientation);

        float remaining = 90f;
        while (remaining > 0f)
        {
            float step = Mathf.Min(remaining, (90f / moveDuration) * Time.deltaTime);
            transform.RotateAround(pivot, axis, step);
            remaining -= step;
            yield return null;
        }

        orientation = nextOrientation;
        SnapToGrid(false);
        isMoving = false;
    }

    private void ComputePivotAndOrientation(Vector3 dir, out Vector3 pivot, out Vector3 axis, out Orientation next)
    {
        // Current up/right/forward
        Vector3 up = transform.up;
        Vector3 right = transform.right;
        Vector3 forward = transform.forward;

        axis = Vector3.Cross(Vector3.up, dir).normalized;
        if (axis.sqrMagnitude < 0.0001f)
        {
            axis = Vector3.right;
        }

        float halfW = worldSize.x * 0.5f;
        float halfH = worldSize.y * 0.5f;
        float halfD = worldSize.z * 0.5f;

        // Determine orientation
        float upDotR = Mathf.Abs(Vector3.Dot(up, right));
        float upDotF = Mathf.Abs(Vector3.Dot(up, forward));
        float upDotU = Mathf.Abs(Vector3.Dot(up, Vector3.up));

        if (upDotU > upDotR && upDotU > upDotF)
        {
            orientation = Orientation.Standing;
        }
        else if (upDotR > upDotF)
        {
            orientation = Orientation.LyingX;
        }
        else
        {
            orientation = Orientation.LyingZ;
        }

        switch (orientation)
        {
            case Orientation.Standing:
                pivot = transform.position + dir * halfW + Vector3.down * halfH;
                next = dir.x != 0 ? Orientation.LyingX : Orientation.LyingZ;
                break;
            case Orientation.LyingX:
                pivot = transform.position + dir * (dir.x != 0 ? halfH : halfW) + Vector3.down * halfW;
                next = dir.x != 0 ? Orientation.Standing : Orientation.LyingX;
                break;
            case Orientation.LyingZ:
                pivot = transform.position + dir * (dir.z != 0 ? halfH : halfW) + Vector3.down * halfW;
                next = dir.z != 0 ? Orientation.Standing : Orientation.LyingZ;
                break;
            default:
                pivot = transform.position;
                next = Orientation.Standing;
                break;
        }
    }

    private void SnapToGrid(bool forceRecalcOrientation)
    {
        if (forceRecalcOrientation)
        {
            Vector3 up = transform.up;
            float upDotR = Mathf.Abs(Vector3.Dot(up, transform.right));
            float upDotF = Mathf.Abs(Vector3.Dot(up, transform.forward));
            float upDotU = Mathf.Abs(Vector3.Dot(up, Vector3.up));
            if (upDotU > upDotR && upDotU > upDotF) orientation = Orientation.Standing;
            else if (upDotR > upDotF) orientation = Orientation.LyingX;
            else orientation = Orientation.LyingZ;
        }

        Vector3 pos = transform.position;
        switch (orientation)
        {
            case Orientation.Standing:
                pos.x = Mathf.Round(pos.x);
                pos.z = Mathf.Round(pos.z);
                pos.y = worldSize.y * 0.5f + groundHeight;
                break;
            case Orientation.LyingX:
                pos.x = Mathf.Round(pos.x * 2f) / 2f; // allow half steps along X
                pos.z = Mathf.Round(pos.z);
                pos.y = worldSize.x * 0.5f + groundHeight;
                break;
            case Orientation.LyingZ:
                pos.x = Mathf.Round(pos.x);
                pos.z = Mathf.Round(pos.z * 2f) / 2f; // allow half steps along Z
                pos.y = worldSize.z * 0.5f + groundHeight;
                break;
        }
        transform.position = pos;

        Vector3 euler = transform.eulerAngles;
        euler.x = Mathf.Round(euler.x / 90f) * 90f;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        euler.z = Mathf.Round(euler.z / 90f) * 90f;
        transform.eulerAngles = euler;
    }

    private void TriggerFall()
    {
        if (isFalling) return;
        isFalling = true;
        if (fallSound != null) AudioSource.PlayClipAtPoint(fallSound, transform.position);
        mapCreation?.StartFailSequence();
    }

    private void PlayMoveSound()
    {
        if (moveSounds != null && moveSounds.Length > 0)
        {
            int index = Random.Range(0, moveSounds.Length);
            AudioSource.PlayClipAtPoint(moveSounds[index], transform.position, 1f);
        }
    }
}
