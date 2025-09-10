using UnityEngine;
using UnityEngine.UIElements;

public class YearsOfExperienceController : MonoBehaviour
{
    private UIDocument uiDocument;
    private TextField experienceInput;
    private Label warningLabel;
    private Button completeButton;
    private Button backButton;
    private Button skipButton;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        experienceInput = root.Q<TextField>("firstName"); // Name from UXML
        warningLabel = root.Q<Label>("WarningFirstName");
        completeButton = root.Q<Button>("completeButton");
        backButton = root.Q<Button>("backButton");
        skipButton = root.Q<Button>("skipButton");

        warningLabel.text = string.Empty;
        completeButton.SetEnabled(false); // Disable submit initially

        experienceInput.RegisterValueChangedCallback(evt => ValidateInput(evt.newValue));

        completeButton.clicked += OnComplete;
        backButton.clicked += OnBack;
        skipButton.clicked += OnSkip;
        
        // If in edit mode, prefill the data
        if (EditOnboardingManager.IsInEditMode)
        {
            PrefillData();
        }
    }

    private void PrefillData()
    {
        if (OnboardingData.YearsOfExperience.HasValue && OnboardingData.YearsOfExperience > 0)
        {
            experienceInput.value = OnboardingData.YearsOfExperience.Value.ToString();
            // Trigger validation to enable the button if the value is valid
            ValidateInput(experienceInput.value);
            Debug.Log($"[YearsOfExperienceController] Prefilled years of experience: {OnboardingData.YearsOfExperience}");
        }
    }

    private void ValidateInput(string input)
    {
        input = input.Trim();

        if (int.TryParse(input, out int value) && value >= 1 && value <= 99)
        {
            warningLabel.text = string.Empty;
            completeButton.SetEnabled(true);
        }
        else
        {
            warningLabel.text = "Please enter a whole number between 1 and 99.";
            completeButton.SetEnabled(false);
        }
    }

    private void OnComplete()
    {
        string input = experienceInput.value.Trim();

        if (int.TryParse(input, out int years) && years >= 1 && years <= 99)
        {
            int? previousValue = OnboardingData.YearsOfExperience;
            OnboardingData.YearsOfExperience = years;
        
            if (EditOnboardingManager.IsInEditMode && previousValue != years)
            {
                EditOnboardingManager.TrackDataChange("YearsOfExperience", years, previousValue);
            }
        
            UIManager.Instance.OpenScreen(UIScreenType.taglineSocials);
        }
        else
        {
            warningLabel.text = "Invalid input. Please correct it.";
        }
    }

    private void OnBack()
    {
        if (EditOnboardingManager.IsInEditMode)
        {
            UIManager.Instance.OpenScreen(UIScreenType.CreatorType);
        }
        else
        {
            UIManager.Instance.OpenScreen(UIScreenType.CreatorType);
        }
    }

    private void OnSkip()
    {
        int? previousValue = OnboardingData.YearsOfExperience;
        OnboardingData.YearsOfExperience = null;
    
        if (EditOnboardingManager.IsInEditMode && previousValue != null)
        {
            EditOnboardingManager.TrackDataChange("YearsOfExperience", null, previousValue);
        }
    
        UIManager.Instance.OpenScreen(UIScreenType.taglineSocials);
    }
}