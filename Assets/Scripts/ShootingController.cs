using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Выстрел (ЛКМ): луч строго из центра камеры вперёд (туда, где прицел).
// Стреляет только во время раунда. Сообщает подписчикам:
//   ShotFired    - время до выстрела (мс) и попал ли игрок (простое событие);
//   ShotDetailed - то же + тип промаха (недолёт/перелёт) и положение мишени (для карты слепых зон).
[DefaultExecutionOrder(10)] // после PlayerLook и AimTracker (луч идёт по уже повёрнутой камере), до RoundController
public class ShootingController : MonoBehaviour
{
    [SerializeField] private TargetSpawner spawner;
    [SerializeField] private PlayerLook look;
    [SerializeField] private RoundController round;
    [SerializeField] private AimTracker aimTracker;
    [SerializeField] private Camera cam;

    public event Action<float, bool> ShotFired; // (timeToHitMs, isHit)
    public event Action<ShotInfo> ShotDetailed;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (look == null) look = FindFirstObjectByType<PlayerLook>();
        if (spawner == null) spawner = FindFirstObjectByType<TargetSpawner>();
        if (round == null) round = FindFirstObjectByType<RoundController>();
        if (aimTracker == null) aimTracker = FindFirstObjectByType<AimTracker>();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (round.State != RoundState.Running) return; // вне раунда выстрелы не считаем
        if (!look.CanShoot) return;                    // курсор не захвачен

        Target current = spawner.CurrentTarget;
        if (current == null) return;                   // между мишенями выстрелы не считаем

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        Target target = null;
        bool isHit = Physics.Raycast(ray, out RaycastHit info)
                     && info.collider.TryGetComponent(out target);

        float timeToHitMs = (Time.time - spawner.LastSpawnTime) * 1000f;

        // Вектор от прицела к центру мишени в градусах (вправо, вверх)
        Vector3 local = cam.transform.InverseTransformDirection(current.transform.position - cam.transform.position);
        Vector2 offset = new Vector2(Mathf.Atan2(local.x, local.z), Mathf.Atan2(local.y, local.z)) * Mathf.Rad2Deg;

        MissType missType = MissType.None;
        if (!isHit)
            missType = aimTracker != null ? aimTracker.ClassifyMiss(offset) : MissType.Other;

        float targetYaw = current.Yaw;
        float targetPitch = current.Pitch;

        if (isHit) spawner.HandleHit(target);

        ShotFired?.Invoke(timeToHitMs, isHit);
        ShotDetailed?.Invoke(new ShotInfo(isHit, timeToHitMs, missType, targetYaw, targetPitch, offset));
    }
}
