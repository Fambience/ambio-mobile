using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class CompleteProfileController : MonoBehaviour
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

    [Header("Data")] private List<string> indianCities;
    // {
    //     "Mumbai", "Delhi", "Bangalore", "Hyderabad", "Chennai", "Kolkata", "Pune", "Ahmedabad",
    //     "Jaipur", "Surat", "Lucknow", "Kanpur", "Nagpur", "Indore", "Thane", "Bhopal",
    //     "Visakhapatnam", "Pimpri-Chinchwad", "Patna", "Vadodara", "Ghaziabad", "Ludhiana",
    //     "Agra", "Nashik", "Faridabad", "Meerut", "Rajkot", "Kalyan-Dombivali", "Vasai-Virar",
    //     "Varanasi", "Srinagar", "Dhanbad", "Jodhpur", "Amritsar", "Raipur", "Allahabad",
    //     "Coimbatore", "Jabalpur", "Gwalior", "Vijayawada", "Madurai", "Guwahati", "Chandigarh",
    //     "Hubli-Dharwad", "Mysore", "Tiruchirappalli", "Bareilly", "Aligarh", "Tiruppur",
    //     "Moradabad", "Jalandhar", "Bhubaneswar", "Salem", "Warangal", "Guntur", "Bhiwandi",
    //     "Saharanpur", "Gorakhpur", "Bikaner", "Amravati", "Noida", "Jamshedpur", "Bhilai",
    //     "Cuttack", "Firozabad", "Kochi", "Nellore", "Bhavnagar", "Dehradun", "Durgapur",
    //     "Asansol", "Rourkela", "Nanded", "Kolhapur", "Ajmer", "Akola", "Gulbarga",
    //     "Jamnagar", "Ujjain", "Loni", "Siliguri", "Jhansi", "Ulhasnagar", "Jammu",
    //     "Sangli-Miraj & Kupwad", "Mangalore", "Erode", "Belgaum", "Ambattur", "Tirunelveli",
    //     "Malegaon", "Gaya", "Jalgaon", "Udaipur", "Maheshtala", "Davanagere", "Kozhikode"
    // };

    private List<string> filteredCities;
    private string selectedCity = "";
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
        
        // Get UI elements
        dropdownTrigger = root.Q<VisualElement>("dropdownTrigger");
        dropdownContent = root.Q<VisualElement>("dropdownContent");
        selectedText = root.Q<Label>("selectedText");
        dropdownArrow = root.Q<Label>("dropdown-arrow");
        searchField = root.Q<TextField>("searchField");
        optionsList = root.Q<ScrollView>("optionsList");
        backButton = root.Q<Button>("backButton");
        skipButton = root.Q<Button>("skipButton");
        completeButton = root.Q<Button>("completeButton");

        // Set initial state
        dropdownContent.style.display = DisplayStyle.None;
        isDropdownOpen = false;
    }

    void SetupEventListeners()
    {
        // Dropdown trigger click
        dropdownTrigger.RegisterCallback<ClickEvent>(OnDropdownTriggerClick);
        
        // Search field input
        searchField.RegisterCallback<ChangeEvent<string>>(OnSearchFieldChanged);
        
        // Button clicks
        backButton.clicked += OnBackButtonClicked;
        skipButton.clicked += OnSkipButtonClicked;
        completeButton.clicked += OnCompleteButtonClicked;
        
        // Close dropdown when clicking outside
        root.RegisterCallback<ClickEvent>(OnRootClicked);
    }
    
    private IEnumerator FetchCitiesFromAPI()
    {
        string url = "https://ambiobackend-stage.onrender.com/api/v1/public/cities";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");

            // TODO: Show loading indicator here
            yield return request.SendWebRequest();
            // TODO: Hide loading indicator here

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("City fetch failed: " + request.error);
                filteredCities = new List<string>(indianCities); // fallback
                PopulateOptionsList();
            }
            else
            {
                string json = request.downloadHandler.text;
                Debug.Log("Fetched City JSON: " + json);

                CityWrapper wrapper = JsonUtility.FromJson<CityWrapper>(json);
                if (wrapper != null && wrapper.success && wrapper.data != null)
                {
                    indianCities = wrapper.data.Select(c => c.cityName).ToList();
                    filteredCities = new List<string>(indianCities);
                    PopulateOptionsList();
                }
                else
                {
                    Debug.LogError("Failed to parse city list.");
                }
            }
        }
    }


    void OnDropdownTriggerClick(ClickEvent evt)
    {
        evt.StopPropagation();
        ToggleDropdown();
    }

    void OnRootClicked(ClickEvent evt)
    {
        // Close dropdown if clicking outside of it
        if (isDropdownOpen && !dropdownContent.worldBound.Contains(evt.position))
        {
            CloseDropdown();
        }
    }

    void ToggleDropdown()
    {
        if (isDropdownOpen)
        {
            CloseDropdown();
        }
        else
        {
            OpenDropdown();
        }
    }

    void OpenDropdown()
    {
        isDropdownOpen = true;
        dropdownContent.style.display = DisplayStyle.Flex;
        dropdownTrigger.AddToClassList("active");
        
        // Focus search field
        searchField.Focus();
    }

    void CloseDropdown()
    {
        isDropdownOpen = false;
        dropdownContent.style.display = DisplayStyle.None;
        dropdownTrigger.RemoveFromClassList("active");
        
        // Clear search field
        searchField.value = "";
        filteredCities = new List<string>(indianCities);
        PopulateOptionsList();
    }

    void OnSearchFieldChanged(ChangeEvent<string> evt)
    {
        string searchQuery = evt.newValue.ToLower();
        
        if (string.IsNullOrEmpty(searchQuery))
        {
            filteredCities = new List<string>(indianCities);
        }
        else
        {
            filteredCities = indianCities.Where(city => 
                city.ToLower().Contains(searchQuery)).ToList();
        }
        
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
            
            // Add selection state
            if (city == selectedCity)
            {
                optionItem.AddToClassList("selected");
            }
            
            // Add click event
            optionItem.RegisterCallback<ClickEvent>(evt => OnOptionClicked(city, optionItem));
            
            optionsList.Add(optionItem);
        }
    }

    void OnOptionClicked(string city, VisualElement optionItem)
    {
        // Update selected city
        selectedCity = city;
        selectedText.text = city;
        selectedText.AddToClassList("has-selection");
        
        // Update visual state
        RefreshOptionSelection();
        
        // Close dropdown
        CloseDropdown();
        
        Debug.Log($"Selected city: {city}");
    }

    void RefreshOptionSelection()
    {
        foreach (VisualElement option in optionsList.Children())
        {
            option.RemoveFromClassList("selected");
            Label optionText = option.Q<Label>("option-text");
            if (optionText != null && optionText.text == selectedCity)
            {
                option.AddToClassList("selected");
            }
        }
    }

    void OnBackButtonClicked()
    {
        Debug.Log("Back button clicked");
        OnboardingData.HomeLocation = null;
        UIManager.Instance.OpenScreen(UIScreenType.UserDetails);
    }

    void OnSkipButtonClicked()
    {
        Debug.Log("Skip button clicked");
        OnboardingData.HomeLocation = null;
        UIManager.Instance.OpenScreen(UIScreenType.Budget);
    }

    void OnCompleteButtonClicked()
    {
        if (string.IsNullOrEmpty(selectedCity))
        {
            Debug.Log("Please select a city before continuing");
            // You can show a warning message here
            return;
        }
        
        Debug.Log($"Complete button clicked with selected city: {selectedCity}");
        OnboardingData.HomeLocation = selectedCity;
        UIManager.Instance.OpenScreen(UIScreenType.Budget);
    }

    // Public method to get selected city
    public string GetSelectedCity()
    {
        return selectedCity;
    }

    // Public method to set selected city programmatically
    public void SetSelectedCity(string city)
    {
        if (indianCities.Contains(city))
        {
            selectedCity = city;
            selectedText.text = city;
            selectedText.AddToClassList("has-selection");
            RefreshOptionSelection();
        }
    }

    // Public method to add custom cities
    public void AddCustomCity(string city)
    {
        if (!indianCities.Contains(city))
        {
            indianCities.Add(city);
            filteredCities = new List<string>(indianCities);
            PopulateOptionsList();
        }
    }

    void OnDestroy()
    {
        // Clean up event listeners
        if (dropdownTrigger != null)
            dropdownTrigger.UnregisterCallback<ClickEvent>(OnDropdownTriggerClick);
        
        if (searchField != null)
            searchField.UnregisterCallback<ChangeEvent<string>>(OnSearchFieldChanged);
        
        if (root != null)
            root.UnregisterCallback<ClickEvent>(OnRootClicked);
    }
    
    [System.Serializable]
    public class City
    {
        public string cityId;
        public string cityName;
    }

    [System.Serializable]
    public class CityResponse
    {
        public bool success;
        public List<City> data;
    }

    [System.Serializable]
    public class CityWrapper
    {
        public bool success;
        public List<City> data;
    }
}