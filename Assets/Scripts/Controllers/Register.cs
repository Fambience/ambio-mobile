using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class Register : MonoBehaviour
{
    [Header("UI Toolkit")]
    public UIDocument uiDocument;

    private string intent = "";
    private TextField emailField;
    private TextField passwordField;
    private TextField confirmPasswordField;
    private Button continueButton;
    private Button continuewithGoogle;
    private Button backToLoginButton;
    private Image eyeIcon1;
    private Image eyeIcon2;

    private Label warningEmail;
    private Label warningPassword;
    private Label warningConfirmPassword;
    private Label warningRegister;

    private string baseURL;
    private string token;

    private string registerEndPoint = "/api/v1/auth/register";
    private string sendOtpEndPoint = "/api/v1/user/trigger-otp";
    
    private bool isPasswordVisible = false;
    private bool isConPasswordVisible = false;
    private bool isTermsAndConditionsAccepted = false;

    private void OnEnable()
    {
        baseURL = baseScript.baseURL;

        var root = uiDocument.rootVisualElement;
        emailField = root.Q<TextField>("emailField");
        passwordField = root.Q<TextField>("passwordField");
        confirmPasswordField = root.Q<TextField>("confirmPasswordField");
        continueButton = root.Q<Button>("continueButton");
        continuewithGoogle = root.Q<Button>("OAuth");
        backToLoginButton = root.Q<Button>("BackToLoginLabel");
        eyeIcon1 = root.Q<Image>("eyeIconPass");
        eyeIcon2 = root.Q<Image>("eyeIconConPass");
        Toggle termsToggle = root.Q<Toggle>("termsToggle");
        termsToggle.RegisterValueChangedCallback(evt =>
        {
            isTermsAndConditionsAccepted = evt.newValue;
            if (isTermsAndConditionsAccepted)
            {
                isTermsAndConditionsAccepted = true;
                Debug.Log("TermsAndConditions accepted");
                warningRegister.text = "";
            }
        });
        Label termsLink = root.Q<Label>("termsLink");
        termsLink.RegisterCallback<ClickEvent>(evt =>
        {
            Application.OpenURL("https://example.com");
        });

        passwordField.isPasswordField = true;
        confirmPasswordField.isPasswordField = true;
        
        eyeIcon1.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("eye-off-outline"));
        eyeIcon2.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("eye-off-outline"));

        warningEmail = root.Q<Label>("WarningEmail");
        warningPassword = root.Q<Label>("WarningPassword");
        warningConfirmPassword = root.Q<Label>("WarningConfirmPassword");
        warningRegister = root.Q<Label>("WarningRegister");

        confirmPasswordField.RegisterValueChangedCallback(evt =>
        {
            warningConfirmPassword.text = (confirmPasswordField.value != passwordField.value)
                ? "Passwords do not match." : "";
        });
        
        eyeIcon1.RegisterCallback<ClickEvent>(_ => TogglePasswordVisibility("password"));
        eyeIcon2.RegisterCallback<ClickEvent>(_ => TogglePasswordVisibility("confirmPassword"));

        continueButton.clicked += RegisterUser;
        continuewithGoogle.clicked += OnContinueWithGoogleClicked;

        backToLoginButton?.RegisterCallback<ClickEvent>(evt =>
        {
            UIManager.Instance.OpenScreen(UIScreenType.Login);
        });
    }
    
    private void TogglePasswordVisibility(string which)
    {
        if (which == "password")
        {
            isPasswordVisible = !isPasswordVisible;
            passwordField.isPasswordField = !isPasswordVisible;
            eyeIcon1.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>(isPasswordVisible ? "eye-outline" : "eye-off-outline"));
        }
        else if (which == "confirmPassword")
        {
            isConPasswordVisible = !isConPasswordVisible;
            confirmPasswordField.isPasswordField = !isConPasswordVisible;
            eyeIcon2.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>(isConPasswordVisible ? "eye-outline" : "eye-off-outline"));
        }
    }

    private async void OnContinueWithGoogleClicked()
    {
        if (!isTermsAndConditionsAccepted)
        {
            warningRegister.text = "Please accept the terms and conditions to continue.";
            return;
        }

        SetUIInteractable(false);
        warningRegister.text = "Preparing authentication...";

        // wait for AuthenticationManager to be ready
        var ready = await AuthenticationManager.WaitUntilReady();
        if (!ready || AuthenticationManager.Instance == null)
        {
            warningRegister.text = "Auth manager not ready. Check Firebase dependencies.";
            SetUIInteractable(true);
            return;
        }

        warningRegister.text = "Connecting to Google...";

        try
        {
            AuthResponse response = await AuthenticationManager.Instance.SignInWithGoogleAsync();

            if (!string.IsNullOrEmpty(response?.sessionToken))
            {
                AuthTokenManager.SetToken(response.sessionToken);

                if (response.user != null && response.user.isNewUser)
                    UIManager.Instance.OpenScreen(UIScreenType.BasicDetails);
                else
                    UIManager.Instance.OpenScreen(UIScreenType.Home);

                warningRegister.text = "";
            }
            else
            {
                warningRegister.text = response?.error ?? "Sign-in failed.";
                SetUIInteractable(true);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception during Google Sign-In: {ex.Message}");
            warningRegister.text = "An unexpected error occurred.";
            SetUIInteractable(true);
        }
    }

    private void RegisterUser()
    {
        string email = emailField.value.Trim();
        string password = passwordField.value.Trim();
        string confirmPassword = confirmPasswordField.value.Trim();

        bool hasError = false;

        if (string.IsNullOrWhiteSpace(email))
        {
            warningEmail.text = "Email is required.";
            hasError = true;
        }
        else if (!emailValidator.isValidEmail(email))
        {
            warningEmail.text = "Invalid email format.";
            hasError = true;
        }
        else warningEmail.text = "";

        if (string.IsNullOrWhiteSpace(password))
        {
            warningPassword.text = "Password is required.";
            hasError = true;
        }
        else if (!IsValidPassword(password))
        {
            warningPassword.text = "Should be of at least 8 characters, with one number.";
            hasError = true;
        }
        else warningPassword.text = "";

        if (string.IsNullOrWhiteSpace(confirmPassword))
        {
            warningConfirmPassword.text = "Please confirm your password.";
            hasError = true;
        }
        else if (confirmPassword != password)
        {
            warningConfirmPassword.text = "Passwords do not match.";
            hasError = true;
        }
        else warningConfirmPassword.text = "";
        
        if (!isTermsAndConditionsAccepted)
        {
            warningRegister.text = "Please accept the terms and conditions.";
            hasError = true;
        }
        else warningRegister.text = "";

        if (hasError) { Debug.LogWarning("Validation failed. Registration aborted."); return; }

        StartCoroutine(RegisterUserCoroutine(email, password));
    }

    private IEnumerator RegisterUserCoroutine(string email, string password)
    {
        SetUIInteractable(false);

        string jsonData = JsonUtility.ToJson(new RegisterData(email, password, isTermsAndConditionsAccepted));
        using (UnityWebRequest request = new UnityWebRequest(baseURL + registerEndPoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                try
                {
                    messege error = JsonUtility.FromJson<messege>(request.downloadHandler.text);
                    warningRegister.text = error?.message ?? "Registration failed. Try again.";
                }
                catch
                {
                    warningRegister.text = "Registration failed. Try again.";
                }
            }
            else
            {
                warningRegister.text = "";
                var response = JsonUtility.FromJson<RegisterResponse>(request.downloadHandler.text);
                AuthTokenManager.SetToken(response.token);
                sendOTPFunc(email);
            }
        }

        SetUIInteractable(true);
    }
    
    private void SetUIInteractable(bool state)
    {
        emailField.SetEnabled(state);
        passwordField.SetEnabled(state);
        confirmPasswordField.SetEnabled(state);
        continueButton.SetEnabled(state);
        backToLoginButton.SetEnabled(state);
        continuewithGoogle.SetEnabled(state);
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
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", token);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                warningRegister.text = "Failed to send OTP. Please try again.";
                UIManager.Instance.OpenScreen(UIScreenType.Register);
            }
            else
            {
                string responseText = request.downloadHandler.text;
                OTPResponse otpResponse = JsonUtility.FromJson<OTPResponse>(responseText);

                if (otpResponse != null && otpResponse.success)
                    UIManager.Instance.OpenScreen(UIScreenType.OTP);
                else
                {
                    string errorMsg = otpResponse?.message ?? "Failed to send OTP.";
                    warningRegister.text = errorMsg;
                    UIManager.Instance.OpenScreen(UIScreenType.Register);
                }
            }
        }
    }

    private static readonly string passwordPattern = @"^(?=.*[a-zA-Z])(?=.*\d).{8,}$";
    private bool IsValidPassword(string password) => Regex.IsMatch(password, passwordPattern);

    [System.Serializable] public class RegisterData { public string email; public string password; public bool isTermsAndConditionsAccepted;
        public RegisterData(string email, string password, bool isTermsAndConditionsAccepted)
        { this.email = email; this.password = password; this.isTermsAndConditionsAccepted = isTermsAndConditionsAccepted; } }

    [System.Serializable] public class OTPResponse { public bool success; public string message; }
    [System.Serializable] public class EmailData { public string email; public EmailData(string e) { email = e; } }
    [System.Serializable] public class RegisterResponse { public string token; public bool success; public string message; }
    [System.Serializable] public class messege { public string message; }
}
