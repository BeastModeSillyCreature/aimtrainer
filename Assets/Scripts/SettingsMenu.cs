using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Меню настроек (Esc): чувствительность мыши, DPI для пересчёта в см/360, Resume и Quit.
// Всё создаётся кодом. Значения сохраняются в PlayerPrefs и работают в билде.
public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private PlayerLook look;

    private GameObject _panel;
    private Text _sensLabel;
    private Text _dpiLabel;
    private Slider _sensSlider;
    private Slider _dpiSlider;
    private Font _font;

    private void Awake()
    {
        if (look == null) look = FindFirstObjectByType<PlayerLook>();
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        BuildUi();
    }

    private void Start()
    {
        look.MenuToggled += OnMenuToggled;
    }

    private void OnDestroy()
    {
        if (look != null) look.MenuToggled -= OnMenuToggled;
    }

    private void OnMenuToggled()
    {
        _panel.SetActive(look.InMenu);
        if (look.InMenu) SyncFromSettings();
    }

    private void SyncFromSettings()
    {
        _sensSlider.SetValueWithoutNotify(Mathf.Round(GameSettings.Sensitivity * 1000f));
        _dpiSlider.SetValueWithoutNotify(GameSettings.Dpi / 100f);
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        _sensLabel.text = $"Sensitivity: {GameSettings.Sensitivity:F3} deg/count";
        _dpiLabel.text = $"Mouse DPI: {GameSettings.Dpi}   =   {GameSettings.CmPer360:F1} cm/360";
    }

    private void OnSensChanged(float value)
    {
        GameSettings.Sensitivity = value / 1000f;
        UpdateLabels();
    }

    private void OnDpiChanged(float value)
    {
        GameSettings.Dpi = Mathf.RoundToInt(value) * 100;
        UpdateLabels();
    }

    private void Quit()
    {
        GameSettings.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Построение интерфейса кодом ----------

    // Для кликов по кнопкам и слайдерам нужен EventSystem с модулем нового Input System
    private static void EnsureEventSystem()
    {
        EventSystem es = FindFirstObjectByType<EventSystem>();
        if (es == null) es = new GameObject("EventSystem").AddComponent<EventSystem>();

        if (es.GetComponent<InputSystemUIInputModule>() == null)
        {
            StandaloneInputModule oldModule = es.GetComponent<StandaloneInputModule>();
            if (oldModule != null) Destroy(oldModule);

            InputSystemUIInputModule module = es.gameObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
    }

    private void BuildUi()
    {
        GameObject canvasGo = new GameObject("SettingsCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20; // поверх остального интерфейса

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        _panel = CreateRect(canvasGo.transform, "Panel", Vector2.zero, new Vector2(760f, 640f));
        Image panelBg = _panel.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.09f, 0.12f, 0.96f);

        Transform p = _panel.transform;
        CreateLabel(p, "Title", "SETTINGS", 46, new Vector2(0f, 255f), new Vector2(700f, 70f));

        _sensLabel = CreateLabel(p, "SensLabel", "", 32, new Vector2(0f, 150f), new Vector2(700f, 50f));
        _sensSlider = CreateSlider(p, new Vector2(0f, 100f), 10f, 500f, OnSensChanged);

        _dpiLabel = CreateLabel(p, "DpiLabel", "", 32, new Vector2(0f, 10f), new Vector2(700f, 50f));
        _dpiSlider = CreateSlider(p, new Vector2(0f, -40f), 4f, 32f, OnDpiChanged);

        CreateButton(p, "Resume", new Vector2(0f, -150f), new Vector2(380f, 70f), look.Resume);
        CreateButton(p, "Quit", new Vector2(0f, -240f), new Vector2(380f, 64f), Quit);

        _panel.SetActive(false);
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

    private Text CreateLabel(Transform parent, string name, string text, int size, Vector2 position, Vector2 rectSize)
    {
        GameObject go = CreateRect(parent, name, position, rectSize);
        Text label = go.AddComponent<Text>();
        label.font = _font;
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = text;
        label.raycastTarget = false; // чтобы клики проходили к кнопкам под текстом
        return label;
    }

    private void CreateButton(Transform parent, string label, Vector2 position, Vector2 size,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = CreateRect(parent, label + "Button", position, size);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.45f, 0.9f, 1f);

        Button button = go.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        CreateLabel(go.transform, "Text", label, 32, Vector2.zero, size);
    }

    private Slider CreateSlider(Transform parent, Vector2 position, float min, float max,
        UnityEngine.Events.UnityAction<float> onChanged)
    {
        GameObject root = CreateRect(parent, "Slider", position, new Vector2(600f, 30f));

        // Фон
        GameObject bg = CreateRect(root.transform, "Background", Vector2.zero, Vector2.zero);
        RectTransform bgRt = (RectTransform)bg.transform;
        bgRt.anchorMin = new Vector2(0f, 0.25f);
        bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.25f, 0.27f, 0.32f, 1f);

        // Заливка
        GameObject fillArea = CreateRect(root.transform, "FillArea", Vector2.zero, Vector2.zero);
        RectTransform fillAreaRt = (RectTransform)fillArea.transform;
        fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRt.offsetMin = new Vector2(5f, 0f);
        fillAreaRt.offsetMax = new Vector2(-15f, 0f);

        GameObject fill = CreateRect(fillArea.transform, "Fill", Vector2.zero, new Vector2(10f, 0f));
        RectTransform fillRt = (RectTransform)fill.transform;
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.sizeDelta = new Vector2(10f, 0f);
        fill.AddComponent<Image>().color = new Color(0.2f, 0.45f, 0.9f, 1f);

        // Ползунок
        GameObject handleArea = CreateRect(root.transform, "HandleArea", Vector2.zero, Vector2.zero);
        RectTransform handleAreaRt = (RectTransform)handleArea.transform;
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(10f, 0f);
        handleAreaRt.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = CreateRect(handleArea.transform, "Handle", Vector2.zero, new Vector2(20f, 0f));
        RectTransform handleRt = (RectTransform)handle.transform;
        handleRt.anchorMin = new Vector2(0f, 0f);
        handleRt.anchorMax = new Vector2(0f, 1f);
        handleRt.sizeDelta = new Vector2(20f, 0f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        Slider slider = root.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = true;
        slider.onValueChanged.AddListener(onChanged);
        return slider;
    }
}
