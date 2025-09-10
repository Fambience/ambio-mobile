using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class DesignStyleSelector : MonoBehaviour
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
    private List<Button> selectedButtons = new List<Button>();
    private Dictionary<Button, string> buttonStyleNames = new Dictionary<Button, string>();

    private const int MAX_SELECTIONS = 3;

    void OnEnable()
    {
        if (currentScreen == null)
            currentScreen = this.gameObject;

        root = GetComponent<UIDocument>().rootVisualElement;

        InitializeUIElements();
        SetupButtonListeners();
        CreateWarningLabel();
        
        if (EditOnboardingManager.IsInEditMode)
        {
            PrefillDesignStyleSelections();
        }
    }
    
    void PrefillDesignStyleSelections()
    {
        if (OnboardingData.DesignInspoScreen1 != null && OnboardingData.DesignInspoScreen1.Count > 0)
        {
            Debug.Log($"[DesignStyleSelector] Prefilling creative styles: {string.Join(", ", OnboardingData.DesignInspoScreen1)}");
        
            foreach (var styleName in OnboardingData.DesignInspoScreen1)
            {
                // Find the button that corresponds to this style
                var button = buttonStyleNames.FirstOrDefault(kvp => kvp.Value.ToUpper() == styleName.ToUpper()).Key;
                if (button != null && selectedButtons.Count < MAX_SELECTIONS)
                {
                    SelectButton(button);
                }
            }
        }
        else
        {
            Debug.Log("[DesignStyleSelector] No existing creative style data to prefill");
        }
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
        string[] styleNames = { "ARTDECO", "BOHEMIAN", "COASTAL", "ECLECTIC", "SCANDINAVIAN", "RUSTIC" };
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
            DeselectButton(clickedButton);
            HideWarning();
        }
        else
        {
            if (selectedButtons.Count < MAX_SELECTIONS)
            {
                SelectButton(clickedButton);
                HideWarning();
            }
            else
            {
                ShowWarning();
            }
        }

        LogSelectedStyles();
    }

    void SelectButton(Button button)
    {
        buttonSelectionState[button] = true;
        selectedButtons.Add(button);

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
        selectedButtons.Remove(button);

        button.style.borderTopWidth = 0;
        button.style.borderBottomWidth = 0;
        button.style.borderLeftWidth = 0;
        button.style.borderRightWidth = 0;
    }

    void ShowWarning()
    {
        if (warningLabel != null)
        {
            warningLabel.text = "You can select maximum 3 design styles!";
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
        if (EditOnboardingManager.IsInEditMode)
        {
            // In edit mode, save current state and go back to budget
            StoreSelectedStyles();
            UIManager.Instance.OpenScreen(UIScreenType.Budget);
        }
        else
        {
            UIManager.Instance.OpenScreen(UIScreenType.Budget);
        }
    }

    void OnNextButtonClicked()
    {
        if (selectedButtons.Count == 0)
        {
            warningLabel.text = "Please select at least one design style!";
            ShowWarning();
            return;
        }
        StoreSelectedStyles();
        if (EditOnboardingManager.IsInEditMode)
        {
            UIManager.Instance.OpenScreen(UIScreenType.ModernStyles);
        }
        else
        {
            UIManager.Instance.OpenScreen(UIScreenType.ModernStyles);
        }
    }
    
    void OnSkipButtonClicked()
    {
        if (EditOnboardingManager.IsInEditMode)
        {
            StoreSelectedStyles();
        }
        UIManager.Instance.OpenScreen(UIScreenType.ModernStyles);
    }


    void SwitchToScreen(GameObject targetScreen)
    {
        if (currentScreen != null)
            currentScreen.SetActive(false);

        if (targetScreen != null)
            targetScreen.SetActive(true);
    }

    void StoreSelectedStyles()
    {
        // Get old values for change tracking
        List<string> oldStyles = OnboardingData.DesignInspoScreen1 != null ? 
            new List<string>(OnboardingData.DesignInspoScreen1) : new List<string>();
    
        List<string> selectedStyleNames = new List<string>();
        foreach (Button button in selectedButtons)
        {
            if (buttonStyleNames.TryGetValue(button, out var styleName))
            {
                string apiStyleName = styleName.Replace("-", "").ToUpper();
                selectedStyleNames.Add(apiStyleName);
            }
        }

        // Store in central onboarding data
        OnboardingData.DesignInspoScreen1 = selectedStyleNames;

        Debug.Log("Design styles stored to OnboardingData: " + string.Join(", ", selectedStyleNames));
    
        // Track changes if in edit mode
        if (EditOnboardingManager.IsInEditMode)
        {
            if (!ListsEqual(oldStyles, selectedStyleNames))
            {
                EditOnboardingManager.TrackDataChange("Creative Styles", selectedStyleNames, oldStyles);
            }
        }
    }
    
    private bool ListsEqual(List<string> list1, List<string> list2)
    {
        if (list1 == null && list2 == null) return true;
        if (list1 == null || list2 == null) return false;
        if (list1.Count != list2.Count) return false;
    
        var sorted1 = list1.OrderBy(x => x).ToList();
        var sorted2 = list2.OrderBy(x => x).ToList();
    
        return sorted1.SequenceEqual(sorted2);
    }

    void LogSelectedStyles()
    {
        Debug.Log($"Selected styles ({selectedButtons.Count}/{MAX_SELECTIONS}):");
        foreach (Button button in selectedButtons)
        {
            if (buttonStyleNames.ContainsKey(button))
                Debug.Log($"- {buttonStyleNames[button]}");
        }
    }

    public List<string> GetSelectedStyles()
    {
        List<string> styles = new List<string>();
        foreach (Button button in selectedButtons)
        {
            if (buttonStyleNames.ContainsKey(button))
                styles.Add(buttonStyleNames[button]);
        }
        return styles;
    }

    public static List<string> GetStoredSelectedStyles()
    {
        string stored = PlayerPrefs.GetString("SelectedDesignStyles", "");
        return string.IsNullOrEmpty(stored) ? new List<string>() : new List<string>(stored.Split(','));
    }

    public void ResetSelections()
    {
        foreach (Button button in selectedButtons.ToArray())
        {
            DeselectButton(button);
        }
        selectedButtons.Clear();
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
