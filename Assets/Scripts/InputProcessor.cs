// Сборщик метрик ввода (ЛР №1–3, сценарий 1: FR-1, FR-2, E-1.2).
// Обычный C#-класс (не MonoBehaviour). Без списков: хранит только суммы и счётчики,
// поэтому не создаёт мусор при каждом выстреле.
public sealed class InputProcessor : IInputProcessor
{
    private const float MinReactionMs = 50f; // E-1.2: реакция быстрее 50 мс = мисклик, отбрасываем

    private readonly float[] _features = new float[2]; // буфер переиспользуется (без new на каждый вызов)
    private float _reactionSumMs;
    private int _hits;
    private int _shots;
    private int _undershoots;
    private int _overshoots;

    public int ShotCount => _shots;

    public void RegisterInput(float reactionTimeMs, bool isHit)
    {
        if (reactionTimeMs < MinReactionMs) return;

        _reactionSumMs += reactionTimeMs;
        _shots++;
        if (isHit) _hits++;
    }

    // Выстрел с подробностями: учитывает попадание/время (как RegisterInput) и тип промаха (FR-3)
    public void RegisterShot(ShotInfo shot)
    {
        int before = _shots;
        RegisterInput(shot.TimeToHitMs, shot.IsHit);

        if (_shots == before || shot.IsHit) return; // выстрел отброшен фильтром или это попадание
        if (shot.MissType == MissType.Undershoot) _undershoots++;
        else if (shot.MissType == MissType.Overshoot) _overshoots++;
    }

    // Вектор признаков для модели: [точность, среднее время реакции]
    public float[] BuildInputFeatures()
    {
        _features[0] = _shots > 0 ? (float)_hits / _shots : 0f;
        _features[1] = _shots > 0 ? _reactionSumMs / _shots : 0f;
        return _features;
    }

    public PlayerMetrics BuildMetrics()
    {
        float hitRate = _shots > 0 ? (float)_hits / _shots : 0f;
        float avgReaction = _shots > 0 ? _reactionSumMs / _shots : 0f;
        return new PlayerMetrics(hitRate, avgReaction, _shots, _undershoots, _overshoots);
    }

    public void Reset()
    {
        _reactionSumMs = 0f;
        _hits = 0;
        _shots = 0;
        _undershoots = 0;
        _overshoots = 0;
    }
}
