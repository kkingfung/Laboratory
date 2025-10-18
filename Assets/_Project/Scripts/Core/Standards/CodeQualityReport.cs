using System;
using System.Collections.Generic;

namespace Laboratory.Core.Standards
{
    /// <summary>
    /// Code quality report for analysis
    /// </summary>
    [Serializable]
    public class CodeQualityReport
    {
        public string ClassName;
        public DateTime Timestamp;
        public List<string> Recommendations = new();
        public float QualityScore;
        public List<string> Violations = new();
    }
}