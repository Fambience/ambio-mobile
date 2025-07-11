using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using Services;

public class SetNewPasswordScreenController : MonoBehaviour
{
    private TextField newPasswordField;
    private TextField confirmPasswordField;
    private Label warningNewPasswordLabel;
    private Label warningConfirmPasswordLabel;
    private Label globalWarningLabel;
    private Button savePasswordButton;

    private string baseUrl;

    void OnEnable()
    {
        baseUrl = baseScript.baseURL;

        var root = GetComponent<UIDocument>().rootVisualElement;

        newPasswordField = root.Q<TextField>("newPasswordField");
        confirmPasswordField = root.Q<TextField>("confirmPasswordField");
        warningNewPasswordLabel = root.Q<Label>("WarningNewPassword");
        warningConfirmPasswordLabel = root.Q<Label>("WarningCnfNewPassword");
        globalWarningLabel = root.Q<Label>("warningForgotPassword");
        savePasswordButton = root.Q<Button>("savePassword");

        warningNewPasswordLabel.text = "";
        warningConfirmPasswordLabel.text = "";
        globalWarningLabel.text = "";

        savePasswordButton.clicked += OnConfirmClicked;
    }

    private void OnConfirmClicked()
    {
        string newPassword = newPasswordField.value.Trim();
        string confirmPassword = confirmPasswordField.value.Trim();

        warningNewPasswordLabel.text = "";
        warningConfirmPasswordLabel.text = "";
        globalWarningLabel.text = "";

        if (newPassword.Length < 6)
        {
            warningNewPasswordLabel.text = "Password must be at least 6 characters.";
            return;
        }

        if (newPassword != confirmPassword)
        {
            warningConfirmPasswordLabel.text = "Passwords do not match.";
            return;
        }

        StartCoroutine(SendResetPasswordRequest(newPassword));
    }

    private IEnumerator SendResetPasswordRequest(string newPassword)
    {
        string endpoint = "/api/v1/auth/password/reset";
        string fullUrl = baseUrl + endpoint;

        string resetToken = PasswordResetSession.ResetToken;
        if (string.IsNullOrEmpty(resetToken))
        {
            globalWarningLabel.text = "Missing token. Please restart.";
            yield break;
        }

        var payload = $"{{\"newPassword\":\"{newPassword}\"}}";
        var request = new UnityWebRequest(fullUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-reset-token", resetToken);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Password reset successful: " + request.downloadHandler.text);
            UIManager.Instance.OpenScreen(UIScreenType.PasswordChanged); // or show a success screen
        }
        else
        {
            Debug.LogError("Password reset failed: " + request.downloadHandler.text);
            globalWarningLabel.text = "Failed to reset password. Try again.";
        }

        request.Dispose();
    }
}
