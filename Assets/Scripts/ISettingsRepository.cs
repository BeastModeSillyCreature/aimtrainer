// Пользовательские настройки (чувствительность, DPI). Откуда они берутся (PlayerPrefs, файл), игре не важно.
public interface ISettingsRepository
{
    float Sensitivity { get; set; }  // градусов поворота камеры на одно деление мыши, шаг 0.0001
    int Dpi { get; set; }            // DPI мыши (нужен для пересчёта в см/360)
    float CmPer360 { get; }
    void Save();
}
