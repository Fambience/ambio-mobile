using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Services; // ← Uncomment if UIManager/baseScript live here
// using YourNamespace; // ← Uncomment if PasswordValidator is in a namespace

public class UpdatePasswordController : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument uiDocument;

    // UI elements
    private Button backButton;
    private Button savePasswordButton;
    private TextField oldPasswordField;
    private TextField newPasswordField;
    private TextField confirmPasswordField;
    private Label forgotPasswordLabel;
    private Label warningOldPassword;
    private Label warningNewPassword;
    private Label warningCnfNewPassword;
    private Label warningForgotPassword;

    // API endpoint
    private readonly string updatePasswordEndpoint = "/api/v1/profile/update-password";

    // Cached delegates to unregister cleanly
    private EventCallback<FocusOutEvent> _onOldFocusOut;
    private EventCallback<FocusOutEvent> _onNewFocusOut;
    private EventCallback<FocusOutEvent> _onCnfFocusOut;
    private EventCallback<ChangeEvent<string>> _onNewChanged;
    private EventCallback<ChangeEvent<string>> _onCnfChanged;

    private void Start()
    {
        InitializeUIElements();
        RegisterCallbacks();
        ClearAllWarnings();
    }

    private void InitializeUIElements()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[UpdatePasswordController] UIDocument reference is missing.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("[UpdatePasswordController] rootVisualElement is null.");
            return;
        }

        backButton = root.Q<Button>("backButton");
        savePasswordButton = root.Q<Button>("savePassword");
        oldPasswordField = root.Q<TextField>("oldPasswordField");
        newPasswordField = root.Q<TextField>("newPasswordField");
        confirmPasswordField = root.Q<TextField>("confirmPasswordField");
        forgotPasswordLabel = root.Q<Label>("forgotPassword");
        warningOldPassword = root.Q<Label>("WarningOldPassword");
        warningNewPassword = root.Q<Label>("WarningNewPassword");
        warningCnfNewPassword = root.Q<Label>("WarningCnfNewPassword");
        warningForgotPassword = root.Q<Label>("warningForgotPassword");

        if (oldPasswordField != null) oldPasswordField.isPasswordField = true;
        if (newPasswordField != null) newPasswordField.isPasswordField = true;
        if (confirmPasswordField != null) confirmPasswordField.isPasswordField = true;

        if (forgotPasswordLabel != null)
        {
            forgotPasswordLabel.pickingMode = PickingMode.Position;
            forgotPasswordLabel.AddToClassList("clickable");
        }
    }

    private void RegisterCallbacks()
    {
        if (backButton != null) backButton.clicked += OnBackButtonClicked;
        if (savePasswordButton != null) savePasswordButton.clicked += OnSavePasswordClicked;
        if (forgotPasswordLabel != null) forgotPasswordLabel.RegisterCallback<ClickEvent>(OnForgotPasswordClicked);

        _onOldFocusOut = OnOldPasswordFocusOut;
        _onNewFocusOut = OnNewPasswordFocusOut;
        _onCnfFocusOut = OnConfirmPasswordFocusOut;
        _onNewChanged = OnNewPasswordChanged;
        _onCnfChanged = OnConfirmPasswordChanged;

        if (oldPasswordField != null) oldPasswordField.RegisterCallback(_onOldFocusOut);
        if (newPasswordField != null) newPasswordField.RegisterCallback(_onNewFocusOut);
        if (confirmPasswordField != null) confirmPasswordField.RegisterCallback(_onCnfFocusOut);

        if (newPasswordField != null) newPasswordField.RegisterValueChangedCallback(_onNewChanged);
        if (confirmPasswordField != null) confirmPasswordField.RegisterValueChangedCallback(_onCnfChanged);
    }

    private void OnBackButtonClicked()
    {
        Debug.Log("Back button clicked");
        // UIManager.Instance.OpenScreen(UIScreenType.ProfileSettings);
    }

    private void OnSavePasswordClicked()
    {
        if (ValidateAllFields())
        {
            StartCoroutine(UpdatePasswordAPI());
        }
    }

    private void OnForgotPasswordClicked(ClickEvent evt)
    {
        Debug.Log("Forgot password clicked");
        // UIManager.Instance.OpenScreen(UIScreenType.ForgotPassword);
    }

    private void OnOldPasswordFocusOut(FocusOutEvent evt) => ValidateOldPassword();
    private void OnNewPasswordFocusOut(FocusOutEvent evt) => ValidateNewPassword();
    private void OnConfirmPasswordFocusOut(FocusOutEvent evt) => ValidateConfirmPassword();
    private void OnNewPasswordChanged(ChangeEvent<string> evt) => ValidateNewPassword();
    private void OnConfirmPasswordChanged(ChangeEvent<string> evt) => ValidateConfirmPassword();

    private bool ValidateAllFields()
    {
        ClearAllWarnings();
        bool ok = true;
        if (!ValidateOldPassword()) ok = false;
        if (!ValidateNewPassword()) ok = false;
        if (!ValidateConfirmPassword()) ok = false;
        return ok;
    }

    private bool ValidateOldPassword()
    {
        string oldPassword = oldPasswordField?.value?.Trim() ?? "";
        if (string.IsNullOrEmpty(oldPassword))
        {
            ShowWarning(warningOldPassword, "Current password is required");
            return false;
        }
        HideWarning(warningOldPassword);
        return true;
    }

    private bool ValidateNewPassword()
    {
        string newPassword = newPasswordField?.value ?? "";
        string oldPassword = oldPasswordField?.value ?? "";

        if (string.IsNullOrEmpty(newPassword))
        {
            ShowWarning(warningNewPassword, "New password is required");
            return false;
        }

        if (!string.IsNullOrEmpty(oldPassword) && newPassword == oldPassword)
        {
            ShowWarning(warningNewPassword, "New password must be different from current password");
            return false;
        }

        // Use shared validator
        if (!PasswordValidator.IsValidPassword(newPassword, out var errorMsg))
        {
            ShowWarning(warningNewPassword, errorMsg);
            return false;
        }

        HideWarning(warningNewPassword);
        if (!string.IsNullOrEmpty(confirmPasswordField?.value))
            ValidateConfirmPassword();

        return true;
    }

    private bool ValidateConfirmPassword()
    {
        string confirmPassword = confirmPasswordField?.value ?? "";
        string newPassword = newPasswordField?.value ?? "";

        if (string.IsNullOrEmpty(confirmPassword))
        {
            ShowWarning(warningCnfNewPassword, "Please confirm your new password");
            return false;
        }

        if (confirmPassword != newPassword)
        {
            ShowWarning(warningCnfNewPassword, "Passwords do not match");
            return false;
        }

        HideWarning(warningCnfNewPassword);
        return true;
    }

    private void ShowWarning(Label label, string message)
    {
        if (label == null) return;
        label.text = message;
        label.style.display = DisplayStyle.Flex;
    }

    private void HideWarning(Label label)
    {
        if (label == null) return;
        label.text = "";
        label.style.display = DisplayStyle.None;
    }

    private void ClearAllWarnings()
    {
        HideWarning(warningOldPassword);
        HideWarning(warningNewPassword);
        HideWarning(warningCnfNewPassword);
        HideWarning(warningForgotPassword);
    }

    private IEnumerator UpdatePasswordAPI()
    {
        if (savePasswordButton != null) savePasswordButton.SetEnabled(false);
        HideWarning(warningForgotPassword);

        string token = AuthTokenManager.GetToken();
        if (string.IsNullOrEmpty(token))
        {
            ShowWarning(warningForgotPassword, "Authentication token not found. Please login again.");
            if (savePasswordButton != null) savePasswordButton.SetEnabled(true);
            yield break;
        }

        var payload = new UpdatePasswordRequest
        {
            oldPassword = oldPasswordField?.value ?? "",
            newPassword = newPasswordField?.value ?? "",
            reEnterNewPassword = confirmPasswordField?.value ?? ""
        };

        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        string fullUrl = baseScript.baseURL + updatePasswordEndpoint;
        using (var request = new UnityWebRequest(fullUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {token}");
            request.timeout = 20;

            yield return request.SendWebRequest();

            string resp = request.downloadHandler != null ? request.downloadHandler.text : "";
            Debug.Log($"[UpdatePassword] {request.responseCode} | {request.result} | {resp}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 1) String check (forgiving)
                if (resp.Contains("\"success\":true") || resp.Contains("\"success\": true"))
                {
                    OnPasswordUpdatedSuccess();
                }
                else
                {
                    // 2) Try JsonUtility
                    bool handled = false;
                    try
                    {
                        var parsed = JsonUtility.FromJson<SimpleResponse>(resp);
                        if (parsed != null && parsed.success)
                        {
                            OnPasswordUpdatedSuccess();
                            handled = true;
                        }
                        else if (parsed != null)
                        {
                            ShowWarning(warningForgotPassword, parsed.message ?? "Failed to update password");
                            handled = true;
                        }
                    }
                    catch { /* fall through */ }

                    // 3) Manual fallback
                    if (!handled)
                    {
                        if (ParseSuccessManually(resp))
                            OnPasswordUpdatedSuccess();
                        else
                            ShowWarning(warningForgotPassword, "Unexpected response. Please try again.");
                    }
                }
            }
            else
            {
                switch (request.responseCode)
                {
                    case 400: ShowWarning(warningForgotPassword, "Invalid password format or data"); break;
                    case 401: ShowWarning(warningForgotPassword, "Current password is incorrect or session expired"); break;
                    case 403: ShowWarning(warningForgotPassword, "Authentication expired. Please login again."); break;
                    case 500: ShowWarning(warningForgotPassword, "Server error. Please try again later."); break;
                    default:  ShowWarning(warningForgotPassword, "Network or server error. Please check your connection."); break;
                }
            }
        }

        if (savePasswordButton != null) savePasswordButton.SetEnabled(true);
    }

    private void OnPasswordUpdatedSuccess()
    {
        Debug.Log("Password updated successfully");
        ShowWarning(warningForgotPassword, "Password updated successfully!");
        ClearPasswordFields();
        // UIManager.Instance.OpenScreen(UIScreenType.ProfileSettings);
    }

    private void ClearPasswordFields()
    {
        if (oldPasswordField != null) oldPasswordField.value = "";
        if (newPasswordField != null) newPasswordField.value = "";
        if (confirmPasswordField != null) confirmPasswordField.value = "";
        ClearAllWarnings();
    }

    private void OnDestroy()
    {
        if (backButton != null) backButton.clicked -= OnBackButtonClicked;
        if (savePasswordButton != null) savePasswordButton.clicked -= OnSavePasswordClicked;
        if (forgotPasswordLabel != null) forgotPasswordLabel.UnregisterCallback<ClickEvent>(OnForgotPasswordClicked);

        if (oldPasswordField != null && _onOldFocusOut != null) oldPasswordField.UnregisterCallback(_onOldFocusOut);
        if (newPasswordField != null && _onNewFocusOut != null) newPasswordField.UnregisterCallback(_onNewFocusOut);
        if (confirmPasswordField != null && _onCnfFocusOut != null) confirmPasswordField.UnregisterCallback(_onCnfFocusOut);

        if (newPasswordField != null && _onNewChanged != null) newPasswordField.UnregisterValueChangedCallback(_onNewChanged);
        if (confirmPasswordField != null && _onCnfChanged != null) confirmPasswordField.UnregisterValueChangedCallback(_onCnfChanged);
    }

    // ===== Models & helpers =====

    [Serializable]
    private class UpdatePasswordRequest
    {
        public string oldPassword;
        public string newPassword;
        public string reEnterNewPassword;
    }

    [Serializable]
    private class SimpleResponse
    {
        public bool success;
        public string message;
    }

    private bool ParseSuccessManually(string json)
    {
        try
        {
            int idx = json.IndexOf("\"success\"", StringComparison.Ordinal);
            if (idx < 0) return false;
            int colon = json.IndexOf(":", idx);
            if (colon < 0) return false;
            string rem = json.Substring(colon + 1).TrimStart();
            return rem.StartsWith("true");
        }
        catch (Exception e)
        {
            Debug.LogError("Manual parse failed: " + e.Message);
            return false;
        }
    }
}
