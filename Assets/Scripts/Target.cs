using System;
using UnityEngine;

// Одна мишень в 3D. Положение хранится как углы (yaw/pitch) и расстояние от игрока.
// Мишень "ходит" по сектору перед игроком, отражается от границ и исчезает по таймеру.
// Объект создаётся один раз и переиспользуется (Init + включение/выключение).
public class Target : MonoBehaviour
{
    public event Action<Target> Expired;

    // Положение мишени относительно игрока: углы по горизонтали (вправо +) и вертикали (вверх +)
    public float Yaw => _yaw;
    public float Pitch => _pitch;

    private SpawnArea _area;
    private float _yaw, _pitch, _distance;
    private float _yawVel, _pitchVel;   // угловая скорость, градусов в секунду
    private float _timeLeft;
    private bool _expired;

    public void Init(TargetConfiguration config, SpawnArea area, float yaw, float pitch, float distance)
    {
        _area = area;
        _yaw = yaw;
        _pitch = pitch;
        _distance = distance;
        _timeLeft = config.LifetimeSeconds;
        _expired = false;
        transform.localScale = Vector3.one * config.TargetScale;

        // Скорость в м/с переводим в угловую: чем дальше мишень, тем медленнее она "ползёт" по углу
        float angularSpeed = config.TargetSpeed / distance * Mathf.Rad2Deg;
        float dir = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        _yawVel = Mathf.Cos(dir) * angularSpeed;
        _pitchVel = Mathf.Sin(dir) * angularSpeed;

        UpdatePosition();
    }

    private void Update()
    {
        _yaw += _yawVel * Time.deltaTime;
        if (Mathf.Abs(_yaw) > _area.MaxYaw)
        {
            _yawVel = -_yawVel;
            _yaw = Mathf.Clamp(_yaw, -_area.MaxYaw, _area.MaxYaw);
        }

        _pitch += _pitchVel * Time.deltaTime;
        if (_pitch < _area.MinPitch || _pitch > _area.MaxPitch)
        {
            _pitchVel = -_pitchVel;
            _pitch = Mathf.Clamp(_pitch, _area.MinPitch, _area.MaxPitch);
        }

        UpdatePosition();

        _timeLeft -= Time.deltaTime;
        if (_timeLeft <= 0f && !_expired)
        {
            _expired = true;
            Expired?.Invoke(this);
        }
    }

    private void UpdatePosition()
    {
        // pitch > 0 = выше горизонта, поэтому в Euler знак минус
        Vector3 direction = Quaternion.Euler(-_pitch, _yaw, 0f) * Vector3.forward;
        transform.position = _area.Origin + direction * _distance;
    }
}
