using UnityEngine;

public class MenuPropBehavior : MonoBehaviour
{
    [Header("Rotación")]
    public float rotSpeed = 20f; // Velocidad de giro sobre sí misma

    [Header("Movimiento por la Pantalla")]
    public float moveSpeed = 0.5f; // Qué tan rápido viaja
    public float widthDistance = 6.0f; // Cuánto se mueve a izquierda/derecha (Ancho)
    public float heightDistance = 2.5f; // Cuánto se mueve arriba/abajo (Alto)

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // 1. Rotación constante (Gira sobre sí misma para lucir el modelo)
        transform.Rotate(Vector3.up * rotSpeed * Time.deltaTime, Space.World);

        // Inclinación suave según se mueve (opcional, queda muy pro)
        transform.Rotate(Vector3.right * Mathf.Sin(Time.time) * 0.2f, Space.Self);

        // 2. Movimiento Amplio (Flotar por la pantalla)
        // Usamos Sin y Cos para hacer círculos o elipses grandes
        float x = Mathf.Sin(Time.time * moveSpeed) * widthDistance;
        float y = Mathf.Cos(Time.time * moveSpeed * 0.8f) * heightDistance; // El 0.8f es para que no sea un círculo perfecto

        transform.position = startPos + new Vector3(x, y, 0);
    }
}