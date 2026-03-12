using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DeathSpriteOnDestroy : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite deathSprite;
    [SerializeField] private float destroyDelay = 0.25f;

    private bool hasPlayed;

    private void Reset()
    {
        targetImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
    }

    public void Play()
    {
        if (hasPlayed)
        {
            return;
        }

        hasPlayed = true;

        if (targetImage != null && deathSprite != null)
        {
            targetImage.sprite = deathSprite;
        }

        if (destroyDelay <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject, destroyDelay);
    }
}
