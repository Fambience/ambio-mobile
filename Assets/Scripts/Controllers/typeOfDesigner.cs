using UnityEngine;
using UnityEngine.UIElements;

public class DesignerTypeController : MonoBehaviour
{
    private UIDocument uiDocument;
    private RadioButtonGroup designerRadioGroup;
    private Button completeButton;
    private Button backButton;
    private Button skipButton;

    private readonly string[] designerOptions = { "Interior Designer", "Design Studio", "Contractor" };

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        designerRadioGroup = root.Q<RadioButtonGroup>();
        completeButton = root.Q<Button>("completeButton");
        backButton = root.Q<Button>("backButton");
        skipButton = root.Q<Button>("skipButton");

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
        if (!string.IsNullOrEmpty(OnboardingData.Occupation))
        {
            int selectedIndex = EditOnboardingManager.GetDesignerTypeIndex(designerOptions);
            if (selectedIndex >= 0)
            {
                designerRadioGroup.value = selectedIndex;
                Debug.Log($"[DesignerTypeController] Prefilled designer type: {OnboardingData.Occupation} (index: {selectedIndex})");
            }
        }
    }

    private void OnComplete()
    {
        int selectedIndex = designerRadioGroup.value;

        if (selectedIndex >= 0 && selectedIndex < designerOptions.Length)
        {
            string selectedType = designerOptions[selectedIndex];
            string previousValue = OnboardingData.Occupation;
            OnboardingData.Occupation = selectedType;
        
            if (EditOnboardingManager.IsInEditMode && previousValue != selectedType)
            {
                EditOnboardingManager.TrackDataChange("Occupation", selectedType, previousValue);
            }
        
            Debug.Log("Selected Designer Type: " + selectedType);
            UIManager.Instance.OpenScreen(UIScreenType.CeatorExperience);
        }
        else
        {
            Debug.LogWarning("Please select a designer type before continuing.");
        }
    }

    private void OnBack()
    {
        if (EditOnboardingManager.IsInEditMode)
        {
            // In edit mode, cancel and go back to profile settings
            EditOnboardingManager.Instance.CancelEditOnboarding();
        }
        else
        {
            // Normal onboarding flow
            UIManager.Instance.OpenScreen(UIScreenType.CreatorLocation); // Replace with actual previous screen enum
        }
    }

    private void OnSkip()
    {
        string previousValue = OnboardingData.Occupation;
        OnboardingData.Occupation = null;
    
        if (EditOnboardingManager.IsInEditMode && previousValue != null)
        {
            EditOnboardingManager.TrackDataChange("Occupation", null, previousValue);
        }
    
        UIManager.Instance.OpenScreen(UIScreenType.CeatorExperience);
    }
}