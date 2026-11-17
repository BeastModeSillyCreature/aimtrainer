using UnityEngine;

// Назначает объекту однотонный материал на нашем шейдере (Assets/Resources/SolidColor.shader).
// Нужен, потому что стандартный материал примитивов в билде может стать розовым.
public static class SolidMaterial
{
    private static Shader _shader;

    // shade: 0 = плоский цвет, 1 = лёгкое затенение (для шара)
    public static void Apply(GameObject go, Color color, float shade)
    {
        Renderer renderer = go.GetComponent<Renderer>();

        if (_shader == null) _shader = Resources.Load<Shader>("SolidColor");
        if (_shader == null)
        {
            Debug.LogWarning("[Materials] Shader 'SolidColor' not found in Assets/Resources: using default material");
            renderer.material.color = color;
            return;
        }

        Material material = new Material(_shader);
        material.SetColor("_Color", color);
        material.SetFloat("_Shade", shade);
        renderer.sharedMaterial = material;
    }
}
