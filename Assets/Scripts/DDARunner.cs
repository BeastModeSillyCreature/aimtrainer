using System;
using UnityEngine;

// Точка сборки модуля DDA в Unity. После рефакторинга (ЛР №5) класс стал короче и не знает про конкретные реализации:
//  - ввод берёт из IInputProvider, границы сложности из хранилища (IGameStateStorage);
//  - стратегии создаёт StrategyFactory по ассету DDASettings;
//  - об изменении сложности сообщает событиями (OnDifficultyChanged и канал ScriptableObject).
// Горячая замена: смена ассета DDASettings или типа сервиса предсказания в Play Mode пересобирает контроллер на лету.
// Клавиши 1 / 2 / 3: режим Soft / Aggressive / Fixed.
public class DDARunner : MonoBehaviour
{
    [SerializeField] private DDASettings settings;
    [SerializeField] private DifficultyChangedEvent difficultyChangedEvent; // необязательно: канал ScriptableObject
    [SerializeField] private TargetSpawner spawner;
    [SerializeField] private MetricsCollector metrics;
    [SerializeField] private RoundController round;
    [SerializeField] private PlayerLook look;

    public event Action Changed; // изменилось состояние или режим

    // US-3: сложность изменилась (с параметрами для подписчиков)
    public event Action<DifficultyChange> OnDifficultyChanged;

    // Окно метрик обработано: метрики, состояние FSM и параметры мишени (для записи в профиль)
    public event Action<PlayerMetrics, DDAState, TargetConfiguration> WindowProcessed;

    public DDAState State => _controller.CurrentState;
    public DDAMode Mode => _mode;

    private IInputProvider _input;
    private IGameStateStorage _storage;
    private DDASettings _defaultSettings; // создаётся, если ассет не назначен
    private DifficultyBoundsValidator _validator;
    private IAIController _controller;
    private IGameplayInteractor _interactor;
    private ConfigSmoother _smoother;
    private DDAMode _mode;

    // Что применено сейчас (нужно, чтобы заметить смену настроек в Play Mode)
    private DDASettings _appliedSettings;
    private PredictionServiceType _appliedPrediction;
    private UnityEngine.Object _appliedModel;

    private DDASettings Current => settings != null ? settings : _defaultSettings;

    private void Awake()
    {
        if (spawner == null) spawner = FindFirstObjectByType<TargetSpawner>();
        if (metrics == null) metrics = FindFirstObjectByType<MetricsCollector>();
        if (round == null) round = FindFirstObjectByType<RoundController>();
        if (look == null) look = FindFirstObjectByType<PlayerLook>();

        _input = ServiceLocator.Get<IInputProvider>();
        _storage = ServiceLocator.Get<IGameStateStorage>();

        if (settings == null)
        {
            _defaultSettings = ScriptableObject.CreateInstance<DDASettings>();
            Debug.LogWarning("[DDA] DDASettings is not assigned in DDARunner: using default settings");
        }

        _mode = Current.defaultMode;
        _validator = new DifficultyBoundsValidator(_storage) { Mode = _mode };
        BuildController();

        _interactor = new SpawnerInteractor(spawner);
        _smoother = new ConfigSmoother(_controller.GetCurrentConfiguration(), Current.transitionSeconds);
        _interactor.ApplyConfiguration(_smoother.Current);
    }

    private void Start()
    {
        metrics.WindowCompleted += OnWindow;
        round.RoundStarted += OnRoundStarted;
        look.MenuToggled += OnMenuToggled;
    }

    private void OnDestroy()
    {
        (_controller as IDisposable)?.Dispose();
        if (_defaultSettings != null) Destroy(_defaultSettings);

        if (metrics != null) metrics.WindowCompleted -= OnWindow;
        if (round != null) round.RoundStarted -= OnRoundStarted;
        if (look != null) look.MenuToggled -= OnMenuToggled;
    }

    private void Update()
    {
        CheckSettingsChanged();
        HandleModeKeys();
        _interactor.ApplyConfiguration(_smoother.Tick(Time.deltaTime));
    }

    // Собирает контроллер по текущим настройкам; состояние FSM сохраняется
    private void BuildController()
    {
        DDASettings s = Current;
        DDAState state = _controller != null ? _controller.CurrentState : DDAState.Medium;

        (_controller as IDisposable)?.Dispose();

        AimDDAController controller = StrategyFactory.CreateController(s, _validator);
        controller.ResetState(state);
        _controller = controller;

        _appliedSettings = s;
        _appliedPrediction = s.prediction;
        _appliedModel = s.ModelReference;
    }

    // Горячая замена: другой ассет, другой тип сервиса или другая модель
    private void CheckSettingsChanged()
    {
        DDASettings s = Current;
        if (s == _appliedSettings && s.prediction == _appliedPrediction && s.ModelReference == _appliedModel) return;

        BuildController();
        _smoother.SetTarget(_controller.GetCurrentConfiguration());
        Debug.Log($"[DDA] Settings changed in Play Mode: prediction={s.prediction}");
        Publish(default, _controller.GetCurrentConfiguration());
    }

    private void OnWindow(PlayerMetrics m)
    {
        if (_mode == DDAMode.Fixed)
        {
            // В режиме Fixed сложность не меняется, но окно всё равно попадает в историю раунда
            WindowProcessed?.Invoke(m, _controller.CurrentState, _controller.GetCurrentConfiguration());
            return;
        }

        TargetConfiguration target = _controller.UpdateDifficulty(m);
        _smoother.SetTarget(target);

        Debug.Log($"[DDA] state={_controller.CurrentState}, mode={_mode}, " +
                  $"scale={target.TargetScale:F2}, speed={target.TargetSpeed:F1}, lifetime={target.LifetimeSeconds:F1}");

        WindowProcessed?.Invoke(m, _controller.CurrentState, target);
        Publish(m, target);
    }

    private void OnRoundStarted()
    {
        // Каждый раунд начинается со средней сложности
        _controller.ResetState(DDAState.Medium);
        _smoother.Snap(_controller.GetCurrentConfiguration());
        _interactor.ApplyConfiguration(_smoother.Current);
        Publish(default, _smoother.Current);
    }

    // Меню закрыто: перечитываем границы сложности из JSON, правки геймдизайнера применяются сразу
    private void OnMenuToggled()
    {
        if (look.InMenu) return;

        _validator.Reload();
        _smoother.SetTarget(_controller.GetCurrentConfiguration());
        Publish(default, _controller.GetCurrentConfiguration());
    }

    private void HandleModeKeys()
    {
        switch (_input.ModeKeyPressed)
        {
            case 1: SetMode(DDAMode.Soft); break;
            case 2: SetMode(DDAMode.Aggressive); break;
            case 3: SetMode(DDAMode.Fixed); break;
        }
    }

    private void SetMode(DDAMode newMode)
    {
        if (newMode == _mode) return;

        _mode = newMode;
        _validator.Mode = newMode;
        if (newMode == DDAMode.Fixed) _controller.ResetState(DDAState.Medium);

        _smoother.SetTarget(_controller.GetCurrentConfiguration());
        Publish(default, _controller.GetCurrentConfiguration());
    }

    // Сообщить всем подписчикам: событие C#, событие US-3 и канал ScriptableObject
    private void Publish(PlayerMetrics metricsSnapshot, TargetConfiguration config)
    {
        DifficultyChange change = new DifficultyChange(_controller.CurrentState, _mode, config, metricsSnapshot);
        OnDifficultyChanged?.Invoke(change);
        if (difficultyChangedEvent != null) difficultyChangedEvent.Raise(change);
        Changed?.Invoke();
    }
}
