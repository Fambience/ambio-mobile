using System.Collections;
using System.Collections.Generic;
using System.Text;
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
    private string sendOtpEndPoint = "/api/v1/auth/password/send-otp";
    private string baseURL;

    private List<TextField> otpFields = new List<TextField>();
    private Label warningLabel;
    private Button verifyButton;
    private Button resendButton;
    private Label resendTimeLabel;
    private Label resendTextLabel;

    private const string otpBoxClass = "otp-box";
    private const string otpErrorClass = "otp-box-error";
    private const string warningMessage = "Invalid OTP. Please try again.";

    private Coroutine timerCoroutine;
    private readonly int countdownSeconds = 120;

    private void OnEnable()
    {
        baseURL = baseScript.baseURL;
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

        resendTextLabel = root.Q<Label>("ResendOTPText");
        resendTimeLabel = root.Q<Label>("ResendOTPTimer");
        warningLabel = root.Q<Label>("warningLabelRole");
        verifyButton = root.Q<Button>("verfiyOTP");
        resendButton = root.Q<Button>("ResendOTPButton");

        if (verifyButton != null)
        {
            verifyButton.clicked += HandleVerifyClicked;
        }

        if (resendButton != null)
        {
            resendButton.clicked += () =>
            {
                ResendOTP();
                StartTimer();
            };
        }

        ResetOtpUI();
        StartTimer(); // Start timer on screen enable
    }

    private void StartTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        resendButton.style.display = DisplayStyle.None;
        timerCoroutine = StartCoroutine(TimerCountdown(countdownSeconds));
    }

    private IEnumerator TimerCountdown(int totalSeconds)
    {
        int secondsRemaining = totalSeconds;

        while (secondsRemaining > 0)
        {
            int minutes = secondsRemaining / 60;
            int seconds = secondsRemaining % 60;
            resendTimeLabel.text = $"{minutes:00}:{seconds:00}";
            yield return new WaitForSeconds(1f);
            secondsRemaining--;
        }

        resendTimeLabel.text = "00:00";
        resendButton.style.display = DisplayStyle.Flex;
    }

    private IEnumerator SendOtpRequest(string email)
    {
        string payload = $"{{\"email\":\"{email}\"}}";
        var request = new UnityWebRequest(baseURL + sendOtpEndPoint, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("OTP sent successfully.");
        }
        else
        {
            if (request.result == UnityWebRequest.Result.ConnectionError)
                warningLabel.text = "Connection error. Please check your internet.";
            else if (request.result == UnityWebRequest.Result.ProtocolError)
                warningLabel.text = "Server error. Please try again.";
            else
                warningLabel.text = "Something went wrong. Try again.";
        }

        request.Dispose();
    }

    private void ResendOTP()
    {
        if (!string.IsNullOrEmpty(PasswordResetSession.Email))
        {
            StartCoroutine(SendOtpRequest(PasswordResetSession.Email));
        }
        else
        {
            warningLabel.text = "Unable to resend OTP. Please restart the process.";
        }
    }

    private void HandleVerifyClicked()
    {
        string otp = "";
        foreach (var field in otpFields)
        {
            otp += field.value;
        }

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
        string email = PasswordResetSession.Email;
        if (string.IsNullOrEmpty(email))
        {
            warningLabel.text = "Something went wrong. Please restart the process.";
            yield break;
        }

        string jsonData = JsonUtility.ToJson(new OTPData(otp, email));
        var request = new UnityWebRequest(baseURL + verifyOtpEndPoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            SetOtpErrorState(false);
            warningLabel.text = "";

            try
            {
                if (responseText.Contains("\"success\":true"))
                {
                    string token = ExtractResetToken(responseText);
                    if (!string.IsNullOrEmpty(token))
                    {
                        PasswordResetSession.ResetToken = token;
                        UIManager.Instance.OpenScreen(UIScreenType.PasswordReset);
                        yield break;
                    }
                }

                var jsonResponse = JsonUtility.FromJson<OTPVerificationResponse>(responseText);
                if (jsonResponse != null && jsonResponse.success && !string.IsNullOrEmpty(jsonResponse.resetToken))
                {
                    PasswordResetSession.ResetToken = jsonResponse.resetToken;
                    UIManager.Instance.OpenScreen(UIScreenType.PasswordReset);
                    yield break;
                }

                var miniJson = MiniJSON.JSON.Deserialize(responseText) as Dictionary<string, object>;
                if (miniJson != null && miniJson.TryGetValue("resetToken", out object tokenObj))
                {
                    PasswordResetSession.ResetToken = tokenObj.ToString();
                    UIManager.Instance.OpenScreen(UIScreenType.PasswordReset);
                    yield break;
                }

                warningLabel.text = "Unexpected response. Please try again.";
            }
            catch
            {
                warningLabel.text = "Unable to process server response.";
            }
        }
        else
        {
            SetOtpErrorState(true);
            warningLabel.text = warningMessage;
        }

        request.Dispose();
    }

    private string ExtractResetToken(string jsonResponse)
    {
        try
        {
            string pattern = "\"resetToken\"";
            int tokenIndex = jsonResponse.IndexOf(pattern);
            if (tokenIndex == -1) return null;

            int colonIndex = jsonResponse.IndexOf(":", tokenIndex);
            int openQuoteIndex = jsonResponse.IndexOf("\"", colonIndex);
            int closeQuoteIndex = jsonResponse.IndexOf("\"", openQuoteIndex + 1);

            return jsonResponse.Substring(openQuoteIndex + 1, closeQuoteIndex - openQuoteIndex - 1);
        }
        catch
        {
            return null;
        }
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
        public OTPData(string o, string e) { otp = o; email = e; }
    }

    [System.Serializable]
    public class OTPVerificationResponse
    {
        public bool success;
        public string message;
        public string resetToken;
    }
}
