using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace Laboratory.Tools
{
    /// <summary>
    /// Recorder for gameplay replays
    /// </summary>
    public class ReplayRecorder : MonoBehaviour
    {
        private bool isRecording = false;
        private bool isReplaying = false;
        private List<ReplayFrame> frames = new List<ReplayFrame>();
        private int currentReplayIndex = 0;
        
        public bool IsRecording => isRecording;
        public bool IsReplaying => isReplaying;
        public int FrameCount => frames.Count;
        
        public void StartRecording()
        {
            isRecording = true;
            isReplaying = false;
            frames.Clear();
        }
        
        public void StopRecording()
        {
            isRecording = false;
        }
        
        public void StartReplay()
        {
            isReplaying = true;
            isRecording = false;
            currentReplayIndex = 0;
        }
        
        public void StopReplay()
        {
            isReplaying = false;
            currentReplayIndex = 0;
        }
        
        public void UpdateReplay()
        {
            if (isReplaying && currentReplayIndex < frames.Count)
            {
                currentReplayIndex++;
            }
        }
        
        public (float2 movement, bool jump, bool attack) GetCurrentReplayInput()
        {
            if (isReplaying && currentReplayIndex < frames.Count)
            {
                var frame = frames[currentReplayIndex];
                if (frame.customData.ContainsKey("movement") &&
                    frame.customData.ContainsKey("jump") &&
                    frame.customData.ContainsKey("attack"))
                {
                    return ((float2)frame.customData["movement"], 
                           (bool)frame.customData["jump"], 
                           (bool)frame.customData["attack"]);
                }
            }
            return (float2.zero, false, false);
        }
        
        public void RecordFrame(ReplayFrame frame)
        {
            if (isRecording)
            {
                frames.Add(frame);
            }
        }
        
        public ReplayFrame GetFrame(int index)
        {
            if (index >= 0 && index < frames.Count)
            {
                return frames[index];
            }
            return null;
        }
    }
    
    [Serializable]
    public class ReplayFrame
    {
        public float timestamp;
        public Vector3 position;
        public Quaternion rotation;
        public Dictionary<string, object> customData = new Dictionary<string, object>();
    }
}
