using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 
public class CreditsScroll : MonoBehaviour
{
    public float speed = 100f;
    public float endY = 1500f;

    public RectTransform creditsContent;

    void Update()
    {
        creditsContent.Translate(Vector3.up * speed * Time.deltaTime);

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            VolverAlMenu();
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            VolverAlMenu();
        }

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