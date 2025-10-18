using System;
using UnityEngine;

namespace Laboratory.Core.Standards
{
    /// <summary>
    /// Style guide examples for ChimeraOS coding standards
    /// </summary>
    [Serializable]
    public class StyleGuideExamples
    {
        [Header("✅ CORRECT Examples")]
        [TextArea(3, 5)]
        public string correctClassNaming = @"
public class EquipmentManager : MonoBehaviour
public class SocialFeaturesManager : MonoBehaviour
public class EducationalContentSystem : MonoBehaviour
public interface IResourceManager : IDisposable";

        [TextArea(3, 5)]
        public string correctMethodNaming = @"
public bool EquipItem(Monster monster, string itemId)
public TownResources GetCurrentResources()
public void InitializeSystem(SystemConfig config)
public bool CanAffordBuilding(BuildingConfig config)";

        [Header("❌ INCORRECT Examples")]
        [TextArea(3, 5)]
        public string incorrectClassNaming = @"
public class equipmentMgr : MonoBehaviour
public class socialFeatures : MonoBehaviour
public class education : MonoBehaviour
public interface resourceManager";

        [TextArea(3, 5)]
        public string incorrectMethodNaming = @"
public bool equipItem(Monster monster, string itemId)
public TownResources resources()
public void init(SystemConfig config)
public bool afford(BuildingConfig config)";
    }
}