using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum RoundState
{
    Waiting,   // ждём клик для старта
    Running,   // идёт раунд
    Finished   // показываем результаты
}

// Управляет раундом: старт по клику -> N секунд -> результаты -> клик для нового раунда.
// Если во время раунда нажать Esc (курсор освобождён), игра ставится на паузу.
[DefaultExecutionOrder(20)] // после PlayerLook и ShootingController: клик старта не засчитывается как выстрел
public class RoundController : MonoBehaviour
{
    [SerializeField] private float roundDuration = 60f;
    [SerializeField] private TargetSpawner spawner;
    [SerializeField] private ShootingController shooting;
    [SerializeField] private PlayerLook look;
    [SerializeField] private ReactionTracker reactionTracker;

    public event Action RoundStarted;
    public event Action RoundFinished;
    public event Action StatsChanged;

    public RoundState State { get; private set; } = RoundState.Waiting;
    public float TimeLeft { get; private set; }
    public RoundStats Stats { get; } = new RoundStats();
    public bool IsPaused => State == RoundState.Running && Time.timeScale == 0f;

    private float _finishedAt;

    private void Awake()
    {
        if (spawner == null) spawner = FindFirstObjectByType<TargetSpawner>();
        if (shooting == null) shooting = FindFirstObjectByType<ShootingController>();
        if (look == null) look = FindFirstObjectByType<PlayerLook>();
        if (reactionTracker == null) reactionTracker = FindFirstObjectByType<ReactionTracker>();
        TimeLeft = roundDuration;
    }

    private void Start()
    {
        shooting.ShotFired += OnShot;
        spawner.TargetExpired += OnExpired;
        reactionTracker.ReactionMeasured += OnReaction;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        if (shooting != null) shooting.ShotFired -= OnShot;
        if (spawner != null) spawner.TargetExpired -= OnExpired;
        if (reactionTracker != null) reactionTracker.ReactionMeasured -= OnReaction;
    }

    private void Update()
    {
        switch (State)
        {
            case RoundState.Waiting:
                if (ClickedThisFrame()) StartRound();
                break;

            case RoundState.Running:
                Time.timeScale = Cursor.lockState == CursorLockMode.Locked ? 1f : 0f; // пауза по Esc
                TimeLeft -= Time.deltaTime;
                if (TimeLeft <= 0f) FinishRound();
                break;

            case RoundState.Finished:
                // секунду игнорируем клики, чтобы не перезапустить раунд случайной серией выстрелов
                if (Time.unscaledTime - _finishedAt > 1f && ClickedThisFrame()) StartRound();
                break;
        }
    }

    private bool ClickedThisFrame()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && look.CanShoot && mouse.leftButton.wasPressedThisFrame;
    }

    private void StartRound()
    {
        Stats.Reset();
        TimeLeft = roundDuration;
        State = RoundState.Running;
        Time.timeScale = 1f;
        spawner.StartSpawning();
        RoundStarted?.Invoke();
        StatsChanged?.Invoke();
    }

    private void FinishRound()
    {
        TimeLeft = 0f;
        State = RoundState.Finished;
        Time.timeScale = 1f;
        _finishedAt = Time.unscaledTime;
        spawner.StopSpawning();
        RoundFinished?.Invoke();
    }

    private void OnShot(float reactionMs, bool isHit)
    {
        if (State != RoundState.Running) return;
        Stats.RegisterShot(reactionMs, isHit);
        StatsChanged?.Invoke();
    }

    private void OnExpired()
    {
        if (State != RoundState.Running) return;
        Stats.RegisterExpired();
        StatsChanged?.Invoke();
    }

    private void OnReaction(float reactionMs)
    {
        if (State != RoundState.Running) return;
        Stats.RegisterReaction(reactionMs);
        StatsChanged?.Invoke();
    }
}
