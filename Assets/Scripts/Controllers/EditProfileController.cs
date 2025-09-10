using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class EditProfileController : MonoBehaviour
{
    private CreatorLocationDropdownHandler creatorLocationDropdownRef;

    [Header("UI Document")]
    public UIDocument uiDocument;

    private VisualElement root;
    private Button backButton;
    private Button updateProfileButton;

    private TextField fullNameField;
    private TextField firstNameField;
    private TextField lastNameField;
    private TextField userNameField;
    private TextField ageField;

    private DropdownHandler genderDropdown;
    private LocationDropdownHandler locationDropdown;

    private VisualElement fullNameGroup;
    private VisualElement firstNameGroup;
    private VisualElement lastNameGroup;

    private Label warningFullName;
    private Label warningFirstName;
    private Label warningLastName;
    private Label warningUserName;
    private Label warningAgeGender;

    private ProfileCache originalData;
    private Dictionary<string, object> changedFields = new Dictionary<string, object>();

    private string[] genderOptions = { "Male", "Female", "Others" };

    private List<string> citiesList = new List<string>();
    private bool citiesLoaded = false;

    private bool isUpdatingProfile = false;

    private void OnEnable()
    {
        InitializeUI();
        LoadProfileData();
        SetupFieldVisibility();
        SetupChangeTracking();
        SetupOutsideClickHandler();
        StartCoroutine(FetchCitiesFromAPI());
        UpdateUpdateButtonState();
    }

    private void InitializeUI()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        root = uiDocument.rootVisualElement;

        backButton = root.Q<Button>("backButton");
        updateProfileButton = root.Q<Button>("updateProfile");

        fullNameField = root.Q<TextField>("fullName");
        firstNameField = root.Q<TextField>("firstName");
        lastNameField = root.Q<TextField>("lastName");
        userNameField = root.Q<TextField>("userName");
        ageField = root.Q<TextField>("age");

        fullNameGroup = root.Q<VisualElement>("fullNameInputGroup");
        firstNameGroup = root.Q<VisualElement>("firstNameInputGroup");
        lastNameGroup = root.Q<VisualElement>("lastNameInputGroup");

        warningFullName = root.Q<Label>("WarningFullName");
        warningFirstName = root.Q<Label>("WarningFirstName");
        warningLastName = root.Q<Label>("WarningLastName");
        warningUserName = root.Q<Label>("warningLabelUserName");
        warningAgeGender = root.Q<Label>("warningLabelAgeGender");

        InitializeGenderDropdown();

        backButton?.RegisterCallback<ClickEvent>(_ => OnBackButtonClicked());
        updateProfileButton?.RegisterCallback<ClickEvent>(_ => OnUpdateProfileClicked());
    }

    private void InitializeGenderDropdown()
    {
        var genderDropdownElement = root.Q<VisualElement>("genderDropdown");
        if (genderDropdownElement != null)
        {
            genderDropdown = new DropdownHandler(genderDropdownElement, genderOptions.ToList(), uiDocument);
            genderDropdown.OnSelectionChanged += (selectedValue) =>
            {
                TrackFieldChange("gender", selectedValue);
            };
        }
    }

    private void InitializeLocationDropdown()
    {
        var locationDropdownElement = root.Q<VisualElement>("locationDropdown");
        if (locationDropdownElement != null && citiesLoaded)
        {
            bool isCreator = !string.IsNullOrEmpty(originalData.role) &&
                             originalData.role.ToLower() == "creator";

            if (isCreator)
            {
                var creatorLocationDropdown = new CreatorLocationDropdownHandler(locationDropdownElement, citiesList, uiDocument);
                creatorLocationDropdown.OnSelectionChanged += (selectedValues) =>
                {
                    TrackFieldChange("region", selectedValues);
                };

                if (originalData != null && originalData.region != null && originalData.region.Count > 0)
                {
                    creatorLocationDropdown.SetSelectedValues(originalData.region);
                }

                creatorLocationDropdownRef = creatorLocationDropdown;
            }
            else
            {
                locationDropdown = new LocationDropdownHandler(locationDropdownElement, citiesList, uiDocument);
                locationDropdown.OnSelectionChanged += (selectedValue) =>
                {
                    TrackFieldChange("homeLocation", selectedValue);
                };

                if (originalData != null && !string.IsNullOrEmpty(originalData.homeLocation))
                {
                    locationDropdown.SetSelectedValue(originalData.homeLocation);
                }
            }
        }
    }

    private IEnumerator FetchCitiesFromAPI()
    {
        string url = $"{baseScript.baseURL}/api/v1/public/cities";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                CityWrapper wrapper = JsonUtility.FromJson<CityWrapper>(json);
                if (wrapper != null && wrapper.success && wrapper.data != null)
                {
                    citiesList = wrapper.data.Select(c => c.cityName).ToList();
                    citiesLoaded = true;
                    InitializeLocationDropdown();
                }
            }
        }
    }

    private void SetupOutsideClickHandler()
    {
        root.RegisterCallback<ClickEvent>(evt =>
        {
            if (genderDropdown != null && genderDropdown.IsOpen() &&
                !genderDropdown.IsClickInsideDropdown(evt.position))
            {
                genderDropdown.CloseDropdown();
            }

            if (locationDropdown != null && locationDropdown.IsOpen() &&
                !locationDropdown.IsClickInsideDropdown(evt.position))
            {
                locationDropdown.CloseDropdown();
            }

            if (creatorLocationDropdownRef != null && creatorLocationDropdownRef.IsOpen() &&
                !creatorLocationDropdownRef.IsClickInsideDropdown(evt.position))
            {
                creatorLocationDropdownRef.CloseDropdown();
            }
        });
    }

    private void LoadProfileData()
    {
        if (ProfileDataHandlers.Instance != null && ProfileDataHandlers.Instance.ProfileData != null)
        {
            originalData = ProfileDataHandlers.Instance.ProfileData;
            PopulateFields(originalData);
        }
        else
        {
            if (ProfileDataHandlers.Instance != null)
            {
                var cachedData = ProfileDataHandlers.Instance.LoadProfileCache();
                if (cachedData != null)
                {
                    originalData = cachedData;
                    PopulateFields(cachedData);
                }
            }
        }
    }

    private void SetupFieldVisibility()
    {
        if (originalData == null) return;

        bool isCreator = !string.IsNullOrEmpty(originalData.role) &&
                        originalData.role.ToLower() == "creator";

        if (isCreator)
        {
            if (fullNameGroup != null) fullNameGroup.style.display = DisplayStyle.Flex;
            if (firstNameGroup != null) firstNameGroup.style.display = DisplayStyle.None;
            if (lastNameGroup != null) lastNameGroup.style.display = DisplayStyle.None;
        }
        else
        {
            if (fullNameGroup != null) fullNameGroup.style.display = DisplayStyle.None;
            if (firstNameGroup != null) firstNameGroup.style.display = DisplayStyle.Flex;
            if (lastNameGroup != null) lastNameGroup.style.display = DisplayStyle.Flex;
        }
    }

    private string ConvertGenderForDisplay(string apiGender)
    {
        if (string.IsNullOrEmpty(apiGender))
            return "Select your Gender";
        switch (apiGender.ToUpper())
        {
            case "MALE": return "Male";
            case "FEMALE": return "Female";
            case "OTHER":
            case "OTHERS": return "Others";
            default: return apiGender;
        }
    }

    private void PopulateFields(ProfileCache profileData)
    {
        if (profileData == null) return;

        bool isCreator = !string.IsNullOrEmpty(profileData.role) &&
                         profileData.role.ToLower() == "creator";

        if (isCreator)
        {
            string fullName = "";
            if (!string.IsNullOrEmpty(profileData.firstName) || !string.IsNullOrEmpty(profileData.lastName))
            {
                fullName = $"{profileData.firstName} {profileData.lastName}".Trim();
            }
            else if (!string.IsNullOrEmpty(profileData.userName))
            {
                fullName = profileData.userName;
            }

            if (fullNameField != null)
            {
                fullNameField.value = fullName;
            }
        }
        else
        {
            if (firstNameField != null)
                firstNameField.value = profileData.firstName ?? "";

            if (lastNameField != null)
                lastNameField.value = profileData.lastName ?? "";
        }

        if (userNameField != null)
            userNameField.value = profileData.userName ?? "";

        if (ageField != null)
            ageField.value = profileData.age > 0 ? profileData.age.ToString() : "";

        if (genderDropdown != null)
        {
            string currentGender = ConvertGenderForDisplay(profileData.gender);
            genderDropdown.SetSelectedValue(currentGender);
        }
    }

    private void SetupChangeTracking()
    {
        fullNameField?.RegisterCallback<ChangeEvent<string>>(evt =>
        {
            if (evt.newValue != evt.previousValue)
            {
                var (ok, msg) = ValidateFullName(evt.newValue);
                SetWarning(warningFullName, msg);
                if (ok) TrackFieldChange("fullName", evt.newValue);
                else RemoveChange("fullName");
            }
        });

        firstNameField?.RegisterCallback<ChangeEvent<string>>(evt =>
        {
            if (evt.newValue != evt.previousValue)
            {
                var (ok, msg) = ValidateName(evt.newValue, "First name");
                SetWarning(warningFirstName, msg);
                if (ok) TrackFieldChange("firstName", evt.newValue);
                else RemoveChange("firstName");
            }
        });

        lastNameField?.RegisterCallback<ChangeEvent<string>>(evt =>
        {
            if (evt.newValue != evt.previousValue)
            {
                var (ok, msg) = ValidateName(evt.newValue, "Last name");
                SetWarning(warningLastName, msg);
                if (ok) TrackFieldChange("lastName", evt.newValue);
                else RemoveChange("lastName");
            }
        });

        userNameField?.RegisterCallback<ChangeEvent<string>>(evt =>
        {
            if (evt.newValue != evt.previousValue)
            {
                SetWarning(warningUserName, "");
                TrackFieldChange("userName", evt.newValue);
            }
        });

        ageField?.RegisterCallback<ChangeEvent<string>>(evt =>
        {
            if (evt.newValue == evt.previousValue) return;
            var (ok, clamped, msg) = ValidateAge(evt.newValue);
            if (ageField.value != clamped) ageField.SetValueWithoutNotify(clamped);
            SetWarning(warningAgeGender, msg);
            if (ok)
            {
                if (string.IsNullOrEmpty(clamped)) RemoveChange("age");
                else TrackFieldChange("age", clamped);
            }
            else
            {
                RemoveChange("age");
            }
        });
    }

    private void UpdateUpdateButtonState()
    {
        if (updateProfileButton == null) return;

        bool hasChanges = changedFields.Count > 0;
        bool hasValidationErrors =
            !string.IsNullOrEmpty(warningFirstName?.text) ||
            !string.IsNullOrEmpty(warningLastName?.text) ||
            !string.IsNullOrEmpty(warningFullName?.text) ||
            !string.IsNullOrEmpty(warningUserName?.text) ||
            !string.IsNullOrEmpty(warningAgeGender?.text);

        bool enable = hasChanges && !isUpdatingProfile && !hasValidationErrors;

        updateProfileButton.SetEnabled(enable);

        if (enable) updateProfileButton.RemoveFromClassList("btn-disabled");
        else updateProfileButton.AddToClassList("btn-disabled");
    }

    private void TrackFieldChange(string fieldName, object newValue)
    {
        if (originalData == null) return;

        object originalValue = GetOriginalFieldValue(fieldName);

        if (!AreValuesEqual(originalValue, newValue))
        {
            changedFields[fieldName] = newValue;
        }
        else
        {
            if (changedFields.ContainsKey(fieldName))
                changedFields.Remove(fieldName);
        }

        UpdateUpdateButtonState();
    }

    private void RemoveChange(string fieldName)
    {
        if (changedFields.ContainsKey(fieldName))
            changedFields.Remove(fieldName);
        UpdateUpdateButtonState();
    }

    private object GetOriginalFieldValue(string fieldName)
    {
        if (originalData == null) return null;

        bool isCreator = !string.IsNullOrEmpty(originalData.role) &&
                         originalData.role.ToLower() == "creator";

        return fieldName switch
        {
            "firstName"     => originalData.firstName ?? "",
            "lastName"      => originalData.lastName ?? "",
            "userName"      => originalData.userName ?? "",
            "fullName"      => $"{originalData.firstName} {originalData.lastName}".Trim(),
            "homeLocation"  => isCreator ? null : (originalData.homeLocation ?? ""),
            "region"        => isCreator ? (originalData.region ?? new List<string>()) : null,
            "gender"        => ConvertGenderForDisplay(originalData.gender),
            "age"           => originalData.age > 0 ? originalData.age.ToString() : "",
            _               => null
        };
    }

    private bool AreValuesEqual(object value1, object value2)
    {
        if (value1 == null && value2 == null) return true;
        if (value1 == null || value2 == null) return false;

        if (value1 is List<string> list1 && value2 is List<string> list2)
        {
            if (list1.Count != list2.Count) return false;
            return list1.SequenceEqual(list2);
        }

        if (value1 is int int1 && value2 is int int2)
            return int1 == int2;

        string str1 = value1.ToString();
        string str2 = value2.ToString();

        if (int.TryParse(str1, out int num1) && int.TryParse(str2, out int num2))
            return num1 == num2;

        return str1 == str2;
    }

    private (string firstName, string lastName) SplitFullName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName?.Trim()))
            return ("", "");

        string trimmedName = fullName.Trim();
        int firstSpaceIndex = trimmedName.IndexOf(' ');

        if (firstSpaceIndex == -1)
            return (trimmedName, "");

        string firstName = trimmedName.Substring(0, firstSpaceIndex);
        string lastName = trimmedName.Substring(firstSpaceIndex + 1);

        return (firstName, lastName);
    }

    private Dictionary<string, object> PrepareAPIPayload()
    {
        var payload = new Dictionary<string, object>();
        bool isCreator = !string.IsNullOrEmpty(originalData.role) &&
                         originalData.role.ToLower() == "creator";

        foreach (var change in changedFields)
        {
            switch (change.Key)
            {
                case "fullName":
                    if (isCreator)
                    {
                        var (firstName, lastName) = SplitFullName(change.Value.ToString());
                        if (!string.IsNullOrEmpty(firstName)) payload["firstName"] = firstName;
                        if (!string.IsNullOrEmpty(lastName))  payload["lastName"]  = lastName;
                    }
                    break;

                case "firstName":
                case "lastName":
                case "userName":
                    if (!string.IsNullOrEmpty(change.Value?.ToString()))
                        payload[change.Key] = change.Value.ToString();
                    break;

                case "homeLocation":
                    if (!isCreator && !string.IsNullOrEmpty(change.Value?.ToString()))
                        payload["homeLocation"] = change.Value.ToString();
                    break;

                case "region":
                    if (isCreator && change.Value is List<string> regionList && regionList.Count > 0)
                        payload["region"] = regionList;
                    break;

                case "age":
                    if (int.TryParse(change.Value?.ToString(), out int age))
                        payload["age"] = age;
                    break;

                case "gender":
                    string genderValue = change.Value?.ToString();
                    if (!string.IsNullOrEmpty(genderValue) && genderValue != "Select your Gender")
                        payload["gender"] = genderValue.ToUpper();
                    break;
            }
        }

        return payload;
    }

    private void OnBackButtonClicked()
    {
        UIManager.Instance.TransitionScreens(UIScreenType.EditProfile, UIScreenType.ProfileSetting);
    }

    private void OnUpdateProfileClicked()
    {
        if (isUpdatingProfile) return;
        if (changedFields.Count > 0)
        {
            StartCoroutine(UpdateProfileAPI());
        }
    }

    private IEnumerator UpdateProfileAPI()
    {
        isUpdatingProfile = true;
        UpdateUpdateButtonState();

        var payload = PrepareAPIPayload();
        if (payload.Count == 0)
        {
            isUpdatingProfile = false;
            UpdateUpdateButtonState();
            yield break;
        }

        string jsonPayload = MiniJSON.JSON.Serialize(payload);
        string url = $"{baseScript.baseURL}/api/v1/profile/edit-profile";

        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            string authToken = AuthTokenManager.GetToken();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", authToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    ProfileUpdateResponse response = JsonUtility.FromJson<ProfileUpdateResponse>(request.downloadHandler.text);
                    if (response.success)
                    {
                        UpdateOriginalDataWithChanges(response.data);
                        changedFields.Clear();
                        ShowUpdateSuccessMessage();
                    }
                    else
                    {
                        ShowUpdateErrorMessage(response.message);
                    }
                }
                catch
                {
                    ShowUpdateErrorMessage("Unable to parse server response.");
                }
            }
            else
            {
                ShowUpdateErrorMessage(request.error);
            }
        }

        isUpdatingProfile = false;
        UpdateUpdateButtonState();
    }

    private void UpdateOriginalDataWithChanges(ProfileUpdateData updatedData)
    {
        if (originalData == null || updatedData == null) return;

        bool isCreator = !string.IsNullOrEmpty(originalData.role) &&
                         originalData.role.ToLower() == "creator";

        if (!string.IsNullOrEmpty(updatedData.firstName)) originalData.firstName = updatedData.firstName;
        if (!string.IsNullOrEmpty(updatedData.lastName))  originalData.lastName  = updatedData.lastName;
        if (!string.IsNullOrEmpty(updatedData.userName))  originalData.userName  = updatedData.userName;
        if (updatedData.age > 0)                          originalData.age       = updatedData.age;
        if (!string.IsNullOrEmpty(updatedData.gender))    originalData.gender    = updatedData.gender;

        if (isCreator)
        {
            if (changedFields.ContainsKey("region") && changedFields["region"] is List<string> regionList)
                originalData.region = new List<string>(regionList);
        }
        else
        {
            if (!string.IsNullOrEmpty(updatedData.homeLocation))
                originalData.homeLocation = updatedData.homeLocation;
        }
    }

    private void ShowUpdateSuccessMessage() { }
    private void ShowUpdateErrorMessage(string error) { }

    private void OnDestroy()
    {
        genderDropdown?.Dispose();
        locationDropdown?.Dispose();
        creatorLocationDropdownRef?.Dispose();
    }

    // ===== Validation helpers =====

    private (bool ok, string msg) ValidateName(string value, string fieldLabel)
    {
        string v = value?.Trim() ?? "";
        if (string.IsNullOrEmpty(v)) return (true, "");
        for (int i = 0; i < v.Length; i++)
        {
            char c = v[i];
            if (!(c >= 'A' && c <= 'Z') && !(c >= 'a' && c <= 'z'))
                return (false, $"{fieldLabel} must contain alphabets only.");
        }
        return (true, "");
    }

    private (bool ok, string msg) ValidateFullNameTextOnly(string value)
    {
        string v = value?.Trim() ?? "";
        if (string.IsNullOrEmpty(v)) return (true, "");
        foreach (var part in v.Split(' '))
        {
            if (string.IsNullOrEmpty(part)) continue;
            for (int i = 0; i < part.Length; i++)
            {
                char c = part[i];
                if (!(c >= 'A' && c <= 'Z') && !(c >= 'a' && c <= 'z'))
                    return (false, "Full name must contain alphabets only.");
            }
        }
        return (true, "");
    }

    private (bool ok, string msg) ValidateFullName(string fullName)
    {
        return ValidateFullNameTextOnly(fullName);
    }

    private (bool ok, string clamped, string msg) ValidateAge(string input)
    {
        string digitsOnly = new string((input ?? "").Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digitsOnly)) return (true, "", "");
        if (!int.TryParse(digitsOnly, out int parsed)) return (false, "", "Age must be a number between 0 and 99.");

        if (parsed < 0 || parsed > 99)
        {
            int clamped = Mathf.Clamp(parsed, 0, 99);
            return (false, clamped.ToString(), "Age must be between 0 and 99.");
        }

        return (true, parsed.ToString(), "");
    }

    private void SetWarning(Label target, string message)
    {
        if (target == null) return;
        target.text = message ?? "";
        if (string.IsNullOrEmpty(message)) target.RemoveFromClassList("error");
        else target.AddToClassList("error");
        UpdateUpdateButtonState();
    }
}

