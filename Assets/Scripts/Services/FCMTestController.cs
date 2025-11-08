using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Test controller for Firebase Cloud Messaging
/// Attach this to a Canvas with UI buttons to test FCM functionality
/// Note: This uses standard Unity UI Text components (not TextMeshPro)
/// </summary>
public class FCMTestController : MonoBehaviour
{
    [Header("UI References (Optional - for visual testing)")]
    [Tooltip("Assign a UI Text component to display FCM status")]
    public Text statusText;

    [Tooltip("Assign a UI Text component to display the FCM token")]
    public Text tokenText;

    [Tooltip("Assign a UI Text component to display test logs")]
    public Text logText;

    [Header("Test Configuration")]
    public string testTopicName = "test_topic";

    private string logMessages = "";
    private const int MAX_LOG_LINES = 20;

    void Start()
    {
        // Wait for Firebase to initialize
        StartCoroutine(WaitAndSetupTests());
    }

    private IEnumerator WaitAndSetupTests()
    {
        Log("Waiting for Firebase to initialize...");

        // Wait for Firebase Initializer
        if (FirebaseInitializer.Instance != null)
        {
            yield return FirebaseInitializer.Instance.WaitForFirebaseReady();
            Log("Firebase is ready!");
        }
        else
        {
            Log("WARNING: FirebaseInitializer not found in scene!");
            yield break;
        }

        // Subscribe to FCM events
        SetupEventListeners();

        // Display initial status
        UpdateStatus();
    }

    private void SetupEventListeners()
    {
        if (FirebaseMessagingManager.Instance != null)
        {
            // Subscribe to token received event
            FirebaseMessagingManager.Instance.OnTokenReceived += OnTokenReceived;

            // Subscribe to notification received event
            FirebaseMessagingManager.Instance.OnNotificationReceived += OnNotificationReceived;

            Log("Event listeners registered");
        }
        else
        {
            Log("ERROR: FirebaseMessagingManager not found!");
        }

        if (FirebaseInitializer.Instance != null)
        {
            FirebaseInitializer.Instance.OnFirebaseReady += OnFirebaseReady;
            FirebaseInitializer.Instance.OnFirebaseError += OnFirebaseError;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (FirebaseMessagingManager.Instance != null)
        {
            FirebaseMessagingManager.Instance.OnTokenReceived -= OnTokenReceived;
            FirebaseMessagingManager.Instance.OnNotificationReceived -= OnNotificationReceived;
        }

        if (FirebaseInitializer.Instance != null)
        {
            FirebaseInitializer.Instance.OnFirebaseReady -= OnFirebaseReady;
            FirebaseInitializer.Instance.OnFirebaseError -= OnFirebaseError;
        }
    }

    #region Event Handlers

    private void OnFirebaseReady()
    {
        Log("✅ Firebase Ready Event Fired");
        UpdateStatus();
    }

    private void OnFirebaseError(string error)
    {
        Log($"❌ Firebase Error: {error}");
        UpdateStatus();
    }

    private void OnTokenReceived(string token)
    {
        Log($"✅ FCM Token Received: {token.Substring(0, Mathf.Min(50, token.Length))}...");
        UpdateTokenDisplay(token);
    }

    private void OnNotificationReceived(Firebase.Messaging.FirebaseMessage message)
    {
        Log("✅ Notification Received!");

        if (message.Notification != null)
        {
            Log($"  Title: {message.Notification.Title}");
            Log($"  Body: {message.Notification.Body}");
        }

        if (message.Data != null && message.Data.Count > 0)
        {
            Log("  Data:");
            foreach (var data in message.Data)
            {
                Log($"    {data.Key}: {data.Value}");
            }
        }
    }

    #endregion

    #region Public Test Methods (Call these from UI buttons or code)

    /// <summary>
    /// Test 1: Check Firebase Status
    /// </summary>
    public void TestFirebaseStatus()
    {
        Log("--- Test: Firebase Status ---");

        if (FirebaseInitializer.Instance != null)
        {
            Log($"Firebase Ready: {FirebaseInitializer.Instance.IsFirebaseReady}");
            Log($"Dependency Status: {FirebaseInitializer.Instance.FirebaseDependencyStatus}");
        }
        else
        {
            Log("❌ FirebaseInitializer not found!");
        }

        UpdateStatus();
    }

    /// <summary>
    /// Test 2: Get FCM Token
    /// </summary>
    public void TestGetToken()
    {
        Log("--- Test: Get FCM Token ---");

        if (FirebaseMessagingManager.Instance != null)
        {
            string token = FirebaseMessagingManager.Instance.GetCurrentToken();

            if (!string.IsNullOrEmpty(token))
            {
                Log($"✅ Token Retrieved: {token}");
                UpdateTokenDisplay(token);

                // Copy to clipboard (if on supported platform)
                GUIUtility.systemCopyBuffer = token;
                Log("Token copied to clipboard!");
            }
            else
            {
                Log("⚠️ Token is empty. Wait a few seconds and try again.");
            }
        }
        else
        {
            Log("❌ FirebaseMessagingManager not found!");
        }
    }

    /// <summary>
    /// Test 3: Send Local Notification
    /// </summary>
    public void TestLocalNotification()
    {
        Log("--- Test: Local Notification ---");

        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.SendNotification(
                "Test Notification",
                "This is a test local notification from FCM Test Controller!"
            );
            Log("✅ Local notification sent!");
        }
        else
        {
            Log("❌ NotificationManager not found!");
        }
    }

