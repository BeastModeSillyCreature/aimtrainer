// Нормализованные метрики игрока за окно агрегации (ЛР №1–3).
// readonly struct: не создаёт мусор для сборщика (Zero-Allocation).
public readonly struct PlayerMetrics
{
    public readonly float HitRate;        // точность, 0..1
    public readonly float AvgReactionMs;  // среднее время реакции, мс
    public readonly int ShotCount;        // сколько выстрелов в окне

    public PlayerMetrics(float hitRate, float avgReactionMs, int shotCount)
    {
        HitRate = hitRate;
        AvgReactionMs = avgReactionMs;
        ShotCount = shotCount;
    }
}
