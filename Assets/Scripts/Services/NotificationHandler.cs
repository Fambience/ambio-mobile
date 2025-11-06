using System.Collections.Generic;
using Firebase.Messaging;
using UnityEngine;

namespace Services
{
    /// <summary>
    /// Handles notification routing and actions based on notification payload.
    /// Designed to integrate with Stream Chat notifications.
    /// </summary>
    public class NotificationHandler : MonoBehaviour
    {
        private static NotificationHandler _instance;
        public static NotificationHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("NotificationHandler");
                    _instance = go.AddComponent<NotificationHandler>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Subscribe to FCM events
            FirebaseMessagingManager.Instance.OnMessageReceived += HandleMessage;
            FirebaseMessagingManager.Instance.OnNotificationTapped += HandleNotificationTap;
        }

        /// <summary>
        /// Handle incoming notification messages
        /// </summary>
        private void HandleMessage(MessageReceivedEventArgs args)
        {
            Debug.Log("[NotificationHandler] Processing notification message");

            // Parse notification type from data
            if (args.Message.Data != null && args.Message.Data.Count > 0)
            {
                ProcessNotificationData(args.Message.Data);
            }

            // Show notification UI if app is in foreground
            if (Application.isFocused)
            {
                ShowNotificationUI(args);
            }
        }

        /// <summary>
        /// Process notification data payload
        /// </summary>
        private void ProcessNotificationData(IDictionary<string, string> data)
        {
            // Stream Chat notifications typically include:
            // - "type": "message.new"
            // - "channel_id": "messaging:channel-123"
            // - "message_id": "msg-456"
            // - "sender": "user-789"

            if (data.ContainsKey("type"))
            {
                string notificationType = data["type"];
                Debug.Log($"[NotificationHandler] Notification type: {notificationType}");

                switch (notificationType)
                {
                    case "message.new":
                        HandleNewMessageNotification(data);
                        break;

                    case "channel.invite":
                        HandleChannelInviteNotification(data);
                        break;

                    case "mention":
                        HandleMentionNotification(data);
                        break;

                    default:
                        HandleGenericNotification(data);
                        break;
                }
            }
        }

