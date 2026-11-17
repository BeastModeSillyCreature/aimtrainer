// Контракт хранилища профиля (паттерн "Репозиторий", ЛР №4).
// Игра знает только этот интерфейс. Сейчас реализация хранит JSON-файл (JsonProfileRepository),
// позже можно написать SqliteProfileRepository с теми же методами и подменить одну строку в SessionRecorder.
public interface IProfileRepository
{
    // Загрузить профиль (если файла нет или он повреждён, возвращает новый пустой профиль)
    PlayerProfile Load();

    // Добавить завершённый раунд и сохранить
    void SaveSession(SessionRecord session);
}
