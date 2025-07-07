using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System.Text.RegularExpressions;
using System.Text;

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
        backButton.clicked += () => UIManager.Instance.OpenScreen(UIScreenType.Login); // example screen transition
    }
    
    private void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            warningEmailLabel.text = "Email is required.";
        else if (!emailValidator.isValidEmail(email))
            warningEmailLabel.text = "Invalid email format.";
        else
            warningEmailLabel.text = string.Empty;
    }

    void OnVerifyClicked()
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

    IEnumerator SendOtpRequest(string email)
    {
        string endpoint = "/api/v1/auth/password/send-otp";
        string fullUrl = baseUrl + endpoint;

        var payload = $"{{\"email\":\"{email}\"}}";
        var request = new UnityWebRequest(fullUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Trigger your own loader here (UIManager.Instance.ShowLoader() etc.)
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("OTP sent successfully: " + request.downloadHandler.text);
            UIManager.Instance.OpenScreen(UIScreenType.PasswordOTP); // Or whatever screen you go to next
        }
        else
        {
            Debug.LogError("Failed to send OTP: " + request.downloadHandler.text);
            globalWarningLabel.text = "Something went wrong. Please try again.";
        }
    }
}
