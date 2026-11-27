using UnityEngine;

// Собирает карту слепых зон за раунд: подписывается на выстрелы и на исчезнувшие мишени.
public class BlindZoneTracker : MonoBehaviour
{
    [SerializeField] private ShootingController shooting;
    [SerializeField] private TargetSpawner spawner;
    [SerializeField] private RoundController round;

    public BlindZoneMap Map { get; } = new BlindZoneMap();

    private void Awake()
    {
        if (shooting == null) shooting = FindFirstObjectByType<ShootingController>();
        if (spawner == null) spawner = FindFirstObjectByType<TargetSpawner>();
        if (round == null) round = FindFirstObjectByType<RoundController>();
    }

    private void Start()
    {
        shooting.ShotDetailed += OnShot;
        spawner.TargetExpiredAt += OnTargetExpired;
        round.RoundStarted += OnRoundStarted;
    }

    private void OnDestroy()
    {
        if (shooting != null) shooting.ShotDetailed -= OnShot;
        if (spawner != null) spawner.TargetExpiredAt -= OnTargetExpired;
        if (round != null) round.RoundStarted -= OnRoundStarted;
    }

    private void OnRoundStarted()
    {
        Map.Reset();
    }

    private void OnShot(ShotInfo shot)
    {
        if (round.State != RoundState.Running) return;
        Map.Register(spawner.Area, shot.TargetYaw, shot.TargetPitch, !shot.IsHit);
    }

    // Мишень исчезла сама: игрок не успел, считаем это промахом в той клетке, где она была
    private void OnTargetExpired(float yaw, float pitch)
    {
        if (round.State != RoundState.Running) return;
        Map.Register(spawner.Area, yaw, pitch, true);
    }
}
