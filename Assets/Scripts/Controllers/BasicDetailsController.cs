using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using System.Linq;

public class BasicDetailsController : MonoBehaviour
{
    [Header("Dependencies")]
    public UIDocument uiDocument;

    [Header("Config")]
    private float typingDelay = .5f;

    private string baseURL;
    private string verifyUsernameEndpoint = "/api/v1/user/verify-username";
    private string basicDetailsEndpoint = "/api/v1/user/basic-details";

    // UI Elements
    private TextField usernameField, ageField;
    private Label warningLabelUsername, warningLabelAgeGender, warningLabelRole;
    private Label selectedText;
    private Button signInButton, backButton;
    private RadioButtonGroup roleGroup;
    private VisualElement genderDropdown, dropdownContent;
    private ScrollView optionsList;
    private VisualElement dropdownTrigger;
    private Label dropdownArrow;

    private string selectedGender = "";
    private Coroutine usernameCheckCoroutine;
    private bool isUsernameValid = false;

    private readonly List<string> genderOptions = new() { "Male", "Female", "Other" };

    private void OnEnable()
    {
        baseURL = baseScript.baseURL;
        var root = uiDocument.rootVisualElement;

        // Bind fields
        usernameField = root.Q<TextField>("userName");
        ageField = root.Q<TextField>("age");
        warningLabelUsername = root.Q<Label>("warningLabelUserName");
        warningLabelAgeGender = root.Q<Label>("warningLabelAgeGender");
        warningLabelRole = root.Q<Label>("warningLabelRole");
        selectedText = root.Q<Label>("selectedText");

        signInButton = root.Q<Button>("signIn");
        backButton = root.Q<Button>("BackToLoginLabel");
        roleGroup = root.Q<RadioButtonGroup>();
        genderDropdown = root.Q<VisualElement>("genderDropdown");
        dropdownContent = root.Q<VisualElement>("dropdownContent");
        optionsList = root.Q<ScrollView>("optionsList");
        dropdownTrigger = root.Q<VisualElement>("dropdownTrigger");
        dropdownArrow = root.Q<Label>("dropdownArrow");

        // Setup UI
        SetupDropdown();
        SetupUsernameValidation();
        SetupAgeValidation();
        SetupButtonCallbacks();
    }

    private void SetupUsernameValidation()
    {
        usernameField.RegisterValueChangedCallback(evt => {
            Debug.Log("Username validation triggered");
            if (usernameCheckCoroutine != null) StopCoroutine(usernameCheckCoroutine);
            warningLabelUsername.text = "Verifying username...";
            isUsernameValid = false;

            if (evt.newValue.Length >= 6)
                usernameCheckCoroutine = StartCoroutine(DelayedUsernameCheck(evt.newValue));
            else
                warningLabelUsername.text = "Username must be at least 6 characters.";
        });
    }

    private void SetupAgeValidation()
    {
        // Restrict input to numbers only
        ageField.RegisterCallback<KeyDownEvent>(evt => {
            // Allow backspace, delete, tab, and arrow keys
            if (evt.keyCode == KeyCode.Backspace || evt.keyCode == KeyCode.Delete || 
                evt.keyCode == KeyCode.Tab || evt.keyCode == KeyCode.LeftArrow || 
                evt.keyCode == KeyCode.RightArrow)
            {
                return;
            }

            // Allow only numeric keys (0-9)
            if (evt.keyCode < KeyCode.Alpha0 || evt.keyCode > KeyCode.Alpha9)
            {
                // Also allow numpad numbers
                if (evt.keyCode < KeyCode.Keypad0 || evt.keyCode > KeyCode.Keypad9)
                {
                    evt.PreventDefault();
                    evt.StopPropagation();
                }
            }
        });

        // Validate age on value change
        ageField.RegisterValueChangedCallback(evt => {
            ValidateAge(evt.newValue);
        });
    }

