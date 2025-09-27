using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.UI
{
    public class OffspringPreviewDisplay : MonoBehaviour
    {
        [Header("Preview Components")]
        [SerializeField] private Image previewPortrait;
        [SerializeField] private TextMeshProUGUI offspringNameText;
        [SerializeField] private TextMeshProUGUI generationText;
        [SerializeField] private Button selectButton;
        
        private GeneticProfile genetics;
        private int previewIndex;
        private bool isSetup = false;
        
        public void SetupPreview(GeneticProfile offspringGenetics, int index)
        {
            genetics = offspringGenetics;
            previewIndex = index;
            
            UpdateDisplay();
            SetupInteractivity();
            
            isSetup = true;
        }
        
        private void UpdateDisplay()
        {
            if (genetics == null) return;
            
            if (offspringNameText != null)
                offspringNameText.text = $"Offspring {previewIndex + 1}";
                
            if (generationText != null)
                generationText.text = $"Gen {genetics.Generation}";
        }
        
        private void SetupInteractivity()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(OnSelectClicked);
        }
        
        private void OnSelectClicked()
        {
            UnityEngine.Debug.Log($"Selected offspring preview {previewIndex}");
        }
        
        public GeneticProfile GetGenetics()
        {
            return genetics;
        }
        
        public int GetPreviewIndex()
        {
            return previewIndex;
        }
        
        public bool IsSetup()
        {
            return isSetup;
        }
    }
}