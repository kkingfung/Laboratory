using UnityEngine;

namespace Laboratory.Core.Player
{
    /// <summary>
    /// Interface for player camera controllers.
    /// Provides abstraction for different camera systems (FPS, TPS, orbit, etc.)
    /// </summary>
    public interface IPlayerCameraController
    {
        /// <summary>Camera transform</summary>
        Transform CameraTransform { get; }

        /// <summary>Main camera component</summary>
        Camera Camera { get; }

        /// <summary>Whether camera can rotate</summary>
        bool CanRotate { get; set; }

        /// <summary>Camera sensitivity for input</summary>
        float Sensitivity { get; set; }

        /// <summary>Current camera mode</summary>
        CameraMode CurrentMode { get; }

        /// <summary>Rotates camera based on input</summary>
        void Rotate(Vector2 input);

        /// <summary>Sets camera mode</summary>
        void SetCameraMode(CameraMode mode);

        /// <summary>Sets camera target to follow</summary>
        void SetTarget(Transform target);

        /// <summary>Zooms camera in/out</summary>
        void Zoom(float delta);

        /// <summary>Resets camera to default state</summary>
        void ResetCamera();

        /// <summary>Event fired when camera mode changes</summary>
        event System.Action<CameraMode> OnCameraModeChanged;
    }

    /// <summary>Camera operation modes</summary>
    public enum CameraMode
    {
        FirstPerson,
        ThirdPerson,
        Orbit,
        Free,
        Fixed
    }
}