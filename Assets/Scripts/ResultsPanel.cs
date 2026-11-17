using UnityEngine;
using UnityEngine.UI;

// Экран итогов раунда (FR-11): статистика со сравнением с прошлым раундом
// и два графика: динамика сложности и точность по окнам метрик.
// Интерфейс строится кодом, столбики графиков создаются заранее и только меняют размер.
public class ResultsPanel : MonoBehaviour
{
    private const int MaxBars = 24;
    private const float ChartWidth = 520f;
    private const float ChartHeight = 150f;

    [SerializeField] private RoundController round;
    [SerializeField] private SessionRecorder recorder;

    private static readonly Color[] StateColors =
    {
        new Color(0.30f, 0.80f, 0.40f), // Easy
        new Color(0.95f, 0.80f, 0.25f), // Medium
        new Color(0.95f, 0.50f, 0.20f), // Hard
        new Color(0.90f, 0.20f, 0.20f)  // AdaptiveExtreme
    };
    private static readonly Color AccuracyColor = new Color(0.30f, 0.60f, 1f);

    private Font _font;
    private GameObject _panel;
    private Text _stats;
    private Text _diffEmpty;
    private Text _accEmpty;
    private readonly Image[] _diffBars = new Image[MaxBars];
    private readonly Image[] _accBars = new Image[MaxBars];

    private void Awake()
    {
        if (round == null) round = FindFirstObjectByType<RoundController>();
        if (recorder == null) recorder = FindFirstObjectByType<SessionRecorder>();
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUi();
    }

    private void Start()
    {
        recorder.SessionSaved += Show;
        round.RoundStarted += Hide;
    }

    private void OnDestroy()
    {
        if (recorder != null) recorder.SessionSaved -= Show;
        if (round != null) round.RoundStarted -= Hide;
    }

    private void Hide()
    {
        _panel.SetActive(false);
    }

    private void Show(SessionRecord s)
    {
        _stats.text = BuildStats(s);

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
        _panel.SetActive(true);
    }

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

    // Разница с прошлым раундом: зелёным, если стало лучше, красным, если хуже
    private static string Delta(float current, float previous, bool lowerIsBetter, string unit)
    {
        float d = current - previous;
        if (Mathf.Abs(d) < 0.5f) return "";

        bool good = lowerIsBetter ? d < 0f : d > 0f;
        string color = good ? "#6BE675" : "#FF6B6B";
        return $"  <color={color}>({(d > 0f ? "+" : "")}{d:F0}{unit})</color>";
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

        _panel = CreateRect(canvasGo.transform, "Panel", Vector2.zero, new Vector2(1100f, 720f));
        _panel.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.95f);
        Transform p = _panel.transform;

        CreateLabel(p, "Title", "ROUND COMPLETE", 52, TextAnchor.MiddleCenter, new Vector2(0f, 310f), new Vector2(1000f, 70f));
        _stats = CreateLabel(p, "Stats", "", 30, TextAnchor.UpperLeft, new Vector2(-300f, -10f), new Vector2(440f, 520f));

        CreateLabel(p, "DiffCaption",
            "Difficulty over time:  <color=#4DCC66>Easy</color>  <color=#F2CC40>Medium</color>  <color=#F28033>Hard</color>  <color=#E63333>Extreme</color>",
            22, TextAnchor.MiddleCenter, new Vector2(250f, 268f), new Vector2(560f, 40f));
        _diffEmpty = CreateChart(p, "DiffChart", new Vector2(250f, 170f), _diffBars);

        CreateLabel(p, "AccCaption", "Accuracy per window", 22, TextAnchor.MiddleCenter, new Vector2(250f, 52f), new Vector2(560f, 40f));
        _accEmpty = CreateChart(p, "AccChart", new Vector2(250f, -45f), _accBars);

        CreateLabel(p, "Footer", "Click to play again", 34, TextAnchor.MiddleCenter, new Vector2(0f, -325f), new Vector2(800f, 50f));

        _panel.SetActive(false);
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
