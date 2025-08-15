using UnityEngine;
using System;

[Serializable]
public class ReplaySession
{
    public ActorReplayData[] actors;
    public float frameRate = 60f; // optional: store recording FPS
}