using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SpawnInvaderFormation : MonoBehaviour {
    #region Constants and Types
    public const string HiScoreKey = "HI-SCORE";
    public static SpawnInvaderFormation Instance { get; private set; }

    private enum AlienShotType {
        Rolling = 0,
        Plunger = 1,
        Squiggly = 2
    }
    #endregion

    #region Serialized Fields
    [Header("Parent")]
    [SerializeField] private RectTransform formationParent;

    [Header("Alien Prefabs (Frame 0)")]
    [SerializeField] private Image squidPrefab;

    [SerializeField] private Image crabPrefab;
    [SerializeField] private Image octopusPrefab;

    [Header("Alien Frame 1 Sprites")]
    [SerializeField] private Sprite squidFrame1;

    [SerializeField] private Sprite crabFrame1;
    [SerializeField] private Sprite octopusFrame1;

    [Header("Player")]
    [SerializeField] private Image playerPrefab;

    [SerializeField] private Vector2 playerStartPosition = new(0f, -430f);
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text hiScoreText;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float returnToMenuDelay = 3f;

    [Header("Shields")]
    [SerializeField] private Image shieldPrefab;

    [SerializeField] private int shieldCount = 4;
    [SerializeField] private float shieldsY = -250f;
    [SerializeField] private float shieldsSpreadWidth = 900f;

    [Header("Layout (1920x1080 baseline)")]
    [SerializeField] private int columns = 11;

    [SerializeField] private int rows = 5;
    [SerializeField] private float columnSpacing = 120f;
    [SerializeField] private float rowSpacing = 100f;
    [SerializeField] private Vector2 topCenter = new(0f, 360f);

    [Header("Alien Shooting")]
    [SerializeField] private RectTransform alienBulletParent;

    [SerializeField] private Image plungerBulletPrefab;
    [SerializeField] private Image rollBulletPrefab;
    [SerializeField] private Image squigglyBulletPrefab;
    [SerializeField] private Vector2 alienBulletSpawnOffset = new(0f, -35f);
    [SerializeField] private float fireIntervalMin = 0.65f;
    [SerializeField] private float fireIntervalMax = 1.2f;
    [SerializeField] private int maxActiveAlienShots = 1;

    [Header("Alien Movement")]
    [SerializeField] private float moveIntervalMax = 0.6f;

    [SerializeField] private float moveIntervalMin = 0.08f;
    [SerializeField] private float horizontalStep = 40f;
    [SerializeField] private float verticalStep = 44f;
    [SerializeField] private float leftBoundX = -860f;
    [SerializeField] private float rightBoundX = 860f;
    [SerializeField] private bool startMovingRight = true;
    #endregion

    #region Runtime State
    private RectTransform spawnedPlayerRect;
    private int initialAlienCount;
    private float nextShotTime;
    private float nextMoveTime;
    private int shotCycleIndex;
    private int moveDirection;
    private bool deathSequenceStarted;
    private bool useFrame1OnStep;
    #endregion

    #region Unity Lifecycle
    private void Awake() {
        Instance = this;
    }

    private void OnEnable() {
        AlienTarget.Killed += HandleAlienKilled;
    }

    private void OnDisable() {
        AlienTarget.Killed -= HandleAlienKilled;
    }

    private void OnDestroy() {
        if(Instance == this) {
            Instance = null;
        }
    }

    private void Start() {
        Spawn();
        UpdateHiScoreText();
        moveDirection = startMovingRight ? 1 : -1;
        ScheduleNextMove();
        ScheduleNextShot();
    }

    private void Update() {
        if(deathSequenceStarted) {
            return;
        }

        if(initialAlienCount > 0 && AlienTarget.Active.Count == 0) {
            OnAllAliensCleared();
            return;
        }

        if(Time.time < nextShotTime) {
            if(!(Time.time >= nextMoveTime)) return;
            MoveAliensOneStep();
            ScheduleNextMove();

            return;
        }

        TryFireAlienShot();
        ScheduleNextShot();

        if(!(Time.time >= nextMoveTime)) return;
        MoveAliensOneStep();
        ScheduleNextMove();
    }
    #endregion

    #region Spawning
    private void Spawn() {
        var parent = formationParent != null ? formationParent : transform as RectTransform;
        if(parent == null) {
            Debug.LogError("SpawnInvaderFormation needs a RectTransform parent.");
            return;
        }

        SpawnPlayer(parent);
        SpawnShields(parent);

        var totalWidth = (columns - 1) * columnSpacing;
        var leftX = -totalWidth * 0.5f;

        for(var row = 0; row < rows; row++) {
            GetAlienForRow(row, out var prefab, out var frame1);
            if(prefab == null) {
                continue;
            }

            for(var col = 0; col < columns; col++) {
                var alien = Instantiate(prefab, parent);
                var alienRect = alien.rectTransform;
                alienRect.anchoredPosition =
                    new Vector2(leftX + (col * columnSpacing), topCenter.y - (row * rowSpacing));

                var frames = alien.GetComponent<AlienFrames>();
                if(frames == null) {
                    Debug.LogError("Spawned alien prefab is missing AlienFrames component.");
                    continue;
                }

                var target = alien.GetComponent<AlienTarget>();
                if(target == null) {
                    Debug.LogError("Spawned alien prefab is missing AlienTarget component.");
                    continue;
                }

                frames.frame0 = alien.sprite;
                frames.frame1 = frame1;
                target.SetColumnIndex(col);
            }
        }

        initialAlienCount = AlienTarget.Active.Count;
    }

    private void SpawnPlayer(RectTransform parent) {
        if(playerPrefab == null) {
            return;
        }

        var player = Instantiate(playerPrefab, parent);
        player.rectTransform.anchoredPosition = playerStartPosition;
        spawnedPlayerRect = player.rectTransform;

        var controller = player.GetComponent<PlayerController>();
        if(controller == null) {
            Debug.LogError("Player prefab is missing PlayerController component.");
            return;
        }

        controller.SetScoreText(scoreText);
        controller.ResetScore();
    }

    private void SpawnShields(RectTransform parent) {
        if(shieldPrefab == null || shieldCount <= 0) {
            return;
        }

        var startX = -shieldsSpreadWidth * 0.5f;
        var step = shieldCount > 1 ? shieldsSpreadWidth / (shieldCount - 1) : 0f;

        for(var i = 0; i < shieldCount; i++) {
            var shield = Instantiate(shieldPrefab, parent);
            shield.rectTransform.anchoredPosition = new Vector2(startX + (i * step), shieldsY);
        }
    }

    private void GetAlienForRow(int row, out Image prefab, out Sprite frame1) {
        switch(row) {
            case 0:
                prefab = squidPrefab;
                frame1 = squidFrame1;
                return;
            case <= 2:
                prefab = crabPrefab;
                frame1 = crabFrame1;
                return;
            default:
                prefab = octopusPrefab;
                frame1 = octopusFrame1;
                break;
        }
    }
    #endregion

    #region Alien Shooting
    private void TryFireAlienShot() {
        if(AlienBullet.ActiveCount >= maxActiveAlienShots) {
            return;
        }

        var frontByColumn = GetFrontAliensByColumn();
        AlienTarget shooter = null;
        Image bulletPrefab = null;

        for(var attempt = 0; attempt < 3; attempt++) {
            var shotType = (AlienShotType)(shotCycleIndex % 3);
            shotCycleIndex++;

            shooter = GetShooterForType(shotType, frontByColumn);
            bulletPrefab = GetBulletPrefabForType(shotType);
            if(shooter != null && bulletPrefab != null) {
                break;
            }
        }

        if(shooter == null || bulletPrefab == null) {
            return;
        }

        var parent = alienBulletParent != null
            ? alienBulletParent
            : (formationParent != null ? formationParent : transform as RectTransform);
        if(parent == null) {
            return;
        }

        var bullet = Instantiate(bulletPrefab, parent);
        var bulletRect = bullet.rectTransform;
        bulletRect.anchoredPosition = shooter.RectTransform.anchoredPosition + alienBulletSpawnOffset;
    }

    private AlienTarget[] GetFrontAliensByColumn() {
        var frontByColumn = new AlienTarget[columns];
        var active = AlienTarget.Active;
        foreach(var candidate in active) {
            if(candidate == null || candidate.RectTransform == null) {
                continue;
            }

            var col = candidate.ColumnIndex;
            if(col < 0 || col >= columns) {
                continue;
            }

            var current = frontByColumn[col];
            if(current == null ||
               candidate.RectTransform.anchoredPosition.y < current.RectTransform.anchoredPosition.y) {
                frontByColumn[col] = candidate;
            }
        }

        return frontByColumn;
    }

    private AlienTarget GetShooterForType(AlienShotType shotType, AlienTarget[] frontByColumn) {
        if(frontByColumn == null || frontByColumn.Length == 0) {
            return null;
        }

        return shotType switch {
            AlienShotType.Rolling => GetRollingShooter(frontByColumn),
            AlienShotType.Plunger when AlienTarget.Active.Count <= 1 => null,
            _ => GetRandomFrontShooter(frontByColumn)
        };
    }

    private AlienTarget GetRollingShooter(AlienTarget[] frontByColumn) {
        var playerX = spawnedPlayerRect != null ? spawnedPlayerRect.anchoredPosition.x : 0f;
        AlienTarget best = null;
        var bestDistance = float.MaxValue;

        foreach(var candidate in frontByColumn) {
            if(candidate == null || candidate.RectTransform == null) {
                continue;
            }

            var distance = Mathf.Abs(candidate.RectTransform.anchoredPosition.x - playerX);
            if(!(distance < bestDistance)) continue;
            best = candidate;
            bestDistance = distance;
        }

        return best;
    }

    private static AlienTarget GetRandomFrontShooter(AlienTarget[] frontByColumn) {
        var count = 0;
        foreach(var alien in frontByColumn) {
            if(alien != null) {
                count++;
            }
        }

        if(count == 0) {
            return null;
        }

        var pick = Random.Range(0, count);
        foreach(var alien in frontByColumn) {
            if(alien == null) {
                continue;
            }

            if(pick == 0) {
                return alien;
            }

            pick--;
        }

        return null;
    }

    private Image GetBulletPrefabForType(AlienShotType shotType) {
        return shotType switch {
            AlienShotType.Rolling => rollBulletPrefab,
            AlienShotType.Plunger => plungerBulletPrefab,
            _ => squigglyBulletPrefab
        };
    }

    private void ScheduleNextShot() {
        var min = Mathf.Max(0.05f, fireIntervalMin);
        var max = Mathf.Max(min, fireIntervalMax);
        nextShotTime = Time.time + Random.Range(min, max);
    }
    #endregion

    #region Game Flow
    public void OnPlayerKilled() {
        if(deathSequenceStarted) {
            return;
        }

        deathSequenceStarted = true;

        var finalScore = PlayerController.Instance != null ? PlayerController.Instance.CurrentScore : 0;
        SaveHiScore(finalScore);
        UpdateHiScoreText();

        if(PlayerController.Instance != null) {
            Destroy(PlayerController.Instance.gameObject);
        }

        StartCoroutine(ReturnToMainMenuAfterDelay());
    }

    private static int GetHiScore() {
        return PlayerPrefs.GetInt(HiScoreKey, 0);
    }

    private static void SaveHiScore(int score) {
        var current = GetHiScore();
        if(score <= current) {
            return;
        }

        PlayerPrefs.SetInt(HiScoreKey, score);
        PlayerPrefs.Save();
    }

    private void UpdateHiScoreText() {
        if(hiScoreText == null) {
            return;
        }

        hiScoreText.text = GetHiScore().ToString("D4");
    }

    private IEnumerator ReturnToMainMenuAfterDelay() {
        yield return new WaitForSeconds(returnToMenuDelay);
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnAllAliensCleared() {
        if(deathSequenceStarted) {
            return;
        }

        deathSequenceStarted = true;
        SaveHiScore(PlayerController.Instance != null ? PlayerController.Instance.CurrentScore : 0);
        UpdateHiScoreText();
        StartCoroutine(ReturnToMainMenuAfterDelay());
    }

    private void HandleAlienKilled(AlienTarget _) {
        // Event-driven notification hook: tighten movement timing immediately after a kill.
        ScheduleNextMove();
    }
    #endregion

    #region Alien Movement
    private void ScheduleNextMove() {
        var alive = AlienTarget.Active.Count;
        if(alive <= 0) {
            nextMoveTime = Time.time + moveIntervalMax;
            return;
        }

        var max = Mathf.Max(0.05f, moveIntervalMax);
        var min = Mathf.Clamp(moveIntervalMin, 0.02f, max);
        var ratio = initialAlienCount > 0 ? (float)alive / initialAlienCount : 1f;
        var interval = Mathf.Lerp(min, max, Mathf.Clamp01(ratio));
        nextMoveTime = Time.time + interval;
    }

    private void MoveAliensOneStep() {
        var active = AlienTarget.Active;
        if(active.Count == 0) {
            return;
        }

        var horizontalDelta = moveDirection * Mathf.Abs(horizontalStep);
        var hitEdge = false;

        foreach(var alien in active) {
            if(alien == null || alien.RectTransform == null) {
                continue;
            }

            var nextX = alien.RectTransform.anchoredPosition.x + horizontalDelta;
            if(!(nextX < leftBoundX) && !(nextX > rightBoundX)) continue;
            hitEdge = true;
            break;
        }

        if(hitEdge) {
            foreach(var alien in active) {
                if(alien == null || alien.RectTransform == null) {
                    continue;
                }

                var pos = alien.RectTransform.anchoredPosition;
                pos.y -= Mathf.Abs(verticalStep);
                alien.RectTransform.anchoredPosition = pos;
            }

            moveDirection *= -1;
        } else {
            foreach(var alien in active) {
                if(alien == null || alien.RectTransform == null) {
                    continue;
                }

                var pos = alien.RectTransform.anchoredPosition;
                pos.x += horizontalDelta;
                alien.RectTransform.anchoredPosition = pos;
            }
        }

        ToggleAlienFrames();
    }

    private void ToggleAlienFrames() {
        useFrame1OnStep = !useFrame1OnStep;
        var active = AlienTarget.Active;
        foreach(var alien in active) {
            if(alien == null) {
                continue;
            }

            var frames = alien.GetComponent<AlienFrames>();
            var image = alien.GetComponent<Image>();
            if(frames == null || image == null) {
                continue;
            }

            image.sprite = useFrame1OnStep && frames.frame1 != null ? frames.frame1 : frames.frame0;
        }
    }
    #endregion
}
