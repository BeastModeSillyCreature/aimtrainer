using System.IO;
using UnityEngine;

// Единая точка сборки зависимостей (composition root). Выполняется раньше остальных скриптов
// и регистрирует сервисы в ServiceLocator: ввод, настройки, хранилище (фасад).
[DefaultExecutionOrder(-100)]
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private bool logRepositoryCalls = true; // обернуть хранилище декоратором с логированием
    [SerializeField] private string profileFileName = "profile.json";
    [SerializeField] private string databaseFileName = "profile.db";
    [SerializeField] private string boundsFileName = "difficulty_bounds.json";

    private PersistenceFacade _storage;

    private void Awake()
    {
        ServiceLocator.Register<IInputProvider>(new UnityInputAdapter());

        ISettingsRepository settings = new PlayerPrefsSettings();
        ServiceLocator.Register(settings);

        IProfileRepository profiles = CreateProfileRepository();
        if (logRepositoryCalls) profiles = new LoggingProfileRepository(profiles);

        IBoundsSource bounds = new JsonBoundsSource(Path.Combine(Application.streamingAssetsPath, boundsFileName));

        _storage = new PersistenceFacade(profiles, settings, bounds);
        ServiceLocator.Register<IGameStateStorage>(_storage);
    }

    private void OnDestroy()
    {
        _storage?.Dispose(); // закрыть файл БД
    }

    private IProfileRepository CreateProfileRepository()
    {
        string jsonPath = Path.Combine(Application.persistentDataPath, profileFileName);

#if USE_SQLITE
        try
        {
            string dbPath = Path.Combine(Application.persistentDataPath, databaseFileName);
            SqliteProfileRepository sqlite = new SqliteProfileRepository(dbPath);

            // Первый запуск с SQLite: переносим раунды из старого profile.json (сам файл остаётся)
            int imported = sqlite.ImportIfEmpty(new JsonProfileRepository(jsonPath).Load());
            if (imported > 0) Debug.Log($"[Profile] imported {imported} rounds from JSON into SQLite");

            Debug.Log($"[Profile] storage: {dbPath}");
            return sqlite;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Profile] SQLite is unavailable ({e.Message}). Falling back to JSON.");
        }
#endif

        Debug.Log($"[Profile] storage: {jsonPath}");
        return new JsonProfileRepository(jsonPath);
    }
}
