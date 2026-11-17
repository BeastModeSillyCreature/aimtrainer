using System;
using UnityEngine;

// Связывает выстрелы с InputProcessor и раз в N секунд выдаёт окно метрик (ЛР №1, сценарий 1).
// Окно публикуется, когда прошло windowSeconds И накоплено не меньше minShots выстрелов;
// пока выстрелов мало, данные продолжают копиться (буфер не засоряется нулями, E-1.1).
// На событие WindowCompleted подписан DDA-контроллер (DDARunner).
public class MetricsCollector : MonoBehaviour
{
    [SerializeField] private float windowSeconds = 5f;
    [SerializeField] private int minShots = 10; // минимальный пакет выстрелов (сценарий 2 из ЛР №1)
    [SerializeField] private bool logToConsole = true;
    [SerializeField] private ShootingController shooting;
    [SerializeField] private RoundController round;

    public event Action<PlayerMetrics> WindowCompleted;

    private readonly InputProcessor _processor = new InputProcessor();
    private float _windowTimer;

    private void Awake()
    {
        if (shooting == null) shooting = FindFirstObjectByType<ShootingController>();
        if (round == null) round = FindFirstObjectByType<RoundController>();
    }

    private void Start()
    {
        shooting.ShotFired += OnShot;
        round.RoundStarted += OnRoundStarted;
    }

    private void OnDestroy()
    {
        if (shooting != null) shooting.ShotFired -= OnShot;
        if (round != null) round.RoundStarted -= OnRoundStarted;
    }

    private void OnRoundStarted()
    {
        _processor.Reset();
        _windowTimer = 0f;
    }

    private void OnShot(float reactionMs, bool isHit)
    {
        if (round.State != RoundState.Running) return;
        _processor.RegisterInput(reactionMs, isHit);
    }

    private void Update()
    {
        if (round.State != RoundState.Running) return;

        _windowTimer += Time.deltaTime; // на паузе deltaTime = 0, окно не идёт
        if (_windowTimer < windowSeconds) return;
        if (_processor.ShotCount < minShots) return; // мало данных: копим дальше
        _windowTimer = 0f;

        PlayerMetrics metrics = _processor.BuildMetrics();
        _processor.Reset();

        if (logToConsole)
            Debug.Log($"[Metrics] shots={metrics.ShotCount}, hitRate={metrics.HitRate:P0}, avgTimeToHit={metrics.AvgReactionMs:F0} ms");

        WindowCompleted?.Invoke(metrics);
    }
}
