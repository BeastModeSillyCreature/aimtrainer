using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Выстрел (ЛКМ): луч строго из центра камеры вперёд (туда, где прицел).
// Стреляет только во время раунда. Сообщает подписчикам: время реакции (мс) и попал ли игрок.
[DefaultExecutionOrder(10)] // после PlayerLook (луч идёт по уже повёрнутой камере), до RoundController
public class ShootingController : MonoBehaviour
{
    [SerializeField] private TargetSpawner spawner;
    [SerializeField] private PlayerLook look;
    [SerializeField] private RoundController round;
    [SerializeField] private Camera cam;

    public event Action<float, bool> ShotFired; // (reactionMs, isHit)

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (look == null) look = FindFirstObjectByType<PlayerLook>();
        if (spawner == null) spawner = FindFirstObjectByType<TargetSpawner>();
        if (round == null) round = FindFirstObjectByType<RoundController>();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (round.State != RoundState.Running) return; // вне раунда выстрелы не считаем
        if (!look.CanShoot) return;                    // курсор не захвачен
        if (spawner.CurrentTarget == null) return;     // между мишенями выстрелы не считаем

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        Target target = null;
        bool isHit = Physics.Raycast(ray, out RaycastHit info)
                     && info.collider.TryGetComponent(out target);

        float reactionMs = (Time.time - spawner.LastSpawnTime) * 1000f;

        if (isHit) spawner.HandleHit(target);
        ShotFired?.Invoke(reactionMs, isHit);
    }
}
