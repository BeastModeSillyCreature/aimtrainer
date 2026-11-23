using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum RoundState
{
    Waiting,    // ждём клик для старта
    Countdown,  // обратный отсчёт перед раундом
    Running,    // идёт раунд
    Finished    // показываем результаты
}

// Управляет раундом: клик -> отсчёт (5 с) -> раунд (N с) -> результаты -> клик или R для нового раунда.
//  R во время отсчёта или раунда: перезапуск, прерванная попытка не сохраняется.
//  Завершённый раунд всегда сохраняется в профиль, удалить его нельзя.
// Если во время отсчёта или раунда нажать Esc (курсор освобождён), игра ставится на паузу.
[DefaultExecutionOrder(20)] // после PlayerLook и ShootingController: клик старта не засчитывается как выстрел
public class RoundController : MonoBehaviour
{
    [SerializeField] private float roundDuration = 60f;
    [SerializeField] private float countdownSeconds = 5f;
    [SerializeField] private TargetSpawner spawner;
    [SerializeField] private ShootingController shooting;
    [SerializeField] private PlayerLook look;
    [SerializeField] private ReactionTracker reactionTracker;

    public event Action CountdownStarted;
    public event Action RoundStarted;        // отсчёт закончился, мишени пошли
    public event Action RoundFinished;
    public event Action RoundAborted;        // раунд прерван клавишей R
    public event Action StatsChanged;

    public RoundState State { get; private set; } = RoundState.Waiting;
    public float TimeLeft { get; private set; }
    public float CountdownLeft { get; private set; }
    public RoundStats Stats { get; } = new RoundStats();
    public bool IsPaused => (State == RoundState.Running || State == RoundState.Countdown) && Time.timeScale == 0f;

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
        // R работает только когда меню настроек закрыто
        Keyboard keyboard = Keyboard.current;
        bool restartPressed = keyboard != null && keyboard.rKey.wasPressedThisFrame
                              && look != null && !look.InMenu;

        switch (State)
        {
            case RoundState.Waiting:
                if (ClickedThisFrame()) BeginCountdown();
                break;

            case RoundState.Countdown:
                if (restartPressed)
                {
                    BeginCountdown(); // отсчёт начинается заново
                    break;
                }
                Time.timeScale = Cursor.lockState == CursorLockMode.Locked ? 1f : 0f; // пауза по Esc
                CountdownLeft -= Time.deltaTime;
                if (CountdownLeft <= 0f) StartRound();
                break;

            case RoundState.Running:
                if (restartPressed)
                {
                    AbortRound();
                    break;
                }
                Time.timeScale = Cursor.lockState == CursorLockMode.Locked ? 1f : 0f; // пауза по Esc
                TimeLeft -= Time.deltaTime;
                if (TimeLeft <= 0f) FinishRound();
                break;

            case RoundState.Finished:
                // секунду игнорируем ввод, чтобы случайный клик или R не перезапустили раунд сразу
                if (Time.unscaledTime - _finishedAt <= 1f) break;

                if (restartPressed || ClickedThisFrame()) BeginCountdown();
                break;
        }
    }

    private bool ClickedThisFrame()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && look.CanShoot && mouse.leftButton.wasPressedThisFrame;
    }

    private void BeginCountdown()
    {
        Stats.Reset();
        TimeLeft = roundDuration;
        CountdownLeft = countdownSeconds;
        State = RoundState.Countdown;
        Time.timeScale = 1f;
        CountdownStarted?.Invoke();
        StatsChanged?.Invoke();
    }

    private void StartRound()
    {
        State = RoundState.Running;
        Time.timeScale = 1f;
        spawner.StartSpawning();
        RoundStarted?.Invoke();
        StatsChanged?.Invoke();
    }

    private void AbortRound()
    {
        spawner.StopSpawning();
        RoundAborted?.Invoke();
        BeginCountdown();
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
