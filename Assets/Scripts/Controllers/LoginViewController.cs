using UnityEngine;
using UnityEngine.UIElements;

public class LoginViewController : MonoBehaviour
{
    [Header("LoginView UI Document")]
    public UIDocument loginDocument;

    private VisualElement root;
    private TextField emailField;
    private TextField passwordField;
    private Button signInButton;
    private Label signUpLabel;
    private Label forgotPasswordLabel;

    void OnEnable()
    {
        if (loginDocument == null)
        {
            Debug.LogError("UIDocument is not assigned to LoginViewController.");
            return;
        }

        root = loginDocument.rootVisualElement;

        // Make root responsive
        root.style.flexGrow = 1;
        root.style.flexDirection = FlexDirection.Column;

        // Query elements
        emailField = root.Q<TextField>("emailField");
        passwordField = root.Q<TextField>("passwordField");
        signInButton = root.Q<Button>("signInButton");
        signUpLabel = root.Q<Label>(className: "signup-link");
        forgotPasswordLabel = root.Q<Label>(className: "forgot-password");

        // Bind actions
        signInButton?.RegisterCallback<ClickEvent>(OnSignInClicked);
        signUpLabel?.RegisterCallback<ClickEvent>(OnSignUpClicked);
        forgotPasswordLabel?.RegisterCallback<ClickEvent>(OnForgotPasswordClicked);
    }

    private void OnSignInClicked(ClickEvent evt)
    {
        string email = emailField?.value.Trim();
        string password = passwordField?.value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Email or Password is empty!");
            return;
        }

        Debug.Log($"[Sign In] Email: {email}, Password: {password}");
        // TODO: Integrate API call here
    }

    private void OnSignUpClicked(ClickEvent evt)
    {
        Debug.Log("[Sign Up] Navigate to sign-up flow.");
        // TODO: Show sign-up UI
    }

    private void OnForgotPasswordClicked(ClickEvent evt)
    {
        Debug.Log("[Forgot Password] Trigger password reset UI.");
        // TODO: Show password recovery UI
    }
}
