// Параметры мишени (ЛР №2–3). readonly struct: значения задаются один раз в конструкторе
// и не меняются, а struct не создаёт мусор для сборщика (требование Zero-Allocation).
public readonly struct TargetConfiguration
{
    public readonly float TargetScale;      // размер мишени
    public readonly float TargetSpeed;      // скорость (юнитов в секунду)
    public readonly float LifetimeSeconds;  // сколько секунд мишень живёт

    public TargetConfiguration(float targetScale, float targetSpeed, float lifetimeSeconds)
    {
        TargetScale = targetScale;
        TargetSpeed = targetSpeed;
        LifetimeSeconds = lifetimeSeconds;
    }
}
