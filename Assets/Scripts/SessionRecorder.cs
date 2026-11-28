using System;
using UnityEngine;

// Записывает ход раунда (окна метрик и ответы DDA) и по окончании сохраняет раунд в профиль.
// После рефакторинга (ЛР №5) рекордер не знает, где хранятся данные: он работает с фасадом IGameStateStorage.
public class SessionRecorder : MonoBehaviour
{
    [SerializeField] private RoundController round;
    [SerializeField] private DDARunner dda;
    [SerializeField] private BlindZoneTracker blindZones;
    [SerializeField] private SessionSavedEvent sessionSavedEvent; // необязательно: канал ScriptableObject

    public event Action<SessionRecord> SessionSaved;

    public PlayerProfile Profile { get; private set; }
    public SessionRecord PreviousSession { get; private set; } // предыдущий раунд (для сравнения), может быть null

    private IGameStateStorage _storage;
    private SessionRecord _current;
    private float _startTime;

    private void Awake()
    {
        if (round == null) round = FindFirstObjectByType<RoundController>();
        if (dda == null) dda = FindFirstObjectByType<DDARunner>();
        if (blindZones == null) blindZones = FindFirstObjectByType<BlindZoneTracker>();

        _storage = ServiceLocator.Get<IGameStateStorage>();
        Profile = _storage.LoadSession();

        Debug.Log($"[Profile] saved rounds: {Profile.sessions.Count}");
    }

    private void Start()
    {
        round.RoundStarted += OnRoundStarted;
        round.RoundFinished += OnRoundFinished;
        round.RoundAborted += OnRoundAborted;
        if (dda != null) dda.WindowProcessed += OnWindow;
    }

    private void OnDestroy()
    {
        if (round != null)
        {
            round.RoundStarted -= OnRoundStarted;
            round.RoundFinished -= OnRoundFinished;
            round.RoundAborted -= OnRoundAborted;
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

    // Раунд прерван клавишей R: прерванная попытка не сохраняется
    private void OnRoundAborted()
    {
        _current = null;
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
            targetLifetime = config.LifetimeSeconds,
            undershoots = metrics.Undershoots,
            overshoots = metrics.Overshoots
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
        _current.undershoots = stats.Undershoots;
        _current.overshoots = stats.Overshoots;
        _current.otherMisses = stats.OtherMisses;
        if (blindZones != null) blindZones.Map.WriteTo(_current);

        int count = Profile.sessions.Count;
        PreviousSession = count > 0 ? Profile.sessions[count - 1] : null;

        // Профиль в памяти обновляется сразу, запись на диск идёт в фоне
        SaveInBackground(_current);

        SessionSaved?.Invoke(_current);
        if (sessionSavedEvent != null) sessionSavedEvent.Raise(_current);
        _current = null;
    }

    // Запустить сохранение и не ждать: главный поток не блокируется, ошибка попадает в Console
    private async void SaveInBackground(SessionRecord session)
    {
        try
        {
            await _storage.SaveSessionAsync(session);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Profile] Cannot save round: {e.Message}");
        }
    }
}
