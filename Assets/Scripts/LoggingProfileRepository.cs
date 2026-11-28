using System;
using System.Threading.Tasks;
using UnityEngine;

// Декоратор: оборачивает любой IProfileRepository и пишет в Console каждый вызов с временем выполнения (ЛР №5, этап 2).
public sealed class LoggingProfileRepository : IProfileRepository, IDisposable
{
    private readonly IProfileRepository _inner;

    public LoggingProfileRepository(IProfileRepository inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public PlayerProfile Load()
    {
        System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
        PlayerProfile profile = _inner.Load();
        Debug.Log($"[Repo] Load: {profile.sessions.Count} rounds, {timer.Elapsed.TotalMilliseconds:F1} ms");
        return profile;
    }

    public async Task SaveSessionAsync(SessionRecord session)
    {
        System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
        await _inner.SaveSessionAsync(session);
        Debug.Log($"[Repo] SaveSessionAsync: round saved, {timer.Elapsed.TotalMilliseconds:F1} ms");
    }

    public void Dispose()
    {
        (_inner as IDisposable)?.Dispose();
    }
}
