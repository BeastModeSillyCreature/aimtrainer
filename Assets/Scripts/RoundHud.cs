using UnityEngine;
using UnityEngine.UI;

// Интерфейс: прицел, статистика во время раунда, сообщения по центру (старт, пауза, результаты).
// Строится кодом (Canvas создаётся сам). Текст обновляется только при изменении данных,
// а не каждый кадр, поэтому мусора почти нет (в отличие от OnGUI).
public class RoundHud : MonoBehaviour
{
    [SerializeField] private RoundController round;
    [SerializeField] private DDARunner dda;
    [SerializeField] private DifficultyChangedEvent difficultyChangedEvent; // необязательно: иначе берём событие DDARunner
    [SerializeField] private PlayerLook look;

    private Text _stats;
    private Text _center;
    private int _lastSecond = -1;
    private bool _lastPaused;

    private void Awake()
    {
        if (round == null) round = FindFirstObjectByType<RoundController>();
        if (dda == null) dda = FindFirstObjectByType<DDARunner>();
        if (look == null) look = FindFirstObjectByType<PlayerLook>();
        BuildUi();
    }

    private void Start()
    {
        round.CountdownStarted += Refresh;
        round.RoundStarted += Refresh;
        round.RoundFinished += Refresh;
        round.StatsChanged += Refresh;
        if (difficultyChangedEvent != null) difficultyChangedEvent.Subscribe(OnDifficultyChanged);
        else if (dda != null) dda.Changed += Refresh;
        if (look != null) look.MenuToggled += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (round == null) return;
        round.CountdownStarted -= Refresh;
        round.RoundStarted -= Refresh;
        round.RoundFinished -= Refresh;
        round.StatsChanged -= Refresh;
        if (difficultyChangedEvent != null) difficultyChangedEvent.Unsubscribe(OnDifficultyChanged);
        if (dda != null) dda.Changed -= Refresh;
        if (look != null) look.MenuToggled -= Refresh;
    }

    private void Update()
    {
        if (round.State == RoundState.Countdown)
        {
            int countdown = Mathf.CeilToInt(round.CountdownLeft);
            if (countdown != _lastSecond)
            {
                _lastSecond = countdown;
                Refresh();
            }
            return;
        }

        if (round.State != RoundState.Running) return;

        int second = Mathf.CeilToInt(round.TimeLeft);
        bool paused = round.IsPaused;
        if (second != _lastSecond || paused != _lastPaused)
        {
            _lastSecond = second;
            _lastPaused = paused;
            Refresh();
        }
    }

    private void OnDifficultyChanged(DifficultyChange change)
    {
        Refresh();
    }

    private void Refresh()
    {
        RoundStats s = round.Stats;

        switch (round.State)
        {
            case RoundState.Waiting:
                _stats.text = "";
                _center.text = "Click to start" + (dda != null ? $"\n\nMode: {dda.Mode}  (keys 1 / 2 / 3)" : "") + "\nEsc - settings";
                break;

            case RoundState.Countdown:
                _stats.text = "";
                _center.text = $"Get ready\n<size=140>{Mathf.CeilToInt(round.CountdownLeft)}</size>\n\nR - restart";
                break;

            case RoundState.Running:
                _stats.text =
                    $"Time: {Mathf.CeilToInt(round.TimeLeft)}\n" +
                    $"Shots: {s.Shots}\nHits: {s.Hits}\n" +
                    $"Accuracy: {s.Accuracy:F0}%\n" +
                    $"Time to hit: {s.AvgTimeToHitMs:F0} ms\n" +
                    $"Reaction: {s.AvgReactionMs:F0} ms" +
                    (dda != null ? $"\nDifficulty: {dda.State}\nMode: {dda.Mode}" : "") +
                    "\nR - restart";
                _center.text = "";
                break;

            case RoundState.Finished:
                _stats.text = "";
                _center.text = ""; // результаты показывает ResultsPanel
                break;
        }

        // Пока открыто меню настроек, центральный текст не показываем
        if (look != null && look.InMenu) _center.text = "";
    }

    // ---------- Построение интерфейса кодом ----------

    private void BuildUi()
    {
        GameObject canvasGo = new GameObject("HudCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _stats = CreateText(canvasGo.transform, "Stats", font, 30, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(600f, 400f));
        _center = CreateText(canvasGo.transform, "CenterMessage", font, 44, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(1000f, 600f));

        CreateRect(canvasGo.transform, "CrosshairV", new Vector2(2f, 18f));
        CreateRect(canvasGo.transform, "CrosshairH", new Vector2(18f, 2f));
    }

    private static Text CreateText(Transform parent, string name, Font font, int size, TextAnchor align,
        Vector2 anchor, Vector2 position, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = align;
        text.color = Color.white;
        text.raycastTarget = false;
        go.AddComponent<Shadow>();

        RectTransform rt = text.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = sizeDelta;
        return text;
    }

    private static void CreateRect(Transform parent, string name, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = Color.green;
        img.raycastTarget = false;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
    }
}
