// Резервная стратегия (E-2.1 из ТЗ): если ИИ-модель дала сбой, сложность считается
// по простой формуле D = Accuracy * 1.5. Принимает ПРИЗНАКИ (features[0] = точность), а не логиты.
public sealed class LinearFallbackDecisionStrategy : IDecisionStrategy
{
    public DDAState EvaluateState(float[] features)
    {
        float accuracy = (features != null && features.Length > 0) ? features[0] : 0.5f;
        float d = accuracy * 1.5f;

        if (d < 0.6f) return DDAState.Easy;    // точность < 40%
        if (d < 1.05f) return DDAState.Medium; // точность < 70%
        return DDAState.Hard;
    }

    public TargetConfiguration GetConfiguration(DDAState state)
    {
        return DifficultyPresets.Get(state);
    }
}
