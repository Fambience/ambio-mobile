using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class BudgetScreen : MonoBehaviour
{
    [Header("UI References")]
    public UIDocument uiDocument;
    
    [Header("Navigation")]
    public string nextSceneName = "NextScreen"; // Name of the next scene to load
    public GameObject nextScreenPrefab; // Alternative: reference to next screen prefab
    
    [Header("Budget Options")]
    public string[] budgetOptions = { "3L", "3L - 5L", "5L - 7L" };
    
    // UI Elements
    private RadioButtonGroup radioButtonGroup;
    private Button completeButton;
    private Button backButton;
    
    // Selected budget data
    private int selectedBudgetIndex = -1;
    private string selectedBudget = "";
    
    void Start()
    {
        // Get the root visual element
        var root = uiDocument.rootVisualElement;
        
        // Find UI elements
        radioButtonGroup = root.Q<RadioButtonGroup>();
        completeButton = root.Q<Button>("completeButton");
        backButton = root.Q<Button>("backButton");
        
        // Subscribe to events
        SetupEventHandlers();
        
        // Initialize UI state
        InitializeUI();
    }
    
    void SetupEventHandlers()
    {
        if (radioButtonGroup != null)
        {
            radioButtonGroup.RegisterValueChangedCallback(OnBudgetSelectionChanged);
        }
        
        if (completeButton != null)
        {
            completeButton.clicked += OnCompleteButtonClicked;
        }
        
        if (backButton != null)
        {
            backButton.clicked += OnBackButtonClicked;
        }
    }
    
    void InitializeUI()
    {
        // Disable complete button initially if no selection
        if (completeButton != null)
        {
            completeButton.SetEnabled(false);
        }
        
        // Set initial selection state
        selectedBudgetIndex = -1;
        selectedBudget = "";
    }
    
    void OnBudgetSelectionChanged(ChangeEvent<int> evt)
    {
        selectedBudgetIndex = evt.newValue;
        
        // Update selected budget string
        if (selectedBudgetIndex >= 0 && selectedBudgetIndex < budgetOptions.Length)
        {
            selectedBudget = budgetOptions[selectedBudgetIndex];
            Debug.Log($"Budget selected: {selectedBudget}");
            
            // Enable complete button
            if (completeButton != null)
            {
                completeButton.SetEnabled(true);
            }
        }
        else
        {
            selectedBudget = "";
            if (completeButton != null)
            {
                completeButton.SetEnabled(false);
            }
        }
    }
    
    void OnCompleteButtonClicked()
    {
        if (selectedBudgetIndex == -1)
        {
            Debug.Log("No budget selected. Proceeding without saving budget.");
            OnboardingData.BudgetMin = 0;
            OnboardingData.BudgetMax = 0;
        }
        else
        {
            // Store the selected budget (you can use PlayerPrefs, ScriptableObject, or a GameManager)
            SaveSelectedBudget();

        }
        
        Debug.Log($"Proceeding with budget: {selectedBudget}");
        
        
        // Navigate to next screen
        NavigateToNextScreen();
    }
    
    void OnBackButtonClicked()
    {
        Debug.Log("Back button clicked");
        
        // Navigate back to previous screen
        // You can implement this based on your navigation system
        NavigateToPreviousScreen();
    }
    
    void SaveSelectedBudget()
    {
        switch (selectedBudget)
        {
            case "3L":
                OnboardingData.BudgetMin = 0;
                OnboardingData.BudgetMax = 300000;
                break;
            case "3L - 5L":
                OnboardingData.BudgetMin = 300000;
                OnboardingData.BudgetMax = 500000;
                break;
            case "5L - 7L":
                OnboardingData.BudgetMin = 500000;
                OnboardingData.BudgetMax = 700000;
                break;
            default:
                OnboardingData.BudgetMin = 0;
                OnboardingData.BudgetMax = 0;
                break;
        }

        Debug.Log($"Budget saved to OnboardingData: ₹{OnboardingData.BudgetMin} - ₹{OnboardingData.BudgetMax}");
    }
    
    void NavigateToNextScreen()
    {
        UIManager.Instance.OpenScreen(UIScreenType.CreativeStyles);
        
    }
    
    void NavigateToPreviousScreen()
    {
        UIManager.Instance.OpenScreen(UIScreenType.Location);
    }
    
    // Public method to get selected budget (useful for other scripts)
    public string GetSelectedBudget()
    {
        return selectedBudget;
    }
    
    public int GetSelectedBudgetIndex()
    {
        return selectedBudgetIndex;
    }
    
    // Method to programmatically set budget selection
    public void SetBudgetSelection(int index)
    {
        if (radioButtonGroup != null && index >= 0 && index < budgetOptions.Length)
        {
            radioButtonGroup.value = index;
        }
    }
    
    void OnDestroy()
    {
        // Clean up event subscriptions
        if (radioButtonGroup != null)
        {
            radioButtonGroup.UnregisterValueChangedCallback(OnBudgetSelectionChanged);
        }
        
        if (completeButton != null)
        {
            completeButton.clicked -= OnCompleteButtonClicked;
        }
        
        if (backButton != null)
        {
            backButton.clicked -= OnBackButtonClicked;
        }
    }
}

// Optional: Data class to hold budget information
[System.Serializable]
public class BudgetData
{
    public string budgetRange;
    public int minAmount;
    public int maxAmount;
    
    public BudgetData(string range, int min, int max)
    {
        budgetRange = range;
        minAmount = min;
        maxAmount = max;
    }
}