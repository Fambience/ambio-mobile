using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    // Notification channel IDs
    private const string DEFAULT_CHANNEL_ID = "ambio_default_channel";
    private const string HIGH_PRIORITY_CHANNEL_ID = "ambio_high_priority_channel";

    // Notification icons (make sure these exist in your Android res/drawable folder)
    private const string SMALL_ICON = "app_icon";
    private const string LARGE_ICON = "app_icon";

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializeNotifications();
    }

    /// <summary>
    /// Initialize notification channels and request permissions
    /// </summary>
    private void InitializeNotifications()
    {
#if UNITY_ANDROID
        RequestNotificationPermission();
        RegisterNotificationChannels();
        ClearAllNotifications();
#elif UNITY_IOS
        RequestIOSNotificationPermission();
#endif
    }

    /// <summary>
    /// Request notification permission for Android 13+ (API level 33+)
    /// </summary>
    private void RequestNotificationPermission()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {
            Debug.Log("[Notifications] Requesting POST_NOTIFICATIONS permission");
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
        }
        else
        {
            Debug.Log("[Notifications] POST_NOTIFICATIONS permission already granted");
        }
#endif
    }

    /// <summary>
    /// Request notification permission for iOS
    /// </summary>
    private void RequestIOSNotificationPermission()
    {
#if UNITY_IOS
        StartCoroutine(RequestIOSPermission());
#endif
    }

#if UNITY_IOS
    private IEnumerator RequestIOSPermission()
    {
        var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
        using (var req = new AuthorizationRequest(authorizationOption, true))
        {
            while (!req.IsCompleted)
            {
                yield return null;
            }

            string res = "\n RequestAuthorization:";
            res += "\n finished: " + req.IsCompleted;
            res += "\n granted :  " + req.Granted;
            res += "\n error:  " + req.Error;
            res += "\n deviceToken:  " + req.DeviceToken;
            Debug.Log("[Notifications] iOS Permission: " + res);
        }
    }
#endif

    /// <summary>
    /// Register notification channels for Android
    /// </summary>
    private void RegisterNotificationChannels()
    {
#if UNITY_ANDROID
        // Default channel
        var defaultChannel = new AndroidNotificationChannel
        {
            Id = DEFAULT_CHANNEL_ID,
            Name = "General Notifications",
            Importance = Importance.Default,
            Description = "General notifications for Ambio app updates and information"
        };
        AndroidNotificationCenter.RegisterNotificationChannel(defaultChannel);

        // High priority channel
        var highPriorityChannel = new AndroidNotificationChannel
        {
            Id = HIGH_PRIORITY_CHANNEL_ID,
            Name = "Important Notifications",
            Importance = Importance.High,
            Description = "Important and urgent notifications"
        };
        AndroidNotificationCenter.RegisterNotificationChannel(highPriorityChannel);

        Debug.Log("[Notifications] Android notification channels registered");
#endif
    }

    /// <summary>
    /// Send a notification immediately
    /// </summary>
    public void SendNotification(string title, string body, bool highPriority = false)
    {
#if UNITY_ANDROID
        var channelId = highPriority ? HIGH_PRIORITY_CHANNEL_ID : DEFAULT_CHANNEL_ID;

        var notification = new AndroidNotification
        {
            Title = title,
            Text = body,
            FireTime = DateTime.Now,
            SmallIcon = SMALL_ICON,
            LargeIcon = LARGE_ICON
        };

        int notificationId = AndroidNotificationCenter.SendNotification(notification, channelId);
        Debug.Log($"[Notifications] Sent notification (ID: {notificationId}): {title}");
#elif UNITY_IOS
        var notification = new iOSNotification
        {
            Title = title,
            Body = body,
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
            Trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = new TimeSpan(0, 0, 1),
                Repeats = false
            }
        };

        iOSNotificationCenter.ScheduleNotification(notification);
        Debug.Log($"[Notifications] Sent iOS notification: {title}");
