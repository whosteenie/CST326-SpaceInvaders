using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMainMenuAfterDelay : MonoBehaviour
{
    [SerializeField] private string sceneName = "MainMenu";
    [SerializeField] private float delaySeconds = 5f;

    private float elapsed;
    private bool hasLoaded;

    private void Update()
    {
        if (hasLoaded)
        {
            return;
        }

        elapsed += Time.deltaTime;
        if (elapsed < delaySeconds)
        {
            return;
        }

        hasLoaded = true;
        SceneManager.LoadScene(sceneName);
    }
}
