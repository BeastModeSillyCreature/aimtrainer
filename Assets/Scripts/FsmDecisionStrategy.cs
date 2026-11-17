using System;

// Основная стратегия: состояние = индекс максимального логита (ЛР №3, этап 5).
public sealed class FsmDecisionStrategy : IDecisionStrategy
{
    public DDAState EvaluateState(float[] predictionLogits)
    {
        if (predictionLogits == null || predictionLogits.Length == 0)
            throw new ArgumentException("Empty prediction logits");

        int maxIndex = 0;
        float maxVal = predictionLogits[0];
        for (int i = 1; i < predictionLogits.Length; i++)
        {
            if (predictionLogits[i] > maxVal)
            {
                maxVal = predictionLogits[i];
                maxIndex = i;
            }
        }

        if (maxIndex > (int)DDAState.AdaptiveExtreme) maxIndex = (int)DDAState.AdaptiveExtreme;
        return (DDAState)maxIndex;
    }

    public TargetConfiguration GetConfiguration(DDAState state)
    {
        return DifficultyPresets.Get(state);
    }
}