#endif
    }

    /// <summary>
    /// Schedule a notification for later
    /// </summary>
    public void ScheduleNotification(string title, string body, int delayInSeconds, bool highPriority = false)
    {
#if UNITY_ANDROID
        var channelId = highPriority ? HIGH_PRIORITY_CHANNEL_ID : DEFAULT_CHANNEL_ID;

        var notification = new AndroidNotification
        {
            Title = title,
            Text = body,
            FireTime = DateTime.Now.AddSeconds(delayInSeconds),
            SmallIcon = SMALL_ICON,
            LargeIcon = LARGE_ICON
        };

        int notificationId = AndroidNotificationCenter.SendNotification(notification, channelId);
        Debug.Log($"[Notifications] Scheduled notification (ID: {notificationId}) for {delayInSeconds}s: {title}");
#elif UNITY_IOS
        var notification = new iOSNotification
        {
            Title = title,
            Body = body,
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
            Trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = new TimeSpan(0, 0, delayInSeconds),
                Repeats = false
            }
        };

        iOSNotificationCenter.ScheduleNotification(notification);
        Debug.Log($"[Notifications] Scheduled iOS notification for {delayInSeconds}s: {title}");
#endif
    }

    /// <summary>
    /// Send a notification with custom data payload
    /// </summary>
    public void SendNotificationWithData(string title, string body, string intentData, bool highPriority = false)
    {
#if UNITY_ANDROID
        var channelId = highPriority ? HIGH_PRIORITY_CHANNEL_ID : DEFAULT_CHANNEL_ID;

        var notification = new AndroidNotification
        {
            Title = title,
            Text = body,
            FireTime = DateTime.Now,
            SmallIcon = SMALL_ICON,
            LargeIcon = LARGE_ICON,
            IntentData = intentData
        };

        int notificationId = AndroidNotificationCenter.SendNotification(notification, channelId);
        Debug.Log($"[Notifications] Sent notification with data (ID: {notificationId}): {title}");
#elif UNITY_IOS
        var notification = new iOSNotification
        {
            Title = title,
            Body = body,
            Data = intentData,
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
            Trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = new TimeSpan(0, 0, 1),
                Repeats = false
            }
        };

        iOSNotificationCenter.ScheduleNotification(notification);
        Debug.Log($"[Notifications] Sent iOS notification with data: {title}");
#endif
    }

    /// <summary>
    /// Cancel a specific notification
    /// </summary>
    public void CancelNotification(int notificationId)
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelNotification(notificationId);
        Debug.Log($"[Notifications] Cancelled notification ID: {notificationId}");
#elif UNITY_IOS
        iOSNotificationCenter.RemoveScheduledNotification(notificationId.ToString());
        Debug.Log($"[Notifications] Cancelled iOS notification ID: {notificationId}");
#endif
    }

    /// <summary>
    /// Cancel all notifications
    /// </summary>
    public void ClearAllNotifications()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllNotifications();
        AndroidNotificationCenter.CancelAllDisplayedNotifications();
        Debug.Log("[Notifications] Cleared all Android notifications");
#elif UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
        iOSNotificationCenter.RemoveAllDeliveredNotifications();
        Debug.Log("[Notifications] Cleared all iOS notifications");
#endif
    }

    /// <summary>
    /// Check the last notification intent data (Android only)
    /// </summary>
    public void CheckNotificationIntent()
    {
#if UNITY_ANDROID
        var notificationIntentData = AndroidNotificationCenter.GetLastNotificationIntent();
        if (notificationIntentData != null)
        {
            Debug.Log("[Notifications] App opened from notification with data: " + notificationIntentData.Notification.IntentData);
            // Handle the intent data here (e.g., navigate to specific screen)
        }
#endif
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            // App resumed - check if opened from notification
            CheckNotificationIntent();
        }
    }
}
