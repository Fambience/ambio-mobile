using System.Collections;
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

    private Label warningEmailText;
    private Label warningPasswordText;
    private Label warningLoginText;

    private string userEmail;
    private string baseURL = baseScript.baseURL;
    private string loginEndPoint = "/api/v1/auth/login";
    private string sendOtpEndPoint = "/api/v1/user/trigger-otp";

    private void Awake()
    {
        var root = uiDocument.rootVisualElement;

        // Bind UXML fields
        loginScreen = root.Q<VisualElement>("LoginScreen");
        emailField = root.Q<TextField>("emailField");
        passwordField = root.Q<TextField>("passwordField");
        loginButton = root.Q<Button>("signInButton");

        // Create warning labels
        warningEmailText = new Label { style = { color = Color.red, unityFontStyleAndWeight = FontStyle.Bold } };
        warningPasswordText = new Label { style = { color = Color.red, unityFontStyleAndWeight = FontStyle.Bold } };
        warningLoginText = new Label { style = { color = Color.red, unityFontStyleAndWeight = FontStyle.Bold } };

        var warningContainer = root.Q<VisualElement>("LoginWarningContainer");
        warningContainer.Add(warningEmailText);
        warningContainer.Add(warningPasswordText);
        warningContainer.Add(warningLoginText);

        emailField.RegisterValueChangedCallback(evt => ValidateEmail(evt.newValue));
        passwordField.RegisterValueChangedCallback(evt => ValidatePassword(evt.newValue));
        loginButton.clicked += LoginUser;

        Label signUpLabel = root.Q<Label>("SignUpLabel");
        signUpLabel.RegisterCallback<ClickEvent>(evt => ShowRegisterScreen());

        PlayerPrefs.DeleteAll();
    }

    private void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            warningEmailText.text = "Email is required.";
        else if (!emailValidator.isValidEmail(email))
            warningEmailText.text = "Invalid email format.";
        else
            warningEmailText.text = string.Empty;
    }

    private void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            warningPasswordText.text = "Password is required.";
        else if (!PasswordValidator.IsValidPassword(password, out string error))
            warningPasswordText.text = error;
        else
            warningPasswordText.text = string.Empty;
    }

    private void LoginUser()
    {
        string email = emailField.value?.Trim();
        string password = passwordField.value?.Trim();

        bool hasEmpty = false;

        if (string.IsNullOrEmpty(email))
        {
            warningEmailText.text = "Email is required.";
            hasEmpty = true;
        }

        if (string.IsNullOrEmpty(password))
        {
            warningPasswordText.text = "Password is required.";
            hasEmpty = true;
        }

        if (hasEmpty) return;

        if (!string.IsNullOrEmpty(warningEmailText.text) || !string.IsNullOrEmpty(warningPasswordText.text))
            return;

        StartCoroutine(LoginUserCoroutine(email, password));
    }

    private IEnumerator LoginUserCoroutine(string email, string password)
    {
        Debug.Log("LoginUserCoroutine Called");
        //ShowLoader("Logging in...");
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
                warningLoginText.text = error?.message ?? "Login failed. Please try again.";
                passwordField.value = "";
                yield break;
            }

            warningLoginText.text = "";
            passwordField.value = "";

            var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            string token = response.token;

            AuthTokenManager.SetToken(token);
            userEmail = email;

            PlayerPrefs.SetString("email", email);
            PlayerPrefs.SetString("password", password);
            PlayerPrefs.SetString("role", response.data.role);
            PlayerPrefs.Save();

            HandleLoginStage(response.data.onboardingState, response.data.role, token);
        }
    }

    private void ShowRegisterScreen()
    {
            UIManager.Instance.OpenScreen(UIScreenType.Register);
    }

    private void HandleLoginStage(string state, string role, string token)
    {
        switch (state)
        {
            case "VERIFY_EMAIL":
                //UIManager.Instance.OpenScreen(UIScreenType.OTP);
                //ShowLoader("Sending OTP on");
                sendOTPFunc(userEmail);
                break;

            case "ONBOARD_DETAILS":
                //UIManager.Instance.OpenScreen(UIScreenType.OTP);
                break;

            case "ONBOARDING_COMPLETED":
                //ShowOnly(homeScreen);
                if (!string.IsNullOrEmpty(token))
                    userProfileManager.InitializeProfile(token);
                break;
            
            case "Basic_Details":
                break;

            default:
                Debug.LogError("Unknown onboarding state.");
                break;
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
                warningLoginText.text = "Failed to send OTP. Please try again.";
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
                    warningLoginText.text = errorMsg;
                    UIManager.Instance.OpenScreen(UIScreenType.Register);
                }
            }
        }
    }
    
    public class OTPResponse
    {
        public bool success;
        public string message;
    }

    private void ShowLoader(string message)
    {
        if (loaderScreen != null)
        {
            loaderText.text = message;
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
    }

    [System.Serializable]
    public class messege
    {
        public string message;
    }
}
