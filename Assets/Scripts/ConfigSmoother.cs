using UnityEngine;

// Плавная смена параметров (FR-7, сценарий 2): линейная интерполяция (Lerp) за заданное время (1.5 с).
public sealed class ConfigSmoother
{
    private readonly float _duration;
    private TargetConfiguration _from;
    private TargetConfiguration _to;
    private TargetConfiguration _current;
    private float _t;

    public TargetConfiguration Current => _current;

    public ConfigSmoother(TargetConfiguration initial, float durationSeconds)
    {
        _duration = Mathf.Max(0.01f, durationSeconds);
        Snap(initial);
    }

    // Мгновенно (без интерполяции), например при старте раунда
    public void Snap(TargetConfiguration config)
    {
        _from = _to = _current = config;
        _t = _duration;
    }

    // Начать плавный переход к новым параметрам
    public void SetTarget(TargetConfiguration target)
    {
        _from = _current;
        _to = target;
        _t = 0f;
    }

    public TargetConfiguration Tick(float deltaTime)
    {
        if (_t >= _duration) return _current;

        _t += deltaTime;
        float k = Mathf.Clamp01(_t / _duration);
        _current = new TargetConfiguration(
            Mathf.Lerp(_from.TargetScale, _to.TargetScale, k),
            Mathf.Lerp(_from.TargetSpeed, _to.TargetSpeed, k),
            Mathf.Lerp(_from.LifetimeSeconds, _to.LifetimeSeconds, k));
        return _current;
    }
}
