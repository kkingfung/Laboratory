using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.Companion
{
    /// <summary>
    /// Implementation of second screen experience service
    /// </summary>
    public class SecondScreenService : ISecondScreenService
    {
        private readonly CompanionSubsystemConfig _config;
        private readonly List<CompanionDevice> _registeredDevices = new();
        private readonly Dictionary<CompanionDeviceType, List<SecondScreenContent>> _availableContent = new();

        public SecondScreenService(CompanionSubsystemConfig config)
        {
            _config = config;
            InitializeContent();
        }

        public async Task<bool> InitializeAsync()
        {
            Debug.Log("[SecondScreenService] Initializing second screen service...");
            await Task.Delay(50);
            return true;
        }

        public async Task<bool> SendSecondScreenContentAsync(string deviceId, SecondScreenContent content)
        {
            Debug.Log($"[SecondScreenService] Sending content to device: {deviceId}");
            await Task.Delay(100);
            return true;
        }

        public List<SecondScreenContent> GetAvailableContent(CompanionDeviceType deviceType)
        {
            if (_availableContent.TryGetValue(deviceType, out var content))
            {
                return content;
            }

            return new List<SecondScreenContent>();
        }

        public async Task<bool> HandleSecondScreenActionAsync(string deviceId, SecondScreenAction action)
        {
            Debug.Log($"[SecondScreenService] Handling action {action.actionId} from device: {deviceId}");
            await Task.Delay(50);
            return true;
        }

        public void RegisterSecondScreenDevice(CompanionDevice device)
        {
            Debug.Log($"[SecondScreenService] Registering second screen device: {device.deviceId}");
            if (!_registeredDevices.Exists(d => d.deviceId == device.deviceId))
            {
                _registeredDevices.Add(device);
            }
        }

        public void UnregisterSecondScreenDevice(string deviceId)
        {
            Debug.Log($"[SecondScreenService] Unregistering second screen device: {deviceId}");
            _registeredDevices.RemoveAll(d => d.deviceId == deviceId);
        }

        private void InitializeContent()
        {
            // Initialize default content for different device types
            foreach (CompanionDeviceType deviceType in Enum.GetValues(typeof(CompanionDeviceType)))
            {
                _availableContent[deviceType] = new List<SecondScreenContent>();
            }
        }
    }
}