using UnityEngine;

public class AlienBullet : MonoBehaviour
{
    public static int ActiveCount { get; private set; }

    [SerializeField] private float speed = 650f;
    [SerializeField] private float offscreenMargin = 50f;

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private readonly Vector3[] cornersA = new Vector3[4];
    private readonly Vector3[] cornersB = new Vector3[4];

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        parentRect = transform.parent as RectTransform;
    }

    private void OnEnable()
    {
        ActiveCount++;
    }

    private void OnDisable()
    {
        ActiveCount = Mathf.Max(0, ActiveCount - 1);
    }

    private void Update()
    {
        MoveDown();

        if (IsOffscreen())
        {
            Destroy(gameObject);
            return;
        }

        if (TryHitShield())
        {
            return;
        }

        TryHitPlayer();
    }

    private void MoveDown()
    {
        float deltaY = speed * Time.deltaTime;

        if (rectTransform != null)
        {
            Vector2 pos = rectTransform.anchoredPosition;
            pos.y -= deltaY;
            rectTransform.anchoredPosition = pos;
            return;
        }

        transform.position += Vector3.down * deltaY;
    }

    private bool IsOffscreen()
    {
        if (rectTransform != null && parentRect != null)
        {
            return rectTransform.anchoredPosition.y < parentRect.rect.yMin - offscreenMargin;
        }

        if (Camera.main == null)
        {
            return false;
        }

        Vector3 viewport = Camera.main.WorldToViewportPoint(transform.position);
        return viewport.y < 0f - (offscreenMargin / Screen.height);
    }

    private bool TryHitShield()
    {
        var targets = ShieldTarget.Active;
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            ShieldTarget target = targets[i];
            if (target == null || target.RectTransform == null)
            {
                continue;
            }

            if (!Overlaps(target.RectTransform))
            {
                continue;
            }

            Destroy(gameObject);
            return true;
        }

        return false;
    }

    private void TryHitPlayer()
    {
        PlayerController player = PlayerController.Instance;
        if (player == null || player.RectTransform == null)
        {
            return;
        }

        if (!Overlaps(player.RectTransform))
        {
            return;
        }

        if (SpawnInvaderFormation.Instance != null)
        {
            SpawnInvaderFormation.Instance.OnPlayerKilled();
        }
        else
        {
            Debug.LogError("AlienBullet hit player, but no SpawnInvaderFormation instance was found.");
            Destroy(player.gameObject);
        }

        Destroy(gameObject);
    }

    private bool Overlaps(RectTransform other)
    {
        if (rectTransform == null)
        {
            return false;
        }

        rectTransform.GetWorldCorners(cornersA);
        other.GetWorldCorners(cornersB);

        float aMinX = cornersA[0].x;
        float aMinY = cornersA[0].y;
        float aMaxX = cornersA[2].x;
        float aMaxY = cornersA[2].y;

        float bMinX = cornersB[0].x;
        float bMinY = cornersB[0].y;
        float bMaxX = cornersB[2].x;
        float bMaxY = cornersB[2].y;

        return aMinX < bMaxX && aMaxX > bMinX && aMinY < bMaxY && aMaxY > bMinY;
    }
}
