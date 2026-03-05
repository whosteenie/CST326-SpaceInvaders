using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    [SerializeField] private float speed = 900f;
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

    private void Update()
    {
        MoveUp();

        if (IsOffscreen())
        {
            Destroy(gameObject);
            return;
        }

        if (TryHitShield())
        {
            return;
        }

        TryHitAlien();
    }

    private void MoveUp()
    {
        float deltaY = speed * Time.deltaTime;

        if (rectTransform != null)
        {
            Vector2 pos = rectTransform.anchoredPosition;
            pos.y += deltaY;
            rectTransform.anchoredPosition = pos;
            return;
        }

        transform.position += Vector3.up * deltaY;
    }

    private bool IsOffscreen()
    {
        if (rectTransform != null && parentRect != null)
        {
            return rectTransform.anchoredPosition.y > parentRect.rect.yMax + offscreenMargin;
        }

        if (Camera.main == null)
        {
            return false;
        }

        Vector3 viewport = Camera.main.WorldToViewportPoint(transform.position);
        return viewport.y > 1f + (offscreenMargin / Screen.height);
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

    private void TryHitAlien()
    {
        var targets = AlienTarget.Active;
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            AlienTarget target = targets[i];
            if (target == null || target.RectTransform == null)
            {
                continue;
            }

            if (!Overlaps(target.RectTransform))
            {
                continue;
            }

            target.Kill();
            Destroy(gameObject);
            return;
        }
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
