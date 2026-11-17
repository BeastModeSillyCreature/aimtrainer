// Параметры мишени для каждого состояния FSM (таблица из ЛР №3, этап 5).
public static class DifficultyPresets
{
    public static TargetConfiguration Get(DDAState state)
    {
        switch (state)
        {
            case DDAState.Easy:            return new TargetConfiguration(1.2f, 2.0f, 3.0f);
            case DDAState.Medium:          return new TargetConfiguration(0.8f, 4.5f, 2.0f);
            case DDAState.Hard:            return new TargetConfiguration(0.5f, 7.0f, 1.2f);
            case DDAState.AdaptiveExtreme: return new TargetConfiguration(0.3f, 10.0f, 0.8f);
            default:                       return new TargetConfiguration(0.8f, 4.5f, 2.0f);
        }
    }
}
