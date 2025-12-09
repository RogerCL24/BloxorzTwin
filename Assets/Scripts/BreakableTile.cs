using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableTile : MonoBehaviour
{
    public void Fracture()
    {
        // Disable the main collider so the player falls through immediately
        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null)
        {
            mainCollider.enabled = false;
        }

        // Check if we have children to simulate debris (e.g. planks)
        if (transform.childCount > 0)
        {
            // Iterate backwards because we might be unparenting
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                
                // Add Rigidbody if not present to make it fall
                Rigidbody rb = child.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = child.gameObject.AddComponent<Rigidbody>();
                }

                // Enable gravity
                rb.useGravity = true;
                rb.isKinematic = false;
                
                // Add random rotation and force to simulate breaking apart
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 10f, ForceMode.Impulse);
                // Push them down and a bit outwards
                rb.linearVelocity = Vector3.down * 2f + UnityEngine.Random.insideUnitSphere * 2f;

                // Unparent so they fall independently
                child.SetParent(null);
                
                // Destroy the piece after a few seconds to clean up
                Destroy(child.gameObject, 3f);
            }
        }
        
        // Destroy the main container object
        Destroy(gameObject);
    }
}
