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

    private void Start()
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
                Debug.Log("Basic Details Endpoint Called");
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
