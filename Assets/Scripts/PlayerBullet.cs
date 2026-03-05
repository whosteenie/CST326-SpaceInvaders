using System;
using System.Collections.Generic;
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
        var tr = transform;
        rectTransform = tr as RectTransform;
        parentRect = tr.parent as RectTransform;
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
        var deltaY = speed * Time.deltaTime;

        if (rectTransform != null)
        {
            var pos = rectTransform.anchoredPosition;
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

        var viewport = Camera.main.WorldToViewportPoint(transform.position);
        return viewport.y > 1f + (offscreenMargin / Screen.height);
    }

    private bool TryHitShield() =>
        TryHitTargets(ShieldTarget.Active, target => target.RectTransform, target => target.Kill());

    private bool TryHitAlien() =>
        TryHitTargets(AlienTarget.Active, target => target.RectTransform, target => target.Kill());

    private bool TryHitTargets<T>(
        IReadOnlyList<T> targets,
        Func<T, RectTransform> getRect,
        Action<T> onHit)
    {
        for (var i = targets.Count - 1; i >= 0; i--)
        {
            var target = targets[i];
            if (target == null)
            {
                continue;
            }

            var targetRect = getRect(target);
            if (targetRect == null || !Overlaps(targetRect))
            {
                continue;
            }

            onHit(target);
            Destroy(gameObject);
            return true;
        }

        return false;
    }

    private bool Overlaps(RectTransform other)
    {
        if (rectTransform == null)
        {
            return false;
        }

        rectTransform.GetWorldCorners(cornersA);
        other.GetWorldCorners(cornersB);

        var aMinX = cornersA[0].x;
        var aMinY = cornersA[0].y;
        var aMaxX = cornersA[2].x;
        var aMaxY = cornersA[2].y;

        var bMinX = cornersB[0].x;
        var bMinY = cornersB[0].y;
        var bMaxX = cornersB[2].x;
        var bMaxY = cornersB[2].y;

        return aMinX < bMaxX && aMaxX > bMinX && aMinY < bMaxY && aMaxY > bMinY;
    }
}
