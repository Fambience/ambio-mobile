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

    void Start()
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
        string[] styleNames = { "WARM", "CALM", "NEUTRAL", "BOLD" };
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
        bool isCurrentlySelected = buttonSelectionState[clickedButton];

        if (isCurrentlySelected)
        {
            // Deselect if clicking the same button
            DeselectButton(clickedButton);
        }
        else
        {
            // Deselect previously selected button if any
            if (selectedButton != null)
            {
                DeselectButton(selectedButton);
            }
            
            // Select the new button
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
        if (selectedButton == null)
        {
            warningLabel.text = "Please select a color scheme!";
            ShowWarning();
            return;
        }

        StoreSelectedStyle();
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
        if (selectedButton != null && buttonStyleNames.TryGetValue(selectedButton, out string selectedStyleName))
        {
            // Normalize the style name for backend format: CALM, WARM, etc.
            OnboardingData.ColorScheme = selectedStyleName.ToUpper();
            Debug.Log($"Color scheme saved to OnboardingData: {OnboardingData.ColorScheme}");
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