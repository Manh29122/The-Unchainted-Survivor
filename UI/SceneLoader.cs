using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string sceneName;
    [SerializeField] private int sceneIndex = -1;

    public void LoadConfiguredScene()
    {
        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        if (sceneIndex >= 0)
        {
            SceneManager.LoadScene(sceneIndex);
            return;
        }

        Debug.LogWarning("[SceneLoader] No scene configured to load.");
    }

    public void LoadSceneByName(string targetSceneName)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("[SceneLoader] Scene name is empty.");
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }

    public void LoadSceneByIndex(int targetSceneIndex)
    {
        if (targetSceneIndex < 0)
        {
            Debug.LogWarning("[SceneLoader] Scene index must be >= 0.");
            return;
        }

        SceneManager.LoadScene(targetSceneIndex);
    }

    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        int nextSceneIndex = currentScene.buildIndex + 1;

        if (nextSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("[SceneLoader] No next scene in Build Settings.");
            return;
        }

        SceneManager.LoadScene(nextSceneIndex);
    }

    public void QuitGame()
    {
        Debug.Log("[SceneLoader] Quit Game");
        Application.Quit();
    }
}
