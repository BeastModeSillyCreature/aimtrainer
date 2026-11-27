// Реализация IProfileRepository на SQLite (библиотека sqlite-net).
// Файл компилируется только если в Scripting Define Symbols добавлен USE_SQLITE
// (после установки пакета sqlite-net), поэтому проект собирается и без SQLite.
#if USE_SQLITE
using System;
using System.Collections.Generic;
using SQLite;
using UnityEngine;

// ---------- Таблицы (схема БД) ----------
// Profile (1) ---- (много) Sessions (1) ---- (много) Windows
//                                  (1) ---- (много) HeatCells

[Table("Profile")]
public class ProfileRow
{
    [PrimaryKey] public int Id { get; set; }          // всегда 1: профиль на устройстве один
    public string PlayerId { get; set; }
    public int Version { get; set; }
}

[Table("Sessions")]
public class SessionRow
{
    [PrimaryKey] public string Id { get; set; }
    [Indexed] public string StartedAtUtc { get; set; }
    public string Mode { get; set; }
    public float DurationSeconds { get; set; }
    public int Shots { get; set; }
    public int Hits { get; set; }
    public int ExpiredTargets { get; set; }
    public float Accuracy { get; set; }
    public float AvgTimeToHitMs { get; set; }
    public float AvgReactionMs { get; set; }
    public int Undershoots { get; set; }
    public int Overshoots { get; set; }
    public int OtherMisses { get; set; }
    public int HeatCols { get; set; }
    public int HeatRows { get; set; }
}

[Table("Windows")]
public class WindowRow
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public string SessionId { get; set; }   // внешний ключ -> Sessions.Id
    public float TimeSeconds { get; set; }
    public int Shots { get; set; }
    public float HitRate { get; set; }
    public float AvgTimeToHitMs { get; set; }
    public int StateIndex { get; set; }
    public string State { get; set; }
    public float TargetScale { get; set; }
    public float TargetSpeed { get; set; }
    public float TargetLifetime { get; set; }
    public int Undershoots { get; set; }
    public int Overshoots { get; set; }
}

// Карта слепых зон (FR-4): непустые клетки сетки для каждого раунда
[Table("HeatCells")]
public class HeatCellRow
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public string SessionId { get; set; }   // внешний ключ -> Sessions.Id
    public int Col { get; set; }
    public int Row { get; set; }
    public int Attempts { get; set; }
    public int Misses { get; set; }
}

public sealed class SqliteProfileRepository : IProfileRepository, IDisposable
{
    private const int MaxLoadedSessions = 200; // в памяти держим последние раунды, в БД хранятся все

    private readonly SQLiteConnection _db;
    private PlayerProfile _profile;

    public SqliteProfileRepository(string databasePath)
    {
        _db = new SQLiteConnection(databasePath);
        _db.CreateTable<ProfileRow>();
        _db.CreateTable<SessionRow>();
        _db.CreateTable<WindowRow>();
        _db.CreateTable<HeatCellRow>(); // новые колонки и таблицы в старой базе добавляются автоматически
    }

    public PlayerProfile Load()
    {
        if (_profile == null) _profile = ReadAll();
        return _profile;
    }

    public void SaveSession(SessionRecord session)
    {
        PlayerProfile profile = Load();

        try
        {
            // Раунд и его окна записываются одной транзакцией: либо всё, либо ничего
            _db.RunInTransaction(() => InsertSession(session));
        }
        catch (Exception e)
        {
            Debug.LogError($"[Profile] Cannot save round to SQLite: {e.Message}");
        }

        profile.sessions.Add(session);
        while (profile.sessions.Count > MaxLoadedSessions)
            profile.sessions.RemoveAt(0);
    }

    // Первый запуск с SQLite: переносим раунды из старого JSON-профиля (если БД ещё пуста)
    public int ImportIfEmpty(PlayerProfile legacy)
    {
        if (legacy == null || legacy.sessions.Count == 0) return 0;
        if (_db.Table<SessionRow>().Count() > 0) return 0;

        _db.RunInTransaction(() =>
        {
            _db.InsertOrReplace(new ProfileRow { Id = 1, PlayerId = legacy.playerId, Version = legacy.version });
            foreach (SessionRecord s in legacy.sessions) InsertSession(s);
        });

        _profile = null; // при следующем Load() данные перечитаются из БД
        return legacy.sessions.Count;
    }

    public void Dispose()
    {
        _db.Close();
    }

    // ---------- Чтение ----------