    /// <summary>
    /// Test 4: Schedule Notification
    /// </summary>
    public void TestScheduledNotification()
    {
        Log("--- Test: Scheduled Notification ---");

        if (NotificationManager.Instance != null)
        {
            int delaySeconds = 10;
            NotificationManager.Instance.ScheduleNotification(
                "Scheduled Test",
                $"This notification was scheduled {delaySeconds} seconds ago!",
                delaySeconds
            );
            Log($"✅ Notification scheduled for {delaySeconds} seconds from now");
        }
        else
        {
            Log("❌ NotificationManager not found!");
        }
    }

    /// <summary>
    /// Test 5: Subscribe to Topic
    /// </summary>
    public void TestSubscribeToTopic()
    {
        Log($"--- Test: Subscribe to Topic '{testTopicName}' ---");

        if (FirebaseMessagingManager.Instance != null)
        {
            FirebaseMessagingManager.Instance.SubscribeToTopic(testTopicName);
            Log($"✅ Subscription request sent for topic: {testTopicName}");
            Log("Check console for confirmation");
        }
        else
        {
            Log("❌ FirebaseMessagingManager not found!");
        }
    }

    /// <summary>
    /// Test 6: Unsubscribe from Topic
    /// </summary>
    public void TestUnsubscribeFromTopic()
    {
        Log($"--- Test: Unsubscribe from Topic '{testTopicName}' ---");

        if (FirebaseMessagingManager.Instance != null)
        {
            FirebaseMessagingManager.Instance.UnsubscribeFromTopic(testTopicName);
            Log($"✅ Unsubscribe request sent for topic: {testTopicName}");
            Log("Check console for confirmation");
        }
        else
        {
            Log("❌ FirebaseMessagingManager not found!");
        }
    }

    /// <summary>
    /// Test 7: Send Token to Backend
    /// </summary>
    public void TestSendTokenToBackend()
    {
        Log("--- Test: Send Token to Backend ---");

        if (FCMTokenService.Instance != null)
        {
            string token = FirebaseMessagingManager.Instance?.GetCurrentToken();

            if (!string.IsNullOrEmpty(token))
            {
                FCMTokenService.Instance.SendTokenToBackend(token);
                Log("✅ Token send request initiated");
                Log("Check console for backend response");
            }
            else
            {
                Log("⚠️ No token available yet");
            }
        }
        else
        {
            Log("❌ FCMTokenService not found!");
        }
    }

