using UnityEngine;
using UnityEngine.UI;

public class BulletFrame : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 12f;

    private int frameIndex;
    private float frameTimer;

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
    }

    private void OnEnable()
    {
        frameIndex = 0;
        frameTimer = 0f;
        ApplyFrame();
    }

    private void Update()
    {
        if (frames is not { Length: > 1 } || targetImage == null || frameRate <= 0f)
        {
            return;
        }

        frameTimer += Time.deltaTime;
        var frameDuration = 1f / frameRate;
        if (frameTimer < frameDuration)
        {
            return;
        }

        frameTimer -= frameDuration;
        frameIndex = (frameIndex + 1) % frames.Length;
        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (targetImage == null || frames == null || frames.Length == 0)
        {
            return;
        }

        targetImage.sprite = frames[frameIndex];
    }
}
