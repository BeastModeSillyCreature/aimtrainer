using System;
using UnityEngine;

// Границы параметров для одного режима (одна запись в JSON-конфиге геймдизайнера)
[Serializable]
public class ModeBounds
{
    public string mode;
    public float minScale;
    public float maxScale;
    public float minSpeed;
    public float maxSpeed;
    public float minLifetime;
    public float maxLifetime;

    public bool IsValid()
    {
        return minScale > 0f && minSpeed >= 0f && minLifetime > 0f
               && minScale <= maxScale && minSpeed <= maxSpeed && minLifetime <= maxLifetime;
    }
}

// Корень JSON-файла: { "modes": [ {...}, {...} ] }
[Serializable]
public class DifficultyConfigFile
{
    public ModeBounds[] modes;
}

// Валидация и ограничение параметров (FR-9, FR-10, сценарий 3, E-3.1).
// Границы приходят из хранилища (фасада). Если JSON отсутствует или повреждён, применяются безопасные значения
// по умолчанию и выводится предупреждение. Reload() перечитывает конфиг: его вызывают на паузе, чтобы правки
// геймдизайнера применялись без перезапуска игры.
public sealed class DifficultyBoundsValidator : IDifficultyValidator
{
    private readonly IGameStateStorage _storage;
    private readonly ModeBounds[] _bounds = new ModeBounds[3]; // по индексу DDAMode

    public DDAMode Mode { get; set; } = DDAMode.Aggressive;

    public DifficultyBoundsValidator(IGameStateStorage storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        Reload();
    }

    public TargetConfiguration ValidateAndClamp(TargetConfiguration config)
    {
        ModeBounds b = _bounds[(int)Mode];
        return new TargetConfiguration(
            Mathf.Clamp(config.TargetScale, b.minScale, b.maxScale),
            Mathf.Clamp(config.TargetSpeed, b.minSpeed, b.maxSpeed),
            Mathf.Clamp(config.LifetimeSeconds, b.minLifetime, b.maxLifetime));
    }

    public void Reload()
    {
        for (int i = 0; i < _bounds.Length; i++)
            _bounds[i] = CreateDefaults((DDAMode)i);

        try
        {
            ModeBounds[] loaded = _storage.LoadBounds();
            foreach (ModeBounds m in loaded)
            {
                if (!Enum.TryParse(m.mode, out DDAMode parsed) || !m.IsValid())
                {
                    Debug.LogWarning($"[DDA] Skipping invalid bounds entry for mode '{m.mode}'");
                    continue;
                }
                _bounds[(int)parsed] = m;
            }
        }
        catch (Exception e)
        {
            // E-3.1: файл отсутствует или повреждён -> зашитые безопасные константы
            Debug.LogWarning($"[DDA] Cannot read bounds config: {e.Message}. Using built-in defaults.");
        }
    }

    private static ModeBounds CreateDefaults(DDAMode mode)
    {
        return new ModeBounds
        {
            mode = mode.ToString(),
            minScale = 0.5f, maxScale = 1.5f,
            minSpeed = 1.0f, maxSpeed = 8.0f,
            minLifetime = 0.8f, maxLifetime = 4.0f
        };
    }
}
