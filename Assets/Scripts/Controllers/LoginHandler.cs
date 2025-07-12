using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class LoginHandler : MonoBehaviour
{
    [Header("UI Toolkit")]
    public UIDocument uiDocument;

    [Header("External References")]
    public Register register;
    public UserProfileManager userProfileManager;
    public VisualTreeAsset registerUXMLAsset;

    private VisualElement loginScreen;
    private VisualElement registerScreen;

    [Header("Screens")]
    public VisualElement otpScreen;
    public VisualElement onboardingScreen;
    public VisualElement homeScreen;
    public VisualElement loaderScreen;
    public Label loaderText;

    private TextField emailField;
    private TextField passwordField;
    private Button loginButton;
    private Image eyeIcon;

    private Label warningEmailText;
    private Label warningPasswordText;
    private VisualElement loginWarningContainer;

    private string userEmail;
    private string baseURL = baseScript.baseURL;
    private string loginEndPoint = "/api/v1/auth/login";
    private string sendOtpEndPoint = "/api/v1/user/trigger-otp";

    private bool isPasswordVisible = false;
    
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        loginScreen = root.Q<VisualElement>("LoginScreen");
        emailField = root.Q<TextField>("emailField");
        passwordField = root.Q<TextField>("passwordField");
        loginButton = root.Q<Button>("signInButton");
        eyeIcon = root.Q<Image>("eyeIcon");

        // Ensure password field is initially hidden
        passwordField.isPasswordField = true;
        
        // Initialize eye icon to show the correct initial state
        eyeIcon.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("eye-off-outline"));

        warningEmailText = root.Q<Label>("WarningEmail");
        warningPasswordText = root.Q<Label>("WarningPassword");
        loginWarningContainer = root.Q<VisualElement>("LoginWarningContainer");

        emailField.RegisterValueChangedCallback(evt =>
        {
            if (string.IsNullOrWhiteSpace(evt.newValue))
                ShowEmailWarning("Email is required.");
            else
                ClearEmailWarning();
        });

        passwordField.RegisterValueChangedCallback(evt =>
        {
            if (string.IsNullOrWhiteSpace(evt.newValue))
                ShowPasswordWarning("Password is required.");
            else
                ClearPasswordWarning();
        });

        loginButton.clicked += LoginUser;

        // Register eye icon click event for password visibility toggle
        eyeIcon.RegisterCallback<ClickEvent>(_ => TogglePasswordVisibility());
        
        Label forgotPassword = root.Q<Label>("forgotPassword");
        forgotPassword.RegisterCallback<ClickEvent>(evt => showForgotPassword());

        Label signUpLabel = root.Q<Label>("SignUpLabel");
        signUpLabel.RegisterCallback<ClickEvent>(evt => ShowRegisterScreen());

        PlayerPrefs.DeleteAll();
    }

    private void TogglePasswordVisibility()
    {
        isPasswordVisible = !isPasswordVisible;
        
        if (isPasswordVisible)
        {
            // Show password - change to text field and update icon
            passwordField.isPasswordField = false;
            eyeIcon.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("eye-outline"));
        }
        else
        {
            // Hide password - change to password field and update icon
            passwordField.isPasswordField = true;
            eyeIcon.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("eye-off-outline"));
        }
    }

    private void ShowEmailWarning(string msg)
    {
        warningEmailText.text = msg;
        warningEmailText.AddToClassList("show");
    }

    private void ShowPasswordWarning(string msg)
    {
        warningPasswordText.text = msg;
        warningPasswordText.AddToClassList("show");
    }

    private void ClearEmailWarning()
    {
        warningEmailText.text = "";
        warningEmailText.RemoveFromClassList("show");
    }

    private void ClearPasswordWarning()
    {
        warningPasswordText.text = "";
        warningPasswordText.RemoveFromClassList("show");
    }

    private void LoginUser()
    {
        string email = emailField.value?.Trim();
        string password = passwordField.value?.Trim();
        bool hasError = false;

        loginWarningContainer.Clear();

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowEmailWarning("Email is required.");
            hasError = true;
        }
        else if (!emailValidator.isValidEmail(email))
        {
            ShowEmailWarning("Invalid email format.");
            hasError = true;
        }
        else
        {
            ClearEmailWarning();
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowPasswordWarning("Password is required.");
            hasError = true;
        }
        else
        {
            ClearPasswordWarning();
        }

        if (hasError) return;

        StartCoroutine(LoginUserCoroutine(email, password));
    }

    private IEnumerator LoginUserCoroutine(string email, string password)
    {
        SetUIInteractable(false);
        string jsonData = JsonUtility.ToJson(new LoginData(email, password));

        using (UnityWebRequest request = new UnityWebRequest(baseURL + loginEndPoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();
            HideLoader();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Login Error: {request.error}");
                messege error = JsonUtility.FromJson<messege>(request.downloadHandler.text);

                Label apiError = new Label(error?.message ?? "Login failed. Please try again.");
                apiError.AddToClassList("login-error-message");

                loginWarningContainer.Clear();
                loginWarningContainer.Add(apiError);
                SetUIInteractable(true);
                yield break;
            }

            loginWarningContainer.Clear();

            var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            string token = response.token;

            AuthTokenManager.SetToken(token);
            userEmail = email;

            PlayerPrefs.SetString("email", email);
            PlayerPrefs.SetString("password", password);
            PlayerPrefs.SetString("role", response.data.role);
            PlayerPrefs.Save();

            HandleLoginStage(response);
        }
        SetUIInteractable(true);
    }

    private void ShowRegisterScreen()
    {
        UIManager.Instance.OpenScreen(UIScreenType.Register);
    }

    public void showForgotPassword()
    {
        UIManager.Instance.OpenScreen(UIScreenType.forgotPassword);
    }

    private void HandleLoginStage(LoginResponse response)
    {
        switch (response.data.onboardingState)
        {
            case "VERIFY_EMAIL":
                sendOTPFunc(userEmail);
                break;
            case "ONBOARD_DETAILS":
                HandleDynamicOnboardingFromQuestions(response.data.remainingQuestions);
                break;
            
            case "ONBOARDING_PARTIALLY_COMPLETED":
                HandleDynamicOnboardingFromQuestions(response.data.remainingQuestions);
                break;

            case "ONBOARDING_COMPLETED":
                if (!string.IsNullOrEmpty(response.token))
                    userProfileManager.InitializeProfile(response.token);
                break;

            case "BASIC_DETAILS":
                UIManager.Instance.OpenScreen(UIScreenType.BasicDetails);
                break;

            default:
                Debug.LogError("Unknown onboarding state.");
                UIManager.Instance.OpenScreen(UIScreenType.Home);
                break;
        }
    }

    private void HandleDynamicOnboardingFromQuestions(List<string> questions)
    {
        var screen = BackendQuestionToScreenMapper.GetFirstMatchingScreen(questions);
        if (screen.HasValue)
        {
            UIManager.Instance.OpenScreen(screen.Value);
        }
        else
        {
            Debug.LogWarning("No valid screen mapping found in remaining onboarding questions.");
            UIManager.Instance.OpenScreen(UIScreenType.Home);
        }
    }

    public void sendOTPFunc(string email)
    {
        Debug.Log("Sending OTP for: " + email);
        StartCoroutine(SendOtpCoroutine(email));
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
                Label otpError = new Label("Failed to send OTP. Please try again.");
                otpError.AddToClassList("login-error-message");
                loginWarningContainer.Clear();
                loginWarningContainer.Add(otpError);

                UIManager.Instance.OpenScreen(UIScreenType.Register);
            }
            else
            {
                OTPResponse otpResponse = JsonUtility.FromJson<OTPResponse>(request.downloadHandler.text);
                if (otpResponse != null && otpResponse.success)
                {
                    Debug.Log("OTP sent successfully. Opening OTP screen...");
                    UIManager.Instance.OpenScreen(UIScreenType.OTP);
                }
                else
                {
                    Label otpFail = new Label(otpResponse?.message ?? "Failed to send OTP.");
                    otpFail.AddToClassList("login-error-message");
                    loginWarningContainer.Clear();
                    loginWarningContainer.Add(otpFail);

                    UIManager.Instance.OpenScreen(UIScreenType.Register);
                }
            }
        }
    }
    
    private void SetUIInteractable(bool state)
    {
        emailField.SetEnabled(state);
        passwordField.SetEnabled(state);
    }

    [Serializable]
    public class LoginData
    {
        public string email;
        public string password;
        public LoginData(string e, string p) { email = e; password = p; }
    }

    [System.Serializable] public class EmailData { public string email; public EmailData(string e) { email = e; } }

    [System.Serializable]
    public class LoginResponse
    {
        public bool success;
        public string message;
        public string token;
        public Data data;
    }

    [System.Serializable]
    public class Data
    {
        public string onboardingState;
        public string role;
        public List<string> remainingQuestions;
    }

    [System.Serializable]
    public class messege
    {
        public string message;
    }

    [Serializable]
    public class OTPResponse
    {
        public bool success;
        public string message;
    }
}
