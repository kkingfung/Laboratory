using UnityEngine;
using UnityEngine.UI;

namespace Laboratory.UI.Utils
{
    /// <summary>
    /// Applies gradient color effects to UI Graphics (Image, Text, etc.) using mesh modification.
    /// Supports both vertical and horizontal gradient directions with customizable colors.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    [DisallowMultipleComponent]
    public class UIGradient : BaseMeshEffect
    {
        #region Enums

        /// <summary>
        /// Direction of the gradient effect.
        /// </summary>
        public enum GradientDirection
        {
            Vertical,
            Horizontal
        }

        #endregion

        #region Fields

        [Header("Gradient Configuration")]
        [Tooltip("Direction of the gradient effect.")]
        [SerializeField] private GradientDirection gradientDirection = GradientDirection.Vertical;

        [Tooltip("First color of the gradient (top for vertical, left for horizontal).")]
        [SerializeField] private Color color1 = Color.white;

        [Tooltip("Second color of the gradient (bottom for vertical, right for horizontal).")]
        [SerializeField] private Color color2 = Color.black;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the gradient direction. Updates the mesh when changed.
        /// </summary>
        public GradientDirection Direction
        {
            get => gradientDirection;
            set
            {
                if (gradientDirection != value)
                {
                    gradientDirection = value;
                    UpdateMesh();
                }
            }
        }

        /// <summary>
        /// Gets or sets the first gradient color. Updates the mesh when changed.
        /// </summary>
        public Color Color1
        {
            get => color1;
            set
            {
                if (color1 != value)
                {
                    color1 = value;
                    UpdateMesh();
                }
            }
        }

        /// <summary>
        /// Gets or sets the second gradient color. Updates the mesh when changed.
        /// </summary>
        public Color Color2
        {
            get => color2;
            set
            {
                if (color2 != value)
                {
                    color2 = value;
                    UpdateMesh();
                }
            }
        }

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize and update the mesh when the component is enabled.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateMesh();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Modifies the mesh to apply gradient colors to vertices.
        /// </summary>
        /// <param name="vh">VertexHelper containing mesh data</param>
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0)
                return;

            ApplyGradientToMesh(vh);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the mesh by marking vertices as dirty.
        /// </summary>
        private void UpdateMesh()
        {
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }

        /// <summary>
        /// Applies gradient colors to the mesh vertices based on their positions.
        /// </summary>
        /// <param name="vh">VertexHelper to modify</param>
        private void ApplyGradientToMesh(VertexHelper vh)
        {
            var (minPos, maxPos) = CalculatePositionBounds(vh);
            float delta = maxPos - minPos;

            if (Mathf.Approximately(delta, 0f))
                delta = 1f; // Avoid divide by zero

            ApplyGradientColors(vh, minPos, delta);
        }

        /// <summary>
        /// Calculates the minimum and maximum positions of vertices along the gradient axis.
        /// </summary>
        /// <param name="vh">VertexHelper containing mesh data</param>
        /// <returns>Tuple containing minimum and maximum positions</returns>
        private (float minPos, float maxPos) CalculatePositionBounds(VertexHelper vh)
        {
            UIVertex vertex = new UIVertex();
            float minPos = float.MaxValue;
            float maxPos = float.MinValue;
            int vertexCount = vh.currentVertCount;

            for (int i = 0; i < vertexCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                float pos = GetPositionAlongGradientAxis(vertex.position);

                if (pos < minPos) minPos = pos;
                if (pos > maxPos) maxPos = pos;
            }

            return (minPos, maxPos);
        }

        /// <summary>
        /// Applies gradient colors to all vertices based on their position along the gradient axis.
        /// </summary>
        /// <param name="vh">VertexHelper to modify</param>
        /// <param name="minPos">Minimum position along gradient axis</param>
        /// <param name="delta">Range of positions along gradient axis</param>
        private void ApplyGradientColors(VertexHelper vh, float minPos, float delta)
        {
            UIVertex vertex = new UIVertex();
            int vertexCount = vh.currentVertCount;

            for (int i = 0; i < vertexCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);

                float pos = GetPositionAlongGradientAxis(vertex.position);
                float t = (pos - minPos) / delta;

                // Lerp color between color1 and color2
                Color lerpedColor = Color.Lerp(color1, color2, t);
                vertex.color = vertex.color * lerpedColor;

                vh.SetUIVertex(vertex, i);
            }
        }

        /// <summary>
        /// Gets the position component along the gradient axis based on direction.
        /// </summary>
        /// <param name="position">Vertex position</param>
        /// <returns>Position value along the gradient axis</returns>
        private float GetPositionAlongGradientAxis(Vector3 position)
        {
            return gradientDirection == GradientDirection.Vertical ? position.y : position.x;
        }

        #endregion
    }
}
