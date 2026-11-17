// Контракт сборщика метрик (ЛР №3, этап 2)
public interface IInputProcessor
{
    void RegisterInput(float reactionTimeMs, bool isHit);
    float[] BuildInputFeatures();
}
