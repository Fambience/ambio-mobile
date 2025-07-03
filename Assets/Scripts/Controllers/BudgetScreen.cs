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
            Debug.LogWarning("No budget selected!");
            return;
        }
        
        Debug.Log($"Proceeding with budget: {selectedBudget}");
        
        // Store the selected budget (you can use PlayerPrefs, ScriptableObject, or a GameManager)
        SaveSelectedBudget();
        
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
        // Save using PlayerPrefs (persistent across sessions)
        PlayerPrefs.SetString("SelectedBudget", selectedBudget);
        PlayerPrefs.SetInt("SelectedBudgetIndex", selectedBudgetIndex);
        PlayerPrefs.Save();
        
        // Alternative: Use a GameManager or ScriptableObject for session data
        // GameManager.Instance.SetSelectedBudget(selectedBudget);
    }
    
    void NavigateToNextScreen()
    {
        // Method 1: Load next scene
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        // Method 2: Instantiate next screen prefab
        else if (nextScreenPrefab != null)
        {
            // Assuming you're using a root screen container:
            var root = uiDocument.rootVisualElement;

            // Remove old content
            root.Clear();

            // Load the new UXML manually
            var nextScreenAsset = nextScreenPrefab.GetComponent<UIDocument>()?.visualTreeAsset;
            if (nextScreenAsset != null)
            {
                VisualElement newScreen = nextScreenAsset.CloneTree();
                root.Add(newScreen);
            }
            else
            {
                Debug.LogWarning("nextScreenPrefab does not have a UIDocument with VisualTreeAsset.");
            }
        }
        // Method 3: Use a custom navigation system
        else
        {
            // Example: Using a hypothetical ScreenManager
            // ScreenManager.Instance.ShowNextScreen();
            Debug.LogWarning("No navigation method configured!");
        }
    }
    
    void NavigateToPreviousScreen()
    {
        // Simple back navigation - you can customize this
        // Method 1: Load previous scene (if you know the scene name)
        // SceneManager.LoadScene("PreviousScreenName");
        
        // Method 2: Use a navigation stack
        // NavigationManager.Instance.GoBack();
        
        // Method 3: Simple scene reload (for testing)
        Debug.Log("Navigating back...");
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