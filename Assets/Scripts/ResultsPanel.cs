using UnityEngine;
using UnityEngine.UI;

// Экран итогов раунда (FR-11): статистика со сравнением с прошлым раундом, графики сложности и точности,
// разбор промахов по типам (FR-3) и карта слепых зон (FR-4).
// Интерфейс строится кодом, столбики и клетки создаются заранее и только меняют размер и цвет.
public class ResultsPanel : MonoBehaviour
{
    private const int MaxBars = 24;
    private const float ChartWidth = 520f;
    private const float ChartHeight = 130f;
    private const float CellWidth = 58f;
    private const float CellHeight = 44f;

    [SerializeField] private RoundController round;
    [SerializeField] private SessionRecorder recorder;
    [SerializeField] private SessionSavedEvent sessionSavedEvent; // необязательно: если не назначен, берём событие рекордера

    private static readonly Color[] StateColors =
    {
        new Color(0.30f, 0.80f, 0.40f), // Easy
        new Color(0.95f, 0.80f, 0.25f), // Medium
        new Color(0.95f, 0.50f, 0.20f), // Hard
        new Color(0.90f, 0.20f, 0.20f)  // AdaptiveExtreme
    };
    private static readonly Color AccuracyColor = new Color(0.30f, 0.60f, 1f);
    private static readonly Color EmptyCellColor = new Color(0.18f, 0.19f, 0.23f);

    private Font _font;
    private GameObject _panel;
    private Text _stats;
    private Text _missText;
    private Text _zoneText;
    private Text _diffEmpty;
    private Text _accEmpty;
    private Text _heatEmpty;
    private readonly Image[] _diffBars = new Image[MaxBars];
    private readonly Image[] _accBars = new Image[MaxBars];
    private readonly Image[] _cellImages = new Image[BlindZoneMap.Cols * BlindZoneMap.Rows];
    private readonly Text[] _cellTexts = new Text[BlindZoneMap.Cols * BlindZoneMap.Rows];

    private void Awake()
    {
        if (round == null) round = FindFirstObjectByType<RoundController>();
        if (recorder == null) recorder = FindFirstObjectByType<SessionRecorder>();
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUi();
    }

    private void Start()
    {
        if (sessionSavedEvent != null) sessionSavedEvent.Subscribe(Show);
        else recorder.SessionSaved += Show;
        round.CountdownStarted += Hide;
    }

    private void OnDestroy()
    {
        if (sessionSavedEvent != null) sessionSavedEvent.Unsubscribe(Show);
        if (recorder != null) recorder.SessionSaved -= Show;
        if (round != null) round.CountdownStarted -= Hide;
    }

    private void Hide()
    {
        _panel.SetActive(false);
    }

    private void Show(SessionRecord s)
    {
        _stats.text = BuildStats(s);
        _missText.text = BuildMissText(s);
        FillCharts(s);
        FillHeatmap(s);
        _panel.SetActive(true);
    }

    // ---------- Графики сложности и точности ----------

    private void FillCharts(SessionRecord s)
    {
        int count = s.windows.Count;
        int n = Mathf.Min(count, MaxBars);   // показываем последние MaxBars окон
        int skip = count - n;
        int slots = Mathf.Max(n, 8);         // чтобы при малом числе окон столбики не были гигантскими

        for (int i = 0; i < MaxBars; i++)
        {
            if (i < n)
            {
                WindowRecord w = s.windows[skip + i];
                int level = Mathf.Clamp(w.stateIndex, 0, 3);
                SetBar(_diffBars[i], i, slots, (level + 1) / 4f, StateColors[level]);
                SetBar(_accBars[i], i, slots, Mathf.Clamp01(w.hitRate), AccuracyColor);
            }
            else
            {
                _diffBars[i].gameObject.SetActive(false);
                _accBars[i].gameObject.SetActive(false);
            }
        }

        _diffEmpty.gameObject.SetActive(n == 0);
        _accEmpty.gameObject.SetActive(n == 0);
    }

