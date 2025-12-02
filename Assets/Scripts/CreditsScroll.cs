using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // <--- ¡IMPRESCINDIBLE para el nuevo sistema!

public class CreditsScroll : MonoBehaviour
{
    public float speed = 100f;
    public float endY = 1500f;

    public RectTransform creditsContent;

    void Update()
    {
        // 1. Mover hacia arriba
        creditsContent.Translate(Vector3.up * speed * Time.deltaTime);

        // 2. DETECTAR CLIC O TECLA (Versión Nuevo Input System)
        // Verificamos si alguien pulsa cualquier tecla del teclado
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            VolverAlMenu();
        }

        // Verificamos si alguien hace clic izquierdo con el ratón
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            VolverAlMenu();
        }

        // 3. Límite de altura
        if (creditsContent.anchoredPosition.y > endY)
        {
            VolverAlMenu();
        }
    }

    void VolverAlMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}