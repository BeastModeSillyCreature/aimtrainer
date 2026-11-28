// Данные события "сложность изменилась" (US-3: OnDifficultyChanged).
public readonly struct DifficultyChange
{
    public readonly DDAState State;
    public readonly DDAMode Mode;
    public readonly TargetConfiguration Config;
    public readonly PlayerMetrics Metrics;

    public DifficultyChange(DDAState state, DDAMode mode, TargetConfiguration config, PlayerMetrics metrics)
    {
        State = state;
        Mode = mode;
        Config = config;
        Metrics = metrics;
    }

    public override string ToString()
    {
        return $"{State} ({Mode})\nscale={Config.TargetScale:F2} speed={Config.TargetSpeed:F1} lifetime={Config.LifetimeSeconds:F1}";
    }
}
