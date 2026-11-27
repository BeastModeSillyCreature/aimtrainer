// Статистика раунда для экрана результатов (FR-11).
//  - Time to hit: от появления мишени до попадания (по ТЗ это "время реакции", FR-2)
//  - Reaction: от появления мишени до начала движения прицела к ней (как "reaction time" в аим-тренажёрах)
public sealed class RoundStats
{
    private float _timeToHitSumMs;
    private float _reactionSumMs;
    private int _reactionCount;

    public int Shots { get; private set; }
    public int Hits { get; private set; }
    public int ExpiredTargets { get; private set; }

    // Промахи по типу (FR-3)
    public int Undershoots { get; private set; }
    public int Overshoots { get; private set; }
    public int OtherMisses { get; private set; }

    public float Accuracy => Shots > 0 ? 100f * Hits / Shots : 0f;
    public float AvgTimeToHitMs => Hits > 0 ? _timeToHitSumMs / Hits : 0f;
    public float AvgReactionMs => _reactionCount > 0 ? _reactionSumMs / _reactionCount : 0f;

    public void RegisterShot(float timeToHitMs, bool isHit)
    {
        Shots++;
        if (isHit)
        {
            Hits++;
            _timeToHitSumMs += timeToHitMs;
        }
    }

    public void RegisterReaction(float reactionMs)
    {
        _reactionSumMs += reactionMs;
        _reactionCount++;
    }

    public void RegisterMiss(MissType type)
    {
        switch (type)
        {
            case MissType.Undershoot: Undershoots++; break;
            case MissType.Overshoot: Overshoots++; break;
            case MissType.Other: OtherMisses++; break;
        }
    }

    public void RegisterExpired()
    {
        ExpiredTargets++;
    }

    public void Reset()
    {
        _timeToHitSumMs = 0f;
        _reactionSumMs = 0f;
        _reactionCount = 0;
        Shots = 0;
        Hits = 0;
        ExpiredTargets = 0;
        Undershoots = 0;
        Overshoots = 0;
        OtherMisses = 0;
    }
}
