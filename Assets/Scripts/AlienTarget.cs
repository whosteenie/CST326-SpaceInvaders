using System.Collections.Generic;
using UnityEngine;
using System;

public class AlienTarget : MonoBehaviour
{
    private static readonly List<AlienTarget> ActiveTargets = new();
    public static event Action<AlienTarget> Killed;

    [SerializeField] private int scoreValue = 10;
    [SerializeField] private float destroyDelay;
    [SerializeField] private int columnIndex = -1;

    private bool isDead;

    public static IReadOnlyList<AlienTarget> Active => ActiveTargets;
    public RectTransform RectTransform { get; private set; }

    public int ColumnIndex => columnIndex;

    private void Awake()
    {
        RectTransform = transform as RectTransform;
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

        Killed?.Invoke(this);

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