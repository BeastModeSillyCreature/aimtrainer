using System;
using UnityEngine;

// Спавнер мишеней. Держит текущие параметры сложности (Config).
// Мишень создаётся один раз в Awake и дальше только прячется/показывается (пул из одного объекта),
// поэтому во время игры нет Instantiate/Destroy и мусора для сборщика.
// Раунд включает и выключает спавн через StartSpawning / StopSpawning.
public class TargetSpawner : MonoBehaviour
{
    [SerializeField] private SpawnArea area = new SpawnArea();
    [SerializeField] private float respawnDelay = 0.3f;

    // Событие: мишень исчезла сама (игрок не успел)
    public event Action TargetExpired;

    // Событие: появилась новая мишень
    public event Action TargetSpawned;

    public TargetConfiguration Config { get; private set; } = new TargetConfiguration(0.8f, 4.5f, 2.0f);
    public Target CurrentTarget { get; private set; }
    public float LastSpawnTime { get; private set; }

    private Target _target;
    private bool _active;
    private float _respawnAt;

    private void Awake()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Target";
        SolidMaterial.Apply(go, new Color(1f, 0.3f, 0.2f), 1f);

        _target = go.AddComponent<Target>();
        _target.Expired += HandleExpired;
        go.SetActive(false);
    }

    private void Update()
    {
        if (_active && CurrentTarget == null && Time.time >= _respawnAt)
            SpawnTarget();
    }

    // Точка входа для DDA: применить новые параметры (действуют со следующей мишени)
    public void ApplyConfiguration(TargetConfiguration config)
    {
        Config = config;
    }

    public void StartSpawning()
    {
        _active = true;
        _respawnAt = Time.time;
    }

    public void StopSpawning()
    {
        _active = false;
        HideTarget();
    }

    public void HandleHit(Target target)
    {
        HideTarget();
        _respawnAt = Time.time + respawnDelay;
    }

    private void SpawnTarget()
    {
        _target.Init(
            Config,
            area,
            UnityEngine.Random.Range(-area.MaxYaw, area.MaxYaw),
            UnityEngine.Random.Range(area.MinPitch, area.MaxPitch),
            UnityEngine.Random.Range(area.MinDistance, area.MaxDistance));
        _target.gameObject.SetActive(true);

        CurrentTarget = _target;
        LastSpawnTime = Time.time;
        TargetSpawned?.Invoke();
    }

    private void HandleExpired(Target target)
    {
        HideTarget();
        _respawnAt = Time.time + respawnDelay;
        TargetExpired?.Invoke();
    }

    private void HideTarget()
    {
        _target.gameObject.SetActive(false);
        CurrentTarget = null;
    }
}
