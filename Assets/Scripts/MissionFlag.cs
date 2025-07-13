// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;

[System.Serializable]
public class MissionFlag
{
    [Header("Mission Properties")]
    public string flagName; // Name of the mission flag
    public bool isCompleted; // Status of the mission flag

    [TextArea]
    public string description; // Description of the mission flag

    [Header("Dependencies")]
    public MissionFlag[] requiredFlags; // Flags that must be completed before this one

    // Method to check if all required flags are completed
    public bool CanActivate()
    {
        foreach (var flag in requiredFlags)
        {
            if (flag != null && !flag.isCompleted)
            {
                return false;
            }
        }
        return true;
    }

    // Method to complete the mission flag
    public void CompleteFlag()
    {
        if (CanActivate())
        {
            isCompleted = true;
            Debug.Log($"Mission Flag '{flagName}' has been completed.");
        }
        else
        {
            Debug.LogWarning($"Cannot complete Mission Flag '{flagName}' because required flags are not completed.");
        }
    }
}