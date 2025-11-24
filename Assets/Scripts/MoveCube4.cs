using System.Collections;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Movement logic for a 1x2x1 block similar to Bloxorz.
// States: Standing (vertical), LyingX (length along X), LyingZ (length along Z).
// Rotations are animated around the bottom edge in the direction of movement
// and final positions are snapped to the tile grid to ensure correct alignment.
public class MoveCube : MonoBehaviour
{
    InputAction moveAction;

    public float rotSpeed = 360f;    // degrees per second for rotation animations
    public float fallSpeed = 5f;

    public AudioClip[] sounds;
    public AudioClip fallSound;

    // Alignment offset to tweak visual mesh position relative to logical grid (adjust in Inspector)
    public Vector3 alignmentOffset = Vector3.zero;

    const float tile = 1f;

    enum Orientation { Standing, LyingX, LyingZ }
    Orientation orientation = Orientation.Standing;

    bool isMoving = false;
    bool isFalling = false;

    LayerMask groundMask;

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        groundMask = LayerMask.GetMask("Ground");

        // Guess initial orientation from scale (optional)
        float sx = transform.localScale.x;
        float sy = transform.localScale.y;
        float sz = transform.localScale.z;
        if (sy > sx + 0.5f && sy > sz + 0.5f) orientation = Orientation.Standing;
        else if (sx > sz) orientation = Orientation.LyingX;
        else orientation = Orientation.LyingZ;
    }

    void Update()
    {
        if (isFalling)
        {
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);
            return;
        }

        if (isMoving) return;

        if (!IsGrounded())
        {
            StartFall();
            return;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();
        if (Math.Abs(input.x) > 0.99f)
        {
            Vector3 dir = input.x > 0 ? Vector3.right : Vector3.left;
            StartCoroutine(MoveStep(dir));
        }
        else if (Math.Abs(input.y) > 0.99f)
        {
            Vector3 dir = input.y > 0 ? Vector3.forward : Vector3.back;
            StartCoroutine(MoveStep(dir));
        }
    }

    void StartFall()
    {
        isFalling = true;
        if (fallSound != null) AudioSource.PlayClipAtPoint(fallSound, transform.position, 1.5f);
    }

    bool IsGrounded()
    {
        RaycastHit hit;
        if (orientation == Orientation.Standing)
        {
            float dist = 1f + 0.05f; // center at y=1 for height 2
            return Physics.Raycast(transform.position, Vector3.down, out hit, dist, groundMask);
        }
        else if (orientation == Orientation.LyingX)
        {
            float dist = 0.5f + 0.05f;
            Vector3 a = transform.position + Vector3.right * 0.5f;
            Vector3 b = transform.position + Vector3.left * 0.5f;
            bool ha = Physics.Raycast(a, Vector3.down, out hit, dist, groundMask);
            bool hb = Physics.Raycast(b, Vector3.down, out hit, dist, groundMask);
            return ha || hb;
        }
        else // LyingZ
        {
            float dist = 0.5f + 0.05f;
            Vector3 a = transform.position + Vector3.forward * 0.5f;
            Vector3 b = transform.position + Vector3.back * 0.5f;
            RaycastHit tmp;
            bool ha = Physics.Raycast(a, Vector3.down, out tmp, dist, groundMask);
            bool hb = Physics.Raycast(b, Vector3.down, out tmp, dist, groundMask);
            return ha || hb;
        }
    }

    int RoundToTile(float v)
    {
        return Mathf.RoundToInt(v);
    }

    IEnumerator MoveStep(Vector3 dir)
    {
        isMoving = true;

        if (sounds != null && sounds.Length > 0)
        {
            int i = UnityEngine.Random.Range(0, sounds.Length);
            AudioSource.PlayClipAtPoint(sounds[i], transform.position, 1.0f);
        }

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 finalPos = startPos;
        Quaternion finalRot = startRot;

        Vector3 pivot = Vector3.zero;
        Vector3 axis = Vector3.zero;

        // Compute final positions by grid logic to ensure exact tile alignment
        if (orientation == Orientation.Standing)
        {
            int tileX = RoundToTile(startPos.x);
            int tileZ = RoundToTile(startPos.z);

            if (Mathf.Abs(dir.x) > 0.5f)
            {
                // tip over to lie along X
                finalPos = new Vector3(tileX + (dir.x > 0 ? 0.5f : -0.5f), 0.5f, tileZ);
                pivot = startPos + new Vector3(dir.x * 0.5f, -1f, 0f);
                axis = Vector3.forward * (dir.x > 0 ? -1f : 1f);
                orientation = Orientation.LyingX;
            }
            else
            {
                // tip over to lie along Z
                finalPos = new Vector3(tileX, 0.5f, tileZ + (dir.z > 0 ? 0.5f : -0.5f));
                pivot = startPos + new Vector3(0f, -1f, dir.z * 0.5f);
                axis = Vector3.right * (dir.z > 0 ? 1f : -1f);
                orientation = Orientation.LyingZ;
            }
        }
        else if (orientation == Orientation.LyingX)
        {
            // center.x is k+0.5
            int minTileX = Mathf.RoundToInt(startPos.x - 0.5f); // left tile index
            int tileZ = RoundToTile(startPos.z);

            if (Mathf.Abs(dir.x) > 0.5f)
            {
                // roll to standing on one of the two tiles
                int newTileX = dir.x > 0 ? (minTileX + 1) : minTileX;
                finalPos = new Vector3(newTileX, 1f, tileZ);
                pivot = startPos + new Vector3(dir.x * 1f, -0.5f, 0f);
                axis = Vector3.forward * (dir.x > 0 ? -1f : 1f);
                orientation = Orientation.Standing;
            }
            else
            {
                // roll sideways along Z while remaining lying X
                int newTileZ = RoundToTile(startPos.z) + (dir.z > 0 ? 1 : -1);
                finalPos = new Vector3(startPos.x, 0.5f, newTileZ);
                pivot = startPos + new Vector3(0f, -0.5f, dir.z * 0.5f);
                axis = Vector3.right * (dir.z > 0 ? 1f : -1f);
                // orientation remains LyingX
            }
        }
        else // LyingZ
        {
            int minTileZ = Mathf.RoundToInt(startPos.z - 0.5f);
            int tileX = RoundToTile(startPos.x);

            if (Mathf.Abs(dir.z) > 0.5f)
            {
                int newTileZ = dir.z > 0 ? (minTileZ + 1) : minTileZ;
                finalPos = new Vector3(tileX, 1f, newTileZ);
                pivot = startPos + new Vector3(0f, -0.5f, dir.z * 1f);
                axis = Vector3.right * (dir.z > 0 ? 1f : -1f);
                orientation = Orientation.Standing;
            }
            else
            {
                int newTileX = RoundToTile(startPos.x) + (dir.x > 0 ? 1 : -1);
                finalPos = new Vector3(newTileX, 0.5f, startPos.z);
                pivot = startPos + new Vector3(dir.x * 0.5f, -0.5f, 0f);
                axis = Vector3.forward * (dir.x > 0 ? -1f : 1f);
                // orientation remains LyingZ
            }
        }

        // Animate rotation by 90 degrees around pivot
        float remaining = 90f;
        while (remaining > 0f)
        {
            float step = rotSpeed * Time.deltaTime;
            if (step > remaining) step = remaining;
            transform.RotateAround(pivot, axis, step);
            remaining -= step;
            yield return null;
        }

        // Compute final rotation relative to start
        if (axis != Vector3.zero)
        {
            finalRot = Quaternion.AngleAxis(90f, axis.normalized) * startRot;
            transform.rotation = finalRot;
        }

        // Snap final position exactly to grid-calculated finalPos and apply alignment offset
        transform.position = finalPos + alignmentOffset;

        // Snap small floating errors
        Vector3 p = transform.position;
        p.x = (float)Math.Round(p.x * 1000f) / 1000f;
        p.y = (float)Math.Round(p.y * 1000f) / 1000f;
        p.z = (float)Math.Round(p.z * 1000f) / 1000f;
        transform.position = p;

        // After move, check grounded
        if (!IsGrounded()) StartFall();

        isMoving = false;
    }
}
