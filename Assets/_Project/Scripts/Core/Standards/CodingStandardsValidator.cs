using System;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Core.Standards
{
    /// <summary>
    /// Validation utilities for ChimeraOS coding standards
    /// </summary>
    public static class CodingStandardsValidator
    {
        /// <summary>
        /// Validate that a class name follows ChimeraOS naming conventions
        /// </summary>
        /// <param name="className">The class name to validate</param>
        /// <returns>True if the name follows conventions</returns>
        public static bool ValidateClassName(string className)
        {
            if (string.IsNullOrEmpty(className))
                return false;

            // Should be PascalCase
            if (!char.IsUpper(className[0]))
                return false;

            // Should end with appropriate suffix for system classes
            var validSuffixes = new[] { "Manager", "System", "Database", "Config", "Controller" };
            return validSuffixes.Any(suffix => className.EndsWith(suffix));
        }

        /// <summary>
        /// Validate that a method name follows ChimeraOS naming conventions
        /// </summary>
        /// <param name="methodName">The method name to validate</param>
        /// <returns>True if the name follows conventions</returns>
        public static bool ValidateMethodName(string methodName)
        {
            if (string.IsNullOrEmpty(methodName))
                return false;

            // Should be PascalCase
            if (!char.IsUpper(methodName[0]))
                return false;

            // Should start with appropriate verb for actions
            var validVerbs = new[] { "Get", "Set", "Create", "Update", "Delete", "Add", "Remove",
                                   "Initialize", "Process", "Calculate", "Validate", "Check",
                                   "Can", "Is", "Has", "Should" };

            return validVerbs.Any(verb => methodName.StartsWith(verb));
        }

        /// <summary>
        /// Validate that a namespace follows ChimeraOS organization
        /// </summary>
        /// <param name="namespaceName">The namespace to validate</param>
        /// <returns>True if the namespace follows conventions</returns>
        public static bool ValidateNamespace(string namespaceName)
        {
            if (string.IsNullOrEmpty(namespaceName))
                return false;

            // Should start with Laboratory.Core for ChimeraOS systems
            return namespaceName.StartsWith("Laboratory.Core");
        }

        /// <summary>
        /// Generate a code quality report for a given class
        /// </summary>
        /// <param name="className">Name of the class to analyze</param>
        /// <returns>Quality report with recommendations</returns>
        public static CodeQualityReport GenerateQualityReport(string className)
        {
            var report = new CodeQualityReport
            {
                ClassName = className,
                Timestamp = DateTime.UtcNow,
                Recommendations = new List<string>()
            };

            // Validate naming
            if (!ValidateClassName(className))
            {
                report.Recommendations.Add("Class name should follow PascalCase and end with appropriate suffix (Manager, System, etc.)");
            }

            // Check for common patterns
            if (className.Contains("Mgr") || className.Contains("Ctrl"))
            {
                report.Recommendations.Add("Avoid abbreviations - use full words like 'Manager' instead of 'Mgr'");
            }

            return report;
        }
    }
}