using UnityEngine;

// Карта слепых зон (FR-4): сетка по сектору перед игроком (по горизонтали и вертикали).
// В каждой клетке считаем попытки (выстрелы по мишени и мишени, которые исчезли сами) и промахи.
// Клетка выбирается по положению МИШЕНИ, потому что прицел в шутере от первого лица всегда в центре экрана.
public sealed class BlindZoneMap
{
    public const int Cols = 7; // по горизонтали
    public const int Rows = 4; // по вертикали

    private readonly int[] _attempts = new int[Cols * Rows];
    private readonly int[] _misses = new int[Cols * Rows];

    public void Reset()
    {
        for (int i = 0; i < _attempts.Length; i++)
        {
            _attempts[i] = 0;
            _misses[i] = 0;
        }
    }

    public void Register(SpawnArea area, float yaw, float pitch, bool isMiss)
    {
        int col = Mathf.Clamp(Mathf.FloorToInt((yaw + area.MaxYaw) / (2f * area.MaxYaw) * Cols), 0, Cols - 1);
        int row = Mathf.Clamp(Mathf.FloorToInt((pitch - area.MinPitch) / (area.MaxPitch - area.MinPitch) * Rows), 0, Rows - 1);

        int index = row * Cols + col;
        _attempts[index]++;
        if (isMiss) _misses[index]++;
    }

    // Записать непустые клетки в раунд (для профиля и экрана итогов)
    public void WriteTo(SessionRecord session)
    {
        session.heatCols = Cols;
        session.heatRows = Rows;
        session.heatCells.Clear();

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                int index = row * Cols + col;
                if (_attempts[index] == 0) continue;

                session.heatCells.Add(new HeatCellRecord
                {
                    col = col,
                    row = row,
                    attempts = _attempts[index],
                    misses = _misses[index]
                });
            }
        }
    }
}
