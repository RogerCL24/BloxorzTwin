using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    // Ajusta esto en el inspector para que cada cangrejo se mueva distinto
    public float speed = 2f;
    public float height = 0.1f;

    // Para que no se muevan sincronizados
    public float timeOffset = 0f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        // Fórmula de onda senoidal para subir y bajar suavemente
        float newY = startPos.y + Mathf.Sin((Time.time + timeOffset) * speed) * height;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}