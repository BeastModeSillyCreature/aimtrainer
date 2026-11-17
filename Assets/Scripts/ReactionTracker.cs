using System;
using UnityEngine;

// Измеряет "чистое" время реакции: от появления мишени до начала движения прицела К ней.
// Момент начала движения = первый кадр, когда угловая скорость камеры выше порога
// и угол между прицелом и мишенью уменьшается.
// Движения раньше minReactionMs после появления не считаются (это упреждение или инерция предыдущего движения).
[DefaultExecutionOrder(5)] // после PlayerLook (камера уже повёрнута в этом кадре)
public class ReactionTracker : MonoBehaviour
{
    [SerializeField] private TargetSpawner spawner;
    [SerializeField] private Camera cam;
    [SerializeField] private float speedThresholdDegPerSec = 60f; // порог скорости поворота камеры
    [SerializeField] private float minReactionMs = 100f;          // быстрее человек физически не реагирует

    public event Action<float> ReactionMeasured; // мс, один раз на мишень

    private bool _waiting;
    private Vector3 _prevAim;
    private float _prevAngle;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (spawner == null) spawner = FindFirstObjectByType<TargetSpawner>();
    }

    private void Start()
    {
        spawner.TargetSpawned += OnTargetSpawned;
    }

    private void OnDestroy()
    {
        if (spawner != null) spawner.TargetSpawned -= OnTargetSpawned;
    }

    private void OnTargetSpawned()
    {
        _waiting = true;
        _prevAim = cam.transform.forward;
        _prevAngle = AngleToTarget(_prevAim);
    }

    private void Update()
    {
        if (!_waiting) return;

        if (spawner.CurrentTarget == null) // мишень уже уничтожена или исчезла
        {
            _waiting = false;
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return; // пауза

        Vector3 aim = cam.transform.forward;
        float angle = AngleToTarget(aim);
        float speed = Vector3.Angle(_prevAim, aim) / dt;
        float sinceSpawnMs = (Time.time - spawner.LastSpawnTime) * 1000f;

        if (sinceSpawnMs >= minReactionMs && speed > speedThresholdDegPerSec && angle < _prevAngle)
        {
            _waiting = false;
            ReactionMeasured?.Invoke(sinceSpawnMs);
        }

        _prevAim = aim;
        _prevAngle = angle;
    }

    private float AngleToTarget(Vector3 aim)
    {
        Vector3 toTarget = spawner.CurrentTarget.transform.position - cam.transform.position;
        return Vector3.Angle(aim, toTarget);
    }
}
