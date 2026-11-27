using UnityEngine;

// Следит за движением прицела и определяет тип промаха (FR-3).
// Идея: смотрим, в какую сторону прицел двигался перед выстрелом, и сравниваем с направлением на мишень.
//   - мишень впереди по ходу движения -> Undershoot (недовёл);
//   - мишень позади, прицел её проскочил -> Overshoot (проскочил);
//   - прицел давно стоял или промах в основном вбок -> Other.
[DefaultExecutionOrder(5)] // после PlayerLook, до ShootingController
public class AimTracker : MonoBehaviour
{
    [SerializeField] private PlayerLook look;
    [SerializeField] private float minSpeedDegPerSec = 30f; // медленнее считается, что прицел стоит
    [SerializeField] private float memorySeconds = 0.25f;   // сколько помним направление последнего движения
    [SerializeField] private float smoothingSeconds = 0.06f;
    [SerializeField, Range(0f, 1f)] private float lateralThreshold = 0.3f; // доля "вбок", при которой тип не определяем

    private Vector2 _prevPosition;
    private bool _hasPrevious;
    private Vector2 _smoothedVelocity;
    private Vector2 _lastDirection;
    private float _lastMoveTime = -999f;

    private void Awake()
    {
        if (look == null) look = FindFirstObjectByType<PlayerLook>();
    }

    private void Update()
    {
        Vector2 position = new Vector2(look.Yaw, look.PitchUp);
        if (!_hasPrevious)
        {
            _prevPosition = position;
            _hasPrevious = true;
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return; // пауза

        Vector2 velocity = (position - _prevPosition) / dt; // градусов в секунду (вправо, вверх)
        _prevPosition = position;

        float k = 1f - Mathf.Exp(-dt / Mathf.Max(0.001f, smoothingSeconds));
        _smoothedVelocity = Vector2.Lerp(_smoothedVelocity, velocity, k);

        float speed = _smoothedVelocity.magnitude;
        if (speed > minSpeedDegPerSec)
        {
            _lastDirection = _smoothedVelocity / speed;
            _lastMoveTime = Time.time;
        }
    }

    // offset: вектор от прицела к центру мишени (вправо, вверх), градусы
    public MissType ClassifyMiss(Vector2 offset)
    {
        float distance = offset.magnitude;
        if (distance < 0.001f) return MissType.Other;
        if (Time.time - _lastMoveTime > memorySeconds) return MissType.Other; // прицел стоял

        float along = Vector2.Dot(offset, _lastDirection); // проекция на направление движения
        if (Mathf.Abs(along) < lateralThreshold * distance) return MissType.Other; // промах в основном вбок

        return along > 0f ? MissType.Undershoot : MissType.Overshoot;
    }
}
