using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SpawnInvaderFormation : MonoBehaviour
{
    public const string HiScoreKey = "HI-SCORE";
    public static SpawnInvaderFormation Instance { get; private set; }

    private static readonly int[] ColumnFireTable = new int[]
    {
        1, 7, 1, 1, 1, 4, 11, 1, 6, 3, 1, 1, 11, 9, 2, 8, 2, 11, 4, 7, 10
    };

    private enum AlienShotType
    {
        Rolling = 0,
        Plunger = 1,
        Squiggly = 2
    }

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
    [SerializeField] private Vector2 playerStartPosition = new Vector2(0f, -430f);
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
    [SerializeField] private Vector2 topCenter = new Vector2(0f, 360f);

    [Header("Alien Shooting")]
    [SerializeField] private RectTransform alienBulletParent;
    [SerializeField] private Image plungerBulletPrefab;
    [SerializeField] private Image rollBulletPrefab;
    [SerializeField] private Image squigglyBulletPrefab;
    [SerializeField] private Vector2 alienBulletSpawnOffset = new Vector2(0f, -35f);
    [SerializeField] private float fireIntervalMin = 0.65f;
    [SerializeField] private float fireIntervalMax = 1.2f;
    [SerializeField] private int maxActiveAlienShots = 1;

    private RectTransform spawnedPlayerRect;
    private float nextShotTime;
    private int shotCycleIndex;
    private int plungerTableIndex;
    private int squigglyTableIndex = 6;
    private bool deathSequenceStarted;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        Spawn();
        UpdateHiScoreText();
        ScheduleNextShot();
    }

    private void Update()
    {
        if (deathSequenceStarted)
        {
            return;
        }

        if (Time.time < nextShotTime)
        {
            return;
        }

        TryFireAlienShot();
        ScheduleNextShot();
    }

    private void Spawn()
    {
        RectTransform parent = formationParent != null ? formationParent : transform as RectTransform;
        if (parent == null)
        {
            Debug.LogError("SpawnInvaderFormation needs a RectTransform parent.");
            return;
        }

        SpawnPlayer(parent);
        SpawnShields(parent);

        float totalWidth = (columns - 1) * columnSpacing;
        float leftX = -totalWidth * 0.5f;

        for (int row = 0; row < rows; row++)
        {
            GetAlienForRow(row, out Image prefab, out Sprite frame1);
            if (prefab == null)
            {
                continue;
            }

            for (int col = 0; col < columns; col++)
            {
                Image alien = Instantiate(prefab, parent);
                RectTransform alienRect = alien.rectTransform;
                alienRect.anchoredPosition = new Vector2(leftX + (col * columnSpacing), topCenter.y - (row * rowSpacing));

                AlienFrames frames = alien.GetComponent<AlienFrames>();
                if (frames == null)
                {
                    Debug.LogError("Spawned alien prefab is missing AlienFrames component.");
                    continue;
                }

                AlienTarget target = alien.GetComponent<AlienTarget>();
                if (target == null)
                {
                    Debug.LogError("Spawned alien prefab is missing AlienTarget component.");
                    continue;
                }

                frames.frame0 = alien.sprite;
                frames.frame1 = frame1;
                target.SetColumnIndex(col);
            }
        }
    }

    private void SpawnPlayer(RectTransform parent)
    {
        if (playerPrefab == null)
        {
            return;
        }

        Image player = Instantiate(playerPrefab, parent);
        player.rectTransform.anchoredPosition = playerStartPosition;
        spawnedPlayerRect = player.rectTransform;

        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller == null)
        {
            Debug.LogError("Player prefab is missing PlayerController component.");
            return;
        }

        controller.SetScoreText(scoreText);
        controller.ResetScore();
    }

    private void SpawnShields(RectTransform parent)
    {
        if (shieldPrefab == null || shieldCount <= 0)
        {
            return;
        }

        float startX = -shieldsSpreadWidth * 0.5f;
        float step = shieldCount > 1 ? shieldsSpreadWidth / (shieldCount - 1) : 0f;

        for (int i = 0; i < shieldCount; i++)
        {
            Image shield = Instantiate(shieldPrefab, parent);
            shield.rectTransform.anchoredPosition = new Vector2(startX + (i * step), shieldsY);
        }
    }

    private void GetAlienForRow(int row, out Image prefab, out Sprite frame1)
    {
        if (row == 0)
        {
            prefab = squidPrefab;
            frame1 = squidFrame1;
            return;
        }

        if (row <= 2)
        {
            prefab = crabPrefab;
            frame1 = crabFrame1;
            return;
        }

        prefab = octopusPrefab;
        frame1 = octopusFrame1;
    }

    private void TryFireAlienShot()
    {
        if (AlienBullet.ActiveCount >= maxActiveAlienShots)
        {
            return;
        }

        var frontByColumn = GetFrontAliensByColumn();
        AlienShotType shotType = (AlienShotType)(shotCycleIndex % 3);
        shotCycleIndex++;

        AlienTarget shooter = GetShooterForType(shotType, frontByColumn);
        if (shooter == null)
        {
            return;
        }

        Image bulletPrefab = GetBulletPrefabForType(shotType);
        if (bulletPrefab == null)
        {
            return;
        }

        RectTransform parent = alienBulletParent != null
            ? alienBulletParent
            : (formationParent != null ? formationParent : transform as RectTransform);
        if (parent == null)
        {
            return;
        }

        Image bullet = Instantiate(bulletPrefab, parent);
        RectTransform bulletRect = bullet.rectTransform;
        bulletRect.anchoredPosition = shooter.RectTransform.anchoredPosition + alienBulletSpawnOffset;
    }

    private AlienTarget[] GetFrontAliensByColumn()
    {
        AlienTarget[] frontByColumn = new AlienTarget[columns];
        var active = AlienTarget.Active;
        for (int i = 0; i < active.Count; i++)
        {
            AlienTarget candidate = active[i];
            if (candidate == null || candidate.RectTransform == null)
            {
                continue;
            }

            int col = candidate.ColumnIndex;
            if (col < 0 || col >= columns)
            {
                continue;
            }

            AlienTarget current = frontByColumn[col];
            if (current == null || candidate.RectTransform.anchoredPosition.y < current.RectTransform.anchoredPosition.y)
            {
                frontByColumn[col] = candidate;
            }
        }

        return frontByColumn;
    }

    private AlienTarget GetShooterForType(AlienShotType shotType, AlienTarget[] frontByColumn)
    {
        if (frontByColumn == null || frontByColumn.Length == 0)
        {
            return null;
        }

        if (shotType == AlienShotType.Rolling)
        {
            return GetRollingShooter(frontByColumn);
        }

        if (shotType == AlienShotType.Plunger)
        {
            if (AlienTarget.Active.Count <= 1)
            {
                return null;
            }

            return GetTableShooter(frontByColumn, ref plungerTableIndex, 0, 15);
        }

        return GetTableShooter(frontByColumn, ref squigglyTableIndex, 6, 20);
    }

    private AlienTarget GetRollingShooter(AlienTarget[] frontByColumn)
    {
        float playerX = spawnedPlayerRect != null ? spawnedPlayerRect.anchoredPosition.x : 0f;
        AlienTarget best = null;
        float bestDistance = float.MaxValue;

        for (int col = 0; col < frontByColumn.Length; col++)
        {
            AlienTarget candidate = frontByColumn[col];
            if (candidate == null || candidate.RectTransform == null)
            {
                continue;
            }

            float distance = Mathf.Abs(candidate.RectTransform.anchoredPosition.x - playerX);
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }

    private AlienTarget GetTableShooter(AlienTarget[] frontByColumn, ref int tableIndex, int minIndex, int maxIndex)
    {
        int attempts = maxIndex - minIndex + 1;
        for (int i = 0; i < attempts; i++)
        {
            int clamped = Mathf.Clamp(tableIndex, minIndex, maxIndex);
            int oneBasedColumn = ColumnFireTable[clamped];
            int zeroBasedColumn = oneBasedColumn - 1;

            tableIndex++;
            if (tableIndex > maxIndex)
            {
                tableIndex = minIndex;
            }

            if (zeroBasedColumn < 0 || zeroBasedColumn >= frontByColumn.Length)
            {
                continue;
            }

            AlienTarget candidate = frontByColumn[zeroBasedColumn];
            if (candidate != null)
            {
                return candidate;
            }
        }

        return null;
    }

    private Image GetBulletPrefabForType(AlienShotType shotType)
    {
        if (shotType == AlienShotType.Rolling)
        {
            return rollBulletPrefab;
        }

        if (shotType == AlienShotType.Plunger)
        {
            return plungerBulletPrefab;
        }

        return squigglyBulletPrefab;
    }

    private void ScheduleNextShot()
    {
        float min = Mathf.Max(0.05f, fireIntervalMin);
        float max = Mathf.Max(min, fireIntervalMax);
        nextShotTime = Time.time + Random.Range(min, max);
    }

    public void OnPlayerKilled()
    {
        if (deathSequenceStarted)
        {
            return;
        }

        deathSequenceStarted = true;

        int finalScore = PlayerController.Instance != null ? PlayerController.Instance.CurrentScore : 0;
        SaveHiScore(finalScore);
        UpdateHiScoreText();

        if (PlayerController.Instance != null)
        {
            Destroy(PlayerController.Instance.gameObject);
        }

        StartCoroutine(ReturnToMainMenuAfterDelay());
    }

    private static int GetHiScore()
    {
        return PlayerPrefs.GetInt(HiScoreKey, 0);
    }

    private static void SaveHiScore(int score)
    {
        int current = GetHiScore();
        if (score <= current)
        {
            return;
        }

        PlayerPrefs.SetInt(HiScoreKey, score);
        PlayerPrefs.Save();
    }

    private void UpdateHiScoreText()
    {
        if (hiScoreText == null)
        {
            return;
        }

        hiScoreText.text = GetHiScore().ToString("D4");
    }

    private IEnumerator ReturnToMainMenuAfterDelay()
    {
        yield return new WaitForSeconds(returnToMenuDelay);
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }
}