/* ======== DATA CLASSES ======== */
[System.Serializable]
public class CityData { public string cityName; }

[System.Serializable]
public class CityWrapper
{
    public bool success;
    public CityData[] data;
}

[System.Serializable]
public class ProfileUpdateResponse
{
    public bool success;
    public string message;
    public ProfileUpdateData data;
}

[System.Serializable]
public class ProfileUpdateData
{
    public string firstName;
    public string lastName;
    public int age;
    public string gender;
    public string userName;
    public string email;
    public string role;
    public string updatedAt;
    public string homeLocation;
}

[System.Serializable]
public class SerializableDictionary
{
    [SerializeField] private string firstName;
    [SerializeField] private string lastName;
    [SerializeField] private int? age;
    [SerializeField] private string gender;
    [SerializeField] private string userName;
    [SerializeField] private string homeLocation;
    [SerializeField] private string[] region;

    public bool ShouldSerializeFirstName() => !string.IsNullOrEmpty(firstName);
    public bool ShouldSerializeLastName() => !string.IsNullOrEmpty(lastName);
    public bool ShouldSerializeAge() => age.HasValue && age.Value >= 0 && age.Value <= 99;
    public bool ShouldSerializeGender() => !string.IsNullOrEmpty(gender);
    public bool ShouldSerializeUserName() => !string.IsNullOrEmpty(userName);
    public bool ShouldSerializeHomeLocation() => !string.IsNullOrEmpty(homeLocation);
    public bool ShouldSerializeRegion() => region != null && region.Length > 0;

