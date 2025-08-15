using UnityEngine;
using System;

[Serializable]
public class FrameData
{
    public Vector3 position;
    public Quaternion rotation;
    public string animationState; // optional, for animation tracking
}
