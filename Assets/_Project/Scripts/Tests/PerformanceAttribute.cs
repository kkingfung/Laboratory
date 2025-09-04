using System;

namespace Laboratory.Tests
{
    /// <summary>
    /// Attribute to mark performance tests
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PerformanceAttribute : Attribute
    {
        public PerformanceAttribute() { }
    }
}