    public SerializableDictionary(Dictionary<string, object> dict)
    {
        if (dict.ContainsKey("firstName"))
            firstName = dict["firstName"]?.ToString();

        if (dict.ContainsKey("lastName"))
            lastName = dict["lastName"]?.ToString();

        if (dict.ContainsKey("age") && int.TryParse(dict["age"]?.ToString(), out int ageValue))
            age = ageValue;

        if (dict.ContainsKey("gender"))
            gender = dict["gender"]?.ToString();

        if (dict.ContainsKey("userName"))
            userName = dict["userName"]?.ToString();

        if (dict.ContainsKey("homeLocation"))
            homeLocation = dict["homeLocation"]?.ToString();

        if (dict.ContainsKey("region") && dict["region"] is List<string> regionList)
            region = regionList.ToArray();
    }
}

public class DropdownHandler
{
    protected VisualElement dropdownElement;
    protected VisualElement dropdownTrigger;
    protected VisualElement dropdownContent;
    protected ScrollView optionsList;
    protected Label selectedText;
    protected Label dropdownArrow;
    protected UIDocument uiDocument;

    protected List<string> options;
    protected string selectedValue = "";

    public System.Action<string> OnSelectionChanged;

    public DropdownHandler(VisualElement dropdown, List<string> optionsList, UIDocument document)
    {
        dropdownElement = dropdown;
        options = optionsList;
        uiDocument = document;
        Initialize();
    }

