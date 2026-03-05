using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadGameSceneAfterDelay : MonoBehaviour
{
    [SerializeField] private TMP_Text hiScoreText;
    [SerializeField] private string sceneName = "Game";
    [SerializeField] private float delaySeconds = 3f;

    private float elapsed;
    private bool hasLoaded;

    private void Start()
    {
        UpdateHiScoreText();
    }

    private void Update()
    {
        if (hasLoaded)
        {
            return;
        }

        elapsed += Time.deltaTime;
        if (elapsed >= delaySeconds || WasClickPressedThisFrame())
        {
            hasLoaded = true;
            SceneManager.LoadScene(sceneName);
        }
    }

    private static bool WasClickPressedThisFrame()
    {
        return (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
               || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
               || (Pen.current != null && Pen.current.tip.wasPressedThisFrame);
    }

    private void UpdateHiScoreText()
    {
        if (hiScoreText == null)
        {
            return;
        }

        int hiScore = PlayerPrefs.GetInt(SpawnInvaderFormation.HiScoreKey, 0);
        hiScoreText.text = hiScore.ToString("D4");
    }
}