    private static void SetBar(Image bar, int index, int slots, float value01, Color color)
    {
        float slot = ChartWidth / slots;
        RectTransform rt = bar.rectTransform;
        rt.anchoredPosition = new Vector2(index * slot + slot * 0.1f, 0f);
        rt.sizeDelta = new Vector2(slot * 0.8f, Mathf.Max(4f, value01 * ChartHeight));
        bar.color = color;
        bar.gameObject.SetActive(true);
    }

    // ---------- Карта слепых зон (FR-4) ----------

    private void FillHeatmap(SessionRecord s)
    {
        int cells = BlindZoneMap.Cols * BlindZoneMap.Rows;
        int[] attempts = new int[cells]; // выделение памяти только на экране итогов
        int[] misses = new int[cells];

        bool valid = s.heatCols == BlindZoneMap.Cols && s.heatRows == BlindZoneMap.Rows;
        if (valid)
        {
            foreach (HeatCellRecord c in s.heatCells)
            {
                if (c.col < 0 || c.col >= BlindZoneMap.Cols || c.row < 0 || c.row >= BlindZoneMap.Rows) continue;
                int index = c.row * BlindZoneMap.Cols + c.col;
                attempts[index] = c.attempts;
                misses[index] = c.misses;
            }
        }

        int worst = -1;
        float worstRate = 0f;
        bool anyData = false;

        for (int i = 0; i < cells; i++)
        {
            if (attempts[i] == 0)
            {
                _cellImages[i].color = EmptyCellColor;
                _cellTexts[i].text = "";
                continue;
            }

            anyData = true;
            float rate = (float)misses[i] / attempts[i];
            _cellImages[i].color = HeatColor(rate);
            _cellTexts[i].text = $"{misses[i]}/{attempts[i]}";

            // Слабая зона: минимум 3 попытки, максимальная доля промахов (при равенстве, больше промахов)
            if (attempts[i] >= 3 && (rate > worstRate || (Mathf.Approximately(rate, worstRate) && worst >= 0 && misses[i] > misses[worst])))
            {
                worst = i;
                worstRate = rate;
            }
        }

        _heatEmpty.gameObject.SetActive(!anyData);

        if (!anyData)
            _zoneText.text = "";
        else if (worst >= 0 && worstRate >= 0.5f)
            _zoneText.text = $"Weakest zone: <b>{ZoneName(worst)}</b>\n{misses[worst]} of {attempts[worst]} attempts missed";
        else
            _zoneText.text = "No clear blind zone in this round";
    }

    // Зелёный (мало промахов) -> жёлтый -> красный (много промахов)
    private static Color HeatColor(float missRate)
    {
        Color green = new Color(0.25f, 0.70f, 0.38f);
        Color yellow = new Color(0.95f, 0.80f, 0.25f);
        Color red = new Color(0.88f, 0.20f, 0.20f);
        return missRate < 0.5f
            ? Color.Lerp(green, yellow, missRate * 2f)
            : Color.Lerp(yellow, red, (missRate - 0.5f) * 2f);
    }

    // Название зоны словами: "high left", "low right", "center" и т. д. (с точки зрения игрока)
    private static string ZoneName(int index)
    {
        int col = index % BlindZoneMap.Cols;
        int row = index / BlindZoneMap.Cols;

        string horizontal = col < BlindZoneMap.Cols / 3f ? "left" : (col >= BlindZoneMap.Cols * 2f / 3f ? "right" : "center");
        string vertical = row < BlindZoneMap.Rows / 3f ? "low" : (row >= BlindZoneMap.Rows * 2f / 3f ? "high" : "middle");

        if (horizontal == "center" && vertical == "middle") return "center";
        if (horizontal == "center") return vertical + " center";
        if (vertical == "middle") return horizontal;
        return vertical + " " + horizontal;
    }

    // ---------- Тексты ----------

