using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Discovery.Data;

namespace Laboratory.Core.Discovery.Data
{
    /// <summary>
    /// Achievement database
    /// </summary>
    [CreateAssetMenu(fileName = "Achievement Database", menuName = "Chimera/Achievement Database")]
    public class AchievementDatabase : ScriptableObject
    {
        public List<Achievement> achievements = new();
    }
}