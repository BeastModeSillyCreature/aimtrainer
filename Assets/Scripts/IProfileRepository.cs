using System.Threading.Tasks;

// Контракт хранилища профиля (паттерн "Репозиторий", ЛР №4).
// Реализации: JsonProfileRepository, SqliteProfileRepository, декоратор LoggingProfileRepository.
public interface IProfileRepository
{
    // Загрузить профиль (если файла нет или он повреждён, возвращает новый пустой профиль)
    PlayerProfile Load();

    // Добавить завершённый раунд и сохранить. Раунд сразу попадает в профиль в памяти,
    // а запись на диск идёт в фоне и не блокирует главный поток.
    Task SaveSessionAsync(SessionRecord session);
}
