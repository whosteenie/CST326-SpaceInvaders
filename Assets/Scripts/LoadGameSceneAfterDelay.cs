using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadGameSceneAfterDelay : MonoBehaviour
{
    [SerializeField] private TMP_Text hiScoreText;
    [SerializeField] private string sceneName = "Game";
    private bool hasLoaded;

    private void Start()
    {
        UpdateHiScoreText();
    }

    public void LoadScene()
    {
        if (hasLoaded)
        {
            return;
        }

        hasLoaded = true;
        SceneManager.LoadScene(sceneName);
    }

    private void UpdateHiScoreText()
    {
        if (hiScoreText == null)
        {
            return;
        }

        var hiScore = PlayerPrefs.GetInt(SpawnInvaderFormation.HiScoreKey, 0);
        hiScoreText.text = hiScore.ToString("D4");
    }
}
