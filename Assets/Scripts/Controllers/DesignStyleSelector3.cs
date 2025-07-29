using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DesignStyleSelector3 : MonoBehaviour
{
    [Header("Screen Navigation")]
    [SerializeField] private GameObject nextScreen;
    [SerializeField] private GameObject previousScreen;
    [SerializeField] private GameObject currentScreen;

    private VisualElement root;
    private Button backButton;
    private Button nextButton;
    private Label warningLabel;
    private Button skipButton;

    private Dictionary<Button, bool> buttonSelectionState = new Dictionary<Button, bool>();
    private Button selectedButton = null; // Changed from List to single Button
    private Dictionary<Button, string> buttonStyleNames = new Dictionary<Button, string>();

    void OnEnable()
    {
        if (currentScreen == null)
            currentScreen = this.gameObject;

        root = GetComponent<UIDocument>().rootVisualElement;

        InitializeUIElements();
        SetupButtonListeners();
        CreateWarningLabel();
    }

    void InitializeUIElements()
    {
        backButton = root.Q<Button>("backButton");
        nextButton = root.Q<Button>("completeButton");
        skipButton = root.Q<Button>("skipButton");

        var styleButtons = root.Query<Button>().Where(btn =>
            btn.name == "Button" && btn != backButton && btn != nextButton).ToList();

        for (int i = 0; i < styleButtons.Count; i++)
        {
            Button button = styleButtons[i];
            string styleName = GetStyleNameFromButton(button, i);

            buttonStyleNames[button] = styleName;
            buttonSelectionState[button] = false;

            var image = button.Q<Image>();
            if (image != null)
            {
                image.RemoveFromClassList($"image-{i + 1}");
                image.AddToClassList(styleName);
            }
        }
    }

    string GetStyleNameFromButton(Button button, int index)
    {
        // Updated for color schemes instead of design styles
        string[] styleNames = { "WARM_PALATTE", "CALM_PALATTE", "NEUTRAL_PALATTE", "BOLD_PALATTE" };
        return index < styleNames.Length ? styleNames[index] : $"Style-{index + 1}";
    }

    void SetupButtonListeners()
    {
        if (backButton != null)
            backButton.clicked += OnBackButtonClicked;

        if (nextButton != null)
            nextButton.clicked += OnNextButtonClicked;
        
        if (skipButton != null)
            skipButton.clicked += OnSkipButtonClicked;

        foreach (var kvp in buttonSelectionState)
        {
            Button button = kvp.Key;
            button.clicked += () => OnStyleButtonClicked(button);
        }
    }

    void CreateWarningLabel()
    {
        warningLabel = root.Q<Label>(className: "warning-text");

        if (warningLabel != null)
        {
            warningLabel.RemoveFromClassList("show"); // Ensure it's hidden initially
        }
        else
        {
            Debug.LogWarning("Warning label with class 'warning-text' not found!");
        }
    }

    void OnStyleButtonClicked(Button clickedButton)
    {
        Debug.Log($"🎯 Style button clicked: {clickedButton.name}");

        bool isCurrentlySelected = buttonSelectionState[clickedButton];

        if (isCurrentlySelected)
        {
            Debug.Log("🔁 Re-clicked same button: Deselecting");
            DeselectButton(clickedButton);
        }
        else
        {
            if (selectedButton != null)
            {
                Debug.Log($"🔄 Deselecting previously selected: {selectedButton.name}");
                DeselectButton(selectedButton);
            }

            Debug.Log($"✅ Selecting new button: {clickedButton.name}");
            SelectButton(clickedButton);
        }

        HideWarning();
        LogSelectedStyle();
    }

    void SelectButton(Button button)
    {
        buttonSelectionState[button] = true;
        selectedButton = button;

        button.style.borderTopWidth = 10;
        button.style.borderBottomWidth = 10;
        button.style.borderLeftWidth = 10;
        button.style.borderRightWidth = 10;
        Color blue = new Color(0.13f, 0.59f, 0.95f, 1f);
        button.style.borderTopColor = blue;
        button.style.borderBottomColor = blue;
        button.style.borderLeftColor = blue;
        button.style.borderRightColor = blue;
    }

    void DeselectButton(Button button)
    {
        buttonSelectionState[button] = false;
        if (selectedButton == button)
            selectedButton = null;

        button.style.borderTopWidth = 0;
        button.style.borderBottomWidth = 0;
        button.style.borderLeftWidth = 0;
        button.style.borderRightWidth = 0;
    }

    void ShowWarning()
    {
        if (warningLabel != null)
        {
            warningLabel.AddToClassList("show");
            Invoke(nameof(HideWarning), 3f);
        }
    }

    void HideWarning()
    {
        if (warningLabel != null)
        {
            warningLabel.RemoveFromClassList("show");
        }

        CancelInvoke(nameof(HideWarning));
    }

    void OnBackButtonClicked()
    {
        StoreSelectedStyle();
        UIManager.Instance.OpenScreen(UIScreenType.ModernStyles);
    }

    void OnNextButtonClicked()
    {
        Debug.Log("✅ OnNextButtonClicked called");

        if (selectedButton == null)
        {
            Debug.LogWarning("⚠️ No button selected when Next was clicked");
            warningLabel.text = "Please select a color scheme!";
            ShowWarning();
            return;
        }

        Debug.Log($"📌 SelectedButton: {selectedButton.name}");
        StoreSelectedStyle();

        Debug.Log($"➡️ Navigating to Family screen with ColorScheme: {string.Join(",", OnboardingData.ColorScheme ?? new List<string> { "null" })}");

        UIManager.Instance.OpenScreen(UIScreenType.Family);
    }
    
    void OnSkipButtonClicked()
    {
        UIManager.Instance.OpenScreen(UIScreenType.Family);
    }

    void SwitchToScreen(GameObject targetScreen)
    {
        if (currentScreen != null)
            currentScreen.SetActive(false);

        if (targetScreen != null)
            targetScreen.SetActive(true);
    }

    void StoreSelectedStyle()
    {
        Debug.Log("🧠 StoreSelectedStyle called");

        if (selectedButton != null && buttonStyleNames.TryGetValue(selectedButton, out string selectedStyleName))
        {
            OnboardingData.ColorScheme = new List<string> { selectedStyleName.ToUpper() };
            Debug.Log($"✅ Color scheme saved to OnboardingData: {string.Join(",", OnboardingData.ColorScheme)}");
        }
        else
        {
            Debug.LogError("❌ Failed to store color scheme: selectedButton or mapping missing");
        }
    }

    void LogSelectedStyle()
    {
        if (selectedButton != null && buttonStyleNames.ContainsKey(selectedButton))
        {
            Debug.Log($"Selected color scheme: {buttonStyleNames[selectedButton]}");
        }
        else
        {
            Debug.Log("No color scheme selected");
        }
    }

    public string GetSelectedStyle()
    {
        if (selectedButton != null && buttonStyleNames.ContainsKey(selectedButton))
        {
            return buttonStyleNames[selectedButton];
        }
        return null;
    }

    public static string GetStoredSelectedStyle()
    {
        return PlayerPrefs.GetString("SelectedColorScheme", "");
    }

    public void ResetSelection()
    {
        if (selectedButton != null)
        {
            DeselectButton(selectedButton);
        }
        HideWarning();
    }

    void OnDestroy()
    {
        if (backButton != null)
            backButton.clicked -= OnBackButtonClicked;
        if (nextButton != null)
            nextButton.clicked -= OnNextButtonClicked;
        if (skipButton != null)
            skipButton.clicked -= OnSkipButtonClicked;
    }
}