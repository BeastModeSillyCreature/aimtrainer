using System;
using System.Threading.Tasks;

// Фасад над профилем, настройками и конфигом границ (ЛР №5, этап 2).
public sealed class PersistenceFacade : IGameStateStorage, IDisposable
{
    private readonly IProfileRepository _profiles;
    private readonly IBoundsSource _bounds;

    public ISettingsRepository Settings { get; }

    public PersistenceFacade(IProfileRepository profiles, ISettingsRepository settings, IBoundsSource bounds)
    {
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
    }

    public PlayerProfile LoadSession()
    {
        return _profiles.Load();
    }

    public Task SaveSessionAsync(SessionRecord session)
    {
        return _profiles.SaveSessionAsync(session);
    }

    public ModeBounds[] LoadBounds()
    {
        return _bounds.Load();
    }

    public void Dispose()
    {
        (_profiles as IDisposable)?.Dispose();
    }
}
