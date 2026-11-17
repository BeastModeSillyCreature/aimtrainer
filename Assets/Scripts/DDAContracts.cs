// Контракты модуля DDA (ЛР №2–3). Контроллер знает только эти интерфейсы,
// поэтому реализации (эвристика, ONNX, другие стратегии) можно подменять (принципы DIP/OCP).

// Предсказание сложности по признакам. Возвращает "логиты" по состояниям DDAState (Easy..AdaptiveExtreme).
public interface IPredictionService
{
    float[] Predict(float[] features);
}

// Принятие решения: логиты -> состояние FSM, состояние -> параметры мишени.
public interface IDecisionStrategy
{
    DDAState EvaluateState(float[] predictionLogits);
    TargetConfiguration GetConfiguration(DDAState state);
}

// Валидация: зажимает параметры в границы Min/Max (FR-10).
public interface IDifficultyValidator
{
    TargetConfiguration ValidateAndClamp(TargetConfiguration config);
}

// Адаптер к игре: передаёт параметры в спавнер (в ЛР - IXRInteractor / IGameplayInteractor).
public interface IGameplayInteractor
{
    void ApplyConfiguration(TargetConfiguration config);
}

// Главный контроллер ИИ (API для разработчика, US-3).
public interface IAIController
{
    DDAState CurrentState { get; }
    void ResetState(DDAState state);
    TargetConfiguration GetCurrentConfiguration();
    TargetConfiguration UpdateDifficulty(PlayerMetrics metrics);
}
