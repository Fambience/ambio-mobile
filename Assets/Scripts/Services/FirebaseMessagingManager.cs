using System;
using System.Collections;
using Firebase;
using Firebase.Extensions;
using Firebase.Messaging;
using UnityEngine;

namespace Services
{
    /// <summary>
    /// Manages Firebase Cloud Messaging (FCM) for push notifications.
    /// Designed to work seamlessly with Stream Chat when integrated.
    /// </summary>
    public class FirebaseMessagingManager : MonoBehaviour
    {
        private static FirebaseMessagingManager _instance;
        public static FirebaseMessagingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("FirebaseMessagingManager");
                    _instance = go.AddComponent<FirebaseMessagingManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private FirebaseApp firebaseApp;
        private string fcmToken;
        private bool isInitialized = false;

        // Events that can be subscribed to
        public event Action<string> OnTokenReceived;
        public event Action<Firebase.Messaging.MessageReceivedEventArgs> OnMessageReceived;
        public event Action<string> OnNotificationTapped;

        private const string FCM_TOKEN_KEY = "FCM_TOKEN";
        private const string NOTIFICATION_CHANNEL_ID = "ambio_messages";

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

        /// <summary>
        /// Initialize Firebase and setup FCM
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                Debug.Log("[FCM] Already initialized");
                return;
            }