    private void ValidateAge(string ageValue)
    {
        // Clear previous age-related warnings
        if (warningLabelAgeGender.text.Contains("age") || warningLabelAgeGender.text.Contains("Age"))
        {
            warningLabelAgeGender.text = "";
        }

        if (string.IsNullOrEmpty(ageValue))
        {
            return; // Don't show warning for empty field
        }

        if (int.TryParse(ageValue, out int age))
        {
            if (age < 1)
            {
                warningLabelAgeGender.text = "Age must be at least 1.";
                warningLabelAgeGender.style.color = Color.red;
            }
            else if (age > 100)
            {
                warningLabelAgeGender.text = "Age cannot be more than 100.";
                warningLabelAgeGender.style.color = Color.red;
            }
            else
            {
                // Age is valid, clear any age-related warnings
                if (warningLabelAgeGender.text.Contains("Age") || warningLabelAgeGender.text.Contains("age"))
                {
                    warningLabelAgeGender.text = "";
                }
            }
        }
        else
        {
            warningLabelAgeGender.text = "Please enter a valid age.";
            warningLabelAgeGender.style.color = Color.red;
        }
    }

    private IEnumerator DelayedUsernameCheck(string username)
    {
        yield return new WaitForSeconds(typingDelay);
        yield return StartCoroutine(VerifyUsernameCoroutine(username));
    }

    private IEnumerator VerifyUsernameCoroutine(string inputUsername)
    {
        Debug.Log("Verifying username... " + inputUsername);
        string token = AuthTokenManager.GetToken();
        string jsonData = JsonUtility.ToJson(new UsernamePayload { user_name = inputUsername });

        using (UnityWebRequest request = new UnityWebRequest(baseURL + verifyUsernameEndpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", token);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                warningLabelUsername.text = "Username not available.";
                isUsernameValid = false;
                Debug.Log("Username not available.");
            }
            else if (request.responseCode == 200)
            {
                isUsernameValid = true;
                Debug.Log("Username verified and accepted");
                warningLabelUsername.text = "Username available!";
                warningLabelUsername.style.color = Color.green;
            }
            else
            {
                warningLabelUsername.text = "Username verification failed.";
                isUsernameValid = false;
            }
        }
    }

    private void SetupButtonCallbacks()
    {
        signInButton.clicked += () => {
            if (ValidateForm())
            {
                Debug.Log("Basic Details Endpoint Called");
                StartCoroutine(SubmitBasicDetails());
            }
        };

        backButton.clicked += () => {
            Debug.Log("Back button clicked");
        };
    }

    private void SetupDropdown()
    {
        dropdownContent.style.display = DisplayStyle.None;
        PopulateDropdownOptions();
        dropdownTrigger.RegisterCallback<ClickEvent>(_ => ToggleDropdown());
        
        // Close dropdown when clicking outside
        uiDocument.rootVisualElement.RegisterCallback<ClickEvent>(evt => {
            if (dropdownContent.style.display == DisplayStyle.Flex && 
                !genderDropdown.worldBound.Contains(evt.position))
            {
                CloseDropdown();
            }
        });
    }

    private void PopulateDropdownOptions()
    {
        optionsList.Clear();
        foreach (var gender in genderOptions)
        {
            var option = new Label(gender);
            option.AddToClassList("option-text");
            option.RegisterCallback<ClickEvent>(_ => SelectGender(gender));
            optionsList.Add(option);
        }
    }

    private void ToggleDropdown()
    {
        bool isOpen = dropdownContent.style.display == DisplayStyle.Flex;
        
        if (isOpen)
        {
            CloseDropdown();
        }
        else
        {
            OpenDropdown();
        }
    }

