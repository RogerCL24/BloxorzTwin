using UnityEngine;

public class MenuPropBehavior : MonoBehaviour
{
    [Header("Rotación (Giro Peonza)")]
    public float rotSpeed = 20f; // Velocidad de giro en el eje Y (Horizontal)

    [Header("Balanceo (Inclinación)")]
    public float tiltStrength = 0f; // Pon esto a 0 si no quieres que gire hacia delante/atrás

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
        // 1. Rotación constante (Giro Y - Peonza)
        transform.Rotate(Vector3.up * rotSpeed * Time.deltaTime, Space.World);

        // 2. Balanceo Suave (Giro X - Hacia delante/atrás)
        // He cambiado la lógica para que no sea acumulativa (no da vueltas completas, solo se mece)
        float tilt = Mathf.Sin(Time.time * moveSpeed) * tiltStrength;
        // Aplicamos la rotación local en X
        transform.localRotation = Quaternion.Euler(tilt, transform.localEulerAngles.y, transform.localEulerAngles.z);

        // 3. Movimiento Amplio (Flotar por la pantalla)
        float x = Mathf.Sin(Time.time * moveSpeed) * widthDistance;
        float y = Mathf.Cos(Time.time * moveSpeed * 0.8f) * heightDistance;

        transform.position = startPos + new Vector3(x, y, 0);
    }
}