using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Necesario para las Corutinas (el temporizador)

public class SceneNavigator : MonoBehaviour
{
    // Tiempo de espera para dejar que suene el "Click"
    public float delay = 0.4f;

    public void LoadScene(string sceneName)
    {
        // En lugar de cargar ya, iniciamos la cuenta atrás
        StartCoroutine(LoadSceneDelayed(sceneName));
    }

    // Esta es la función con temporizador
    IEnumerator LoadSceneDelayed(string sceneName)
    {
        // Esperamos 0.4 segundos (el juego no se congela, solo espera)
        yield return new WaitForSeconds(delay);

        // AHORA sí cargamos la escena y destruimos el menú
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}