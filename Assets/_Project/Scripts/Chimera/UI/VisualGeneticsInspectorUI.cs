using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Laboratory.Chimera;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Visuals;
using System.Linq;
using System.Text;

namespace Laboratory.Chimera.UI
{
    /// <summary>
    /// Advanced visual genetics inspector UI that shows detailed genetic information
    /// and visual trait breakdowns for creatures. Perfect for debugging and showcasing
    /// the procedural visual system to players.
    /// </summary>
    public class VisualGeneticsInspectorUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject inspectorPanel;
        [SerializeField] private TextMeshProUGUI creatureNameText;
        [SerializeField] private TextMeshProUGUI basicInfoText;
        [SerializeField] private TextMeshProUGUI geneticSummaryText;
        [SerializeField] private TextMeshProUGUI visualTraitsText;
        [SerializeField] private TextMeshProUGUI mutationsText;
        [SerializeField] private Button closeButton;
        
        [Header("Visual Trait Display")]
        [SerializeField] private Transform visualTraitParent;
        [SerializeField] private GameObject traitBarPrefab;
        [SerializeField] private Image colorPreview;
        [SerializeField] private Image secondaryColorPreview;
        [SerializeField] private Image eyeColorPreview;
        
        [Header("Genetic Chart")]
        [SerializeField] private Transform geneticChartParent;
        [SerializeField] private GameObject geneBarPrefab;
        [SerializeField] private ScrollRect geneticScrollView;
        
        [Header("3D Preview")]
        [SerializeField] private Camera previewCamera;
        [SerializeField] private Transform previewTarget;
        [SerializeField] private Light previewLight;
        
        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.I;
        [SerializeField] private bool autoUpdateSelected = true;
        [SerializeField] private float updateInterval = 0.5f;
        
        private CreatureInstanceComponent currentCreature;
        private float lastUpdateTime;
        private bool isVisible = false;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeUI();
            
            if (closeButton != null)
                closeButton.onClick.AddListener(HideInspector);
                