            Debug.Log("[FCM] Initializing Firebase Messaging...");

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    firebaseApp = FirebaseApp.DefaultInstance;
                    InitializeFirebaseMessaging();
                }
                else
                {
                    Debug.LogError($"[FCM] Could not resolve all Firebase dependencies: {dependencyStatus}");
                }
            });
        }

        private void InitializeFirebaseMessaging()
        {
            Debug.Log("[FCM] Firebase Messaging initialization started");

            // Subscribe to token received event
            FirebaseMessaging.TokenReceived += OnTokenReceivedHandler;

            // Subscribe to message received event
            FirebaseMessaging.MessageReceived += OnMessageReceivedHandler;

            // Request FCM token
            RequestFCMToken();

            isInitialized = true;
            Debug.Log("[FCM] Firebase Messaging initialized successfully");
        }

        /// <summary>
        /// Request FCM token from Firebase
        /// </summary>
        private void RequestFCMToken()
        {
            FirebaseMessaging.GetTokenAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("[FCM] Token request was canceled");
                    return;
                }

                if (task.IsFaulted)
                {
                    Debug.LogError($"[FCM] Token request failed: {task.Exception}");
                    return;
                }

                fcmToken = task.Result;
                SaveTokenLocally(fcmToken);
                Debug.Log($"[FCM] Token received: {fcmToken}");

                // Invoke event for other systems to use (e.g., Stream Chat)
                OnTokenReceived?.Invoke(fcmToken);
            });
        }

        /// <summary>
        /// Handler for token received events
        /// </summary>
        private void OnTokenReceivedHandler(object sender, TokenReceivedEventArgs args)
        {
            fcmToken = args.Token;
            SaveTokenLocally(fcmToken);
            Debug.Log($"[FCM] Token refreshed: {fcmToken}");

            // Invoke event for other systems to use
            OnTokenReceived?.Invoke(fcmToken);
        }

        /// <summary>
        /// Handler for message received events (foreground & background)
        /// </summary>
        private void OnMessageReceivedHandler(object sender, MessageReceivedEventArgs args)
        {
            Debug.Log("[FCM] Message received!");

            if (args.Message.Notification != null)
            {
                Debug.Log($"[FCM] Notification - Title: {args.Message.Notification.Title}");
                Debug.Log($"[FCM] Notification - Body: {args.Message.Notification.Body}");
            }

            if (args.Message.Data != null && args.Message.Data.Count > 0)
            {
                Debug.Log("[FCM] Message Data:");
                foreach (var data in args.Message.Data)
                {
                    Debug.Log($"[FCM] {data.Key}: {data.Value}");
                }
            }

            // Invoke event for notification handler to process
            OnMessageReceived?.Invoke(args);

            // Handle the notification based on app state
            HandleNotification(args);
        }

        /// <summary>
        /// Handle notification display and routing
        /// </summary>
        private void HandleNotification(MessageReceivedEventArgs args)
        {
            // Check if app is in foreground
            bool isInForeground = Application.isFocused;

            if (isInForeground)
            {
                // App is in foreground - show custom in-app notification
                Debug.Log("[FCM] App is in foreground, showing in-app notification");
                ShowInAppNotification(args);
            }
            else
            {
                // App is in background - system notification will be shown automatically
                Debug.Log("[FCM] App is in background, system notification will be shown");
            }

            // Extract routing information from data payload
            if (args.Message.Data.ContainsKey("click_action"))
            {
                string clickAction = args.Message.Data["click_action"];
                Debug.Log($"[FCM] Click action: {clickAction}");
                // Store for handling when notification is tapped
                PlayerPrefs.SetString("pending_notification_action", clickAction);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Show in-app notification when app is in foreground
        /// </summary>
        private void ShowInAppNotification(MessageReceivedEventArgs args)
        {
            // This can be customized to show a custom UI
            // For now, we'll just log it
            string title = args.Message.Notification?.Title ?? "New Message";
            string body = args.Message.Notification?.Body ?? "";

            Debug.Log($"[FCM] In-App Notification - {title}: {body}");

            // TODO: Show custom notification UI
            // You can create a notification banner, popup, or use your existing UI system
        }

        /// <summary>
        /// Subscribe to a topic (useful for broadcast notifications)
        /// </summary>
        public void SubscribeToTopic(string topic)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[FCM] Not initialized yet, cannot subscribe to topic");
                return;
            }

            FirebaseMessaging.SubscribeAsync(topic).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"[FCM] Failed to subscribe to topic '{topic}': {task.Exception}");
                    return;
                }

                Debug.Log($"[FCM] Successfully subscribed to topic: {topic}");
            });
        }

        /// <summary>
        /// Unsubscribe from a topic
        /// </summary>
        public void UnsubscribeFromTopic(string topic)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[FCM] Not initialized yet, cannot unsubscribe from topic");
                return;
            }

            FirebaseMessaging.UnsubscribeAsync(topic).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"[FCM] Failed to unsubscribe from topic '{topic}': {task.Exception}");
                    return;
                }

                Debug.Log($"[FCM] Successfully unsubscribed from topic: {topic}");
            });
        }

        /// <summary>
        /// Get the current FCM token (useful for Stream Chat integration)
        /// </summary>
        public string GetFCMToken()
        {
            if (string.IsNullOrEmpty(fcmToken))
            {
                fcmToken = PlayerPrefs.GetString(FCM_TOKEN_KEY, "");
            }
            return fcmToken;
        }

        /// <summary>
        /// Save FCM token locally for future use
        /// </summary>
        private void SaveTokenLocally(string token)
        {
            PlayerPrefs.SetString(FCM_TOKEN_KEY, token);
            PlayerPrefs.Save();
            Debug.Log("[FCM] Token saved locally");
        }

        /// <summary>
        /// Register FCM token with Stream Chat
        /// Call this method when integrating Stream Chat
        /// </summary>
        public void RegisterTokenWithStreamChat()
        {
            string token = GetFCMToken();

            if (string.IsNullOrEmpty(token))
            {
                Debug.LogWarning("[FCM] No token available for Stream Chat registration");
                return;
            }

            Debug.Log($"[FCM] Ready to register token with Stream Chat: {token}");

            // TODO: When Stream Chat is integrated, add the registration code here
            // Example:
            // var chatClient = StreamChatClient.Instance;
            // await chatClient.AddDevice(token, PushProvider.Firebase);

            Debug.Log("[FCM] Stream Chat integration pending - token saved for future use");
        }

        /// <summary>
        /// Check for pending notification actions (when app is opened from notification)
        /// </summary>
        public void CheckPendingNotificationActions()
        {
            if (PlayerPrefs.HasKey("pending_notification_action"))
            {
                string action = PlayerPrefs.GetString("pending_notification_action");
                PlayerPrefs.DeleteKey("pending_notification_action");
                PlayerPrefs.Save();

                Debug.Log($"[FCM] Processing pending notification action: {action}");
                OnNotificationTapped?.Invoke(action);

                // Route to appropriate screen based on action
                HandleNotificationAction(action);
            }
        }

        /// <summary>
        /// Handle notification actions/deep links
        /// </summary>
        private void HandleNotificationAction(string action)
        {
            // Parse the action and route to appropriate screen
            // Examples:
            // - "open_chat" -> Open chat screen
            // - "open_channel:123" -> Open specific channel
            // - "open_profile:456" -> Open user profile

            Debug.Log($"[FCM] Handling notification action: {action}");

            if (action.StartsWith("open_chat"))
            {
                // Navigate to chat screen
                Debug.Log("[FCM] Navigating to chat screen");
                // UIManager.Instance.OpenScreen(UIScreenType.Chat);
            }
            else if (action.StartsWith("open_channel:"))
            {
                string channelId = action.Split(':')[1];
                Debug.Log($"[FCM] Navigating to channel: {channelId}");
                // Open specific channel
            }
            // Add more routing logic as needed
        }

        /// <summary>
        /// Send backend API request to save FCM token (for future backend integration)
        /// </summary>
        public IEnumerator SendTokenToBackend(string token)
        {
            // TODO: Implement backend API call when ready
            // This method is ready for when you want to send the token to your backend

            Debug.Log($"[FCM] Backend token registration pending - Token: {token}");

            // Example implementation:
            // string endpoint = baseScript.baseURL + "/api/v1/user/fcm-token";
            // var payload = new { fcmToken = token };
            // string jsonData = JsonUtility.ToJson(payload);
            //
            // using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
            // {
            //     request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            //     request.downloadHandler = new DownloadHandlerBuffer();
            //     request.SetRequestHeader("Content-Type", "application/json");
            //     request.SetRequestHeader("Authorization", AuthTokenManager.GetToken());
            //
            //     yield return request.SendWebRequest();
            //
            //     if (request.result == UnityWebRequest.Result.Success)
            //     {
            //         Debug.Log("[FCM] Token successfully sent to backend");
            //     }
            //     else
            //     {
            //         Debug.LogError($"[FCM] Failed to send token to backend: {request.error}");
            //     }
            // }

            yield return null;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (isInitialized)
            {
                FirebaseMessaging.TokenReceived -= OnTokenReceivedHandler;
                FirebaseMessaging.MessageReceived -= OnMessageReceivedHandler;
            }
        }
    }
}