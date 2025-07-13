// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;

public static class PlayerPreferenceManager
{
    // Save an integer value
    public static void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    // Retrieve an integer value with a default fallback
    public static int GetInt(string key, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(key, defaultValue);
    }

    // Save a float value
    public static void SetFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    // Retrieve a float value with a default fallback
    public static float GetFloat(string key, float defaultValue = 0f)
    {
        return PlayerPrefs.GetFloat(key, defaultValue);
    }

    // Save a string value
    public static void SetString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    // Retrieve a string value with a default fallback
    public static string GetString(string key, string defaultValue = "")
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }

    // Check if a key exists
    public static bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    // Delete a specific key
    public static void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }

    // Clear all saved preferences
    public static void ClearAll()
    {
        PlayerPrefs.DeleteAll();
    }
}