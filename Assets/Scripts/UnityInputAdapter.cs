using UnityEngine;
using UnityEngine.InputSystem;

// Адаптер: приводит Unity Input System к интерфейсу IInputProvider.
public sealed class UnityInputAdapter : IInputProvider
{
    public Vector2 LookDelta => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

    public bool FirePressed => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

    public bool RestartPressed => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;

    public bool MenuTogglePressed => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

    public int ModeKeyPressed
    {
        get
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return 0;
            if (keyboard.digit1Key.wasPressedThisFrame) return 1;
            if (keyboard.digit2Key.wasPressedThisFrame) return 2;
            if (keyboard.digit3Key.wasPressedThisFrame) return 3;
            return 0;
        }
    }
}
