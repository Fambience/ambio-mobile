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
        backButton.clicked += () => UIManager.Instance.OpenScreen(UIScreenType.CreatorType);
        skipButton.clicked += () =>
        {
            OnboardingData.YearsOfExperience = null;
            UIManager.Instance.OpenScreen(UIScreenType.taglineSocials);
        };
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
            OnboardingData.YearsOfExperience = years;
            UIManager.Instance.OpenScreen(UIScreenType.taglineSocials);
        }
        else
        {
            warningLabel.text = "Invalid input. Please correct it.";
        }
    }
}
