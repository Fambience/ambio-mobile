using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Services;

public class forgotPasswordController : MonoBehaviour
{
    private TextField emailField;
    private Label warningEmailLabel;
    private Label globalWarningLabel;
    private Button verifyButton;
    private Button backButton;

    private string baseUrl;

    void OnEnable()
    {
        baseUrl = baseScript.baseURL;
        var root = GetComponent<UIDocument>().rootVisualElement;

        emailField = root.Q<TextField>("emailField");
        warningEmailLabel = root.Q<Label>("WarningEmail");
        globalWarningLabel = root.Q<Label>("warningForgotPassword");
        verifyButton = root.Q<Button>("verfiyOTP");
        backButton = root.Q<Button>("BackToLoginLabel");

        emailField.RegisterValueChangedCallback(evt => ValidateEmail(evt.newValue));

        warningEmailLabel.text = "";
        globalWarningLabel.text = "";

        verifyButton.clicked += OnVerifyClicked;
        backButton.clicked += () => UIManager.Instance.OpenScreen(UIScreenType.Login);
    }

    private void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            warningEmailLabel.text = "Email is required.";
        }
        else if (!emailValidator.isValidEmail(email))
        {
            warningEmailLabel.text = "Invalid email format.";
        }
        else
        {
            warningEmailLabel.text = "";
        }
    }

    private void OnVerifyClicked()
    {
        string email = emailField.value.Trim();

        if (!emailValidator.isValidEmail(email))
        {
            warningEmailLabel.text = "Please enter a valid email.";
            return;
        }

        warningEmailLabel.text = "";
        globalWarningLabel.text = "";

        StartCoroutine(SendOtpRequest(email));
    }

private IEnumerator SendOtpRequest(string email) 
{ 
    string endpoint = "/api/v1/auth/password/send-otp"; 
    string fullUrl = baseUrl + endpoint; 
 
    Debug.Log("Request URL: " + fullUrl); 
    Debug.Log("Entered email: " + email); 
 
    var payload = $"{{\"email\":\"{email}\"}}"; 
    Debug.Log("Request payload: " + payload); 
 
    var request = new UnityWebRequest(fullUrl, "POST"); 
    byte[] bodyRaw = Encoding.UTF8.GetBytes(payload); 
 
    request.uploadHandler = new UploadHandlerRaw(bodyRaw); 
    request.downloadHandler = new DownloadHandlerBuffer(); 
    request.SetRequestHeader("Content-Type", "application/json"); 
 
    yield return request.SendWebRequest(); 
 
    Debug.Log("Request result: " + request.result); 
    Debug.Log("Response code: " + request.responseCode); 
    Debug.Log("Response text: " + request.downloadHandler.text); 
 
    if (request.result == UnityWebRequest.Result.Success) 
    { 
        string responseText = request.downloadHandler.text; 
        Debug.Log("OTP send response: " + responseText); 
 
        try 
        { 
            // Method 1: Simple string-based check (most reliable)
            if (responseText.Contains("\"success\":true") || responseText.Contains("\"success\": true"))
            {
                Debug.Log("SUCCESS! (String check) OTP sent successfully. Navigating to OTP screen.");
                UserData.Email = email;
                UIManager.Instance.OpenScreen(UIScreenType.PasswordOTP);
                request.Dispose();
                yield break;
            }
            
            // Method 2: Try Unity's JsonUtility (if available)
            try
            {
                var simpleResponse = JsonUtility.FromJson<SimpleResponse>(responseText);
                if (simpleResponse != null && simpleResponse.success)
                {
                    Debug.Log("SUCCESS! (JsonUtility) OTP sent successfully. Navigating to OTP screen.");
                    UserData.Email = email;
                    UIManager.Instance.OpenScreen(UIScreenType.PasswordOTP);
                    request.Dispose();
                    yield break;
                }
            }
            catch (System.Exception jsonUtilityException)
            {
                Debug.Log("JsonUtility failed: " + jsonUtilityException.Message);
            }
            
            // Method 3: Manual JSON parsing as fallback
            if (ParseSuccessManually(responseText))
            {
                Debug.Log("SUCCESS! (Manual parsing) OTP sent successfully. Navigating to OTP screen.");
                UserData.Email = email;
                UIManager.Instance.OpenScreen(UIScreenType.PasswordOTP);
                request.Dispose();
                yield break;
            }
 
            Debug.LogWarning("All parsing methods failed. Response: " + responseText); 
            globalWarningLabel.text = "Unable to send OTP. Please try again."; 
        } 
        catch (System.Exception e) 
        { 
            Debug.LogError("Error processing response: " + e.Message); 
            globalWarningLabel.text = "Unexpected response. Please try again."; 
        } 
    } 
    else 
    { 
        Debug.LogError("Failed to send OTP - Network error: " + request.error); 
        Debug.LogError("Response text: " + request.downloadHandler.text); 
 
        if (request.result == UnityWebRequest.Result.ConnectionError) 
        { 
            globalWarningLabel.text = "Connection error. Please check your internet."; 
        } 
        else if (request.result == UnityWebRequest.Result.ProtocolError) 
        { 
            globalWarningLabel.text = "Server error. Please try again."; 
        } 
        else 
        { 
            globalWarningLabel.text = "Something went wrong. Try again."; 
        } 
    } 
 
    request.Dispose(); 
}

// Simple response class for JsonUtility
[System.Serializable]
public class SimpleResponse
{
    public bool success;
    public string message;
}

// Manual JSON parsing method
private bool ParseSuccessManually(string jsonString)
{
    try
    {
        // Find the success field manually
        string pattern = "\"success\"";
        int successIndex = jsonString.IndexOf(pattern);
        if (successIndex == -1) return false;
        
        // Find the value after success
        int colonIndex = jsonString.IndexOf(":", successIndex);
        if (colonIndex == -1) return false;
        
        // Extract the value part
        string remainder = jsonString.Substring(colonIndex + 1);
        remainder = remainder.Trim();
        
        // Check if it starts with true
        if (remainder.StartsWith("true"))
        {
            return true;
        }
        
        return false;
    }
    catch (System.Exception e)
    {
        Debug.LogError("Manual parsing failed: " + e.Message);
        return false;
    }
}

}