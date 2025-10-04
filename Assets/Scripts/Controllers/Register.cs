    using System.Collections;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.UIElements;
    using System.Text.RegularExpressions;

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
            // Ensure password field is initially hidden
            passwordField.isPasswordField = true;
            confirmPasswordField.isPasswordField = true;
            
            // Initialize eye icon to show the correct initial state
            eyeIcon1.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("eye-off-outline"));
            eyeIcon2.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("eye-off-outline"));

            warningEmail = root.Q<Label>("WarningEmail");
            warningPassword = root.Q<Label>("WarningPassword");
            warningConfirmPassword = root.Q<Label>("WarningConfirmPassword");
            warningRegister = root.Q<Label>("WarningRegister");

            // Only real-time update on confirm password field for matching
            confirmPasswordField.RegisterValueChangedCallback(evt =>
            {
                if (confirmPasswordField.value != passwordField.value)
                    warningConfirmPassword.text = "Passwords do not match.";
                else
                    warningConfirmPassword.text = "";
            });
            
            eyeIcon1.RegisterCallback<ClickEvent>(_ => TogglePasswordVisibility("passowrd"));
            eyeIcon2.RegisterCallback<ClickEvent>(_ => TogglePasswordVisibility("confirmPassword"));

            continueButton.clicked += RegisterUser;
            continuewithGoogle.clicked += registerwithGoogle;

            backToLoginButton?.RegisterCallback<ClickEvent>(evt =>
            {
                UIManager.Instance.OpenScreen(UIScreenType.Login);
            });
        }
        
        private void TogglePasswordVisibility(string check)
        {
            if (check == "passowrd")
            {
                isPasswordVisible = !isPasswordVisible;

                if (isPasswordVisible)
                {
                    // Show password - change to text field and update icon
                    passwordField.isPasswordField = false;
                    eyeIcon1.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("eye-outline"));
                }
                else
                {
                    // Hide password - change to password field and update icon
                    passwordField.isPasswordField = true;
                    eyeIcon1.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("eye-off-outline"));
                }
            } else if (check == "confirmPassword")
            {
                isConPasswordVisible = !isConPasswordVisible;

                if (isConPasswordVisible)
                {
                    // Show password - change to text field and update icon
                    confirmPasswordField.isPasswordField = false;
                    eyeIcon2.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("eye-outline"));
                }
                else
                {
                    // Hide password - change to password field and update icon
                    confirmPasswordField.isPasswordField = true;
                    eyeIcon2.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("eye-off-outline"));
                }
            }
        }

        private void registerwithGoogle()
        {
            intent = "Register";
            // TODO: write function to connect firebase
        }

        // private IEnumerator RegisterWithGoogleCoroutine()
        // {
        //     
        // }

        private void RegisterUser()
        {
            string email = emailField.value.Trim();
            string password = passwordField.value.Trim();
            string confirmPassword = confirmPasswordField.value.Trim();

            bool hasError = false;

            // Email validation
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
            else
            {
                warningEmail.text = "";
            }

            // Password validation
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
            else
            {
                warningPassword.text = "";
            }

            // Confirm password validation
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
            else
            {
                warningConfirmPassword.text = "";
            }
            
            //terms and conditions
            if (!isTermsAndConditionsAccepted)
            {
                warningRegister.text = "Please accept the terms and conditions.";
                hasError = true;
            }
            else if  (isTermsAndConditionsAccepted)
            {
                warningRegister.text = "";
            }

            if (hasError)
            {
                Debug.LogWarning("Validation failed. Registration aborted.");
                return;
            }

            StartCoroutine(RegisterUserCoroutine(email, password));
        }

        private IEnumerator RegisterUserCoroutine(string email, string password)
        {
            SetUIInteractable(false); // Disable input

            string jsonData = JsonUtility.ToJson(new RegisterData(email, password, isTermsAndConditionsAccepted));
            Debug.Log("Payload ="  + jsonData );

            using (UnityWebRequest request = new UnityWebRequest(baseURL + registerEndPoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Registration failed: " + request.error);

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

            SetUIInteractable(true); // Re-enable input no matter what
        }
        
        private void SetUIInteractable(bool state)
        {
            emailField.SetEnabled(state);
            passwordField.SetEnabled(state);
            confirmPasswordField.SetEnabled(state);
            continueButton.SetEnabled(state);
            backToLoginButton.SetEnabled(state);
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
                    {
                        UIManager.Instance.OpenScreen(UIScreenType.OTP);
                    }
                    else
                    {
                        string errorMsg = otpResponse?.message ?? "Failed to send OTP.";
                        warningRegister.text = errorMsg;
                        UIManager.Instance.OpenScreen(UIScreenType.Register);
                    }
                }
            }
        }

        // Helper for Google-like password rule
        private static readonly string passwordPattern = @"^(?=.*[a-zA-Z])(?=.*\d).{8,}$";
        private bool IsValidPassword(string password)
        {
            return Regex.IsMatch(password, passwordPattern);
        }

        // Data Models
        [System.Serializable]
        public class RegisterData
        {
            public string email;
            public string password;
            public bool isTermsAndConditionsAccepted;

            public RegisterData(string email, string password,  bool isTermsAndConditionsAccepted)
            {
                this.email = email;
                this.password = password;
                this.isTermsAndConditionsAccepted = isTermsAndConditionsAccepted;
            }
        }

        [System.Serializable] public class OTPResponse { public bool success; public string message; }
        [System.Serializable] public class EmailData { public string email; public EmailData(string e) { email = e; } }
        [System.Serializable] public class RegisterResponse { public string token; public bool success; public string message; }
        [System.Serializable] public class messege { public string message; }
        [System.Serializable] public class GoogleAuthRequest { public string firebaseToker; public string googleSubID; public string intent; }
        [System.Serializable] public class GoogleAuthResponse {} //public string firebaseToker; public string googleSubID; public string intent; } 
    }
