using UnityEngine;

public class MenuPropBehavior : MonoBehaviour
{
    [Header("Rotaci�n (Giro Peonza)")]
    public float rotSpeed = 20f; // Velocidad de giro en el eje Y (Horizontal)

    [Header("Balanceo (Inclinaci�n)")]
    public float tiltStrength = 0f; // Pon esto a 0 si no quieres que gire hacia delante/atr�s

    [Header("Movimiento por la Pantalla")]
    public float moveSpeed = 0.5f; // Velocidad del viaje
    public float widthDistance = 6.0f; // Distancia horizontal
    public float heightDistance = 2.5f; // Distancia vertical

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(Vector3.up * rotSpeed * Time.deltaTime, Space.World);

        float tilt = Mathf.Sin(Time.time * moveSpeed) * tiltStrength;
        transform.localRotation = Quaternion.Euler(tilt, transform.localEulerAngles.y, transform.localEulerAngles.z);

        float x = Mathf.Sin(Time.time * moveSpeed) * widthDistance;
        float y = Mathf.Cos(Time.time * moveSpeed * 0.8f) * heightDistance;

        transform.position = startPos + new Vector3(x, y, 0);
    }
}