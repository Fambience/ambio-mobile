using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class Register : MonoBehaviour
{
    [Header("UI Toolkit")]
    public UIDocument uiDocument;
    //public UserProfileManager userProfileManager;

    private TextField emailField;
    private TextField passwordField;
    private TextField confirmPasswordField;
    private Button continueButton;
    private Button backToLoginButton;

    private Label warningEmail;
    private Label warningPassword;
    private Label warningConfirmPassword;
    private Label warningRegister;

    private string baseURL;
    private string token;

    private string registerEndPoint = "/api/v1/auth/register";
    private string sendOtpEndPoint = "/api/v1/user/trigger-otp";
    private string verifyOtpEndPoint = "/api/v1/user/verify-otp";

    private void OnEnable()
    {
        baseURL = baseScript.baseURL;

        var root = uiDocument.rootVisualElement;
        emailField = root.Q<TextField>("emailField");
        passwordField = root.Q<TextField>("passwordField");
        confirmPasswordField = root.Q<TextField>("confirmPasswordField");
        continueButton = root.Q<Button>("continueButton");
        backToLoginButton = root.Q<Button>("BackToLoginLabel");

        // Bind pre-defined warning labels
        warningEmail = root.Q<Label>("WarningEmail");
        warningPassword = root.Q<Label>("WarningPassword");
        warningConfirmPassword = root.Q<Label>("WarningConfirmPassword");
        warningRegister = root.Q<Label>("WarningRegister");

        emailField.RegisterValueChangedCallback(evt => ValidateEmail(evt.newValue));
        passwordField.RegisterValueChangedCallback(evt => ValidatePassword(evt.newValue));
        confirmPasswordField.RegisterValueChangedCallback(evt => ValidateConfirmPassword(evt.newValue));

        continueButton.clicked += RegisterUser;

        backToLoginButton?.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("Back to Login Called");
            UIManager.Instance.OpenScreen(UIScreenType.Login);
        });
    }

    private void ValidateEmail(string email)
    {
        if (!emailValidator.isValidEmail(email))
        {
            warningEmail.text = "Invalid email format.";
            Debug.LogWarning(warningEmail.text);
        }
        else
        {
            warningEmail.text = "";
        }
    }

    private void ValidatePassword(string password)
    {
        if (!PasswordValidator.IsValidPassword(password, out string error))
        {
            warningPassword.text = error;
            Debug.LogWarning(error);
        }
        else
        {
            warningPassword.text = "";
        }
    }

    private void ValidateConfirmPassword(string confirm)
    {
        if (confirm != passwordField.value)
        {
            warningConfirmPassword.text = "Passwords do not match.";
            Debug.LogWarning(warningConfirmPassword.text);
        }
        else
        {
            warningConfirmPassword.text = "";
        }
    }

    private void RegisterUser()
    {
        string email = emailField.value.Trim();
        string password = passwordField.value.Trim();
        string confirmPassword = confirmPasswordField.value.Trim();

        ValidateEmail(email);
        ValidatePassword(password);
        ValidateConfirmPassword(confirmPassword);

        if (!string.IsNullOrEmpty(warningEmail.text) ||
            !string.IsNullOrEmpty(warningPassword.text) ||
            !string.IsNullOrEmpty(warningConfirmPassword.text))
        {
            Debug.LogWarning("Validation failed. Registration aborted.");
            return;
        }

        Debug.Log("Sending registration request...");
        StartCoroutine(RegisterUserCoroutine(email, password));
    }

    private IEnumerator RegisterUserCoroutine(string email, string password)
    {
        // ShowLoader("Registering your account...");
        string jsonData = JsonUtility.ToJson(new LoginData(email, password));

        using (UnityWebRequest request = new UnityWebRequest(baseURL + registerEndPoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            // HideLoader();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Registration failed: " + request.error);
                messege error = JsonUtility.FromJson<messege>(request.downloadHandler.text);
                warningRegister.text = error?.message ?? "Registration failed. Try again.";
            }
            else
            {
                warningRegister.text = "";
                var response = JsonUtility.FromJson<RegisterResponse>(request.downloadHandler.text);
                Debug.Log("Registration successful. Token: " + response.token);
                AuthTokenManager.SetToken(response.token);
                //userProfileManager.InitializeProfile(response.token);
                sendOTPFunc(email);
                //UIManager.Instance.OpenScreen(UIScreenType.OTP); 
            }
        }
    }

    public void sendOTPFunc(string email)
    {
        Debug.Log("Sending OTP for: " + email);
        StartCoroutine(SendOtpCoroutine(email));
    }

    private IEnumerator SendOtpCoroutine(string email)
    {
        // TODO: Show loading screen
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

            // TODO: Hide loading screen

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("OTP send failed (network issue): " + request.error);
                warningRegister.text = "Failed to send OTP. Please try again.";
                UIManager.Instance.OpenScreen(UIScreenType.Register);
            }
            else
            {
                string responseText = request.downloadHandler.text;
                Debug.Log("OTP Response: " + responseText);

                OTPResponse otpResponse = JsonUtility.FromJson<OTPResponse>(responseText);

                if (otpResponse != null && otpResponse.success)
                {
                    Debug.Log("OTP sent successfully. Opening OTP screen...");
                    UIManager.Instance.OpenScreen(UIScreenType.OTP);
                }
                else
                {
                    string errorMsg = otpResponse?.message ?? "Failed to send OTP.";
                    Debug.LogWarning("OTP response failure: " + errorMsg);
                    warningRegister.text = errorMsg;
                    UIManager.Instance.OpenScreen(UIScreenType.Register);
                }
            }
        }
    }



    [System.Serializable] public class LoginData { public string email, password; public LoginData(string e, string p) { email = e; password = p; } }
    
    [System.Serializable]
    public class OTPResponse
    {
        public bool success;
        public string message;
    }

    [System.Serializable] public class OTPData { public string otp; public OTPData(string o) { otp = o; } }
    [System.Serializable] public class EmailData { public string email; public EmailData(string e) { email = e; } }
    [System.Serializable] public class RegisterResponse { public string token; public bool success; public string message; }
    [System.Serializable] public class messege { public string message; }
}