    /// <summary>
    /// Test 8: Clear All Notifications
    /// </summary>
    public void TestClearNotifications()
    {
        Log("--- Test: Clear All Notifications ---");

        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ClearAllNotifications();
            Log("✅ All notifications cleared");
        }
        else
        {
            Log("❌ NotificationManager not found!");
        }
    }

    /// <summary>
    /// Test 9: Run All Tests
    /// </summary>
    public void RunAllTests()
    {
        Log("========================================");
        Log("Running All FCM Tests...");
        Log("========================================");

        StartCoroutine(RunAllTestsSequentially());
    }

    private IEnumerator RunAllTestsSequentially()
    {
        TestFirebaseStatus();
        yield return new WaitForSeconds(1);

        TestGetToken();
        yield return new WaitForSeconds(1);

        TestLocalNotification();
        yield return new WaitForSeconds(2);

        TestScheduledNotification();
        yield return new WaitForSeconds(1);

        TestSubscribeToTopic();
        yield return new WaitForSeconds(1);

        Log("========================================");
        Log("All tests completed!");
        Log("========================================");
    }

    #endregion

    #region UI Update Methods

    private void UpdateStatus()
    {
        if (statusText != null)
        {
            string status = "FCM Status:\n";

            if (FirebaseInitializer.Instance != null)
            {
                status += $"Firebase: {(FirebaseInitializer.Instance.IsFirebaseReady ? "✅ Ready" : "⏳ Loading")}\n";
                status += $"Dependencies: {FirebaseInitializer.Instance.FirebaseDependencyStatus}\n";
            }

            if (FirebaseMessagingManager.Instance != null)
            {
                string token = FirebaseMessagingManager.Instance.GetCurrentToken();
                status += $"Token: {(string.IsNullOrEmpty(token) ? "⏳ Waiting" : "✅ Received")}\n";
            }

            if (NotificationManager.Instance != null)
            {
                status += "Notifications: ✅ Ready\n";
            }

            if (FCMTokenService.Instance != null)
            {
                status += "Token Service: ✅ Ready\n";
            }

            statusText.text = status;
        }
    }

    private void UpdateTokenDisplay(string token)
    {
        if (tokenText != null)
        {
            if (token.Length > 100)
            {
                tokenText.text = $"Token: {token.Substring(0, 100)}...";
            }
            else
            {
                tokenText.text = $"Token: {token}";
            }
        }
    }

    private void Log(string message)
    {
        Debug.Log($"[FCM Test] {message}");

        // Add to log text
        logMessages += $"\n{System.DateTime.Now:HH:mm:ss} - {message}";

        // Keep only last MAX_LOG_LINES
        string[] lines = logMessages.Split('\n');
        if (lines.Length > MAX_LOG_LINES)
        {
            logMessages = string.Join("\n", lines, lines.Length - MAX_LOG_LINES, MAX_LOG_LINES);
        }

        if (logText != null)
        {
            logText.text = logMessages;
        }
    }

    #endregion

    #region Quick Debug Menu (for testing in Editor)

    [ContextMenu("1. Check Firebase Status")]
    private void Menu_TestFirebaseStatus() => TestFirebaseStatus();

    [ContextMenu("2. Get FCM Token")]
    private void Menu_TestGetToken() => TestGetToken();

    [ContextMenu("3. Send Local Notification")]
    private void Menu_TestLocalNotification() => TestLocalNotification();

    [ContextMenu("4. Schedule Notification (10s)")]
    private void Menu_TestScheduledNotification() => TestScheduledNotification();

    [ContextMenu("5. Subscribe to Test Topic")]
    private void Menu_TestSubscribeToTopic() => TestSubscribeToTopic();

    [ContextMenu("6. Unsubscribe from Test Topic")]
    private void Menu_TestUnsubscribeFromTopic() => TestUnsubscribeFromTopic();

    [ContextMenu("7. Send Token to Backend")]
    private void Menu_TestSendTokenToBackend() => TestSendTokenToBackend();

    [ContextMenu("8. Clear All Notifications")]
    private void Menu_TestClearNotifications() => TestClearNotifications();

    [ContextMenu("9. Run All Tests")]
    private void Menu_RunAllTests() => RunAllTests();

    #endregion
}