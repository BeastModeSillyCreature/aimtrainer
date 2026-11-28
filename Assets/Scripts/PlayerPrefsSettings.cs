using UnityEngine;

// Хранение настроек в PlayerPrefs (раньше это была статическая GameSettings).
public sealed class PlayerPrefsSettings : ISettingsRepository
{
    public const float MinSensitivity = 0.01f;
    public const float MaxSensitivity = 0.50f;
    public const float DefaultSensitivity = 0.10f;
    public const int DefaultDpi = 800;

    private const string SensitivityKey = "settings.sensitivity";
    private const string DpiKey = "settings.dpi";

    private float _sensitivity = -1f; // -1 = ещё не загружено
    private int _dpi = -1;

    public float Sensitivity
    {
        get
        {
            if (_sensitivity < 0f) _sensitivity = PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);
            return _sensitivity;
        }
        set
        {
            // Округляем до 4 знаков: шаг настройки 0.0001
            _sensitivity = Mathf.Clamp(Mathf.Round(value * 10000f) / 10000f, MinSensitivity, MaxSensitivity);
            PlayerPrefs.SetFloat(SensitivityKey, _sensitivity);
        }
    }

    public int Dpi
    {
        get
        {
            if (_dpi < 0) _dpi = PlayerPrefs.GetInt(DpiKey, DefaultDpi);
            return _dpi;
        }
        set
        {
            _dpi = Mathf.Clamp(value, 100, 32000);
            PlayerPrefs.SetInt(DpiKey, _dpi);
        }
    }

    public float CmPer360 => 360f / Sensitivity / Dpi * 2.54f;

    public void Save()
    {
        PlayerPrefs.Save();
    }
}