    private string BuildStats(SessionRecord s)
    {
        SessionRecord prev = recorder.PreviousSession;
        bool hasPrev = prev != null;

        string accuracy = $"Accuracy: {s.accuracy:F0}%" + (hasPrev ? Delta(s.accuracy, prev.accuracy, false, "%") : "");
        string timeToHit = $"Time to hit: {s.avgTimeToHitMs:F0} ms" + (hasPrev ? Delta(s.avgTimeToHitMs, prev.avgTimeToHitMs, true, " ms") : "");
        string reaction = $"Reaction: {s.avgReactionMs:F0} ms" + (hasPrev ? Delta(s.avgReactionMs, prev.avgReactionMs, true, " ms") : "");

        return
            $"Mode: {s.mode}\n" +
            $"Duration: {s.durationSeconds:F0} s\n\n" +
            $"Shots: {s.shots}\n" +
            $"Hits: {s.hits}\n" +
            accuracy + "\n" +
            timeToHit + "\n" +
            reaction + "\n" +
            $"Missed targets: {s.expiredTargets}\n\n" +
            $"Best accuracy: {recorder.Profile.GetBestAccuracy():F0}%\n" +
            $"Rounds played: {recorder.Profile.sessions.Count}";
    }

    // Разбор промахов по типам (FR-3) и совет
    private static string BuildMissText(SessionRecord s)
    {
        int total = s.undershoots + s.overshoots + s.otherMisses;
        if (total == 0) return "Misses by type: none";

        string text =
            $"Misses by type ({total}):\n" +
            $"<color=#F2CC40>Undershoot</color> (stopped short): {s.undershoots} ({Percent(s.undershoots, total)}%)\n" +
            $"<color=#F28033>Overshoot</color> (flew past): {s.overshoots} ({Percent(s.overshoots, total)}%)\n" +
            $"Other (no clear direction): {s.otherMisses} ({Percent(s.otherMisses, total)}%)";

        int directional = s.undershoots + s.overshoots;
        if (directional >= 4)
        {
            if (s.undershoots >= s.overshoots * 1.5f)
                text += "\nTip: you often stop short of targets";
            else if (s.overshoots >= s.undershoots * 1.5f)
                text += "\nTip: you often fly past targets";
        }
        return text;
    }

    private static int Percent(int part, int total)
    {
        return Mathf.RoundToInt(100f * part / total);
    }

    // Разница с прошлым раундом: зелёным, если стало лучше, красным, если хуже
    private static string Delta(float current, float previous, bool lowerIsBetter, string unit)
    {
        float d = current - previous;
        if (Mathf.Abs(d) < 0.5f) return "";

        bool good = lowerIsBetter ? d < 0f : d > 0f;
        string color = good ? "#6BE675" : "#FF6B6B";
        return $"  <color={color}>({(d > 0f ? "+" : "")}{d:F0}{unit})</color>";
    }

    // ---------- Построение интерфейса кодом ----------

    private void BuildUi()
    {
        GameObject canvasGo = new GameObject("ResultsCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5; // выше основного HUD, ниже меню настроек

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _panel = CreateRect(canvasGo.transform, "Panel", Vector2.zero, new Vector2(1500f, 760f));
        _panel.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.95f);
        Transform p = _panel.transform;

        CreateLabel(p, "Title", "ROUND COMPLETE", 52, TextAnchor.MiddleCenter, new Vector2(0f, 335f), new Vector2(1000f, 70f));

        // Левая колонка: статистика
        _stats = CreateLabel(p, "Stats", "", 30, TextAnchor.UpperLeft, new Vector2(-520f, -10f), new Vector2(420f, 560f));

        // Средняя колонка: графики и разбор промахов
        CreateLabel(p, "DiffCaption",
            "Difficulty over time:  <color=#4DCC66>Easy</color>  <color=#F2CC40>Medium</color>  <color=#F28033>Hard</color>  <color=#E63333>Extreme</color>",
            22, TextAnchor.MiddleCenter, new Vector2(-30f, 270f), new Vector2(560f, 40f));
        _diffEmpty = CreateChart(p, "DiffChart", new Vector2(-30f, 190f), _diffBars);

        CreateLabel(p, "AccCaption", "Accuracy per window", 22, TextAnchor.MiddleCenter, new Vector2(-30f, 95f), new Vector2(560f, 40f));
        _accEmpty = CreateChart(p, "AccChart", new Vector2(-30f, 0f), _accBars);

        _missText = CreateLabel(p, "MissText", "", 26, TextAnchor.UpperLeft, new Vector2(-30f, -190f), new Vector2(540f, 200f));

        // Правая колонка: карта слепых зон
        CreateLabel(p, "HeatCaption", "Blind zones (misses / attempts)", 22, TextAnchor.MiddleCenter,
            new Vector2(470f, 270f), new Vector2(440f, 40f));
        BuildHeatmap(p, new Vector2(470f, 165f));

        CreateLabel(p, "HeatHint", "Cells show where the target was, as you see it", 20, TextAnchor.MiddleCenter,
            new Vector2(470f, 55f), new Vector2(440f, 30f)).color = new Color(0.6f, 0.6f, 0.65f);
        _zoneText = CreateLabel(p, "ZoneText", "", 26, TextAnchor.UpperCenter, new Vector2(470f, -40f), new Vector2(440f, 120f));

        CreateLabel(p, "Footer", "Click or R - play again", 34, TextAnchor.MiddleCenter, new Vector2(0f, -335f), new Vector2(800f, 50f));

        _panel.SetActive(false);
    }

