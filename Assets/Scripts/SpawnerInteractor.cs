// Адаптер между модулем DDA и игрой: передаёт параметры в спавнер мишеней.
// Модуль DDA знает только интерфейс IGameplayInteractor, а не Unity-классы.
public sealed class SpawnerInteractor : IGameplayInteractor
{
    private readonly TargetSpawner _spawner;

    public SpawnerInteractor(TargetSpawner spawner)
    {
        _spawner = spawner;
    }

    public void ApplyConfiguration(TargetConfiguration config)
    {
        _spawner.ApplyConfiguration(config);
    }
}