    private void Initialize()
    {
        dropdownTrigger = dropdownElement.Q<VisualElement>("dropdownTrigger");
        dropdownContent = dropdownElement.Q<VisualElement>("dropdownContent");
        optionsList = dropdownElement.Q<ScrollView>("optionsList");
        selectedText = dropdownElement.Q<Label>("selectedText");
        dropdownArrow = dropdownElement.Q<Label>("dropdownArrow");
        dropdownContent.style.display = DisplayStyle.None;

        SetupTriggerCallback();
        PopulateOptions();
    }

    private void SetupTriggerCallback()
    {
        dropdownTrigger?.RegisterCallback<ClickEvent>(evt => {
            evt.StopPropagation();
            ToggleDropdown();
        });
    }

    private void ToggleDropdown()
    {
        bool isVisible = dropdownContent.style.display == DisplayStyle.Flex;

        if (isVisible)
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
        if (dropdownContent.parent != uiDocument.rootVisualElement)
        {
            dropdownContent.RemoveFromHierarchy();
            uiDocument.rootVisualElement.Add(dropdownContent);
        }
        var triggerBounds = dropdownTrigger.worldBound;
        var rootBounds = uiDocument.rootVisualElement.worldBound;
        float leftPosition = triggerBounds.x - rootBounds.x;
        float topPosition = triggerBounds.y + triggerBounds.height - rootBounds.y;

        dropdownContent.style.position = Position.Absolute;
        dropdownContent.style.left = leftPosition;
        dropdownContent.style.top = topPosition;
        dropdownContent.style.width = triggerBounds.width;
        dropdownContent.style.display = DisplayStyle.Flex;
        if (dropdownArrow != null)
        {
            dropdownArrow.text = "▲";
        }
    }

