using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
#if USE_ONNX
using Unity.InferenceEngine;
#endif

// Точка сборки модуля DDA в Unity: создаёт все объекты, подписывается на метрики
// и каждый кадр передаёт плавно меняющиеся параметры в спавнер.
// Клавиши 1 / 2 / 3: режим Soft / Aggressive / Fixed.
public class DDARunner : MonoBehaviour
{
    [SerializeField] private DDAMode mode = DDAMode.Aggressive;
    [SerializeField] private float transitionSeconds = 1.5f;
    [SerializeField] private string boundsFileName = "difficulty_bounds.json";
#if USE_ONNX
    [SerializeField] private ModelAsset onnxModel; // сюда перетащить dda_model.onnx
#endif
    [SerializeField] private TargetSpawner spawner;
    [SerializeField] private MetricsCollector metrics;
    [SerializeField] private RoundController round;

    public event Action Changed; // изменилось состояние или режим

    // Окно метрик обработано: метрики, состояние FSM и параметры мишени (для записи в профиль)
    public event Action<PlayerMetrics, DDAState, TargetConfiguration> WindowProcessed;

    public DDAState State => _controller.CurrentState;
    public DDAMode Mode => mode;

    private IAIController _controller;
    private IPredictionService _prediction;
    private DifficultyBoundsValidator _validator;
    private IGameplayInteractor _interactor;
    private ConfigSmoother _smoother;

    private void Awake()
    {
        if (spawner == null) spawner = FindFirstObjectByType<TargetSpawner>();
        if (metrics == null) metrics = FindFirstObjectByType<MetricsCollector>();
        if (round == null) round = FindFirstObjectByType<RoundController>();

        string path = Path.Combine(Application.streamingAssetsPath, boundsFileName);
        _validator = new DifficultyBoundsValidator(path) { Mode = mode };

        _prediction = CreatePredictionService();
        _controller = new AimDDAController(
            _prediction,
            new FsmDecisionStrategy(),
            new LinearFallbackDecisionStrategy(),
            _validator);

        _interactor = new SpawnerInteractor(spawner);
        _smoother = new ConfigSmoother(_controller.GetCurrentConfiguration(), transitionSeconds);
        _interactor.ApplyConfiguration(_smoother.Current);
    }

    // Выбор сервиса предсказания: нейросеть ONNX, а если её нет или она не загрузилась, эвристика
    private IPredictionService CreatePredictionService()
    {
#if USE_ONNX
        if (onnxModel != null)
        {
            try
            {
                IPredictionService onnx = new OnnxPredictionService(onnxModel);
                Debug.Log("[DDA] Prediction service: ONNX neural network");
                return onnx;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DDA] Cannot load ONNX model ({e.Message}). Using heuristic instead.");
            }
        }
        else
        {
            Debug.LogWarning("[DDA] ONNX model is not assigned in DDARunner. Using heuristic instead.");
        }
#endif
        Debug.Log("[DDA] Prediction service: heuristic");
        return new HeuristicPredictionService();
    }

    private void Start()
    {
        metrics.WindowCompleted += OnWindow;
        round.RoundStarted += OnRoundStarted;
    }

    private void OnDestroy()
    {
        (_prediction as IDisposable)?.Dispose(); // освободить ресурсы нейросети
        if (metrics != null) metrics.WindowCompleted -= OnWindow;
        if (round != null) round.RoundStarted -= OnRoundStarted;
    }

    private void Update()
    {
        HandleModeKeys();
        _interactor.ApplyConfiguration(_smoother.Tick(Time.deltaTime));
    }

    private void OnWindow(PlayerMetrics m)
    {
        if (mode == DDAMode.Fixed)
        {
            // В режиме Fixed сложность не меняется, но окно всё равно попадает в историю раунда
            WindowProcessed?.Invoke(m, _controller.CurrentState, _controller.GetCurrentConfiguration());
            return;
        }

        TargetConfiguration target = _controller.UpdateDifficulty(m);
        _smoother.SetTarget(target);

        Debug.Log($"[DDA] state={_controller.CurrentState}, mode={mode}, " +
                  $"scale={target.TargetScale:F2}, speed={target.TargetSpeed:F1}, lifetime={target.LifetimeSeconds:F1}");
        WindowProcessed?.Invoke(m, _controller.CurrentState, target);
        Changed?.Invoke();
    }

    private void OnRoundStarted()
    {
        // Каждый раунд начинается со средней сложности
        _controller.ResetState(DDAState.Medium);
        _smoother.Snap(_controller.GetCurrentConfiguration());
        _interactor.ApplyConfiguration(_smoother.Current);
        Changed?.Invoke();
    }

    private void HandleModeKeys()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) SetMode(DDAMode.Soft);
        else if (kb.digit2Key.wasPressedThisFrame) SetMode(DDAMode.Aggressive);
        else if (kb.digit3Key.wasPressedThisFrame) SetMode(DDAMode.Fixed);
    }

    private void SetMode(DDAMode newMode)
    {
        if (newMode == mode) return;

        mode = newMode;
        _validator.Mode = newMode;
        if (newMode == DDAMode.Fixed) _controller.ResetState(DDAState.Medium);

        _smoother.SetTarget(_controller.GetCurrentConfiguration());
        Changed?.Invoke();
    }
}
