using UnityEngine;
using UnityEngine.UI;

/// <summary>A scalable eye icon; the slash indicates that the password is hidden.</summary>
public class LoginPasswordEye : MaskableGraphic
{
    private bool visible;
    public void SetVisible(bool value) { visible = value; SetVerticesDirty(); }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float scale = Mathf.Min(rect.width, rect.height) / 24f;
        for (int i = 0; i < 32; i++)
        {
            float a = i * Mathf.PI * 2 / 32;
            float b = (i + 1) * Mathf.PI * 2 / 32;
            Stroke(mesh, center + new Vector2(Mathf.Cos(a) * 10, Mathf.Sin(a) * 6) * scale,
                center + new Vector2(Mathf.Cos(b) * 10, Mathf.Sin(b) * 6) * scale, 1.8f * scale);
            Stroke(mesh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 2.6f * scale,
                center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 2.6f * scale, 1.8f * scale);
        }
        if (!visible) Stroke(mesh, center + new Vector2(-9, 9) * scale, center + new Vector2(9, -9) * scale, 2 * scale);
    }

    private void Stroke(VertexHelper mesh, Vector2 start, Vector2 end, float width)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x) * width * 0.5f;
        int index = mesh.currentVertCount;
        mesh.AddVert(start - normal, color, Vector2.zero);
        mesh.AddVert(start + normal, color, Vector2.zero);
        mesh.AddVert(end + normal, color, Vector2.zero);
        mesh.AddVert(end - normal, color, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index, index + 2, index + 3);
    }
}