    private void OpenDropdown()
    {
        // Position the dropdown content relative to the trigger
        var triggerBounds = dropdownTrigger.worldBound;
        var rootBounds = uiDocument.rootVisualElement.worldBound;
        
        // Calculate position relative to root
        float leftPosition = triggerBounds.x - rootBounds.x;
        float topPosition = triggerBounds.y + triggerBounds.height - rootBounds.y;
        
        dropdownContent.style.position = Position.Absolute;
        dropdownContent.style.left = leftPosition;
        dropdownContent.style.top = topPosition;
        dropdownContent.style.width = triggerBounds.width;
        dropdownContent.style.display = DisplayStyle.Flex;
        
        dropdownArrow.AddToClassList("rotated");
        
        // Move dropdown to root level to ensure it appears on top
        if (dropdownContent.parent != uiDocument.rootVisualElement)
        {
            dropdownContent.RemoveFromHierarchy();
            uiDocument.rootVisualElement.Add(dropdownContent);
        }
    }

    private void CloseDropdown()
    {
        dropdownContent.style.display = DisplayStyle.None;
        dropdownArrow.RemoveFromClassList("rotated");
    }

    private void SelectGender(string gender)
    {
        selectedGender = gender;
        selectedText.text = gender;
        selectedText.AddToClassList("has-selection");
        CloseDropdown();
    }

    private bool ValidateForm()
    {
        bool valid = true;

        // Reset warning labels
        if (!isUsernameValid)
        {
            warningLabelUsername.text = "Invalid username.";
            warningLabelUsername.style.color = Color.red;
            valid = false;
        }
        else if (warningLabelUsername.text == "Username available!")
        {
            warningLabelUsername.style.color = Color.green;
        }
        warningLabelAgeGender.text = "";
        warningLabelRole.text = "";

        if (!isUsernameValid)
        {
            warningLabelUsername.text = "Invalid username.";
            valid = false;
        }

        // Age validation
        if (string.IsNullOrEmpty(ageField.value))
        {
            warningLabelAgeGender.text = "Age is required.";
            valid = false;
        }
        else if (!int.TryParse(ageField.value, out int age))
        {
            warningLabelAgeGender.text = "Please enter a valid age.";
            valid = false;
        }
        else if (age < 1)
        {
            warningLabelAgeGender.text = "Age must be at least 1.";
            valid = false;
        }
        else if (age > 100)
        {
            warningLabelAgeGender.text = "Age cannot be more than 100.";
            valid = false;
        }

        // Gender validation
        if (string.IsNullOrEmpty(selectedGender))
        {
            if (!string.IsNullOrEmpty(warningLabelAgeGender.text))
                warningLabelAgeGender.text += " Please select gender.";
            else
                warningLabelAgeGender.text = "Please select gender.";
            valid = false;
        }

        // Role validation
        if (roleGroup.value < 0)
        {
            warningLabelRole.text = "Please select a role.";
            valid = false;
        }

        return valid;
    }

    private IEnumerator SubmitBasicDetails()
    {
        string token = AuthTokenManager.GetToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Auth token missing.");
            yield break;
        }

        string username = usernameField.value.Trim();
        int age = int.Parse(ageField.value.Trim());
        string gender = selectedGender.ToUpper();
        var choicesList = roleGroup.choices.ToList();
        string role = choicesList[roleGroup.value].ToUpper();

        BasicDetailsPayload data = new BasicDetailsPayload
        {
            userName = username,
            age = age,
            gender = gender,
            role = role
        };

        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest request = new UnityWebRequest(baseURL + basicDetailsEndpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", token);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Submission failed: " + request.error);
                warningLabelAgeGender.text = "Submission failed.";
            }
            else
            {
                Debug.Log("Basic details submitted successfully.");
                if (role == "USER")
                {
                    UIManager.Instance.OpenScreen(UIScreenType.UserDetails);
                }else if (role == "CREATOR")
                {
                    UIManager.Instance.OpenScreen(UIScreenType.CreatorBasicDetails);
                }
            }
        }
    }

    [System.Serializable]
    private class UsernamePayload { public string user_name; }
    
    [System.Serializable]
    private class BasicDetailsPayload
    {
        public string userName;
        public int age;
        public string gender;
        public string role;
    }
}
