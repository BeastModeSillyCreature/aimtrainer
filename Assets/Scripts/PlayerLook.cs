using System;
using UnityEngine;

// Обзор мышью от первого лица (как в Aim Lab / KovaaK's).
// Вешается на Main Camera. Курсор скрыт и заблокирован в центре экрана.
// Esc открывает меню настроек (курсор освобождается), кнопка Resume возвращает в игру.
public class PlayerLook : MonoBehaviour
{
    [SerializeField] private float maxPitch = 85f;

    private IInputProvider _input;
    private ISettingsRepository _settings;

    private float _yaw;
    private float _pitch;
    private int _ignoreInputFrame = -1; // в этом кадре ввод игнорируется (например, клик по кнопке Resume)
    private int _lockCheckFrame;        // до этого кадра не проверяем, что курсор потерял блокировку

    // Можно ли сейчас стрелять (курсор захвачен и меню закрыто)
    public bool CanShoot { get; private set; }
    public bool InMenu { get; private set; }

    // Текущее направление прицела (градусы, накопленные): вправо и вверх положительные
    public float Yaw => _yaw;
    public float PitchUp => -_pitch;

    public event Action MenuToggled;

    private void Awake()
    {
        _input = ServiceLocator.Get<IInputProvider>();
        _settings = ServiceLocator.Get<ISettingsRepository>();
    }

    private void Start()
    {
        _yaw = transform.eulerAngles.y;
        LockCursor();
        _lockCheckFrame = Time.frameCount + 5;
    }

    private void Update()
    {
        CanShoot = false;

        bool escPressed = _input.MenuTogglePressed;

        if (!InMenu)
        {
            // Меню открывается по Esc или если курсор потерял блокировку (Esc в редакторе, Alt+Tab)
            bool cursorLost = Time.frameCount > _lockCheckFrame && Cursor.lockState != CursorLockMode.Locked;
            if (escPressed || cursorLost) OpenMenu();
        }
        else if (escPressed)
        {
            Resume();
        }

        if (InMenu) return;
        if (Time.frameCount <= _ignoreInputFrame) return;

        Vector2 delta = _input.LookDelta * _settings.Sensitivity;
        _yaw += delta.x;
        _pitch = Mathf.Clamp(_pitch - delta.y, -maxPitch, maxPitch);
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        CanShoot = true;
    }

    public void OpenMenu()
    {
        InMenu = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        MenuToggled?.Invoke();
    }

    public void Resume()
    {
        InMenu = false;
        _settings.Save();
        LockCursor();
        _ignoreInputFrame = Time.frameCount;      // клик по кнопке не считается выстрелом
        _lockCheckFrame = Time.frameCount + 5;    // даём курсору время заблокироваться
        MenuToggled?.Invoke();
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
