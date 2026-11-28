using UnityEngine;

// Абстракция ввода (паттерн "Адаптер", ЛР №5). Игровой код знает только этот интерфейс,
// а не Mouse.current / Keyboard.current, поэтому ввод можно подменить (геймпад, тесты, повтор записи).
public interface IInputProvider
{
    Vector2 LookDelta { get; }      // движение мыши за кадр (пиксели)
    bool FirePressed { get; }       // выстрел нажат в этом кадре
    bool RestartPressed { get; }    // перезапуск раунда
    bool MenuTogglePressed { get; } // открыть или закрыть меню
    int ModeKeyPressed { get; }     // клавиша режима DDA: 1, 2 или 3 (0 = ничего не нажато)
}
