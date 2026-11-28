using System.IO;
using UnityEngine;

// Читает границы сложности из JSON-файла. Ошибки чтения пробрасываются выше (там подставляются значения по умолчанию, E-3.1).
public sealed class JsonBoundsSource : IBoundsSource
{
    private readonly string _path;

    public JsonBoundsSource(string path)
    {
        _path = path;
    }

    public ModeBounds[] Load()
    {
        string json = File.ReadAllText(_path);
        DifficultyConfigFile file = JsonUtility.FromJson<DifficultyConfigFile>(json);
        if (file == null || file.modes == null)
            throw new InvalidDataException("no 'modes' array in JSON");
        return file.modes;
    }
}
