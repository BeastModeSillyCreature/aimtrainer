using System;
using System.Collections.Generic;

// Модель данных профиля игрока (FR-8). Классы подготовлены под реляционную схему (ЛР №4):
//   PlayerProfile (1) --- (много) SessionRecord (1) --- (много) WindowRecord
// В JSON это вложенные списки, а в SQLite позже станут таблицами Sessions и Windows (связь по sessionId).
// Даты хранятся строками ISO 8601, так как JsonUtility не умеет DateTime.

// Одно окно метрик внутри раунда (каждые ~5 с) и ответ DDA на него
[Serializable]
public class WindowRecord
{
    public float timeSeconds;      // время от начала раунда
    public int shots;
    public float hitRate;          // 0..1
    public float avgTimeToHitMs;
    public int stateIndex;         // 0 Easy, 1 Medium, 2 Hard, 3 AdaptiveExtreme
    public string state;
    public float targetScale;
    public float targetSpeed;
    public float targetLifetime;
    public int undershoots;        // промахи "недовёл" в окне (FR-3)
    public int overshoots;         // промахи "проскочил" в окне (FR-3)
}

// Клетка карты слепых зон (FR-4): сколько попыток и промахов по мишеням в этой области
[Serializable]
public class HeatCellRecord
{
    public int col;                // 0 слева направо
    public int row;                // 0 снизу вверх
    public int attempts;
    public int misses;
}

// Один раунд
[Serializable]
public class SessionRecord
{
    public string id;
    public string startedAtUtc;
    public string mode;            // Soft / Aggressive / Fixed
    public float durationSeconds;
    public int shots;
    public int hits;
    public int expiredTargets;
    public float accuracy;         // проценты 0..100
    public float avgTimeToHitMs;
    public float avgReactionMs;
    public int undershoots;        // промахи по типу за раунд (FR-3)
    public int overshoots;
    public int otherMisses;
    public int heatCols;           // размер сетки карты слепых зон (FR-4)
    public int heatRows;
    public List<HeatCellRecord> heatCells = new List<HeatCellRecord>();
    public List<WindowRecord> windows = new List<WindowRecord>();
}

// Профиль игрока целиком
[Serializable]
public class PlayerProfile
{
    public int version = 1;
    public string playerId = Guid.NewGuid().ToString("N");
    public List<SessionRecord> sessions = new List<SessionRecord>();

    // Лучшая точность среди раундов, где было хотя бы 10 выстрелов
    public float GetBestAccuracy()
    {
        float best = 0f;
        foreach (SessionRecord s in sessions)
        {
            if (s.shots >= 10 && s.accuracy > best) best = s.accuracy;
        }
        return best;
    }
}
