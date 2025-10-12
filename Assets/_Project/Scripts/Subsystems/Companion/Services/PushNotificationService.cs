using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.Companion
{
    /// <summary>
    /// Implementation of push notification delivery service
    /// </summary>
    public class PushNotificationService : IPushNotificationService
    {
        private readonly CompanionSubsystemConfig _config;
        private readonly Dictionary<string, NotificationPreferences> _userPreferences = new();
        private readonly List<CompanionNotification> _pendingNotifications = new();

        public PushNotificationService(CompanionSubsystemConfig config)
        {
            _config = config;
        }

        public async Task<bool> InitializeAsync()
        {
            Debug.Log("[PushNotificationService] Initializing push notification service...");
            await Task.Delay(50);
            return true;
        }

        public async Task<bool> SendNotificationAsync(CompanionNotification notification)
        {
            Debug.Log($"[PushNotificationService] Sending notification: {notification.title}");
            await Task.Delay(100);
            notification.isSent = true;
            return true;
        }

        public void ScheduleNotification(CompanionNotification notification, DateTime deliveryTime)
        {
            Debug.Log($"[PushNotificationService] Scheduling notification for: {deliveryTime}");
            notification.scheduledTime = deliveryTime;
            _pendingNotifications.Add(notification);
        }

        public void CancelNotification(string notificationId)
        {
            Debug.Log($"[PushNotificationService] Cancelling notification: {notificationId}");
            _pendingNotifications.RemoveAll(n => n.notificationId == notificationId);
        }

        public void ProcessPendingNotifications()
        {
            var now = DateTime.Now;
            var toProcess = _pendingNotifications.FindAll(n => n.scheduledTime <= now);

            foreach (var notification in toProcess)
            {
                _ = SendNotificationAsync(notification);
                _pendingNotifications.Remove(notification);
            }
        }

        public void UpdateUserPreferences(string userId, NotificationPreferences preferences)
        {
            Debug.Log($"[PushNotificationService] Updating preferences for user: {userId}");
            _userPreferences[userId] = preferences;
        }

        public NotificationPreferences GetUserPreferences(string userId)
        {
            if (_userPreferences.TryGetValue(userId, out var preferences))
            {
                return preferences;
            }

            return new NotificationPreferences { userId = userId };
        }
    }
}