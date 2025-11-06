# Firebase Cloud Messaging (FCM) & Stream Chat Integration Guide

## Overview

This document explains the Firebase Cloud Messaging (FCM) implementation for push notifications, specifically designed to work seamlessly with Stream Chat when you're ready to integrate it.

## Current Implementation Status

✅ **Completed:**
- Firebase Messaging SDK installed (v13.3.0)
- Android configuration complete
- FCM initialization and token management
- Notification handling (foreground, background, and click actions)
- Stream Chat-ready architecture

⏳ **Pending (for future Stream Chat integration):**
- Stream Chat SDK integration
- Token registration with Stream Chat
- Chat-specific notification payloads
- Backend API token sync

---

## Architecture

### Components

1. **FirebaseMessagingManager.cs** (`Assets/Scripts/Services/`)
   - Initializes Firebase and FCM
   - Manages FCM token lifecycle
   - Handles incoming notifications
   - Provides Stream Chat-ready token registration

2. **NotificationHandler.cs** (`Assets/Scripts/Services/`)
   - Routes notifications to appropriate screens
   - Handles Stream Chat notification types
   - Manages notification UI display

3. **LoginHandler.cs** (`Assets/Scripts/Controllers/`)
   - Initializes FCM after login
   - Subscribes to token events

---

## How It Works

### 1. Initialization Flow

```
User Login → LoginHandler.InitializeFirebaseMessaging()
    ↓
FirebaseMessagingManager.Initialize()
    ↓
Request FCM Token → OnTokenReceived event
    ↓
Save token locally (PlayerPrefs)
```

### 2. Notification Flow

```
FCM receives notification
    ↓
FirebaseMessagingManager.OnMessageReceived
    ↓
NotificationHandler processes notification type
    ↓
Show notification (foreground) or system handles (background)
    ↓
User taps → Navigate to appropriate screen
```

---

## Testing FCM (Without Stream Chat)

### Test via Firebase Console

1. Go to Firebase Console → Cloud Messaging
2. Click "Send your first message"
3. Enter notification title and text
4. Click "Send test message"
5. Enter your FCM token (visible in Unity logs after login)

### Test Notification Payload

For Stream Chat-compatible testing, use this data payload:

```json
{
  "notification": {
    "title": "New message from John",
    "body": "Hey, how are you?"
  },
  "data": {
    "type": "message.new",
    "channel_id": "messaging:general-123",
    "message_id": "msg-456",
    "sender": "john-user-id"
  }
}
```

---

## Stream Chat Integration (When Ready)

### Step 1: Install Stream Chat SDK for Unity

1. Download Stream Chat Unity SDK
2. Import into your project
3. Add Stream Chat dependencies

### Step 2: Initialize Stream Chat

```csharp
using StreamChat.Core;

public class StreamChatManager : MonoBehaviour
{
    private IStreamChatClient chatClient;

    public async void Initialize()
    {
        // Initialize Stream Chat
        chatClient = StreamChatClient.CreateDefaultClient();

        var credentials = new Credentials(
            apiKey: "YOUR_STREAM_API_KEY",
            userId: UserData.userName,
            userToken: "YOUR_STREAM_USER_TOKEN"
        );

        await chatClient.ConnectUserAsync(credentials);

        // Register FCM token with Stream Chat
        RegisterFCMTokenWithStream();
    }

    private void RegisterFCMTokenWithStream()
    {
        string fcmToken = FirebaseMessagingManager.Instance.GetFCMToken();

        if (!string.IsNullOrEmpty(fcmToken))
        {
            // Register device for push notifications
            await chatClient.AddDeviceAsync(fcmToken, PushProvider.Firebase);
            Debug.Log("[Stream Chat] FCM token registered successfully");
        }
    }
}
```

### Step 3: Update FirebaseMessagingManager

Uncomment and implement the `RegisterTokenWithStreamChat` method in `FirebaseMessagingManager.cs`:

```csharp
public async void RegisterTokenWithStreamChat()
{
    string token = GetFCMToken();

    if (string.IsNullOrEmpty(token))
    {
        Debug.LogWarning("[FCM] No token available for Stream Chat registration");
        return;
    }

    try
    {
        var chatClient = StreamChatClient.Instance;
        await chatClient.AddDevice(token, PushProvider.Firebase);
        Debug.Log("[FCM] Token registered with Stream Chat successfully");
    }
    catch (Exception ex)
    {
        Debug.LogError($"[FCM] Failed to register token with Stream Chat: {ex.Message}");
    }
}
```

### Step 4: Handle Stream Chat Notifications

The `NotificationHandler.cs` is already set up to handle Stream Chat notification types:

- `message.new` - New message received
- `channel.invite` - User invited to channel
- `mention` - User mentioned in a message

These will automatically route to the appropriate screens when notification is tapped.

### Step 5: Update Backend API (Optional)

If you want to send FCM tokens to your backend:

1. Uncomment the code in `LoginHandler.OnFCMTokenReceived()`
2. Create a backend endpoint to store FCM tokens:

```csharp
// In LoginHandler.cs
private void OnFCMTokenReceived(string token)
{
    Debug.Log($"[FCM] Token received: {token}");

    // Register with Stream Chat
    FirebaseMessagingManager.Instance.RegisterTokenWithStreamChat();

    // Send to backend
    StartCoroutine(SendTokenToBackend(token));
}

private IEnumerator SendTokenToBackend(string token)
{
    string endpoint = baseURL + "/api/v1/user/fcm-token";
    var payload = new { fcmToken = token };
    string jsonData = JsonUtility.ToJson(payload);

    using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
    {
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", AuthTokenManager.GetToken());

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[FCM] Token sent to backend successfully");
        }
        else
        {
            Debug.LogError($"[FCM] Failed to send token to backend: {request.error}");
        }
    }
}
```

