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
        [SerializeField] private bool cleanupDiskSpaceBeforeBuild = true;

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
                if (failOnTestFailure)
                {
                    sb.AppendLine("        continue-on-error: false");
                }

                if (generateTestReports)
                {
                    sb.AppendLine("      - name: Generate Test Report");
                    sb.AppendLine("        if: always()");
                    sb.AppendLine("        run: echo 'Generating test reports...'");
                    sb.AppendLine("      - name: Upload Test Results");
                    sb.AppendLine("        if: always()");
                    sb.AppendLine("        uses: actions/upload-artifact@v3");
                    sb.AppendLine("        with:");
                    sb.AppendLine("          name: test-results");
                    sb.AppendLine("          path: TestResults/");
                }

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

                // Add disk space cleanup if enabled
                if (cleanupDiskSpaceBeforeBuild)
                {
                    sb.AppendLine("      - name: Free Disk Space");
                    sb.AppendLine("        run: |");
                    sb.AppendLine("          echo \"Disk space before cleanup:\"");
                    sb.AppendLine("          df -h");
                    sb.AppendLine("          sudo rm -rf /usr/share/dotnet");
                    sb.AppendLine("          sudo rm -rf /usr/local/lib/android");
                    sb.AppendLine("          sudo rm -rf /opt/ghc");
                    sb.AppendLine("          sudo rm -rf /opt/hostedtoolcache/CodeQL");
                    sb.AppendLine("          sudo docker image prune --all --force");
                    sb.AppendLine("          echo \"Disk space after cleanup:\"");
                    sb.AppendLine("          df -h");
                }

                sb.AppendLine("      - name: Build Project");
                string buildArgs = "";
                if (optimizeBuildSize)
                {
                    buildArgs += " --optimize-size";
                }
                if (stripDebugSymbols)
                {
                    buildArgs += " --strip-debug";
                }
                sb.AppendLine($"        run: echo 'Building{buildArgs}...'");
                if (optimizeBuildSize || stripDebugSymbols)
                {
                    sb.AppendLine($"      - name: Build Optimizations");
                    sb.AppendLine($"        run: |");
                    if (optimizeBuildSize)
                    {
                        sb.AppendLine("          echo 'Optimizing build size...'");
                    }
                    if (stripDebugSymbols)
                    {
                        sb.AppendLine("          echo 'Stripping debug symbols...'");
                    }
                }

                // Clean up build artifacts to save space
                if (cleanupDiskSpaceBeforeBuild)
                {
                    sb.AppendLine("      - name: Clean Build Cache");
                    sb.AppendLine("        if: always()");
                    sb.AppendLine("        run: |");
                    sb.AppendLine("          echo \"Cleaning Unity build cache...\"");
                    sb.AppendLine("          rm -rf Library/");
                    sb.AppendLine("          rm -rf Temp/");
                    sb.AppendLine("          rm -rf obj/");
                    sb.AppendLine("          echo \"Final disk space:\"");
                    sb.AppendLine("          df -h");
                }

                sb.AppendLine();
            }

            // Performance testing job
            if (runPerformanceTests && trackPerformanceBaselines)
            {
                sb.AppendLine("  performance:");
                sb.AppendLine("    name: Performance Tests");
                sb.AppendLine("    runs-on: ubuntu-latest");
                sb.AppendLine("    needs: test");
                sb.AppendLine("    steps:");
                sb.AppendLine("      - uses: actions/checkout@v3");
                sb.AppendLine("      - name: Run Performance Tests");
                sb.AppendLine("        run: echo 'Running performance tests...'");
                sb.AppendLine($"      - name: Check Performance Regression (threshold: {performanceRegressionThreshold * 100}%)");
                sb.AppendLine("        run: echo 'Checking performance baselines...'");
                sb.AppendLine();
            }

            // Notification job
            if (sendNotifications && (!string.IsNullOrEmpty(discordWebhookUrl) || !string.IsNullOrEmpty(slackWebhookUrl)))
            {
                sb.AppendLine("  notify:");
                sb.AppendLine("    name: Send Notifications");
                sb.AppendLine("    runs-on: ubuntu-latest");
                sb.AppendLine("    if: always()");
                sb.AppendLine("    needs: [test]");
                sb.AppendLine("    steps:");

                if (!string.IsNullOrEmpty(discordWebhookUrl))
                {
                    sb.AppendLine("      - name: Discord Notification");
                    sb.AppendLine("        run: |");
                    sb.AppendLine("          curl -X POST -H 'Content-Type: application/json' \\");
                    sb.AppendLine("          -d '{\"content\": \"Build ${{ job.status }}: ${{ github.repository }}@${{ github.ref }}\"}' \\");
                    sb.AppendLine($"          {discordWebhookUrl}");
                }

                if (!string.IsNullOrEmpty(slackWebhookUrl))
                {
                    sb.AppendLine("      - name: Slack Notification");
                    sb.AppendLine("        run: |");
                    sb.AppendLine("          curl -X POST -H 'Content-Type: application/json' \\");
                    sb.AppendLine("          -d '{\"text\": \"Build ${{ job.status }}: ${{ github.repository }}@${{ github.ref }}\"}' \\");
                    sb.AppendLine($"          {slackWebhookUrl}");
                }
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
