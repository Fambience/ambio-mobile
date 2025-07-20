using System.Collections;
using System.Collections.Generic;
using Services;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;

public class OTPController : MonoBehaviour
{
    [Header("References")]
    public UIDocument uiDocument;

    private string verifyOtpEndPoint = "/api/v1/user/verify-otp";
    private string sendOtpEndPoint = "/api/v1/user/send-otp";
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
        resendButton = root.Q<Button>("ResendOTPButton");
        warningLabel = root.Q<Label>("warningLabelRole");
        verifyButton = root.Q<Button>("verfiyOTP");

        if (resendButton != null)
        {
            resendButton.clicked += () =>
            {
                ResendOTP();
                StartTimer();
            };
        }

        if (verifyButton != null)
        {
            verifyButton.clicked += HandleVerifyClicked;
        }

        ResetOtpUI();
        StartTimer(); // Start the resend countdown when screen opens
    }

    private void StartTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        resendButton.style.display = DisplayStyle.None;
        timerCoroutine = StartCoroutine(ResendCountdown(countdownSeconds));
    }

    private IEnumerator ResendCountdown(int totalSeconds)
    {
        int remaining = totalSeconds;
        while (remaining > 0)
        {
            int minutes = remaining / 60;
            int seconds = remaining % 60;
            resendTimeLabel.text = $"{minutes:00}:{seconds:00}";
            yield return new WaitForSeconds(1f);
            remaining--;
        }

        resendTimeLabel.text = "00:00";
        resendButton.style.display = DisplayStyle.Flex;
    }

    private void ResendOTP()
    {
        if (!string.IsNullOrEmpty(PasswordResetSession.Email))
        {
            StartCoroutine(SendOtpCoroutine(PasswordResetSession.Email));
        }
        else
        {
            warningLabel.text = "Unable to send OTP. Please restart the process.";
        }
    }

    private IEnumerator SendOtpCoroutine(string email)
    {
        Debug.Log("Sending OTP request...");

        string jsonData = JsonUtility.ToJson(new EmailData(email));
        string token = AuthTokenManager.GetToken();

        using (UnityWebRequest request = new UnityWebRequest(baseURL + sendOtpEndPoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", token);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                warningLabel.text = "Failed to send OTP. Please try again.";
                UIManager.Instance.OpenScreen(UIScreenType.Register);
            }
            else
            {
                OTPResponse otpResponse = JsonUtility.FromJson<OTPResponse>(request.downloadHandler.text);
                if (otpResponse != null && otpResponse.success)
                {
                    Debug.Log("OTP sent successfully.");
                }
                else
                {
                    warningLabel.text = otpResponse?.message ?? "Failed to send OTP.";
                    UIManager.Instance.OpenScreen(UIScreenType.Register);
                }
            }
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
        string jsonData = JsonUtility.ToJson(new OTPData(otp));
        string token = AuthTokenManager.GetToken();

        using (UnityWebRequest request = new UnityWebRequest(baseURL + verifyOtpEndPoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", token);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("OTP Verification failed: " + request.error);
                SetOtpErrorState(true);
                warningLabel.text = warningMessage;
            }
            else
            {
                Debug.Log("OTP Verified successfully.");
                SetOtpErrorState(false);
                warningLabel.text = "";

                UIManager.Instance.OpenScreen(UIScreenType.BasicDetails);
            }
        }
    }

    private void SetOtpErrorState(bool error)
    {
        foreach (var field in otpFields)
        {
            if (error && !field.ClassListContains(otpErrorClass))
            {
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
    public class OTPData { public string otp; public OTPData(string o) { otp = o; } }

    [System.Serializable]
    public class OTPResponse { public bool success; public string message; }

    [System.Serializable]
    public class EmailData { public string email; public EmailData(string e) { email = e; } }
}