        /// <summary>
        /// Handle new message notification (Stream Chat)
        /// </summary>
        private void HandleNewMessageNotification(IDictionary<string, string> data)
        {
            Debug.Log("[NotificationHandler] Handling new message notification");

            string channelId = data.ContainsKey("channel_id") ? data["channel_id"] : "";
            string messageId = data.ContainsKey("message_id") ? data["message_id"] : "";
            string sender = data.ContainsKey("sender") ? data["sender"] : "";

            Debug.Log($"[NotificationHandler] Channel: {channelId}, Message: {messageId}, Sender: {sender}");

            // Store for later routing when notification is tapped
            PlayerPrefs.SetString("notification_channel_id", channelId);
            PlayerPrefs.SetString("notification_message_id", messageId);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Handle channel invite notification
        /// </summary>
        private void HandleChannelInviteNotification(IDictionary<string, string> data)
        {
            Debug.Log("[NotificationHandler] Handling channel invite notification");

            string channelId = data.ContainsKey("channel_id") ? data["channel_id"] : "";
            PlayerPrefs.SetString("notification_channel_id", channelId);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Handle mention notification
        /// </summary>
        private void HandleMentionNotification(IDictionary<string, string> data)
        {
            Debug.Log("[NotificationHandler] Handling mention notification");

            string channelId = data.ContainsKey("channel_id") ? data["channel_id"] : "";
            string messageId = data.ContainsKey("message_id") ? data["message_id"] : "";

            PlayerPrefs.SetString("notification_channel_id", channelId);
            PlayerPrefs.SetString("notification_message_id", messageId);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Handle generic notifications
        /// </summary>
        private void HandleGenericNotification(IDictionary<string, string> data)
        {
            Debug.Log("[NotificationHandler] Handling generic notification");

            // Log all data for debugging
            foreach (var item in data)
            {
                Debug.Log($"[NotificationHandler] {item.Key}: {item.Value}");
            }
        }

        /// <summary>
        /// Handle notification tap/click actions
        /// </summary>
        private void HandleNotificationTap(string action)
        {
            Debug.Log($"[NotificationHandler] Notification tapped with action: {action}");

            // Check if we have stored channel/message info
            if (PlayerPrefs.HasKey("notification_channel_id"))
            {
                string channelId = PlayerPrefs.GetString("notification_channel_id");
                string messageId = PlayerPrefs.GetString("notification_message_id", "");

                // Navigate to the channel
                NavigateToChannel(channelId, messageId);

                // Clean up
                PlayerPrefs.DeleteKey("notification_channel_id");
                PlayerPrefs.DeleteKey("notification_message_id");
                PlayerPrefs.Save();
            }
            else
            {
                // Handle other action types
                RouteNotificationAction(action);
            }
        }

        /// <summary>
        /// Navigate to a specific chat channel
        /// </summary>
        private void NavigateToChannel(string channelId, string messageId)
        {
            Debug.Log($"[NotificationHandler] Navigating to channel: {channelId}");

            // TODO: Implement navigation to chat screen when Stream Chat is integrated
            // Example:
            // UIManager.Instance.OpenScreen(UIScreenType.Chat);
            // StreamChatManager.Instance.OpenChannel(channelId);
            //
            // if (!string.IsNullOrEmpty(messageId))
            // {
            //     StreamChatManager.Instance.ScrollToMessage(messageId);
            // }

            Debug.Log("[NotificationHandler] Chat navigation pending Stream Chat integration");
        }

        /// <summary>
        /// Route notification actions to appropriate screens
        /// </summary>
        private void RouteNotificationAction(string action)
        {
            Debug.Log($"[NotificationHandler] Routing action: {action}");

            if (string.IsNullOrEmpty(action))
                return;

            // Parse action string
            if (action.StartsWith("open_chat"))
            {
                // Open main chat screen
                Debug.Log("[NotificationHandler] Opening chat screen");
                // UIManager.Instance.OpenScreen(UIScreenType.Chat);
            }
            else if (action.StartsWith("open_home"))
            {
                // Open home screen
                Debug.Log("[NotificationHandler] Opening home screen");
                UIManager.Instance.OpenScreen(UIScreenType.Home);
            }
            else if (action.StartsWith("open_profile"))
            {
                // Open profile screen
                Debug.Log("[NotificationHandler] Opening profile screen");
                // UIManager.Instance.OpenScreen(UIScreenType.Profile);
            }
            // Add more routing logic as needed
        }

        /// <summary>
        /// Show notification UI when app is in foreground
        /// </summary>
        private void ShowNotificationUI(MessageReceivedEventArgs args)
        {
            string title = args.Message.Notification?.Title ?? "New Message";
            string body = args.Message.Notification?.Body ?? "";

            Debug.Log($"[NotificationHandler] Showing in-app notification: {title}");

            // TODO: Implement custom notification UI
            // You can create a notification banner or popup here
            // Example:
            // NotificationBanner.Show(title, body, () => {
            //     // Handle tap
            //     HandleNotificationTap("open_chat");
            // });

            Debug.Log($"[NotificationHandler] In-app UI: {title} - {body}");
        }

        /// <summary>
        /// Request notification permissions (Android 13+)
        /// </summary>
        public void RequestNotificationPermissions()
        {
#if UNITY_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
            {
                Debug.Log("[NotificationHandler] Requesting notification permissions");
                UnityEngine.Android.Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
            }
            else
            {
                Debug.Log("[NotificationHandler] Notification permissions already granted");
            }
#endif
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (FirebaseMessagingManager.Instance != null)
            {
                FirebaseMessagingManager.Instance.OnMessageReceived -= HandleMessage;
                FirebaseMessagingManager.Instance.OnNotificationTapped -= HandleNotificationTap;
            }
        }
    }
}