    private void BuildHeatmap(Transform parent, Vector2 position)
    {
        float width = BlindZoneMap.Cols * CellWidth;
        float height = BlindZoneMap.Rows * CellHeight;

        GameObject map = CreateRect(parent, "Heatmap", position, new Vector2(width, height));
        map.AddComponent<Image>().color = new Color(0.14f, 0.15f, 0.19f, 1f);

        for (int row = 0; row < BlindZoneMap.Rows; row++)
        {
            for (int col = 0; col < BlindZoneMap.Cols; col++)
            {
                int index = row * BlindZoneMap.Cols + col;

                GameObject cell = new GameObject("Cell" + index, typeof(RectTransform));
                cell.transform.SetParent(map.transform, false);
                RectTransform rt = (RectTransform)cell.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero; // отсчёт от левого нижнего угла
                rt.anchoredPosition = new Vector2(col * CellWidth + 1f, row * CellHeight + 1f);
                rt.sizeDelta = new Vector2(CellWidth - 2f, CellHeight - 2f);

                Image img = cell.AddComponent<Image>();
                img.color = EmptyCellColor;
                img.raycastTarget = false;
                _cellImages[index] = img;

                _cellTexts[index] = CreateLabel(cell.transform, "Text", "", 18, TextAnchor.MiddleCenter,
                    Vector2.zero, new Vector2(CellWidth - 2f, CellHeight - 2f));
            }
        }

        _heatEmpty = CreateLabel(map.transform, "Empty", "No data", 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(width, height));
        _heatEmpty.color = new Color(0.6f, 0.6f, 0.65f);
        _heatEmpty.rectTransform.anchorMin = _heatEmpty.rectTransform.anchorMax = _heatEmpty.rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    // Создаёт фон графика и набор столбиков; возвращает текст "нет данных"
    private Text CreateChart(Transform parent, string name, Vector2 position, Image[] bars)
    {
        GameObject chart = CreateRect(parent, name, position, new Vector2(ChartWidth, ChartHeight));
        chart.AddComponent<Image>().color = new Color(0.14f, 0.15f, 0.19f, 1f);

        for (int i = 0; i < bars.Length; i++)
        {
            GameObject barGo = new GameObject("Bar" + i, typeof(RectTransform));
            barGo.transform.SetParent(chart.transform, false);
            RectTransform rt = (RectTransform)barGo.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero; // отсчёт от левого нижнего угла графика
            Image img = barGo.AddComponent<Image>();
            img.raycastTarget = false;
            barGo.SetActive(false);
            bars[i] = img;
        }

        Text empty = CreateLabel(chart.transform, "Empty", "No data (need 10+ shots per window)", 22,
            TextAnchor.MiddleCenter, Vector2.zero, new Vector2(ChartWidth, ChartHeight));
        empty.color = new Color(0.6f, 0.6f, 0.65f);
        return empty;
    }

    private static GameObject CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return go;
    }

    private Text CreateLabel(Transform parent, string name, string text, int size, TextAnchor align,
        Vector2 position, Vector2 rectSize)
    {
        GameObject go = CreateRect(parent, name, position, rectSize);
        Text label = go.AddComponent<Text>();
        label.font = _font;
        label.fontSize = size;
        label.alignment = align;
        label.color = Color.white;
        label.supportRichText = true;
        label.raycastTarget = false;
        label.text = text;
        return label;
    }
}
