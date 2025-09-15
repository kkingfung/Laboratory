using UnityEngine;
using Unity.Mathematics;

namespace Laboratory.Tools
{
    // Stub implementation of ReplayRecorder to resolve namespace issues
    public class ReplayRecorderStub : MonoBehaviour
    {
        public bool IsReplaying => false;
        public void StartReplay() { }
        public void StopReplay() { }
        public void UpdateReplay() { }
        
        public (float2 movement, bool jump, bool attack) GetCurrentReplayInput()
        {
            return (float2.zero, false, false);
        }
    }
}
