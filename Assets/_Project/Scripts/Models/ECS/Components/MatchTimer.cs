using UnityEngine;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Temporary stub for MatchTimer component.
    /// TODO: Move this to the appropriate assembly or implement properly.
    /// </summary>
    [System.Serializable]
    public class MatchTimer
    {
        public float timeRemaining;
        public bool isActive;
        
        public MatchTimer()
        {
            timeRemaining = 0f;
            isActive = false;
        }
        
        public void Tick(float deltaTime)
        {
            if (isActive && timeRemaining > 0)
            {
                timeRemaining -= deltaTime;
                if (timeRemaining <= 0)
                {
                    timeRemaining = 0;
                    isActive = false;
                }
            }
        }
    }
}
