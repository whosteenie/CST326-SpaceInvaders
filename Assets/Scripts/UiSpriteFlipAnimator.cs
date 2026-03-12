using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UiSpriteFlipAnimator : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite sprite1;
    [SerializeField] private Sprite sprite2;
    [SerializeField] private float frameDuration = 0.5f;

    private float elapsed;
    private bool showingSecondSprite;

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

    private void OnEnable()
    {
        elapsed = 0f;
        showingSecondSprite = false;
        ApplySprite();
    }

    private void Update()
    {
        if (targetImage == null || sprite1 == null || sprite2 == null)
        {
            return;
        }

        elapsed += Time.deltaTime;
        if (elapsed < frameDuration)
        {
            return;
        }

        elapsed -= frameDuration;
        showingSecondSprite = !showingSecondSprite;
        ApplySprite();
    }

    private void ApplySprite()
    {
        if (targetImage == null)
        {
            return;
        }

        targetImage.sprite = showingSecondSprite ? sprite2 : sprite1;
    }
}
