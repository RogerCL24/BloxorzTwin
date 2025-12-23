using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    public float speed = 2f;
    public float height = 0.1f;

    public float timeOffset = 0f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        // Onda senoidal para subir y bajar suavemente
        float newY = startPos.y + Mathf.Sin((Time.time + timeOffset) * speed) * height;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}