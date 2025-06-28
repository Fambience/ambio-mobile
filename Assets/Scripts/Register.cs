using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class Register : MonoBehaviour
{
    [Header("UI Toolkit")]
    public UIDocument uiDocument;
    public UserProfileManager userProfileManager;
    public VisualElement verifyOtpScreen;
    public VisualElement selectionScreen;
    public VisualElement loaderScreen;
    public Label loaderText;

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

    private void Awake()
    {
        baseURL = baseScript.baseURL;

        var root = uiDocument.rootVisualElement;
        emailField = root.Q<TextField>("emailField");
        passwordField = root.Q<TextField>("passwordField");
        confirmPasswordField = root.Q<TextField>("confirmPasswordField");
        continueButton = root.Q<Button>("continueButton");
        backToLoginButton = root.Q<Button>("BackToLoginLabel");

        // Create and add warning labels manually
        warningEmail = CreateWarningLabel();
        warningPassword = CreateWarningLabel();
        warningConfirmPassword = CreateWarningLabel();
        warningRegister = CreateWarningLabel();

        continueButton.clicked += RegisterUser;
        backToLoginButton.clicked += () =>
        {
            root.Q<VisualElement>("RegisterScreen").style.display = DisplayStyle.None;
            root.Q<VisualElement>("LoginScreen").style.display = DisplayStyle.Flex;
        };

        emailField.RegisterValueChangedCallback(evt => ValidateEmail(evt.newValue));
        passwordField.RegisterValueChangedCallback(evt => ValidatePassword(evt.newValue));
        confirmPasswordField.RegisterValueChangedCallback(evt => ValidateConfirmPassword(evt.newValue));
    }

    private Label CreateWarningLabel()
    {
        return new Label
        {
            style =
            {
                color = Color.red,
                unityFontStyleAndWeight = FontStyle.Bold,
                marginTop = 2
            }
        };
    }

    private void ValidateEmail(string email)
    {
        warningEmail.text = emailValidator.isValidEmail(email) ? "" : "Invalid email format.";
    }

    private void ValidatePassword(string password)
    {
        if (!PasswordValidator.IsValidPassword(password, out string error))
            warningPassword.text = error;
        else
            warningPassword.text = "";
    }

    private void ValidateConfirmPassword(string confirm)
    {
        warningConfirmPassword.text = confirm != passwordField.value ? "Passwords do not match." : "";
    }

    private void RegisterUser()
    {
        string email = emailField.value.Trim();
        string password = passwordField.value.Trim();
        string confirmPassword = confirmPasswordField.value.Trim();

        ValidateEmail(email);
        ValidatePassword(password);
        ValidateConfirmPassword(confirmPassword);

        if (!string.IsNullOrEmpty(warningEmail.text) || !string.IsNullOrEmpty(warningPassword.text) || !string.IsNullOrEmpty(warningConfirmPassword.text))
            return;

        StartCoroutine(RegisterUserCoroutine(email, password));
    }

    private IEnumerator RegisterUserCoroutine(string email, string password)
    {
        ShowLoader("Registering your account...");
        string jsonData = JsonUtility.ToJson(new LoginData(email, password));

        using (UnityWebRequest request = new UnityWebRequest(baseURL + registerEndPoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();
            HideLoader();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Registration failed: " + request.error);
                messege error = JsonUtility.FromJson<messege>(request.downloadHandler.text);
                warningRegister.text = error?.message ?? "Registration failed. Try again.";
            }
            else
            {
                warningRegister.text = "";
                RegisterResponse response = JsonUtility.FromJson<RegisterResponse>(request.downloadHandler.text);
                AuthTokenManager.SetToken(response.token);
                userProfileManager.InitializeProfile(response.token);
                sendOTPFunc(email);

                uiDocument.rootVisualElement.Q<VisualElement>("RegisterScreen").style.display = DisplayStyle.None;
                verifyOtpScreen.style.display = DisplayStyle.Flex;
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
        string jsonData = JsonUtility.ToJson(new EmailData(email));
        string token = AuthTokenManager.GetToken();

        using (UnityWebRequest request = new UnityWebRequest(baseURL + sendOtpEndPoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", token);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                Debug.LogError("OTP send failed: " + request.error);
            else
                Debug.Log("OTP sent successfully.");
        }
    }

    public void VerifyOTP(string otp)
    {
        if (!string.IsNullOrEmpty(otp))
            StartCoroutine(VerifyOtpCoroutine(otp));
    }

    private IEnumerator VerifyOtpCoroutine(string otp)
    {
        ShowLoader("Verifying OTP...");
        string jsonData = JsonUtility.ToJson(new OTPData(otp));
        string token = AuthTokenManager.GetToken();

        using (UnityWebRequest request = new UnityWebRequest(baseURL + verifyOtpEndPoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", token);

            yield return request.SendWebRequest();
            HideLoader();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("OTP Verification failed: " + request.error);
            }
            else
            {
                Debug.Log("OTP Verified.");
                verifyOtpScreen.style.display = DisplayStyle.None;
                selectionScreen.style.display = DisplayStyle.Flex;
            }
        }
    }

    private void ShowLoader(string msg)
    {
        if (loaderScreen != null)
        {
            loaderText.text = msg;
            loaderScreen.style.display = DisplayStyle.Flex;
        }
    }

    private void HideLoader()
    {
        if (loaderScreen != null)
        {
            loaderText.text = "";
            loaderScreen.style.display = DisplayStyle.None;
        }
    }

    [System.Serializable]
    public class LoginData { public string email, password; public LoginData(string e, string p) { email = e; password = p; } }
    [System.Serializable]
    public class OTPData { public string otp; public OTPData(string o) { otp = o; } }
    [System.Serializable]
    public class EmailData { public string email; public EmailData(string e) { email = e; } }
    [System.Serializable]
    public class RegisterResponse { public string token; public bool success; public string message; }
    [System.Serializable]
    public class messege { public string message; }
}
