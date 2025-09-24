using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Text;
using System.Collections;

public class ClaudeConsoleBridge : EditorWindow
{
    private string apiKey = "YOUR_API_KEY_HERE";
    private string lastLog = "";
    private string claudeResponse = "";

    [MenuItem("Window/Claude Console Bridge")]
    public static void ShowWindow()
    {
        GetWindow<ClaudeConsoleBridge>("Claude Bridge");
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void OnGUI()
    {
        GUILayout.Label("Last Unity Log:", EditorStyles.boldLabel);
        GUILayout.TextArea(lastLog, GUILayout.Height(50));

        if (GUILayout.Button("Send to Claude"))
        {
            EditorApplication.delayCall += () => SendToClaudeAsync(lastLog);
        }

        GUILayout.Label("Claude Response:", EditorStyles.boldLabel);
        GUILayout.TextArea(claudeResponse, GUILayout.Height(150));
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        lastLog = $"{type}: {logString}\n{stackTrace}";
    }

    private void SendToClaudeAsync(string text)
    {
        claudeResponse = "Sending request to Claude...";
        // Note: For proper async implementation, you would need to use UnityWebRequest
        // with callbacks or EditorCoroutines package. This is a placeholder.
        claudeResponse = "Claude integration requires additional setup. See documentation.";
    }

    private IEnumerator SendToClaude(string text)
    {
        string endpoint = "https://api.anthropic.com/v1/messages";

        var json = $"{{\"model\":\"claude-3-5-sonnet-20240620\",\"max_tokens\":512,\"messages\":[{{\"role\":\"user\",\"content\":\"{EscapeJson(text)}\"}}]}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(endpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-api-key", apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            claudeResponse = request.downloadHandler.text;
        }
        else
        {
            claudeResponse = "Error: " + request.error;
        }

        Repaint();
    }

    private string EscapeJson(string input)
    {
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
