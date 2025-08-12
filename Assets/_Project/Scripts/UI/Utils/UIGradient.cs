using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
[DisallowMultipleComponent]
public class UIGradient : BaseMeshEffect
{
    public enum GradientDirection
    {
        Vertical,
        Horizontal
    }

    [SerializeField] private GradientDirection gradientDirection = GradientDirection.Vertical;

    [SerializeField] private Color color1 = Color.white;
    [SerializeField] private Color color2 = Color.black;

    public GradientDirection Direction
    {
        get => gradientDirection;
        set
        {
            gradientDirection = value;
            if (graphic != null)
                graphic.SetVerticesDirty();
        }
    }

    public Color Color1
    {
        get => color1;
        set
        {
            color1 = value;
            if (graphic != null)
                graphic.SetVerticesDirty();
        }
    }

    public Color Color2
    {
        get => color2;
        set
        {
            color2 = value;
            if (graphic != null)
                graphic.SetVerticesDirty();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
            return;

        int count = vh.currentVertCount;
        if (count == 0)
            return;

        // Get min and max positions of vertices
        UIVertex vertex = new UIVertex();
        float minPos = float.MaxValue;
        float maxPos = float.MinValue;

        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float pos = (gradientDirection == GradientDirection.Vertical) ? vertex.position.y : vertex.position.x;

            if (pos < minPos) minPos = pos;
            if (pos > maxPos) maxPos = pos;
        }

        float delta = maxPos - minPos;
        if (Mathf.Approximately(delta, 0f))
            delta = 1f; // avoid divide by zero

        // Apply gradient color to vertices
        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);

            float pos = (gradientDirection == GradientDirection.Vertical) ? vertex.position.y : vertex.position.x;
            float t = (pos - minPos) / delta;

            // Lerp color between color1 and color2
            Color lerpedColor = Color.Lerp(color1, color2, t);

            vertex.color = vertex.color * lerpedColor;

            vh.SetUIVertex(vertex, i);
        }
    }
}
