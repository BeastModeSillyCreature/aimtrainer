using UnityEngine;
using UnityEngine.UI;

// Визуальные оповещения:
//  - хитмаркер (четыре белые диагональные чёрточки у прицела) при попадании;
//  - красная виньетка по краям экрана, когда мишень исчезла сама (игрок не успел).
// Весь интерфейс создаётся кодом, текстура виньетки рисуется один раз при запуске.
public class FeedbackHud : MonoBehaviour
{
    [SerializeField] private ShootingController shooting;
    [SerializeField] private TargetSpawner spawner;

    [Header("Hitmarker")]
    [SerializeField] private float hitmarkerDuration = 0.2f;
    [SerializeField] private float hitmarkerOffset = 12f;   // расстояние чёрточек от центра, px
    [SerializeField] private Vector2 hitmarkerLineSize = new Vector2(3f, 12f);

    [Header("Red vignette")]
    [SerializeField] private float vignetteDuration = 0.5f;
    [SerializeField, Range(0f, 1f)] private float vignetteMaxAlpha = 0.6f;

    private CanvasGroup _hitGroup;
    private RectTransform _hitRoot;
    private CanvasGroup _vignetteGroup;
    private float _hitTimer;
    private float _vignetteTimer;

    private void Awake()
    {
        if (shooting == null) shooting = FindFirstObjectByType<ShootingController>();
        if (spawner == null) spawner = FindFirstObjectByType<TargetSpawner>();
        BuildUi();
    }

    private void Start()
    {
        shooting.ShotFired += OnShot;
        spawner.TargetExpired += OnExpired;
    }

    private void OnDestroy()
    {
        if (shooting != null) shooting.ShotFired -= OnShot;
        if (spawner != null) spawner.TargetExpired -= OnExpired;
    }

    private void OnShot(float timeToHitMs, bool isHit)
    {
        if (isHit) _hitTimer = hitmarkerDuration;
    }

    private void OnExpired()
    {
        _vignetteTimer = vignetteDuration;
    }

    private void Update()
    {
        _hitTimer = Mathf.Max(0f, _hitTimer - Time.deltaTime);
        float h = _hitTimer / hitmarkerDuration;          // 1 -> 0
        _hitGroup.alpha = h;
        _hitRoot.localScale = Vector3.one * (1f + 0.4f * h); // чёрточки "схлопываются" к прицелу

        _vignetteTimer = Mathf.Max(0f, _vignetteTimer - Time.deltaTime);
        _vignetteGroup.alpha = vignetteMaxAlpha * (_vignetteTimer / vignetteDuration);
    }

    // ---------- Построение интерфейса кодом ----------

    private void BuildUi()
    {
        GameObject canvasGo = new GameObject("FeedbackCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1; // под текстом основного HUD

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Виньетка (на весь экран)
        GameObject vignetteGo = new GameObject("RedVignette");
        vignetteGo.transform.SetParent(canvasGo.transform, false);
        Image vignette = vignetteGo.AddComponent<Image>();
        vignette.sprite = CreateVignetteSprite();
        vignette.raycastTarget = false;
        RectTransform vrt = vignette.rectTransform;
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = Vector2.zero;
        _vignetteGroup = AddGroup(vignetteGo);

        // Хитмаркер (центр экрана)
        GameObject hitGo = new GameObject("Hitmarker");
        hitGo.transform.SetParent(canvasGo.transform, false);
        _hitRoot = hitGo.AddComponent<RectTransform>();
        _hitRoot.anchorMin = _hitRoot.anchorMax = _hitRoot.pivot = new Vector2(0.5f, 0.5f);
        _hitRoot.anchoredPosition = Vector2.zero;
        _hitGroup = AddGroup(hitGo);

        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sy = -1; sy <= 1; sy += 2)
            {
                // чёрточка лежит вдоль диагонали: на "\" угол -45°, на "/" угол +45°
                float angle = (sx * sy > 0) ? -45f : 45f;
                CreateLine(_hitRoot, new Vector2(sx, sy) * hitmarkerOffset, angle);
            }
        }
    }

    private void CreateLine(RectTransform parent, Vector2 position, float angle)
    {
        GameObject go = new GameObject("Line");
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = hitmarkerLineSize;
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private static CanvasGroup AddGroup(GameObject go)
    {
        CanvasGroup group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }

    // Рисуем текстуру один раз: прозрачный центр, красные края
    private static Sprite CreateVignetteSprite()
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float d = Mathf.Sqrt(dx * dx + dy * dy);               // 0 в центре, 1 у середины края
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.2f, d));
                pixels[y * size + x] = new Color32(235, 20, 20, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }
}
