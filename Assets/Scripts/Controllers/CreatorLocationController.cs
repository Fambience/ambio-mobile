using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class CreatorLocationController : MonoBehaviour
{
    [Header("UI Elements")]
    private VisualElement root;
    private VisualElement dropdownTrigger;
    private VisualElement dropdownContent;
    private Label selectedText;
    private Label dropdownArrow;
    private TextField searchField;
    private ScrollView optionsList;
    private Button backButton;
    private Button skipButton;
    private Button completeButton;
    private Label cityWarning;

    [Header("Data")]
    private List<string> indianCities = new List<string>();
    private List<string> filteredCities = new List<string>();
    private List<string> selectedCities = new List<string>();
    private bool isDropdownOpen = false;

    void OnEnable()
    {
        InitializeUI();
        SetupEventListeners();
        StartCoroutine(FetchCitiesFromAPI());
    }

    void InitializeUI()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        dropdownTrigger = root.Q<VisualElement>("dropdownTrigger");
        dropdownContent = root.Q<VisualElement>("dropdownContent");
        selectedText = root.Q<Label>("selectedText");
        dropdownArrow = root.Q<Label>("dropdown-arrow");
        searchField = root.Q<TextField>("searchField");
        optionsList = root.Q<ScrollView>("optionsList");
        backButton = root.Q<Button>("backButton");
        skipButton = root.Q<Button>("skipButton");
        completeButton = root.Q<Button>("completeButton");
        cityWarning = new Label();
        cityWarning.style.color = Color.red;
        root.Add(cityWarning);

        dropdownContent.style.display = DisplayStyle.None;
        isDropdownOpen = false;
    }

    void SetupEventListeners()
    {
        dropdownTrigger.RegisterCallback<ClickEvent>(OnDropdownTriggerClick);
        searchField.RegisterCallback<ChangeEvent<string>>(OnSearchFieldChanged);
        backButton.clicked += OnBackButtonClicked;
        skipButton.clicked += OnSkipButtonClicked;
        completeButton.clicked += OnCompleteButtonClicked;
        root.RegisterCallback<ClickEvent>(OnRootClicked);
    }

    private IEnumerator FetchCitiesFromAPI()
    {
        string url = "https://ambiobackend-stage.onrender.com/api/v1/public/cities";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("City fetch failed: " + request.error);
            }
            else
            {
                string json = request.downloadHandler.text;
                CityWrapper wrapper = JsonUtility.FromJson<CityWrapper>(json);
                if (wrapper != null && wrapper.success && wrapper.data != null)
                {
                    indianCities = wrapper.data.Select(c => c.cityName).ToList();
                }
            }
            filteredCities = new List<string>(indianCities);
            PopulateOptionsList();
        }
    }

    void ToggleDropdown()
    {
        if (isDropdownOpen) CloseDropdown();
        else OpenDropdown();
    }

    void OpenDropdown()
    {
        isDropdownOpen = true;
        dropdownContent.style.display = DisplayStyle.Flex;
        dropdownTrigger.AddToClassList("active");
        searchField.Focus();
    }

    void CloseDropdown()
    {
        isDropdownOpen = false;
        dropdownContent.style.display = DisplayStyle.None;
        dropdownTrigger.RemoveFromClassList("active");
        searchField.value = "";
        filteredCities = new List<string>(indianCities);
        PopulateOptionsList();
    }

    void OnDropdownTriggerClick(ClickEvent evt) { evt.StopPropagation(); ToggleDropdown(); }
    void OnRootClicked(ClickEvent evt) { if (isDropdownOpen && !dropdownContent.worldBound.Contains(evt.position)) CloseDropdown(); }

    void OnSearchFieldChanged(ChangeEvent<string> evt)
    {
        string query = evt.newValue.ToLower();
        filteredCities = string.IsNullOrEmpty(query)
            ? new List<string>(indianCities)
            : indianCities.Where(city => city.ToLower().Contains(query)).ToList();
        PopulateOptionsList();
    }

    void PopulateOptionsList()
    {
        optionsList.Clear();
        foreach (string city in filteredCities)
        {
            VisualElement optionItem = new VisualElement();
            optionItem.AddToClassList("option-item");
            Label optionText = new Label(city);
            optionText.AddToClassList("option-text");
            optionItem.Add(optionText);

            if (selectedCities.Contains(city))
                optionItem.AddToClassList("selected");

            optionItem.RegisterCallback<ClickEvent>(evt => OnOptionClicked(city, optionItem));
            optionsList.Add(optionItem);
        }
    }

    void OnOptionClicked(string city, VisualElement optionItem)
    {
        if (selectedCities.Contains(city))
        {
            selectedCities.Remove(city);
            optionItem.RemoveFromClassList("selected");
        }
        else
        {
            if (selectedCities.Count >= 3)
            {
                cityWarning.text = "You can select a maximum of 3 cities.";
                return;
            }
            cityWarning.text = "";
            selectedCities.Add(city);
            optionItem.AddToClassList("selected");
        }

        selectedText.text = selectedCities.Count > 0
            ? string.Join(", ", selectedCities)
            : "Select your location";
    }

    void OnBackButtonClicked()
    {
        Debug.Log("Back button clicked");
        selectedCities.Clear();
        UIManager.Instance.OpenScreen(UIScreenType.CreatorBasicDetails);
    }

    void OnSkipButtonClicked()
    {
        Debug.Log("Skip button clicked");
        selectedCities.Clear();
        UIManager.Instance.OpenScreen(UIScreenType.CreatorType);
    }

    void OnCompleteButtonClicked()
    {
        if (selectedCities.Count == 0)
        {
            cityWarning.text = "Please select at least one city.";
            return;
        }

        cityWarning.text = "";
        Debug.Log("Selected Cities: " + string.Join(", ", selectedCities));
        OnboardingData.SelectedCities = new List<string>(selectedCities);
        UIManager.Instance.OpenScreen(UIScreenType.CreatorType);
    }

    void OnDestroy()
    {
        dropdownTrigger?.UnregisterCallback<ClickEvent>(OnDropdownTriggerClick);
        searchField?.UnregisterCallback<ChangeEvent<string>>(OnSearchFieldChanged);
        root?.UnregisterCallback<ClickEvent>(OnRootClicked);
    }

    [System.Serializable]
    public class City { public string cityId; public string cityName; }
    [System.Serializable]
    public class CityWrapper { public bool success; public List<City> data; }
} 
