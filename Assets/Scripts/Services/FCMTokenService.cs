using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class FCMTokenService : MonoBehaviour
{
    public static FCMTokenService Instance { get; private set; }

    [Header("Backend Configuration")]
    [Tooltip("The backend API endpoint to send FCM tokens to")]
    public string tokenRegistrationEndpoint = "https://your-backend-api.com/api/fcm/register";

    private const string TOKEN_PREFS_KEY = "FCM_Token";
    private const string TOKEN_SENT_PREFS_KEY = "FCM_Token_Sent";

    private string currentToken = "";
    private bool isTokenSentToBackend = false;

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
        // Load saved token
        LoadTokenFromPrefs();

        // Subscribe to token updates from FirebaseMessagingManager
        if (FirebaseMessagingManager.Instance != null)
        {
            FirebaseMessagingManager.Instance.OnTokenReceived += HandleTokenReceived;
        }
        else
        {
            Debug.LogWarning("[FCMTokenService] FirebaseMessagingManager not found. Make sure it's in the scene.");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (FirebaseMessagingManager.Instance != null)
        {
            FirebaseMessagingManager.Instance.OnTokenReceived -= HandleTokenReceived;
        }
    }

    /// <summary>
    /// Handle token received from Firebase
    /// </summary>
    private void HandleTokenReceived(string token)
    {
        Debug.Log("[FCMTokenService] Received new FCM token: " + token);

        // Check if token has changed
        if (currentToken != token)
        {
            currentToken = token;
            SaveTokenToPrefs(token);

            // Reset sent status since we have a new token
            isTokenSentToBackend = false;
            PlayerPrefs.SetInt(TOKEN_SENT_PREFS_KEY, 0);
            PlayerPrefs.Save();

            // Send to backend
            SendTokenToBackend(token);
        }
        else
        {
            Debug.Log("[FCMTokenService] Token unchanged, checking if sent to backend");
            // Token is the same, but check if it was sent to backend
            if (!isTokenSentToBackend)
            {
                SendTokenToBackend(token);
            }
        }
    }

    /// <summary>
    /// Send FCM token to backend server
    /// </summary>
    public void SendTokenToBackend(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("[FCMTokenService] Cannot send empty token to backend");
            return;
        }

        if (string.IsNullOrEmpty(tokenRegistrationEndpoint) || tokenRegistrationEndpoint.Contains("your-backend-api"))
        {
            Debug.LogWarning("[FCMTokenService] Backend endpoint not configured. Skipping token registration.");
            return;
        }

        StartCoroutine(SendTokenRequest(token));
    }

    /// <summary>
    /// Coroutine to send token to backend
    /// </summary>
    private IEnumerator SendTokenRequest(string token)
    {
        Debug.Log("[FCMTokenService] Sending token to backend: " + tokenRegistrationEndpoint);

        // Get user ID from AuthTokenManager if available
        string userId = GetUserId();

        // Create JSON payload
        var payload = new FCMTokenPayload
        {
            fcm_token = token,
            user_id = userId,
            platform = Application.platform.ToString(),
            device_model = SystemInfo.deviceModel,
            os_version = SystemInfo.operatingSystem
        };

        string jsonPayload = JsonUtility.ToJson(payload);
        Debug.Log("[FCMTokenService] Payload: " + jsonPayload);

        // Create web request
        using (UnityWebRequest request = new UnityWebRequest(tokenRegistrationEndpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Add authorization header if user is logged in
            string authToken = GetAuthToken();
            if (!string.IsNullOrEmpty(authToken))
            {
                request.SetRequestHeader("Authorization", "Bearer " + authToken);
            }

            // Send request
            yield return request.SendWebRequest();

            // Handle response
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[FCMTokenService] Token successfully sent to backend");
                Debug.Log("[FCMTokenService] Response: " + request.downloadHandler.text);

                isTokenSentToBackend = true;
                PlayerPrefs.SetInt(TOKEN_SENT_PREFS_KEY, 1);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogError($"[FCMTokenService] Failed to send token: {request.error}");
                Debug.LogError($"[FCMTokenService] Response Code: {request.responseCode}");
                Debug.LogError($"[FCMTokenService] Response: {request.downloadHandler.text}");

                isTokenSentToBackend = false;
                PlayerPrefs.SetInt(TOKEN_SENT_PREFS_KEY, 0);
                PlayerPrefs.Save();
            }
        }
    }
    
    private string GetUserId()
    {
        // Get user ID from PlayerPrefs
        // Adjust this based on where your app stores the user ID
        return PlayerPrefs.GetString("UserId", "");
    }

    private string GetAuthToken()
    {
        // AuthTokenManager is a static class, call GetToken directly
        return AuthTokenManager.GetToken();
    }

    private void SaveTokenToPrefs(string token)
    {
        PlayerPrefs.SetString(TOKEN_PREFS_KEY, token);
        PlayerPrefs.Save();
        Debug.Log("[FCMTokenService] Token saved to PlayerPrefs");
    }

    private void LoadTokenFromPrefs()
    {
        currentToken = PlayerPrefs.GetString(TOKEN_PREFS_KEY, "");
        isTokenSentToBackend = PlayerPrefs.GetInt(TOKEN_SENT_PREFS_KEY, 0) == 1;

        if (!string.IsNullOrEmpty(currentToken))
        {
            Debug.Log("[FCMTokenService] Loaded saved token: " + currentToken);
            Debug.Log("[FCMTokenService] Token sent to backend: " + isTokenSentToBackend);
        }
    }

    public string GetCurrentToken()
    {
        return currentToken;
    }

    public void ResendTokenToBackend()
    {
        if (!string.IsNullOrEmpty(currentToken))
        {
            isTokenSentToBackend = false;
            SendTokenToBackend(currentToken);
        }
        else
        {
            Debug.LogWarning("[FCMTokenService] No token available to resend");
        }
    }
    
    public void DeleteTokenFromBackend()
    {
        if (string.IsNullOrEmpty(currentToken))
        {
            Debug.LogWarning("[FCMTokenService] No token to delete");
            return;
        }

        StartCoroutine(DeleteTokenRequest(currentToken));
    }
    
    private IEnumerator DeleteTokenRequest(string token)
    {
        string deleteEndpoint = tokenRegistrationEndpoint.Replace("/register", "/delete");
        Debug.Log("[FCMTokenService] Deleting token from backend: " + deleteEndpoint);

        var payload = new FCMTokenPayload
        {
            fcm_token = token
        };

        string jsonPayload = JsonUtility.ToJson(payload);

        using (UnityWebRequest request = new UnityWebRequest(deleteEndpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            string authToken = GetAuthToken();
            if (!string.IsNullOrEmpty(authToken))
            {
                request.SetRequestHeader("Authorization", "Bearer " + authToken);
            }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[FCMTokenService] Token successfully deleted from backend");
                PlayerPrefs.DeleteKey(TOKEN_PREFS_KEY);
                PlayerPrefs.DeleteKey(TOKEN_SENT_PREFS_KEY);
                PlayerPrefs.Save();

                currentToken = "";
                isTokenSentToBackend = false;
            }
            else
            {
                Debug.LogError($"[FCMTokenService] Failed to delete token: {request.error}");
            }
        }
    }
}

[System.Serializable]
public class FCMTokenPayload
{
    public string fcm_token;
    public string user_id;
    public string platform;
    public string device_model;
    public string os_version;
}
