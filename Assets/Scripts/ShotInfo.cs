using UnityEngine;

// Подробности одного выстрела: попал или нет, время, тип промаха и где была мишень.
public readonly struct ShotInfo
{
    public readonly bool IsHit;
    public readonly float TimeToHitMs;   // от появления мишени до выстрела
    public readonly MissType MissType;   // для попаданий всегда None
    public readonly float TargetYaw;     // положение мишени: угол по горизонтали (вправо +), градусы
    public readonly float TargetPitch;   // положение мишени: угол по вертикали (вверх +), градусы
    public readonly Vector2 Offset;      // вектор от прицела к центру мишени (вправо, вверх), градусы

    public ShotInfo(bool isHit, float timeToHitMs, MissType missType, float targetYaw, float targetPitch, Vector2 offset)
    {
        IsHit = isHit;
        TimeToHitMs = timeToHitMs;
        MissType = missType;
        TargetYaw = targetYaw;
        TargetPitch = targetPitch;
        Offset = offset;
    }
}
