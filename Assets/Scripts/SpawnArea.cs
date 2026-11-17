using System;
using UnityEngine;

// Область, где появляются мишени: сектор перед игроком (углы и расстояние до мишени).
[Serializable]
public class SpawnArea
{
    public Vector3 Origin = new Vector3(0f, 1.7f, 0f); // позиция игрока (камеры)
    public float MaxYaw = 35f;       // насколько влево/вправо от центра, градусы
    public float MinPitch = -3f;     // нижняя граница по вертикали, градусы
    public float MaxPitch = 25f;     // верхняя граница по вертикали, градусы
    public float MinDistance = 10f;  // расстояние до мишени, метры
    public float MaxDistance = 18f;
}
