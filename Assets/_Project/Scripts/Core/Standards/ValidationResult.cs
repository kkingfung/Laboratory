using System;

namespace Laboratory.Core.Standards
{
    /// <summary>
    /// Code standards validation result
    /// </summary>
    [Serializable]
    public struct ValidationResult
    {
        public bool IsValid;
        public string Message;
        public string Recommendation;
    }
}