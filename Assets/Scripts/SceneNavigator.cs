using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Necesario para las Corutinas (el temporizador)

public class SceneNavigator : MonoBehaviour
{
    public float delay = 0.4f;

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneDelayed(sceneName));
    }

    IEnumerator LoadSceneDelayed(string sceneName)
    {
        yield return new WaitForSeconds(delay);

        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}