    public void CloseDropdown()
    {
        dropdownContent.style.display = DisplayStyle.None;
        if (dropdownArrow != null)
        {
            dropdownArrow.text = "▼";
        }
    }

    public bool IsOpen()
    {
        return dropdownContent.style.display == DisplayStyle.Flex;
    }

    public bool IsClickInsideDropdown(Vector2 clickPosition)
    {
        return dropdownElement.worldBound.Contains(clickPosition) ||
               (IsOpen() && dropdownContent.worldBound.Contains(clickPosition));
    }

    private void PopulateOptions()
    {
        if (optionsList == null) return;

        optionsList.Clear();

        foreach (string option in options)
        {
            var optionElement = new VisualElement();
            optionElement.AddToClassList("option-item");
            var label = new Label(option);
            label.AddToClassList("option-text");
            optionElement.Add(label);
            optionElement.RegisterCallback<ClickEvent>(evt => {
                evt.StopPropagation();
                SelectOption(option);
            });

            optionsList.Add(optionElement);
        }
    }

    public void SelectOption(string option)
    {
        selectedValue = option;
        if (selectedText != null)
        {
            selectedText.text = option;
            selectedText.AddToClassList("has-selection");
        }

        CloseDropdown();
        OnSelectionChanged?.Invoke(option);
    }

