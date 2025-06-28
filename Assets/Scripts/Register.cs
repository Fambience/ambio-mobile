using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class Register : MonoBehaviour
{
    [Header("Endpoints")]
    private string registerEndPoint = "/api/v1/auth/register";
    private string sendOtpEndPoint = "/api/v1/user/trigger-otp";
    private string verifyOtpEndPoint = "/api/v1/user/verify-otp";

    [Header("Button")]
    public GameObject registerButton;

    [Header("Game Objects for UI Change")]
    public GameObject registerUI;
    public GameObject verifyOTPUI;
    public GameObject selection;
    public GameObject LogInUI;

    [Header("TMP Input Fields")]
    public TMP_InputField emailInputField;
    public TMP_InputField passwordInputField;
    public TMP_InputField confirmPasswordInputField;

    [Header("Warning Text Fields")]
    public TMP_Text warningEmailText;
    public TMP_Text warningPasswordText;
    public TMP_Text warningConfirmPasswordText;
    public TMP_Text warningRegisterText;

    [Header("Loader")]
    public GameObject loaderScreen;
    public TMP_Text loaderText;

    private string baseURL;
    private string token;
    public UserProfileManager userProfileManager;  // Assign via Inspector

    private void Start()
    {
        baseURL = baseScript.baseURL;
        emailInputField.onEndEdit.AddListener(ValidateEmail);
        passwordInputField.onEndEdit.AddListener(ValidatePassword);
        confirmPasswordInputField.onEndEdit.AddListener(ValidateConfirmPassword);
    }

    public void OnRegisterBack()
    {
        LogInUI.SetActive(true);
        registerUI.SetActive(false);
        emailInputField.text = "";
        passwordInputField.text = "";
        confirmPasswordInputField.text = "";
    }

    private void ValidateEmail(string email)
    {
        if (!emailValidator.isValidEmail(email))
        {
            warningEmailText.text = "Invalid email address.";
            Debug.LogError("Invalid email format.");
        }
        else
        {
            warningEmailText.text = string.Empty;
        }
    }

    private void ValidatePassword(string password)
    {
        if (!PasswordValidator.IsValidPassword(password, out string errorMessage))
        {
            warningPasswordText.text = errorMessage;
            Debug.LogError(errorMessage);
        }
        else
        {
            warningPasswordText.text = string.Empty;
        }
    }

    private void ValidateConfirmPassword(string confirmPassword)
    {
        if (confirmPassword != passwordInputField.text)
        {
            warningConfirmPasswordText.text = "Passwords do not match.";
            registerButton.SetActive(false);
        }
        else
        {
            warningConfirmPasswordText.text = string.Empty;
            registerButton.SetActive(true);
        }
    }

    public void RegisterUser()
    {
        string email = emailInputField.text;
        string password = passwordInputField.text;
        string confirmPassword = confirmPasswordInputField.text;

        if (!string.IsNullOrEmpty(warningEmailText.text) ||
            !string.IsNullOrEmpty(warningPasswordText.text) ||
            !string.IsNullOrEmpty(warningConfirmPasswordText.text))
        {
            return;
        }

        StartCoroutine(RegisterUserCoroutine(email, password));
    }

    public class LoginData
    {
        public string email;
        public string password;

        public LoginData(string email, string password)
        {
            this.email = email;
            this.password = password;
        }
    }

    private IEnumerator RegisterUserCoroutine(string email, string password)
    {
        loaderScreen.SetActive(true);
        // StartCoroutine(TypeWriterEffect("Registering your account..."));
        loaderText.text = "Registering your account...";

        string jsonData = JsonUtility.ToJson(new LoginData(email, password));

        using (UnityWebRequest request = new UnityWebRequest(baseURL + registerEndPoint, "POST"))
        {
            byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            loaderScreen.SetActive(false);

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Registration Failed: {request.error}");
                Debug.LogError($"Server Response: {request.downloadHandler.text}");
                messege errorResponse = JsonUtility.FromJson<messege>(request.downloadHandler.text);
                warningRegisterText.text = errorResponse != null ? errorResponse.message : "Registration failed. Please try again.";
            }
            else
            {
                Debug.Log("Register Successful: " + request.downloadHandler.text);

                RegisterResponse response = JsonUtility.FromJson<RegisterResponse>(request.downloadHandler.text);

                if (!string.IsNullOrEmpty(response.token))
                {
                    Debug.Log("Storing token: " + response.token);
                    AuthTokenManager.SetToken(response.token);
                    userProfileManager.InitializeProfile(AuthTokenManager.GetToken());
                }
                else
                {
                    Debug.LogWarning("Token was not found in the response.");
                }

                warningEmailText.text = string.Empty;
                warningPasswordText.text = string.Empty;
                warningConfirmPasswordText.text = string.Empty;
                warningRegisterText.text = string.Empty;
                registerUI.SetActive(false);
                verifyOTPUI.SetActive(true);
                sendOTPFunc(email);
            }
        }
    }

    [System.Serializable]
    public class EmailData
    {
        public string email;
        public EmailData(string email)
        {
            this.email = email;
        }
    }

    public void sendOTPFunc(string email)
    {
        Debug.Log("Otp Function Called");
        Debug.Log("Email : " + email);
        StartCoroutine(SendOtpCoroutine(email));
    }

    private IEnumerator SendOtpCoroutine(string email)
    {
        token = AuthTokenManager.GetToken();
        Debug.Log("Sending OTP");
        Debug.Log("Email : " + email);
        Debug.Log("Token : " + token);
        string jsonData = JsonUtility.ToJson(new EmailData(email));

        using (UnityWebRequest request = new UnityWebRequest(baseURL + sendOtpEndPoint, "POST"))
        {
            byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", token);
            }
            else
            {
                Debug.LogWarning("Token is missing. OTP request may fail.");
            }

            Debug.Log("Full OTP URL: " + baseURL + sendOtpEndPoint);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to send OTP: " + request.error);
            }
            else
            {
                Debug.Log("OTP sent successfully.");
            }
        }
    }

    public void VerifyOTP(string otp)
    {
        Debug.Log("Verifying OTP: " + otp);
        if (string.IsNullOrEmpty(otp)) return;
        StartCoroutine(VerifyOtpCoroutine(otp));
    }

    [System.Serializable]
    public class OTPData
    {
        public string otp;
        public OTPData(string otp) { this.otp = otp; }
    }

    private IEnumerator VerifyOtpCoroutine(string otp)
    {
        loaderScreen.SetActive(true);
        // StartCoroutine(TypeWriterEffect("Verifying OTP..."));
        loaderText.text = "Verifying OTP...";

        Debug.Log("OTP Provided in coroutine: "+ otp);
        string token = AuthTokenManager.GetToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Authorization token is missing.");
            loaderScreen.SetActive(false);
            yield break;
        }

        string jsonData = JsonUtility.ToJson(new OTPData(otp));

        using (UnityWebRequest request = new UnityWebRequest(baseURL + verifyOtpEndPoint, "POST"))
        {
            byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", token);

            yield return request.SendWebRequest();

            loaderScreen.SetActive(false);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("OTP Verification Failed: " + request.error);
            }
            else
            {
                Debug.Log("OTP Verified Successfully!");
                verifyOTPUI.SetActive(false);
                selection.SetActive(true);
            }
        }
    }

    // private IEnumerator TypeWriterEffect(string message)
    // {
    //     loaderText.text = "";
    //     foreach (char c in message)
    //     {
    //         loaderText.text += c;
    //         yield return new WaitForSeconds(0.05f);
    //     }
    // }

    [System.Serializable]
    public class RegisterResponse
    {
        public bool success;
        public string message;
        public string token;
    }
}

internal class messege
{
    public string message;
} 