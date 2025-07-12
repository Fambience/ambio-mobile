using UnityEngine;
using UnityEngine.UIElements;

public class YearsOfExperienceController : MonoBehaviour
{
    private UIDocument uiDocument;
    private RadioButtonGroup experienceRadioGroup;
    private Button completeButton;
    private Button backButton;
    private Button skipButton;

    // Match this with your UXML choices
    private readonly string[] experienceOptions = { ">1 year", "1-3 years", "3+ years" };

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        experienceRadioGroup = root.Q<RadioButtonGroup>();
        completeButton = root.Q<Button>("completeButton");
        backButton = root.Q<Button>("backButton");
        skipButton = root.Q<Button>("skipButton");

        completeButton.clicked += OnComplete;
        backButton.clicked += OnBack;
        skipButton.clicked += OnSkip;
    }

    private void OnComplete()
    {
        int selectedIndex = experienceRadioGroup.value;

        if (selectedIndex >= 0 && selectedIndex < experienceOptions.Length)
        {
            string selectedExperience = experienceOptions[selectedIndex];
            OnboardingData.YearsOfExperience = selectedExperience;
            Debug.Log("Selected Experience: " + selectedExperience);

            UIManager.Instance.OpenScreen(UIScreenType.taglineSocials); // Replace as needed
        }
        else
        {
            Debug.LogWarning("Please select your years of experience before continuing.");
        }
    }

    private void OnBack()
    {
        UIManager.Instance.OpenScreen(UIScreenType.CreatorType); // Replace as needed
    }

    private void OnSkip()
    {
        OnboardingData.YearsOfExperience = null;
        UIManager.Instance.OpenScreen(UIScreenType.taglineSocials); // Replace as needed
    }
}