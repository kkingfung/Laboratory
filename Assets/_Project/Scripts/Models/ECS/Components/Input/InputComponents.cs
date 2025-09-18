using Unity.Entities;
using Unity.Mathematics;

namespace Laboratory.Models.ECS.Components.Input
{
    /// <summary>
    /// Input-specific components for enhanced input handling
    /// </summary>
    public struct InputDeviceComponent : IComponentData
    {
        public InputDeviceType DeviceType;
        public bool IsConnected;
        public float LastActivityTime;
    }

    /// <summary>
    /// Input device types
    /// </summary>
    public enum InputDeviceType
    {
        KeyboardMouse,
        Gamepad,
        Touch,
        VR
    }

    /// <summary>
    /// Input validation component
    /// </summary>
    public struct InputValidationComponent : IComponentData
    {
        public bool EnableValidation;
        public float MaxInputMagnitude;
        public float MinTimeBetweenInputs;
        public float LastValidInputTime;
    }

    /// <summary>
    /// Input prediction component
    /// </summary>
    public struct InputPredictionComponent : IComponentData
    {
        public int PredictionFrames;
        public float PredictionAccuracy;
        public bool EnablePrediction;
    }
}