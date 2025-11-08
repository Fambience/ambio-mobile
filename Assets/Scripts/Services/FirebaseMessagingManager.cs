using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Messaging;
using UnityEngine.Android;

public class FirebaseMessagingManager : MonoBehaviour
{
    public static FirebaseMessagingManager Instance { get; private set; }

    // Events for other components to subscribe to
    public event Action<string> OnTokenReceived;
    public event Action<Firebase.Messaging.FirebaseMessage> OnNotificationReceived;

    private string currentToken = "";

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
        // Request notification permission for Android 13+
        RequestNotificationPermission();

        // Subscribe to Firebase Messaging events
        Firebase.Messaging.FirebaseMessaging.TokenReceived += HandleTokenReceived;
        Firebase.Messaging.FirebaseMessaging.MessageReceived += HandleMessageReceived;

        // Request the current token
        RequestToken();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        Firebase.Messaging.FirebaseMessaging.TokenReceived -= HandleTokenReceived;
        Firebase.Messaging.FirebaseMessaging.MessageReceived -= HandleMessageReceived;
    }

    /// <summary>
    /// Request notification permission for Android 13+ (API level 33+)
    /// </summary>
    private void RequestNotificationPermission()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {
            Debug.Log("[FCM] Requesting POST_NOTIFICATIONS permission for Android 13+");
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
        }
        else
        {
            Debug.Log("[FCM] POST_NOTIFICATIONS permission already granted");
        }
#endif
    }

    /// <summary>
    /// Request the current FCM token
    /// </summary>
    private void RequestToken()
    {
        Firebase.Messaging.FirebaseMessaging.GetTokenAsync().ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("[FCM] GetTokenAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("[FCM] GetTokenAsync encountered an error: " + task.Exception);
                return;
            }

            string token = task.Result;
            currentToken = token;
            Debug.Log("[FCM] FCM Token retrieved: " + token);
            OnTokenReceived?.Invoke(token);
        });
    }

    /// <summary>
    /// Handle FCM token received event
    /// </summary>
    private void HandleTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs args)
    {
        currentToken = args.Token;
        Debug.Log("[FCM] Token Received: " + args.Token);
        OnTokenReceived?.Invoke(args.Token);
    }

    /// <summary>
    /// Handle incoming FCM messages
    /// </summary>
    private void HandleMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs args)
    {
        Firebase.Messaging.FirebaseMessage message = args.Message;

        Debug.Log("[FCM] Message Received from: " + message.From);

        // Log notification data
        if (message.Notification != null)
        {
            Debug.Log("[FCM] Notification Title: " + message.Notification.Title);
            Debug.Log("[FCM] Notification Body: " + message.Notification.Body);
        }

        // Log data payload
        if (message.Data != null && message.Data.Count > 0)
        {
            Debug.Log("[FCM] Data payload:");
            foreach (var data in message.Data)
            {
                Debug.Log($"[FCM]   {data.Key}: {data.Value}");
            }
        }

        // Invoke event for other components to handle
        OnNotificationReceived?.Invoke(message);

        // Display notification if app is in foreground
        if (message.Notification != null)
        {
            DisplayForegroundNotification(message.Notification.Title, message.Notification.Body);
        }
    }

    /// <summary>
    /// Display notification when app is in foreground
    /// </summary>
    private void DisplayForegroundNotification(string title, string body)
    {
        // Use NotificationManager to display local notification
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.SendNotification(title, body);
        }
        else
        {
            Debug.LogWarning("[FCM] NotificationManager not found. Cannot display foreground notification.");
        }
    }

    /// <summary>
    /// Get the current FCM token
    /// </summary>
    public string GetCurrentToken()
    {
        return currentToken;
    }

    /// <summary>
    /// Subscribe to a topic
    /// </summary>
    public void SubscribeToTopic(string topic)
    {
        Firebase.Messaging.FirebaseMessaging.SubscribeAsync(topic).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError($"[FCM] SubscribeAsync to {topic} was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError($"[FCM] SubscribeAsync to {topic} encountered an error: " + task.Exception);
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
        Firebase.Messaging.FirebaseMessaging.UnsubscribeAsync(topic).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError($"[FCM] UnsubscribeAsync from {topic} was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError($"[FCM] UnsubscribeAsync from {topic} encountered an error: " + task.Exception);
                return;
            }

            Debug.Log($"[FCM] Successfully unsubscribed from topic: {topic}");
        });
    }
}