---

## Backend Configuration for Stream Chat

### Firebase Cloud Functions Example

When a Stream Chat message is sent, trigger FCM notification:

```javascript
const functions = require('firebase-functions');
const admin = require('firebase-admin');

// Stream Chat Webhook Handler
exports.streamChatWebhook = functions.https.onRequest(async (req, res) => {
  const event = req.body;

  if (event.type === 'message.new') {
    const message = event.message;
    const channel = event.channel;
    const sender = message.user;

    // Get recipient's FCM token from your database
    const recipientToken = await getRecipientFCMToken(channel.members);

    // Send FCM notification
    const notification = {
      token: recipientToken,
      notification: {
        title: `${sender.name}`,
        body: message.text
      },
      data: {
        type: 'message.new',
        channel_id: channel.id,
        message_id: message.id,
        sender: sender.id,
        click_action: 'open_chat'
      }
    };

    await admin.messaging().send(notification);
  }

  res.status(200).send('OK');
});
```

---

## Notification Payload Structure for Stream Chat

### Message Notification

```json
{
  "notification": {
    "title": "John Doe",
    "body": "Hey, check this out!"
  },
  "data": {
    "type": "message.new",
    "channel_id": "messaging:channel-123",
    "message_id": "msg-456",
    "sender": "user-789",
    "click_action": "open_chat"
  }
}
```

### Channel Invite Notification

```json
{
  "notification": {
    "title": "Channel Invitation",
    "body": "John Doe invited you to Tech Discussion"
  },
  "data": {
    "type": "channel.invite",
    "channel_id": "messaging:tech-discussion",
    "inviter": "john-doe-id",
    "click_action": "open_channel:tech-discussion"
  }
}
```

### Mention Notification

```json
{
  "notification": {
    "title": "You were mentioned",
    "body": "John Doe mentioned you in Tech Discussion"
  },
  "data": {
    "type": "mention",
    "channel_id": "messaging:tech-discussion",
    "message_id": "msg-789",
    "sender": "john-doe-id",
    "click_action": "open_channel:tech-discussion"
  }
}
```

---

## Configuration Files

### Android Configuration

**AndroidManifest.xml** (`Assets/Plugins/Android/AndroidManifest.xml`)
- Already configured with `MessagingUnityPlayerActivity`
- Uses `MessageForwardingService` for background notifications

**google-services.json** (`Assets/google-services.json`)
- Contains your Firebase project configuration
- Project ID: `ambio-backend`
- Package: `com.fambience.ambio`

**MessagingDependencies.xml** (`Assets/Firebase/Editor/MessagingDependencies.xml`)
- Firebase Messaging: v25.0.1
- Firebase Analytics: v23.0.0

---

## Debugging

### Enable Debug Logging

All FCM operations are logged with `[FCM]` prefix. Check Unity console for:

```
[FCM] Initializing Firebase Messaging...
[FCM] Token received: <your-fcm-token>
[FCM] Message received!
[FCM] Notification type: message.new
```

### Common Issues

**1. No token received**
- Check Firebase configuration (google-services.json)
- Ensure Firebase dependencies resolved
- Check internet connection

**2. Notifications not showing**
- Verify AndroidManifest.xml configuration
- Check notification permissions (Android 13+)
- Ensure app is not in battery optimization

**3. Click actions not working**
- Check notification data payload format
- Verify `click_action` key exists
- Ensure NotificationHandler is initialized

---

## Key Methods & Events

### FirebaseMessagingManager

```csharp
// Initialize FCM
FirebaseMessagingManager.Instance.Initialize();

// Get current FCM token
string token = FirebaseMessagingManager.Instance.GetFCMToken();

// Subscribe to topic
FirebaseMessagingManager.Instance.SubscribeToTopic("general_updates");

// Register with Stream Chat (when integrated)
FirebaseMessagingManager.Instance.RegisterTokenWithStreamChat();

// Events
FirebaseMessagingManager.Instance.OnTokenReceived += (token) => { };
FirebaseMessagingManager.Instance.OnMessageReceived += (args) => { };
FirebaseMessagingManager.Instance.OnNotificationTapped += (action) => { };
```

### NotificationHandler

```csharp
// Request permissions (Android 13+)
NotificationHandler.Instance.RequestNotificationPermissions();
```

---

## Next Steps

1. **Test FCM** - Send test notifications via Firebase Console
2. **Integrate Stream Chat SDK** - When ready to add chat functionality
3. **Update Backend** - Configure webhook for Stream Chat events
4. **Test End-to-End** - Send messages and verify notifications work

---

## References

- [Firebase Cloud Messaging Documentation](https://firebase.google.com/docs/cloud-messaging)
- [Stream Chat Unity SDK](https://getstream.io/chat/sdk/unity/)
- [Stream Chat Push Notifications](https://getstream.io/chat/docs/unity/push_introduction/)

---

## Support

For issues or questions:
- Check Unity console logs (filter by `[FCM]`)
- Review Firebase Console for delivery status
- Verify Stream Chat dashboard for webhook events (when integrated)