    public void SetSelectedValue(string value)
    {
        selectedValue = value;
        if (selectedText != null)
        {
            selectedText.text = value;
            if (value != "Select your Gender" && value != "Select your location")
                selectedText.AddToClassList("has-selection");
        }
    }

    public string GetSelectedValue()
    {
        return selectedValue;
    }

    public void Dispose()
    {
        dropdownTrigger?.UnregisterCallback<ClickEvent>(evt => ToggleDropdown());
    }
}

public class LocationDropdownHandler : DropdownHandler
{
    private TextField searchField;
    private List<string> allOptions;

    public LocationDropdownHandler(VisualElement dropdown, List<string> optionsList, UIDocument document) : base(dropdown, optionsList, document)
    {
        allOptions = new List<string>(optionsList);
        SetupSearchField();
    }

    private void SetupSearchField()
    {
        searchField = dropdownElement.Q<TextField>("searchField");
        if (searchField != null)
        {
            searchField.RegisterCallback<ChangeEvent<string>>(evt => FilterOptions(evt.newValue));
        }
    }

    private void FilterOptions(string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            UpdateOptions(allOptions);
            return;
        }

        var filteredOptions = allOptions
            .Where(option => option.ToLower().Contains(searchText.ToLower()))
            .ToList();

        UpdateOptions(filteredOptions);
    }

    private void UpdateOptions(List<string> newOptions)
    {
        if (optionsList == null) return;
        optionsList.Clear();
        foreach (string option in newOptions)
        {
            var optionElement = new VisualElement();
            optionElement.AddToClassList("option-item");
            var label = new Label(option);
            label.AddToClassList("option-text");
            optionElement.Add(label);

            optionElement.RegisterCallback<ClickEvent>(evt => {
                evt.StopPropagation();
                SelectOption(option);
            });

            optionsList.Add(optionElement);
        }
    }
}

