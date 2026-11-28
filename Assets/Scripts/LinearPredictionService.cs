// "Предсказание" по резервной формуле D = Accuracy * 1.5 (E-2.1). Возвращает логиты в том же формате,
// что и нейросеть (4 числа), поэтому его можно подставить вместо ONNX без изменения остального кода.
public sealed class LinearPredictionService : IPredictionService
{
    private readonly LinearFallbackDecisionStrategy _rule = new LinearFallbackDecisionStrategy();
    private readonly float[] _logits = new float[4];

    public float[] Predict(float[] features)
    {
        int state = (int)_rule.EvaluateState(features);
        for (int i = 0; i < _logits.Length; i++)
            _logits[i] = i == state ? 1f : 0f;
        return _logits;
    }
}