    private PlayerProfile ReadAll()
    {
        ProfileRow row = _db.Find<ProfileRow>(1);
        if (row == null)
        {
            row = new ProfileRow { Id = 1, PlayerId = Guid.NewGuid().ToString("N"), Version = 1 };
            _db.Insert(row);
        }

        PlayerProfile profile = new PlayerProfile { version = row.Version, playerId = row.PlayerId };

        List<SessionRow> sessionRows = _db.Table<SessionRow>()
            .OrderByDescending(s => s.StartedAtUtc)
            .Take(MaxLoadedSessions)
            .ToList();
        sessionRows.Reverse(); // от старых к новым

        foreach (SessionRow sessionRow in sessionRows)
        {
            SessionRecord session = ToRecord(sessionRow);
            string sessionId = sessionRow.Id;

            List<WindowRow> windowRows = _db.Table<WindowRow>()
                .Where(w => w.SessionId == sessionId)
                .OrderBy(w => w.TimeSeconds)
                .ToList();
            foreach (WindowRow windowRow in windowRows)
                session.windows.Add(ToRecord(windowRow));

            List<HeatCellRow> cellRows = _db.Table<HeatCellRow>()
                .Where(c => c.SessionId == sessionId)
                .ToList();
            foreach (HeatCellRow cellRow in cellRows)
            {
                session.heatCells.Add(new HeatCellRecord
                {
                    col = cellRow.Col,
                    row = cellRow.Row,
                    attempts = cellRow.Attempts,
                    misses = cellRow.Misses
                });
            }

            profile.sessions.Add(session);
        }
        return profile;
    }

    // ---------- Запись ----------

    private void InsertSession(SessionRecord s)
    {
        _db.Insert(new SessionRow
        {
            Id = s.id,
            StartedAtUtc = s.startedAtUtc,
            Mode = s.mode,
            DurationSeconds = s.durationSeconds,
            Shots = s.shots,
            Hits = s.hits,
            ExpiredTargets = s.expiredTargets,
            Accuracy = s.accuracy,
            AvgTimeToHitMs = s.avgTimeToHitMs,
            AvgReactionMs = s.avgReactionMs,
            Undershoots = s.undershoots,
            Overshoots = s.overshoots,
            OtherMisses = s.otherMisses,
            HeatCols = s.heatCols,
            HeatRows = s.heatRows
        });

        foreach (HeatCellRecord c in s.heatCells)
        {
            _db.Insert(new HeatCellRow
            {
                SessionId = s.id,
                Col = c.col,
                Row = c.row,
                Attempts = c.attempts,
                Misses = c.misses
            });
        }

        foreach (WindowRecord w in s.windows)
        {
            _db.Insert(new WindowRow
            {
                SessionId = s.id,
                TimeSeconds = w.timeSeconds,
                Shots = w.shots,
                HitRate = w.hitRate,
                AvgTimeToHitMs = w.avgTimeToHitMs,
                StateIndex = w.stateIndex,
                State = w.state,
                TargetScale = w.targetScale,
                TargetSpeed = w.targetSpeed,
                TargetLifetime = w.targetLifetime,
                Undershoots = w.undershoots,
                Overshoots = w.overshoots
            });
        }
    }

    // ---------- Преобразование строк БД <-> модель игры ----------

    private static SessionRecord ToRecord(SessionRow r)
    {
        return new SessionRecord
        {
            id = r.Id,
            startedAtUtc = r.StartedAtUtc,
            mode = r.Mode,
            durationSeconds = r.DurationSeconds,
            shots = r.Shots,
            hits = r.Hits,
            expiredTargets = r.ExpiredTargets,
            accuracy = r.Accuracy,
            avgTimeToHitMs = r.AvgTimeToHitMs,
            avgReactionMs = r.AvgReactionMs,
            undershoots = r.Undershoots,
            overshoots = r.Overshoots,
            otherMisses = r.OtherMisses,
            heatCols = r.HeatCols,
            heatRows = r.HeatRows
        };
    }

    private static WindowRecord ToRecord(WindowRow r)
    {
        return new WindowRecord
        {
            timeSeconds = r.TimeSeconds,
            shots = r.Shots,
            hitRate = r.HitRate,
            avgTimeToHitMs = r.AvgTimeToHitMs,
            stateIndex = r.StateIndex,
            state = r.State,
            targetScale = r.TargetScale,
            targetSpeed = r.TargetSpeed,
            targetLifetime = r.TargetLifetime,
            undershoots = r.Undershoots,
            overshoots = r.Overshoots
        };
    }
}
#endif
