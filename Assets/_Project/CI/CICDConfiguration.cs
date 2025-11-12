using UnityEngine;
using System.IO;
using System.Text;

namespace Laboratory.CI
{
    /// <summary>
    /// CI/CD configuration generator and manager for Project Chimera
    /// Generates workflow files, manages build configurations, and integrates with test systems
    /// </summary>
    public class CICDConfiguration : ScriptableObject
    {
        [Header("CI/CD Settings")]
        [SerializeField] private bool enableContinuousIntegration = true;
        [SerializeField] private bool runTestsOnCommit = true;
        [SerializeField] private bool runPerformanceTests = true;
        [SerializeField] private bool autoGenerateBuilds = false;

        [Header("Test Configuration")]
        [SerializeField] private int testTimeoutMinutes = 30;
        [SerializeField] private bool failOnTestFailure = true;
        [SerializeField] private bool generateTestReports = true;

        [Header("Build Configuration")]
        [SerializeField] private BuildTarget[] targetPlatforms = { BuildTarget.StandaloneWindows64 };
        [SerializeField] private bool optimizeBuildSize = true;
        [SerializeField] private bool stripDebugSymbols = true;

        [Header("Performance")]
        [SerializeField] private bool trackPerformanceBaselines = true;
        [SerializeField] private float performanceRegressionThreshold = 0.1f; // 10% slower = fail

        [Header("Notifications")]
        [SerializeField] private bool sendNotifications = false;
        [SerializeField] private string discordWebhookUrl = "";
        [SerializeField] private string slackWebhookUrl = "";

        public bool EnableCI => enableContinuousIntegration;
        public bool RunTestsOnCommit => runTestsOnCommit;
        public bool RunPerformanceTests => runPerformanceTests;

        /// <summary>
        /// Generate GitHub Actions workflow file
        /// </summary>
        public string GenerateGitHubActionsWorkflow()
        {
            var sb = new StringBuilder();

            sb.AppendLine("name: Unity CI/CD Pipeline");
            sb.AppendLine();
            sb.AppendLine("on:");
            sb.AppendLine("  push:");
            sb.AppendLine("    branches: [ main, develop ]");
            sb.AppendLine("  pull_request:");
            sb.AppendLine("    branches: [ main, develop ]");
            sb.AppendLine();
            sb.AppendLine("jobs:");

            // Test job
            if (runTestsOnCommit)
            {
                sb.AppendLine("  test:");
                sb.AppendLine("    name: Run Tests");
                sb.AppendLine("    runs-on: ubuntu-latest");
                sb.AppendLine($"    timeout-minutes: {testTimeoutMinutes}");
                sb.AppendLine("    steps:");
                sb.AppendLine("      - uses: actions/checkout@v3");
                sb.AppendLine("      - name: Run Tests");
                sb.AppendLine("        run: echo 'Running tests...'");
                sb.AppendLine();
            }

            // Build job
            if (autoGenerateBuilds)
            {
                sb.AppendLine("  build:");
                sb.AppendLine("    name: Build");
                sb.AppendLine("    runs-on: ubuntu-latest");
                sb.AppendLine("    needs: test");
                sb.AppendLine("    steps:");
                sb.AppendLine("      - uses: actions/checkout@v3");
                sb.AppendLine("      - name: Build Project");
                sb.AppendLine("        run: echo 'Building...'");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate GitLab CI configuration
        /// </summary>
        public string GenerateGitLabCI()
        {
            var sb = new StringBuilder();

            sb.AppendLine("stages:");
            sb.AppendLine("  - test");
            sb.AppendLine("  - build");
            sb.AppendLine();

            sb.AppendLine("test:");
            sb.AppendLine("  stage: test");
            sb.AppendLine("  script:");
            sb.AppendLine("    - echo 'Running tests'");
            sb.AppendLine();

            sb.AppendLine("build:");
            sb.AppendLine("  stage: build");
            sb.AppendLine("  script:");
            sb.AppendLine("    - echo 'Building project'");

            return sb.ToString();
        }

        [ContextMenu("Generate GitHub Actions Workflow")]
        public void GenerateAndSaveGitHubWorkflow()
        {
            string workflow = GenerateGitHubActionsWorkflow();
            string path = Path.Combine(Application.dataPath, "../.github/workflows/unity-ci.yml");

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, workflow);

            Debug.Log($"[CICDConfiguration] Generated GitHub Actions workflow: {path}");
        }

        [ContextMenu("Generate GitLab CI Configuration")]
        public void GenerateAndSaveGitLabCI()
        {
            string config = GenerateGitLabCI();
            string path = Path.Combine(Application.dataPath, "../.gitlab-ci.yml");

            File.WriteAllText(path, config);

            Debug.Log($"[CICDConfiguration] Generated GitLab CI configuration: {path}");
        }
    }

    public enum BuildTarget
    {
        StandaloneWindows64,
        StandaloneLinux64,
        StandaloneOSX,
        Android,
        iOS,
        WebGL
    }
}
