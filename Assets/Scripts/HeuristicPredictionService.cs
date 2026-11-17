using UnityEngine;

// ВРЕМЕННАЯ замена нейросети (ЛР №3: HardcodedHeuristicPredictionService).
// Делает то же, что сделает ONNX-модель: берёт признаки [точность, время до попадания]
// и возвращает 4 логита (по одному на DDAState). Позже её заменит OnnxPredictionService
// с тем же интерфейсом, остальной код менять не придётся.
public sealed class HeuristicPredictionService : IPredictionService
{
    // "Центры" уровней навыка для Easy, Medium, Hard, AdaptiveExtreme
    private static readonly float[] Centers = { 0.2f, 0.45f, 0.7f, 0.9f };

    private readonly float[] _logits = new float[4]; // буфер переиспользуется

    public float[] Predict(float[] features)
    {
        float accuracy = Mathf.Clamp01(features[0]);
        float timeToHitMs = features[1];

        // 400 мс и быстрее = 1.0, 1600 мс и медленнее = 0.0
        float speedScore = 1f - Mathf.Clamp01((timeToHitMs - 400f) / 1200f);

        // Общая оценка навыка 0..1
        float skill = 0.6f * accuracy + 0.4f * speedScore;

        // Чем ближе навык к "центру" уровня, тем выше логит этого уровня
        for (int i = 0; i < _logits.Length; i++)
        {
            float d = skill - Centers[i];
            _logits[i] = -20f * d * d;
        }
        return _logits;
    }
}
