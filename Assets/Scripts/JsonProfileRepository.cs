using System;
using System.IO;
using UnityEngine;

// Хранение профиля в JSON-файле (Application.persistentDataPath/profile.json).
// Запись идёт через временный файл, чтобы сбой посреди записи не испортил профиль.
public sealed class JsonProfileRepository : IProfileRepository
{
    private const int MaxStoredSessions = 200; // старые раунды удаляются, чтобы файл не рос бесконечно

    private readonly string _path;
    private PlayerProfile _profile;

    public JsonProfileRepository(string path)
    {
        _path = path;
    }

    public PlayerProfile Load()
    {
        if (_profile == null) _profile = ReadFromDisk();
        return _profile;
    }

    public void SaveSession(SessionRecord session)
    {
        PlayerProfile profile = Load();
        profile.sessions.Add(session);
        while (profile.sessions.Count > MaxStoredSessions)
            profile.sessions.RemoveAt(0);

        WriteToDisk(profile);
    }

    private PlayerProfile ReadFromDisk()
    {
        if (!File.Exists(_path)) return new PlayerProfile();

        try
        {
            string json = File.ReadAllText(_path);
            PlayerProfile profile = JsonUtility.FromJson<PlayerProfile>(json);
            if (profile == null || profile.sessions == null)
                throw new Exception("invalid profile structure");
            return profile;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Profile] Cannot read profile ({e.Message}). Starting with an empty one; the broken file is kept as .broken");
            try { File.Copy(_path, _path + ".broken", true); } catch { /* не критично */ }
            return new PlayerProfile();
        }
    }

    private void WriteToDisk(PlayerProfile profile)
    {
        try
        {
            string json = JsonUtility.ToJson(profile, true);
            string tempPath = _path + ".tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(tempPath, _path);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Profile] Cannot save profile: {e.Message}");
        }
    }
}
