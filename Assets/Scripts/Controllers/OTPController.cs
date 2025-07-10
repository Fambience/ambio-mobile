using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;

public class OTPController : MonoBehaviour
{
    [Header("References")]
    public UIDocument uiDocument;
    
    
    private string verifyOtpEndPoint = "/api/v1/user/verify-otp";

    private string baseURL;
    private List<TextField> otpFields = new List<TextField>();
    private Label warningLabel;
    private Button verifyButton;

    private const string otpBoxClass = "otp-box";
    private const string otpErrorClass = "otp-box-error";
    private const string warningMessage = "Invalid OTP. Please try again.";

    private void Awake()
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

                // Navigate to next screen (e.g., profile setup)
                UIManager.Instance.OpenScreen(UIScreenType.BasicDetails);
            }
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
    public class OTPData { public string otp; public OTPData(string o) { otp = o; } }
}
