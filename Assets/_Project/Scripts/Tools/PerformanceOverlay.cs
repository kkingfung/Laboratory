using UnityEngine;
using UnityEngine.UI;

namespace Infrastructure.UI
{
    /// <summary>
    /// Displays an overlay showing FPS, frame time, and memory usage.
    /// </summary>
    public class PerformanceOverlay : MonoBehaviour
    {
        [SerializeField] private Text _fpsText = null!;
        [SerializeField] private Text _frameTimeText = null!;
        [SerializeField] private Text _memoryText = null!;

        private float _deltaTime = 0.0f;

        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

            float fps = 1.0f / _deltaTime;
            _fpsText.text = $"FPS: {fps:F1}";

            float frameMs = _deltaTime * 1000.0f;
            _frameTimeText.text = $"Frame Time: {frameMs:F2} ms";

            long memory = System.GC.GetTotalMemory(false);
            float memoryMB = memory / (1024f * 1024f);
            _memoryText.text = $"Memory: {memoryMB:F2} MB";
        }
    }
}
