using System.Collections.Generic;
using UnityEngine;

public class AlienTarget : MonoBehaviour
{
    private static readonly List<AlienTarget> ActiveTargets = new List<AlienTarget>();

    [SerializeField] private int scoreValue = 10;
    [SerializeField] private float destroyDelay = 0f;
    [SerializeField] private int columnIndex = -1;

    private bool isDead;
    private RectTransform rectTransform;

    public static IReadOnlyList<AlienTarget> Active => ActiveTargets;
    public RectTransform RectTransform => rectTransform;
    public int ColumnIndex => columnIndex;

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
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.AddScore(scoreValue);
        }

        // Hook point for future death animation/sfx before destroy.
        if (destroyDelay <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject, destroyDelay);
    }

    public void SetColumnIndex(int index)
    {
        columnIndex = index;
    }
}
