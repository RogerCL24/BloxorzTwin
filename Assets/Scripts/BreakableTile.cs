using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableTile : MonoBehaviour
{
    public void Fracture()
    {
        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null)
        {
            mainCollider.enabled = false;
        }

        if (transform.childCount > 0)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                
                Rigidbody rb = child.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = child.gameObject.AddComponent<Rigidbody>();
                }

                rb.useGravity = true;
                rb.isKinematic = false;
                
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 10f, ForceMode.Impulse);
                rb.linearVelocity = Vector3.down * 2f + UnityEngine.Random.insideUnitSphere * 2f;

                child.SetParent(null);
                
                Destroy(child.gameObject, 3f);
            }
        }
        
        Destroy(gameObject);
    }
}
