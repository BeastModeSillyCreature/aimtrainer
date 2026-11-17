using System;
using System.IO;
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
// Если JSON отсутствует или повреждён, применяются безопасные значения по умолчанию и выводится предупреждение.
public sealed class DifficultyBoundsValidator : IDifficultyValidator
{
    private readonly ModeBounds[] _bounds = new ModeBounds[3]; // по индексу DDAMode

    public DDAMode Mode { get; set; } = DDAMode.Aggressive;

    public DifficultyBoundsValidator(string jsonPath)
    {
        for (int i = 0; i < _bounds.Length; i++)
            _bounds[i] = CreateDefaults((DDAMode)i);

        LoadFromFile(jsonPath);
    }

    public TargetConfiguration ValidateAndClamp(TargetConfiguration config)
    {
        ModeBounds b = _bounds[(int)Mode];
        return new TargetConfiguration(
            Mathf.Clamp(config.TargetScale, b.minScale, b.maxScale),
            Mathf.Clamp(config.TargetSpeed, b.minSpeed, b.maxSpeed),
            Mathf.Clamp(config.LifetimeSeconds, b.minLifetime, b.maxLifetime));
    }

    private void LoadFromFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            DifficultyConfigFile file = JsonUtility.FromJson<DifficultyConfigFile>(json);
            if (file == null || file.modes == null)
                throw new Exception("no 'modes' array in JSON");

            foreach (ModeBounds m in file.modes)
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
            Debug.LogWarning($"[DDA] Cannot read bounds config ({path}): {e.Message}. Using built-in defaults.");
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