public class CreatorLocationDropdownHandler
{
    protected VisualElement dropdownElement;
    protected VisualElement dropdownTrigger;
    protected VisualElement dropdownContent;
    protected ScrollView optionsList;
    protected Label selectedText;
    protected Label dropdownArrow;
    protected UIDocument uiDocument;
    protected TextField searchField;

    protected List<string> allOptions;
    protected List<string> filteredOptions;
    protected List<string> selectedValues = new List<string>();
    protected const int MAX_SELECTIONS = 3;

    public System.Action<List<string>> OnSelectionChanged;

    public CreatorLocationDropdownHandler(VisualElement dropdown, List<string> optionsList, UIDocument document)
    {
        dropdownElement = dropdown;
        allOptions = new List<string>(optionsList);
        filteredOptions = new List<string>(optionsList);
        uiDocument = document;
        Initialize();
    }

    private void Initialize()
    {
        dropdownTrigger = dropdownElement.Q<VisualElement>("dropdownTrigger");
        dropdownContent = dropdownElement.Q<VisualElement>("dropdownContent");
        optionsList = dropdownElement.Q<ScrollView>("optionsList");
        selectedText = dropdownElement.Q<Label>("selectedText");
        dropdownArrow = dropdownElement.Q<Label>("dropdownArrow");
        searchField = dropdownElement.Q<TextField>("searchField");

        dropdownContent.style.display = DisplayStyle.None;

        SetupTriggerCallback();
        SetupSearchField();
        PopulateOptions();
    }

