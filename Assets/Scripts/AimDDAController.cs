using System;
using UnityEngine;

// Ядро DDA (ЛР №3, этап 4). Зависит только от интерфейсов.
// Цепочка: метрики -> признаки -> предсказание -> состояние FSM -> параметры -> валидация.
// Если предсказание падает, срабатывает резервная стратегия (E-2.1).
public sealed class AimDDAController : IAIController
{
    private readonly IPredictionService _prediction;
    private readonly IDecisionStrategy _strategy;
    private readonly IDecisionStrategy _fallback;
    private readonly IDifficultyValidator _validator;

    private readonly float[] _features = new float[2]; // буфер переиспользуется

    public DDAState CurrentState { get; private set; } = DDAState.Medium;
    public bool LastUsedFallback { get; private set; }

    // E-2.1: если предсказание длится дольше этого времени, используется резервная стратегия
    public float MaxInferenceMs { get; set; } = 10f;
    public float LastInferenceMs { get; private set; }

    public AimDDAController(
        IPredictionService prediction,
        IDecisionStrategy strategy,
        IDecisionStrategy fallback,
        IDifficultyValidator validator)
    {
        _prediction = prediction ?? throw new ArgumentNullException(nameof(prediction));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public void ResetState(DDAState state)
    {
        CurrentState = state;
    }

    // Параметры мишени для текущего состояния (после проверки границ)
    public TargetConfiguration GetCurrentConfiguration()
    {
        return _validator.ValidateAndClamp(_strategy.GetConfiguration(CurrentState));
    }

    public TargetConfiguration UpdateDifficulty(PlayerMetrics metrics)
    {
        _features[0] = metrics.HitRate;
        _features[1] = metrics.AvgReactionMs; // время до попадания (Time-to-Hit, FR-2)

        DDAState predicted;
        try
        {
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
            float[] logits = _prediction.Predict(_features);
            LastInferenceMs = (float)timer.Elapsed.TotalMilliseconds;

            if (LastInferenceMs > MaxInferenceMs)
                throw new TimeoutException($"inference took {LastInferenceMs:F1} ms (limit {MaxInferenceMs:F0} ms)");

            predicted = _strategy.EvaluateState(logits);
            LastUsedFallback = false;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DDA] Prediction failed ({e.Message}), using linear fallback");
            predicted = _fallback.EvaluateState(_features);
            LastUsedFallback = true;
        }

        CurrentState = StepToward(CurrentState, predicted);
        return GetCurrentConfiguration();
    }

    // Автомат меняет состояние не больше чем на одну ступень за раз (без резких скачков)
    private static DDAState StepToward(DDAState current, DDAState target)
    {
        int c = (int)current;
        int t = (int)target;
        if (t > c) c++;
        else if (t < c) c--;
        return (DDAState)c;
    }
}
