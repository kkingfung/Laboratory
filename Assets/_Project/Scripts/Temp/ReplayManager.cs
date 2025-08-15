using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class ReplayManager : MonoBehaviour
{
    [SerializeField] private ActorRecorder[] recorders;
    [SerializeField] private ActorPlayback[] players;
    [SerializeField] private string saveFileName = "replay.json";

    private ReplaySession session;

    #region Recording
    public void StartRecording()
    {
        session = new ReplaySession();
        foreach (var r in recorders) r.Clear();
    }

    public void StopRecording()
    {
        List<ActorReplayData> data = new List<ActorReplayData>();
        foreach (var r in recorders) data.Add(r.GetReplayData());
        session.actors = data.ToArray();
        SaveToFile();
    }
    #endregion

    #region Playback
    public void StartPlayback()
    {
        LoadFromFile();

        foreach (var p in players)
        {
            var actorData = System.Array.Find(session.actors, a => a.actorName == p.name);
            if (actorData != null)
            {
                p.SetFrames(actorData.frames);
                p.Play();
            }
        }
    }

    public void StopPlayback()
    {
        foreach (var p in players) p.Stop();
    }
    #endregion

    #region JSON Save/Load
    private void SaveToFile()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        string json = JsonUtility.ToJson(session, true);
        File.WriteAllText(path, json);
        Debug.Log($"Replay saved to {path}");
    }

    private void LoadFromFile()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        if (!File.Exists(path))
        {
            Debug.LogError("Replay file not found!");
            return;
        }

        string json = File.ReadAllText(path);
        session = JsonUtility.FromJson<ReplaySession>(json);
        Debug.Log("Replay loaded!");
    }
    #endregion
}
