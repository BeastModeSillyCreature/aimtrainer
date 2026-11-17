using UnityEngine;

// Создаёт простую арену (пол и дальнюю стену) и надпись-watermark на стене.
// Текст, размер, высоту и цвет надписи можно менять в Inspector (поле Watermark Text многострочное).
public class ArenaBuilder : MonoBehaviour
{
    [Header("Watermark on the wall")]
    [SerializeField, TextArea(1, 4)] private string watermarkText = "AIM TRAINER DDA\nyour name";
    [SerializeField] private float watermarkHeight = 7f;    // высота центра надписи над полом, м
    [SerializeField] private float watermarkSize = 0.25f;   // размер букв
    [SerializeField] private Color watermarkColor = new Color(1f, 1f, 1f, 0.35f); // последний параметр - прозрачность

    private const float WallZ = 30f;
    private const float WallThickness = 1f;

    private TextMesh _watermark;

    private void Start()
    {
        BuildFloor();
        BuildWall();
        BuildWatermark();
    }

    private void OnValidate()
    {
        // Изменения в Inspector во время игры сразу видны на стене
        if (_watermark != null) ApplyWatermarkSettings();
    }

    private void BuildFloor()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(8f, 1f, 8f);
        SolidMaterial.Apply(floor, new Color(0.25f, 0.25f, 0.28f), 0f);
    }

    private void BuildWall()
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "BackWall";
        wall.transform.position = new Vector3(0f, 10f, WallZ);
        wall.transform.localScale = new Vector3(80f, 20f, WallThickness);
        SolidMaterial.Apply(wall, new Color(0.12f, 0.13f, 0.18f), 0f);
    }

    private void BuildWatermark()
    {
        if (string.IsNullOrEmpty(watermarkText)) return;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Встроенный шейдер шрифта рисует текст поверх всего. Подменяем его на свой (с проверкой глубины),
        // чтобы мишени закрывали надпись. Шейдер лежит в Assets/Resources/WorldText.shader.
        Shader depthShader = Resources.Load<Shader>("WorldText");
        if (depthShader != null)
            font.material.shader = depthShader;
        else
            Debug.LogWarning("[Arena] Shader 'WorldText' not found in Assets/Resources: watermark will be drawn over targets.");

        GameObject go = new GameObject("Watermark");
        // Чуть перед лицевой стороной стены, чтобы текст не "тонул" в ней
        go.transform.position = new Vector3(0f, watermarkHeight, WallZ - WallThickness * 0.5f - 0.05f);

        _watermark = go.AddComponent<TextMesh>();
        _watermark.font = font;
        _watermark.fontSize = 100;
        _watermark.anchor = TextAnchor.MiddleCenter;
        _watermark.alignment = TextAlignment.Center;
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material; // без этого текст не отображается

        ApplyWatermarkSettings();
    }

    private void ApplyWatermarkSettings()
    {
        _watermark.text = watermarkText;
        _watermark.characterSize = watermarkSize;
        _watermark.color = watermarkColor;
        Vector3 p = _watermark.transform.position;
        _watermark.transform.position = new Vector3(p.x, watermarkHeight, p.z);
    }
}
