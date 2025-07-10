using System.Collections;
using System.Collections.Generic;
using Services;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using MiniJSON;
public class OTPSCreenPassword : MonoBehaviour
{
    [Header("References")]
    public UIDocument uiDocument;
    
    private string verifyOtpEndPoint = "/api/v1/auth/password/verify-otp";

    private string baseURL=baseScript.baseURL;
    private List<TextField> otpFields = new List<TextField>();
    private Label warningLabel;
    private Button verifyButton;

    private const string otpBoxClass = "otp-box";
    private const string otpErrorClass = "otp-box-error";
    private const string warningMessage = "Invalid OTP. Please try again.";
    
    
    private void Awake()
    {
        var root = uiDocument.rootVisualElement;
        for (int i = 0; i < 6; i++)
        {
            var field = root.Q<TextField>($"otp-{i}");
            if (field != null)
            {
                otpFields.Add(field);
                int index = i;

                field.RegisterCallback<ChangeEvent<string>>(evt =>
                {
                    if (!string.IsNullOrEmpty(evt.newValue) && index < 5)
                    {
                        otpFields[index + 1].Focus();
                    }
                });

                field.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Backspace && string.IsNullOrEmpty(field.value) && index > 0)
                    {
                        otpFields[index - 1].Focus();
                    }
                });
            }
        }

        warningLabel = root.Q<Label>("warningLabelRole");
        verifyButton = root.Q<Button>("verfiyOTP");

        if (verifyButton != null)
        {
            verifyButton.clicked += HandleVerifyClicked;
        }

        ResetOtpUI();
    }

    private void HandleVerifyClicked()
    {
        string otp = "";
        foreach (var field in otpFields)
        {
            otp += field.value;
        }

        Debug.Log("Entered OTP: " + otp);

        if (otp.Length == 6)
        {
            StartCoroutine(VerifyOtpCoroutine(otp));
        }
        else
        {
            SetOtpErrorState(true);
            warningLabel.text = warningMessage;
        }
    }

    private IEnumerator VerifyOtpCoroutine(string otp)
{
    string finalURL = baseURL + verifyOtpEndPoint;
    Debug.Log("Request URL: " + finalURL);
    string email = PasswordResetSession.Email;
    Debug.Log(email);

    if (string.IsNullOrEmpty(email))
    {
        Debug.LogError("Missing email. Cannot verify OTP.");
        warningLabel.text = "Something went wrong. Please restart the process.";
        yield break;
    }

    string jsonData = JsonUtility.ToJson(new OTPData(otp, email));
    Debug.Log("Request payload: " + jsonData);

    using (UnityWebRequest request = new UnityWebRequest(finalURL, "POST"))
    {
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        Debug.Log("Request result: " + request.result);
        Debug.Log("Response code: " + request.responseCode);
        Debug.Log("Response text: " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            Debug.Log("OTP Verified successfully: " + responseText);

            SetOtpErrorState(false);
            warningLabel.text = "";

            try
            {
                // Method 1: Simple string-based check for success
                if (responseText.Contains("\"success\":true") || responseText.Contains("\"success\": true"))
                {
                    Debug.Log("Success field found in response");
                    
                    // Extract reset token using string methods
                    string resetToken = ExtractResetToken(responseText);
                    
                    if (!string.IsNullOrEmpty(resetToken))
                    {
                        PasswordResetSession.ResetToken = resetToken;
                        Debug.Log("Reset token received: " + resetToken);
                        UIManager.Instance.OpenScreen(UIScreenType.PasswordReset);
                        yield break;
                    }
                    else
                    {
                        Debug.LogWarning("Reset token not found in response");
                    }
                }

                // Method 2: Try Unity's JsonUtility as fallback
                try
                {
                    var response = JsonUtility.FromJson<OTPVerificationResponse>(responseText);
                    if (response != null && response.success && !string.IsNullOrEmpty(response.resetToken))
                    {
                        PasswordResetSession.ResetToken = response.resetToken;
                        Debug.Log("Reset token received via JsonUtility: " + response.resetToken);
                        UIManager.Instance.OpenScreen(UIScreenType.PasswordReset);
                        yield break;
                    }
                }
                catch (System.Exception jsonUtilityException)
                {
                    Debug.Log("JsonUtility failed: " + jsonUtilityException.Message);
                }

                // Method 3: Try MiniJSON as last resort
                try
                {
                    var response = MiniJSON.JSON.Deserialize(responseText) as Dictionary<string, object>;
                    if (response != null && response.TryGetValue("resetToken", out object tokenObj))
                    {
                        PasswordResetSession.ResetToken = tokenObj.ToString();
                        Debug.Log("Reset token received via MiniJSON: " + PasswordResetSession.ResetToken);
                        UIManager.Instance.OpenScreen(UIScreenType.PasswordReset);
                        yield break;
                    }
                }
                catch (System.Exception miniJsonException)
                {
                    Debug.Log("MiniJSON failed: " + miniJsonException.Message);
                }

                Debug.LogWarning("All parsing methods failed. Response: " + responseText);
                warningLabel.text = "Unexpected response. Please try again.";
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error processing response: " + e.Message);
                warningLabel.text = "Unable to process server response.";
            }
        }
        else
        {
            Debug.LogError("OTP Verification failed: " + request.downloadHandler.text);
            SetOtpErrorState(true);
            warningLabel.text = warningMessage;
        }
    }
}

// Helper method to extract reset token using string manipulation
private string ExtractResetToken(string jsonResponse)
{
    try
    {
        string pattern = "\"resetToken\"";
        int tokenIndex = jsonResponse.IndexOf(pattern);
        if (tokenIndex == -1) return null;
        
        // Find the value after resetToken
        int colonIndex = jsonResponse.IndexOf(":", tokenIndex);
        if (colonIndex == -1) return null;
        
        // Find the opening quote
        int openQuoteIndex = jsonResponse.IndexOf("\"", colonIndex);
        if (openQuoteIndex == -1) return null;
        
        // Find the closing quote
        int closeQuoteIndex = jsonResponse.IndexOf("\"", openQuoteIndex + 1);
        if (closeQuoteIndex == -1) return null;
        
        // Extract the token
        string token = jsonResponse.Substring(openQuoteIndex + 1, closeQuoteIndex - openQuoteIndex - 1);
        return token;
    }
    catch (System.Exception e)
    {
        Debug.LogError("Error extracting reset token: " + e.Message);
        return null;
    }
}

// Response class for JsonUtility
[System.Serializable]
public class OTPVerificationResponse
{
    public bool success;
    public string message;
    public string resetToken;
}




    private void SetOtpErrorState(bool error)
    {
        foreach (var field in otpFields)
        {
            if (error)
            {
                if (!field.ClassListContains(otpErrorClass))
                    field.AddToClassList(otpErrorClass);
            }
            else
            {
                field.RemoveFromClassList(otpErrorClass);
            }
        }
    }

    private void ResetOtpUI()
    {
        foreach (var field in otpFields)
        {
            field.value = "";
            field.RemoveFromClassList(otpErrorClass);
        }

        warningLabel.text = "";
    }

    [System.Serializable]
    public class OTPData
    {
        public string otp;
        public string email;

        public OTPData(string o, string e)
        {
            otp = o;
            email = e;
        }
    }

}
