using UnityEngine;
using UnityEngine.UI;
using System;

public class ColorPicker : MonoBehaviour
{
    [SerializeField] private Slider rSlider;
    [SerializeField] private Slider gSlider;
    [SerializeField] private Slider bSlider;

    public event Action<Color> onColorChanged;

    private void Start()
    {
        rSlider.onValueChanged.AddListener(UpdateColor);
        gSlider.onValueChanged.AddListener(UpdateColor);
        bSlider.onValueChanged.AddListener(UpdateColor);
        UpdateColor(0); // Initialize
    }

    private void UpdateColor(float _)
    {
        Color newColor = new Color(rSlider.value, gSlider.value, bSlider.value);
        onColorChanged?.Invoke(newColor);
    }

    public void SetColor(Color color)
    {
        rSlider.value = color.r;
        gSlider.value = color.g;
        bSlider.value = color.b;
    }
}
