using UnityEngine;

// Пользовательские настройки, которые сохраняются между запусками (PlayerPrefs).
public static class GameSettings
{
    public const float MinSensitivity = 0.01f;
    public const float MaxSensitivity = 0.50f;
    public const float DefaultSensitivity = 0.10f;
    public const int DefaultDpi = 800;

    private const string SensitivityKey = "settings.sensitivity";
    private const string DpiKey = "settings.dpi";

    private static float _sensitivity = -1f; // -1 = ещё не загружено
    private static int _dpi = -1;

    // Чувствительность: градусов поворота камеры на одно "деление" движения мыши
    public static float Sensitivity
    {
        get
        {
            if (_sensitivity < 0f)
                _sensitivity = PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);
            return _sensitivity;
        }
        set
        {
            // Округляем до 4 знаков: шаг настройки 0.0001
            _sensitivity = Mathf.Clamp(Mathf.Round(value * 10000f) / 10000f, MinSensitivity, MaxSensitivity);
            PlayerPrefs.SetFloat(SensitivityKey, _sensitivity);
        }
    }

    // DPI мыши (нужен только для пересчёта в см/360)
    public static int Dpi
    {
        get
        {
            if (_dpi < 0)
                _dpi = PlayerPrefs.GetInt(DpiKey, DefaultDpi);
            return _dpi;
        }
        set
        {
            _dpi = Mathf.Clamp(value, 100, 32000);
            PlayerPrefs.SetInt(DpiKey, _dpi);
        }
    }

    // Сколько сантиметров надо провести мышью для полного оборота на 360 градусов
    public static float CmPer360 => 360f / Sensitivity / Dpi * 2.54f;

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}
