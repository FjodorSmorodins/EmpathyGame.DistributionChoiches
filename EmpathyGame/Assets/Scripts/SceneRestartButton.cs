using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneRestartButton : MonoBehaviour
{
    public void RestartScene()
    {
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}