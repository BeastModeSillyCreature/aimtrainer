using System.Threading.Tasks;

// Единая точка доступа к сохранённым данным (паттерн "Фасад", ЛР №5).
// Игра не знает, где хранится профиль (JSON или SQLite), где настройки и откуда берутся границы сложности.
public interface IGameStateStorage
{
    PlayerProfile LoadSession();                       // профиль с историей раундов
    Task SaveSessionAsync(SessionRecord session);      // запись не блокирует главный поток
    ModeBounds[] LoadBounds();                         // границы сложности (перечитываются на паузе)
    ISettingsRepository Settings { get; }              // чувствительность, DPI
}
