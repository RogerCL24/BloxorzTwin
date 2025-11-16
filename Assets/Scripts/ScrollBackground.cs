using UnityEngine;
using UnityEngine.UI; // Necesario para RawImage

[RequireComponent(typeof(RawImage))]
public class ScrollBackground : MonoBehaviour
{
    private RawImage rawImage;

    // Velocidad de movimiento en X e Y
    public float speedX = 0.02f;
    public float speedY = 0.01f;

    void Start()
    {
        // Obtenemos el componente RawImage
        rawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        // Obtenemos el rectángulo UV actual
        Rect uvRect = rawImage.uvRect;

        // Calculamos el nuevo desplazamiento
        // Time.deltaTime asegura que se mueva suave
        // y a la misma velocidad en todos los ordenadores
        float newX = uvRect.x + speedX * Time.deltaTime;
        float newY = uvRect.y + speedY * Time.deltaTime;

        // Usamos Mathf.Repeat para que el valor 
        // vuelva a 0 cuando llegue a 1 (creando un bucle infinito)
        uvRect.position = new Vector2(Mathf.Repeat(newX, 1), Mathf.Repeat(newY, 1));

        // Asignamos el nuevo rectángulo UV a la imagen
        rawImage.uvRect = uvRect;
    }
}