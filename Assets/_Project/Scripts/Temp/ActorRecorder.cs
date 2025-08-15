using System.Collections.Generic;
using UnityEngine;

public class ActorRecorder : MonoBehaviour
{
    [SerializeField] private string actorName = "Player";
    [SerializeField] private bool record = true;

    private List<FrameData> frames = new List<FrameData>();

    public void Clear() => frames.Clear();

    void FixedUpdate()
    {
        if (!record) return;

        frames.Add(new FrameData
        {
            position = transform.position,
            rotation = transform.rotation
        });
    }

    public ActorReplayData GetReplayData()
    {
        return new ActorReplayData
        {
            actorName = actorName,
            frames = frames.ToArray()
        };
    }
}
