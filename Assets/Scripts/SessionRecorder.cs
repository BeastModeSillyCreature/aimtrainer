using System;
using System.IO;
using UnityEngine;

// Записывает ход раунда (окна метрик и ответы DDA) и по окончании сохраняет раунд в профиль.
// Хранилище подключено через интерфейс IProfileRepository: чтобы перейти на SQLite,
// достаточно заменить одну строку в Awake.
public class SessionRecorder : MonoBehaviour
{
    [SerializeField] private RoundController round;
    [SerializeField] private DDARunner dda;
    [SerializeField] private string fileName = "profile.json";

    public event Action<SessionRecord> SessionSaved;

    public IProfileRepository Repository { get; private set; }
    public PlayerProfile Profile { get; private set; }
    public SessionRecord PreviousSession { get; private set; } // предыдущий раунд (для сравнения), может быть null

    private SessionRecord _current;
    private float _startTime;

    private void Awake()
    {
        if (round == null) round = FindFirstObjectByType<RoundController>();
        if (dda == null) dda = FindFirstObjectByType<DDARunner>();

        string path = Path.Combine(Application.persistentDataPath, fileName);
        Repository = new JsonProfileRepository(path); // <- здесь позже будет SqliteProfileRepository
        Profile = Repository.Load();

        Debug.Log($"[Profile] file: {path}, saved rounds: {Profile.sessions.Count}");
    }

    private void Start()
    {
        round.RoundStarted += OnRoundStarted;
        round.RoundFinished += OnRoundFinished;
        if (dda != null) dda.WindowProcessed += OnWindow;
    }

    private void OnDestroy()
    {
        if (round != null)
        {
            round.RoundStarted -= OnRoundStarted;
            round.RoundFinished -= OnRoundFinished;
        }
        if (dda != null) dda.WindowProcessed -= OnWindow;
    }

    private void OnRoundStarted()
    {
        _current = new SessionRecord
        {
            id = Guid.NewGuid().ToString("N"),
            startedAtUtc = DateTime.UtcNow.ToString("o")
        };
        _startTime = Time.time; // на паузе Time.time стоит, поэтому пауза не входит в длительность
    }

    private void OnWindow(PlayerMetrics metrics, DDAState state, TargetConfiguration config)
    {
        if (_current == null) return;

        _current.windows.Add(new WindowRecord
        {
            timeSeconds = Time.time - _startTime,
            shots = metrics.ShotCount,
            hitRate = metrics.HitRate,
            avgTimeToHitMs = metrics.AvgReactionMs,
            stateIndex = (int)state,
            state = state.ToString(),
            targetScale = config.TargetScale,
            targetSpeed = config.TargetSpeed,
            targetLifetime = config.LifetimeSeconds
        });
    }

    private void OnRoundFinished()
    {
        if (_current == null) return;

        RoundStats stats = round.Stats;
        _current.mode = dda != null ? dda.Mode.ToString() : "None";
        _current.durationSeconds = Time.time - _startTime;
        _current.shots = stats.Shots;
        _current.hits = stats.Hits;
        _current.expiredTargets = stats.ExpiredTargets;
        _current.accuracy = stats.Accuracy;
        _current.avgTimeToHitMs = stats.AvgTimeToHitMs;
        _current.avgReactionMs = stats.AvgReactionMs;

        int count = Profile.sessions.Count;
        PreviousSession = count > 0 ? Profile.sessions[count - 1] : null;

        Repository.SaveSession(_current);
        SessionSaved?.Invoke(_current);
        _current = null;
    }
}
