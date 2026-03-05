using System.Collections.Generic;
using UnityEngine;

public class ShieldTarget : MonoBehaviour
{
    private static readonly List<ShieldTarget> ActiveTargets = new List<ShieldTarget>();

    [SerializeField] private float destroyDelay = 0f;

    private bool isDead;
    private RectTransform rectTransform;

    public static IReadOnlyList<ShieldTarget> Active => ActiveTargets;
    public RectTransform RectTransform => rectTransform;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
    }

    private void OnEnable()
    {
        if (!ActiveTargets.Contains(this))
        {
            ActiveTargets.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveTargets.Remove(this);
    }

    public void Kill()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (destroyDelay <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject, destroyDelay);
    }
}