            HideInspector(); // Start hidden
        }
        
        private void Update()
        {
            HandleInput();
            
            if (isVisible && autoUpdateSelected && Time.time - lastUpdateTime > updateInterval)
            {
                UpdateSelectedCreature();
                lastUpdateTime = Time.time;
            }
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                if (isVisible)
                    HideInspector();
                else
                    ShowInspectorForSelected();
            }
            
            // Click to inspect creatures
            if (Input.GetMouseButtonDown(0) && !isVisible)
            {
                TryInspectCreatureAtMouse();
            }
        }
        
        private void TryInspectCreatureAtMouse()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var creature = hit.collider.GetComponent<CreatureInstanceComponent>();
                if (creature != null)
                {
                    InspectCreature(creature);
                }
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Show inspector for a specific creature
        /// </summary>
        public void InspectCreature(CreatureInstanceComponent creature)
        {
            if (creature == null) return;
            
            currentCreature = creature;
            ShowInspector();
            UpdateDisplay();
            
            UnityEngine.Debug.Log($"🔍 Inspecting creature: {creature.name}");
        }
        
        /// <summary>
        /// Show inspector for currently selected creature
        /// </summary>
        public void ShowInspectorForSelected()
        {
#if UNITY_EDITOR
            var selectedObject = UnityEditor.Selection.activeGameObject;
            if (selectedObject != null)
            {
                var creature = selectedObject.GetComponent<CreatureInstanceComponent>();
                if (creature != null)
                {
                    InspectCreature(creature);
                    return;
                }
            }
#endif
            
            // Fallback - find any creature in scene
            var anyCreature = FindFirstObjectByType<CreatureInstanceComponent>();
            if (anyCreature != null)
            {
                InspectCreature(anyCreature);
            }
            else
            {
                UnityEngine.Debug.LogWarning("No creatures found to inspect!");
            }
        }
        
        /// <summary>
        /// Hide the inspector panel
        /// </summary>
        public void HideInspector()
        {
            if (inspectorPanel != null)
                inspectorPanel.SetActive(false);
            isVisible = false;
        }
        
        /// <summary>
        /// Show the inspector panel
        /// </summary>
        public void ShowInspector()
        {
            if (inspectorPanel != null)
                inspectorPanel.SetActive(true);
            isVisible = true;
        }
        
        #endregion
        
        #region UI Updates
        
        private void UpdateSelectedCreature()
        {
#if UNITY_EDITOR
            // Check if selected object changed
            var selectedObject = UnityEditor.Selection.activeGameObject;
            if (selectedObject != null)
            {
                var creature = selectedObject.GetComponent<CreatureInstanceComponent>();
                if (creature != null && creature != currentCreature)
                {
                    InspectCreature(creature);
                }
            }
#endif
        }
        
        private void UpdateDisplay()
        {
            if (currentCreature == null) return;
            
            UpdateBasicInfo();
            UpdateGeneticSummary();
            UpdateVisualTraits();
            UpdateMutationInfo();
            UpdateColorPreviews();
            UpdateGeneticChart();
            Update3DPreview();
        }
        
        private void UpdateBasicInfo()
        {
            if (creatureNameText != null)
                creatureNameText.text = currentCreature.name;
                
            if (basicInfoText != null)
                basicInfoText.text = currentCreature.GetInfoText();
        }
        
        private void UpdateGeneticSummary()
        {
            if (geneticSummaryText == null || currentCreature.CreatureData?.GeneticProfile == null) return;
            
            var genetics = currentCreature.CreatureData.GeneticProfile;
            var summary = new StringBuilder();
            
            summary.AppendLine($"🧬 <b>Genetic Profile</b>");
            summary.AppendLine($"Generation: {genetics.Generation}");
            summary.AppendLine($"Lineage: {genetics.LineageId}");
            summary.AppendLine($"Genetic Purity: {genetics.GetGeneticPurity():P1}");
            summary.AppendLine($"Active Genes: {genetics.Genes.Count(g => g.isActive)}/{genetics.Genes.Count}");
            summary.AppendLine($"Mutations: {genetics.Mutations.Count}");
            summary.AppendLine();
            summary.AppendLine($"<b>Dominant Traits:</b>");
            summary.AppendLine(genetics.GetTraitSummary(5));
            
            geneticSummaryText.text = summary.ToString();
        }
        
        private void UpdateVisualTraits()
        {
            if (visualTraitsText == null || currentCreature.CreatureData?.GeneticProfile == null) return;
            
            var genetics = currentCreature.CreatureData.GeneticProfile;
            var visualInfo = new StringBuilder();
            
            visualInfo.AppendLine($"🎨 <b>Visual Genetics</b>");
            
            // Color traits
            var colorGenes = genetics.Genes.Where(g => g.traitName.Contains("Color")).ToArray();
            if (colorGenes.Length > 0)
            {
                visualInfo.AppendLine($"\n<b>Color Genetics:</b>");
                foreach (var gene in colorGenes)
                {
                    if (gene.value.HasValue)
                    {
                        visualInfo.AppendLine($"• {gene.traitName}: {gene.value.Value:P0} (Dom: {gene.dominance:P0})");
                    }
                }
            }
            
            // Pattern traits
            var patternGenes = genetics.Genes.Where(g => g.traitName.Contains("Pattern")).ToArray();
            if (patternGenes.Length > 0)
            {
                visualInfo.AppendLine($"\n<b>Pattern Genetics:</b>");
                foreach (var gene in patternGenes)
                {
                    if (gene.value.HasValue && gene.isActive)
                    {
                        visualInfo.AppendLine($"• {gene.traitName}: {gene.value.Value:P0} intensity");
                    }
                }
            }
            
            // Magical traits
            var magicalGenes = genetics.Genes.Where(g => g.traitType.GetCategory() == TraitCategory.Special).ToArray();
            if (magicalGenes.Length > 0)
            {
                visualInfo.AppendLine($"\n<b>Magical Traits:</b>");
                foreach (var gene in magicalGenes)
                {
                    if (gene.value.HasValue && gene.isActive && gene.value.Value > 0.3f)
                    {
                        visualInfo.AppendLine($"• {gene.traitName}: {gene.value.Value:P0}");
                    }
                }
            }
            
            // Physical modifications
            var physicalGenes = genetics.Genes.Where(g => 
                g.traitName.Contains("Size") || 
                g.traitName.Contains("Metallic") || 
                g.traitName.Contains("Hardness")).ToArray();
            if (physicalGenes.Length > 0)
            {
                visualInfo.AppendLine($"\n<b>Physical Modifications:</b>");
                foreach (var gene in physicalGenes)
                {
                    if (gene.value.HasValue && gene.isActive)
                    {
                        visualInfo.AppendLine($"• {gene.traitName}: {gene.value.Value:P0}");
                    }
                }
            }
            
            visualTraitsText.text = visualInfo.ToString();
        }
        
        private void UpdateMutationInfo()
        {
            if (mutationsText == null || currentCreature.CreatureData?.GeneticProfile == null) return;
            
            var genetics = currentCreature.CreatureData.GeneticProfile;
            var mutationInfo = new StringBuilder();
            
            mutationInfo.AppendLine($"🧬 <b>Genetic Mutations</b>");
            
            if (genetics.Mutations.Count == 0)
            {
                mutationInfo.AppendLine("No mutations detected.");
            }
            else
            {
                mutationInfo.AppendLine($"Total Mutations: {genetics.Mutations.Count}");
                mutationInfo.AppendLine();
                
                foreach (var mutation in genetics.Mutations)
                {
                    string harmfulIcon = mutation.isHarmful ? "⚠️" : "✨";
                    mutationInfo.AppendLine($"{harmfulIcon} {mutation.mutationType} in {mutation.affectedTrait}");
                    mutationInfo.AppendLine($"   Severity: {mutation.severity:P0}, Generation: {mutation.generation}");
                    mutationInfo.AppendLine();
                }
            }
            
            mutationsText.text = mutationInfo.ToString();
        }
        
        private void UpdateColorPreviews()
        {
            if (currentCreature.CreatureData?.GeneticProfile == null) return;
            
            var genetics = currentCreature.CreatureData.GeneticProfile;
            
            // Primary color
            var primaryColorGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Primary Color" || g.traitName == "Color");
            if (!primaryColorGene.Equals(default(Gene)) && primaryColorGene.value.HasValue && colorPreview != null)
            {
                Color primaryColor = Color.HSVToRGB(primaryColorGene.value.Value, 0.8f, 0.9f);
                colorPreview.color = primaryColor;
            }
            
            // Secondary color
            var secondaryColorGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Secondary Color");
            if (!secondaryColorGene.Equals(default(Gene)) && secondaryColorGene.value.HasValue && secondaryColorPreview != null)
            {
                Color secondaryColor = Color.HSVToRGB(secondaryColorGene.value.Value, 0.6f, 0.8f);
                secondaryColorPreview.color = secondaryColor;
            }
            
            // Eye color
            var eyeColorGene = genetics.Genes.FirstOrDefault(g => g.traitName == "Eye Color");
            if (!eyeColorGene.Equals(default(Gene)) && eyeColorGene.value.HasValue && eyeColorPreview != null)
            {
                Color eyeColor = Color.HSVToRGB(eyeColorGene.value.Value, 0.8f, 0.9f);
                eyeColorPreview.color = eyeColor;
            }
        }
        
        private void UpdateGeneticChart()
        {
            if (geneticChartParent == null || geneBarPrefab == null || currentCreature.CreatureData?.GeneticProfile == null) return;
            
            // Clear existing bars
            for (int i = geneticChartParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(geneticChartParent.GetChild(i).gameObject);
            }
            
            var genetics = currentCreature.CreatureData.GeneticProfile;
            
            // Create bars for each active gene
            foreach (var gene in genetics.Genes.Where(g => g.isActive && g.value.HasValue))
            {
                CreateGeneBar(gene);
            }
        }
        
        private void CreateGeneBar(Gene gene)
        {
            var barObject = Instantiate(geneBarPrefab, geneticChartParent);
            
            // Set up the gene bar (assuming it has specific child components)
            var nameText = barObject.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = gene.traitName;
            
            var fillImage = barObject.GetComponentInChildren<Image>();
            if (fillImage != null)
            {
                fillImage.fillAmount = gene.value.Value;
                
                // Color code by trait type
                fillImage.color = GetTraitTypeColor(gene.traitType);
            }
            
            // Add dominance indicator
            var dominanceText = barObject.GetComponentsInChildren<TextMeshProUGUI>().Skip(1).FirstOrDefault();
            if (dominanceText != null)
                dominanceText.text = gene.value.HasValue ? $"{gene.value.Value:P0} (Dom: {gene.dominance:P0})" : $"N/A (Dom: {gene.dominance:P0})";
        }
        
        private Color GetTraitTypeColor(TraitType traitType)
        {
            return traitType.GetCategory().GetCategoryColor();
            }
        }
        
        private void Update3DPreview()
        {
            if (previewCamera == null || previewTarget == null || currentCreature == null) return;
            
            // Position preview camera to look at the creature
            Vector3 creaturePos = currentCreature.transform.position;
            previewTarget.position = creaturePos;
            
            // Orbit around the creature
            float orbitAngle = Time.time * 20f; // Slow rotation
            Vector3 orbitPos = creaturePos + Quaternion.Euler(0, orbitAngle, 0) * Vector3.back * 5f;
            orbitPos.y = creaturePos.y + 2f; // Slightly above
            
            previewCamera.transform.position = orbitPos;
            previewCamera.transform.LookAt(creaturePos + Vector3.up * 1f); // Look at center of creature
        }
        
        #endregion
        
        #region UI Initialization
        
        private void InitializeUI()
        {
            // Set up any initial UI state
            if (previewLight != null)
            {
                previewLight.color = Color.white;
                previewLight.intensity = 1f;
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Export genetic data as text for sharing/debugging
        /// </summary>
        [ContextMenu("Export Genetic Data")]
        public void ExportGeneticData()
        {
            if (currentCreature?.CreatureData?.GeneticProfile == null)
            {
                UnityEngine.Debug.LogWarning("No creature selected for export");
                return;
            }
            
            var genetics = currentCreature.CreatureData.GeneticProfile;
            var export = new StringBuilder();
            
            export.AppendLine($"=== GENETIC DATA EXPORT ===");
            export.AppendLine($"Creature: {currentCreature.name}");
            export.AppendLine($"Generation: {genetics.Generation}");
            export.AppendLine($"Lineage: {genetics.LineageId}");
            export.AppendLine($"Genetic Purity: {genetics.GetGeneticPurity():P1}");
            export.AppendLine();
            
            export.AppendLine("GENES:");
            foreach (var gene in genetics.Genes)
            {
                export.AppendLine($"  {gene.traitName}: {gene.value?.ToString("F3") ?? "null"} (Dom: {gene.dominance:F3}, Active: {gene.isActive})");
            }
            
            export.AppendLine();
            export.AppendLine("MUTATIONS:");
            foreach (var mutation in genetics.Mutations)
            {
                export.AppendLine($"  {mutation.mutationType} in {mutation.affectedTrait} (Severity: {mutation.severity:F3}, Harmful: {mutation.isHarmful})");
            }
            
            export.AppendLine("=== END EXPORT ===");
            
            UnityEngine.Debug.Log(export.ToString());
            
            // Copy to clipboard if possible
            GUIUtility.systemCopyBuffer = export.ToString();
            UnityEngine.Debug.Log("✅ Genetic data copied to clipboard!");
        }
        
        /// <summary>
        /// Take a screenshot of the creature for documentation
        /// </summary>
        [ContextMenu("Take Preview Screenshot")]
        public void TakePreviewScreenshot()
        {
            if (previewCamera == null)
            {
                UnityEngine.Debug.LogWarning("No preview camera available for screenshot");
                return;
            }
            
            string filename = $"CreaturePreview_{currentCreature.name}_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            ScreenCapture.CaptureScreenshot(filename);
            UnityEngine.Debug.Log($"📸 Screenshot saved: {filename}");
        }
        
        #endregion
        
        #region Gizmos
        
        private void OnDrawGizmosSelected()
        {
            if (currentCreature != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(currentCreature.transform.position, 1f);
                
                // Draw line to inspected creature
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, currentCreature.transform.position);
            }
        }
        
        #endregion
    }
}
