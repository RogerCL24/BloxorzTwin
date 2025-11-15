using UnityEngine;
using UnityEngine.SceneManagement; // ¡Importante para manejar escenas!

public class SceneNavigator : MonoBehaviour
{
    // Función pública para cargar una escena por su nombre
    // La llamaremos desde un botón
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Función específica para salir del juego (útil en el menú)
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Saliendo del juego..."); // Para que lo veas en el editor
    }
}