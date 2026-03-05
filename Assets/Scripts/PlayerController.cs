using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    public RectTransform RectTransform { get; private set; }

    public int CurrentScore { get; private set; }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 700f;
    [SerializeField] private float minX = -900f;
    [SerializeField] private float maxX = 900f;

    [Header("Shooting")]
    [SerializeField] private GameObject playerBulletPrefab;
    [SerializeField] private Vector2 bulletSpawnOffset = new Vector2(0f, 50f);
    [SerializeField] private float fireCooldown = 0.25f;

    [Header("Score")]
    [SerializeField] private TMP_Text scoreText;

    private float nextFireTime;

    private void Awake()
    {
        Instance = this;
        RectTransform = transform as RectTransform;
        UpdateScoreText();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        Move(keyboard);

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            TryShoot();
        }
    }

    private void Move(Keyboard keyboard)
    {
        var direction = 0f;

        if (keyboard.aKey.isPressed)
        {
            direction -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            direction += 1f;
        }

        if (Mathf.Approximately(direction, 0f))
        {
            return;
        }

        var deltaX = direction * moveSpeed * Time.deltaTime;

        if (RectTransform != null)
        {
            var pos = RectTransform.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x + deltaX, minX, maxX);
            RectTransform.anchoredPosition = pos;
            return;
        }

        var pos3 = transform.position;
        pos3.x = Mathf.Clamp(pos3.x + deltaX, minX, maxX);
        transform.position = pos3;
    }

    private void TryShoot()
    {
        if (playerBulletPrefab == null || Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + fireCooldown;

        var parent = transform.parent;
        var bullet = Instantiate(playerBulletPrefab, parent);

        var bulletRect = bullet.transform as RectTransform;
        if (bulletRect != null && RectTransform != null)
        {
            bulletRect.anchoredPosition = RectTransform.anchoredPosition + bulletSpawnOffset;
            return;
        }

        bullet.transform.position = transform.position + (Vector3)bulletSpawnOffset;
    }

    public void AddScore(int points)
    {
        if (points <= 0)
        {
            return;
        }

        CurrentScore += points;
        UpdateScoreText();
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        UpdateScoreText();
    }

    public void SetScoreText(TMP_Text targetText)
    {
        scoreText = targetText;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.text = CurrentScore.ToString("D4");
    }
}