    private void SetupTriggerCallback()
    {
        dropdownTrigger?.RegisterCallback<ClickEvent>(evt => {
            evt.StopPropagation();
            ToggleDropdown();
        });
    }

    private void SetupSearchField()
    {
        searchField?.RegisterCallback<ChangeEvent<string>>(evt => FilterOptions(evt.newValue));
    }

    private void FilterOptions(string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            filteredOptions = new List<string>(allOptions);
        }
        else
        {
            filteredOptions = allOptions
                .Where(option => option.ToLower().Contains(searchText.ToLower()))
                .ToList();
        }
        PopulateOptions();
    }

    private void ToggleDropdown()
    {
        bool isVisible = dropdownContent.style.display == DisplayStyle.Flex;

        if (isVisible)
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
        if (dropdownContent.parent != uiDocument.rootVisualElement)
        {
            dropdownContent.RemoveFromHierarchy();
            uiDocument.rootVisualElement.Add(dropdownContent);
        }

        var triggerBounds = dropdownTrigger.worldBound;
        var rootBounds = uiDocument.rootVisualElement.worldBound;
        float leftPosition = triggerBounds.x - rootBounds.x;
        float topPosition = triggerBounds.y + triggerBounds.height - rootBounds.y;

        dropdownContent.style.position = Position.Absolute;
        dropdownContent.style.left = leftPosition;
        dropdownContent.style.top = topPosition;
        dropdownContent.style.width = triggerBounds.width;
        dropdownContent.style.display = DisplayStyle.Flex;

        if (dropdownArrow != null)
        {
            dropdownArrow.text = "▲";
        }
    }

    public void CloseDropdown()
    {
        dropdownContent.style.display = DisplayStyle.None;
        if (dropdownArrow != null)
        {
            dropdownArrow.text = "▼";
        }
    }

    public bool IsOpen()
    {
        return dropdownContent.style.display == DisplayStyle.Flex;
    }

    public bool IsClickInsideDropdown(Vector2 clickPosition)
    {
        return dropdownElement.worldBound.Contains(clickPosition) ||
               (IsOpen() && dropdownContent.worldBound.Contains(clickPosition));
    }

    private void PopulateOptions()
    {
        if (optionsList == null) return;

        optionsList.Clear();

        foreach (string option in filteredOptions)
        {
            var optionElement = new VisualElement();
            optionElement.AddToClassList("option-item");

            if (selectedValues.Contains(option))
            {
                optionElement.AddToClassList("selected");
            }

            var label = new Label(option);
            label.AddToClassList("option-text");
            optionElement.Add(label);

            optionElement.RegisterCallback<ClickEvent>(evt => {
                evt.StopPropagation();
                SelectOption(option);
            });

            optionsList.Add(optionElement);
        }
    }

    public void SelectOption(string option)
    {
        if (selectedValues.Contains(option))
        {
            selectedValues.Remove(option);
        }
        else
        {
            if (selectedValues.Count >= MAX_SELECTIONS) return;
            selectedValues.Add(option);
        }

        UpdateSelectedText();
        PopulateOptions();
        OnSelectionChanged?.Invoke(new List<string>(selectedValues));
    }

    public void SetSelectedValues(List<string> values)
    {
        selectedValues = values.Take(MAX_SELECTIONS).ToList();
        UpdateSelectedText();
        PopulateOptions();
    }

    private void UpdateSelectedText()
    {
        if (selectedText != null)
        {
            selectedText.text = selectedValues.Count > 0
                ? string.Join(", ", selectedValues)
                : "Select your locations";

            if (selectedValues.Count > 0)
                selectedText.AddToClassList("has-selection");
            else
                selectedText.RemoveFromClassList("has-selection");
        }
    }

    public List<string> GetSelectedValues()
    {
        return new List<string>(selectedValues);
    }

    public void Dispose()
    {
        dropdownTrigger?.UnregisterCallback<ClickEvent>(evt => ToggleDropdown());
        searchField?.UnregisterCallback<ChangeEvent<string>>(evt => FilterOptions(evt.newValue));
    }
}
