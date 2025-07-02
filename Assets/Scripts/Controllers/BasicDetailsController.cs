// using UnityEngine;
// using UnityEngine.UIElements;
// using System.Collections.Generic;
// using System.Collections;
//
// public class BasicDetailsController : MonoBehaviour
// {
//     // UI Elements
//     private UIDocument uiDocument;
//     private VisualElement root;
//     private TextField userNameField;
//     private TextField ageField;
//     private Button signInButton;
//     private Button backButton;
//     
//     // Gender Dropdown Elements
//     private VisualElement genderDropdown;
//     private VisualElement dropdownTrigger;
//     private VisualElement dropdownContent;
//     private Label selectedText;
//     private Label dropdownArrow;
//     private ScrollView optionsList;
//     
//     // Gender options
//     private List<string> genderOptions = new List<string> { "Male", "Female", "Other" };
//     private string selectedGender = "";
//     private bool isDropdownOpen = false;
//
//     void Start()
//     {
//         // Automatically get the UI Document component from the same GameObject
//         uiDocument = GetComponent<UIDocument>();
//         
//         if (uiDocument == null)
//         {
//             Debug.LogError("UI Document component not found on this GameObject!");
//             return;
//         }
//         
//         // Wait for the UI to be ready before initializing
//         StartCoroutine(InitializeUIWhenReady());
//     }
//     
//     private IEnumerator InitializeUIWhenReady()
//     {
//         // Wait one frame to ensure UI Document is fully loaded
//         yield return null;
//         
//         root = uiDocument.rootVisualElement;
//         
//         if (root == null)
//         {
//             Debug.LogError("Root visual element is null! Make sure your UXML file is assigned to the UI Document.");
//             yield break;
//         }
//         
//         // Initialize UI elements
//         InitializeUIElements();
//         
//         // Setup dropdown functionality
//         SetupGenderDropdown();
//         
//         // Setup button events
//         SetupButtonEvents();
//         
//         Debug.Log("BasicDetailsController initialized successfully!");
//     }
//
//     private void InitializeUIElements()
//     {
//         // Get form fields
//         userNameField = root.Q<TextField>("userName");
//         ageField = root.Q<TextField>("age");
//         signInButton = root.Q<Button>("signIn");
//         backButton = root.Q<Button>("BackToLoginLabel");
//         
//         // Get gender dropdown elements
//         genderDropdown = root.Q<VisualElement>("genderDropdown");
//         dropdownTrigger = root.Q<VisualElement>("dropdownTrigger");
//         dropdownContent = root.Q<VisualElement>("dropdownContent");
//         selectedText = root.Q<Label>("selectedText");
//         dropdownArrow = dropdownTrigger?.Q<Label>(className: "dropdown-arrow"); // Find by class instead of name
//         optionsList = root.Q<ScrollView>("optionsList");
//         
//         // Debug check for missing elements
//         if (userNameField == null) Debug.LogError("userName TextField not found!");
//         if (ageField == null) Debug.LogError("age TextField not found!");
//         if (signInButton == null) Debug.LogError("signIn Button not found!");
//         if (backButton == null) Debug.LogError("BackToLoginLabel Button not found!");
//         if (genderDropdown == null) Debug.LogError("genderDropdown VisualElement not found!");
//         if (dropdownTrigger == null) Debug.LogError("dropdownTrigger VisualElement not found!");
//         if (dropdownContent == null) Debug.LogError("dropdownContent VisualElement not found!");
//         if (selectedText == null) Debug.LogError("selectedText Label not found!");
//         if (dropdownArrow == null) Debug.LogError("dropdownArrow Label not found!");
//         if (optionsList == null) Debug.LogError("optionsList ScrollView not found!");
//         
//         // Ensure age field only accepts numbers
//         if (ageField != null)
//         {
//             ageField.RegisterCallback<KeyDownEvent>(OnAgeKeyDown);
//         }
//     }
//
//     private void SetupGenderDropdown()
//     {
//         if (dropdownTrigger == null || optionsList == null) return;
//         
//         // Ensure dropdown starts closed
//         if (dropdownContent != null)
//         {
//             dropdownContent.style.display = DisplayStyle.None;
//         }
//         
//         // Populate dropdown options
//         PopulateGenderOptions();
//         
//         // Add click event to dropdown trigger
//         dropdownTrigger.RegisterCallback<ClickEvent>(OnDropdownTriggerClick);
//         
//         // Add click event to document to close dropdown when clicking outside
//         root.RegisterCallback<ClickEvent>(OnDocumentClick);
//     }
//
//     private void PopulateGenderOptions()
//     {
//         if (optionsList == null) return;
//         
//         // Clear existing options
//         optionsList.Clear();
//         
//         // Add each gender option
//         foreach (string gender in genderOptions)
//         {
//             var optionItem = new VisualElement();
//             optionItem.AddToClassList("option-item");
//             
//             var optionText = new Label(gender);
//             optionText.AddToClassList("option-text");
//             
//             optionItem.Add(optionText);
//             
//             // Add click event for option selection
//             optionItem.RegisterCallback<ClickEvent>((evt) => SelectGenderOption(gender, optionItem));
//             
//             optionsList.Add(optionItem);
//         }
//         
//         Debug.Log($"Added {genderOptions.Count} gender options to dropdown");
//     }
//
//     private void OnDropdownTriggerClick(ClickEvent evt)
//     {
//         evt.StopPropagation(); // Prevent event bubbling
//         ToggleDropdown();
//     }
//
//     private void ToggleDropdown()
//     {
//         isDropdownOpen = !isDropdownOpen;
//         
//         if (isDropdownOpen)
//         {
//             OpenDropdown();
//         }
//         else
//         {
//             CloseDropdown();
//         }
//     }
//
//     private void OpenDropdown()
//     {
//         if (dropdownContent == null || dropdownTrigger == null || dropdownArrow == null) return;
//         
//         dropdownContent.style.display = DisplayStyle.Flex;
//         dropdownTrigger.AddToClassList("active");
//         dropdownArrow.AddToClassList("rotated");
//         isDropdownOpen = true;
//         
//         Debug.Log("Dropdown opened");
//     }
//
//     private void CloseDropdown()
//     {
//         if (dropdownContent == null || dropdownTrigger == null || dropdownArrow == null) return;
//         
//         dropdownContent.style.display = DisplayStyle.None;
//         dropdownTrigger.RemoveFromClassList("active");
//         dropdownArrow.RemoveFromClassList("rotated");
//         isDropdownOpen = false;
//         
//         Debug.Log("Dropdown closed");
//     }
//
//     private void SelectGenderOption(string gender, VisualElement optionItem)
//     {
//         if (selectedText == null || optionsList == null) return;
//         
//         // Update selected gender
//         selectedGender = gender;
//         
//         // Update UI
//         selectedText.text = gender;
//         selectedText.AddToClassList("has-selection");
//         
//         // Remove selection from all options
//         var allOptions = optionsList.Query<VisualElement>(className: "option-item").ToList();
//         foreach (var option in allOptions)
//         {
//             option.RemoveFromClassList("selected");
//         }
//         
//         // Add selection to clicked option
//         optionItem.AddToClassList("selected");
//         
//         // Close dropdown
//         CloseDropdown();
//         
//         Debug.Log($"Selected gender: {selectedGender}");
//     }
//
//     private void OnDocumentClick(ClickEvent evt)
//     {
//         if (genderDropdown == null) return;
//         
//         // Close dropdown if clicking outside of it
//         if (isDropdownOpen && !genderDropdown.worldBound.Contains(evt.position))
//         {
//             CloseDropdown();
//         }
//     }
//
//     private void OnAgeKeyDown(KeyDownEvent evt)
//     {
//         // Allow only numbers, backspace, delete, and navigation keys
//         if (!char.IsDigit(evt.character) && 
//             evt.keyCode != KeyCode.Backspace && 
//             evt.keyCode != KeyCode.Delete &&
//             evt.keyCode != KeyCode.LeftArrow &&
//             evt.keyCode != KeyCode.RightArrow &&
//             evt.keyCode != KeyCode.Tab)
//         {
//             evt.PreventDefault();
//         }
//     }
//
//     private void SetupButtonEvents()
//     {
//         // Sign In button event
//         if (signInButton != null)
//         {
//             signInButton.RegisterCallback<ClickEvent>(OnSignInClick);
//         }
//         
//         // Back button event
//         if (backButton != null)
//         {
//             backButton.RegisterCallback<ClickEvent>(OnBackClick);
//         }
//     }
//
//     private void OnSignInClick(ClickEvent evt)
//     {
//         Debug.Log("Sign In button clicked");
//         
//         // Validate form data
//         if (ValidateForm())
//         {
//             // Process sign in
//             ProcessSignIn();
//         }
//     }
//
//     private bool ValidateForm()
//     {
//         bool isValid = true;
//         
//         // Validate username
//         if (userNameField == null || string.IsNullOrEmpty(userNameField.value.Trim()))
//         {
//             Debug.LogWarning("Username is required");
//             isValid = false;
//         }
//         
//         // Validate age
//         if (ageField == null || string.IsNullOrEmpty(ageField.value.Trim()) || !int.TryParse(ageField.value, out int age) || age <= 0)
//         {
//             Debug.LogWarning("Valid age is required");
//             isValid = false;
//         }
//         
//         // Validate gender selection
//         if (string.IsNullOrEmpty(selectedGender))
//         {
//             Debug.LogWarning("Gender selection is required");
//             isValid = false;
//         }
//         
//         return isValid;
//     }
//
//     private void ProcessSignIn()
//     {
//         string username = userNameField.value.Trim();
//         string ageValue = ageField.value.Trim();
//         
//         Debug.Log($"Sign In Attempted:");
//         Debug.Log($"Username: {username}");
//         Debug.Log($"Age: {ageValue}");
//         Debug.Log($"Gender: {selectedGender}");
//         
//         // Here you would typically:
//         // 1. Save user data to PlayerPrefs or a data management system
//         // 2. Navigate to the next screen
//         // 3. Send data to a server if needed
//         
//         // Example: Save to PlayerPrefs
//         PlayerPrefs.SetString("Username", username);
//         PlayerPrefs.SetString("Age", ageValue);
//         PlayerPrefs.SetString("Gender", selectedGender);
//         PlayerPrefs.Save();
//         
//         Debug.Log("User data saved successfully!");
//         
//         // Navigate to next scene or show success message
//         // SceneManager.LoadScene("NextScene");
//     }
//
//     private void OnBackClick(ClickEvent evt)
//     {
//         Debug.Log("Back button clicked");
//         
//         // Here you would typically navigate back to the previous screen
//         // SceneManager.LoadScene("LoginScene");
//     }
//
//     // Public methods for external access
//     public string GetSelectedGender()
//     {
//         return selectedGender;
//     }
//
//     public void SetSelectedGender(string gender)
//     {
//         if (genderOptions.Contains(gender))
//         {
//             selectedGender = gender;
//             if (selectedText != null)
//             {
//                 selectedText.text = gender;
//                 selectedText.AddToClassList("has-selection");
//             }
//         }
//     }
//
//     public bool IsFormValid()
//     {
//         return ValidateForm();
//     }
// }

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
    public float typingDelay = 1.0f;
    public string baseURL = "http://localhost:3000";
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

    private void Start()
    {
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
        SetupButtonCallbacks();
    }

    private void SetupUsernameValidation()
    {
        usernameField.RegisterValueChangedCallback(evt => {
            if (usernameCheckCoroutine != null) StopCoroutine(usernameCheckCoroutine);
            warningLabelUsername.text = "Verifying username...";
            isUsernameValid = false;

            if (evt.newValue.Length >= 6)
                usernameCheckCoroutine = StartCoroutine(DelayedUsernameCheck(evt.newValue));
            else
                warningLabelUsername.text = "Username must be at least 6 characters.";
        });
    }

    private IEnumerator DelayedUsernameCheck(string username)
    {
        yield return new WaitForSeconds(typingDelay);
        yield return StartCoroutine(VerifyUsernameCoroutine(username));
    }

    private IEnumerator VerifyUsernameCoroutine(string inputUsername)
    {
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
            }
            else if (request.responseCode == 200)
            {
                isUsernameValid = true;
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
                StartCoroutine(SubmitBasicDetails());
        };

        backButton.clicked += () => {
            // Implement navigation back logic if needed
            Debug.Log("Back button clicked");
        };
    }

    private void SetupDropdown()
    {
        dropdownContent.style.display = DisplayStyle.None;
        PopulateDropdownOptions();
        dropdownTrigger.RegisterCallback<ClickEvent>(_ => ToggleDropdown());
        uiDocument.rootVisualElement.RegisterCallback<ClickEvent>(evt => {
            if (dropdownContent.style.display == DisplayStyle.Flex && !genderDropdown.worldBound.Contains(evt.position))
                CloseDropdown();
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
        dropdownContent.style.display = isOpen ? DisplayStyle.None : DisplayStyle.Flex;
        dropdownArrow.ToggleInClassList("rotated");
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
        CloseDropdown();
    }

    private bool ValidateForm()
    {
        bool valid = true;

        warningLabelAgeGender.text = "";
        warningLabelRole.text = "";

        if (!isUsernameValid)
        {
            warningLabelUsername.text = "Invalid username.";
            valid = false;
        }

        if (string.IsNullOrEmpty(ageField.value) || !int.TryParse(ageField.value, out int age) || age < 1)
        {
            warningLabelAgeGender.text = "Valid age required.";
            valid = false;
        }

        if (string.IsNullOrEmpty(selectedGender))
        {
            warningLabelAgeGender.text += " Select gender.";
            valid = false;
        }

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
            username = username,
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

            // TODO: Show loader here
            yield return request.SendWebRequest();
            // TODO: Hide loader here

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Submission failed: " + request.error);
                warningLabelAgeGender.text = "Submission failed.";
            }
            else
            {
                Debug.Log("Basic details submitted successfully.");
                //UIManager.Instance.OpenScreen(UIScreenType.Feed); // or next screen
            }
        }
    }

    [System.Serializable]
    private class UsernamePayload { public string user_name; }
    [System.Serializable]
    private class BasicDetailsPayload
    {
        public string username;
        public int age;
        public string gender;
        public string role;
    }
}
