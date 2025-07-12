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
    }

    private void OnComplete()
    {
        int selectedIndex = designerRadioGroup.value;

        if (selectedIndex >= 0 && selectedIndex < designerOptions.Length)
        {
            string selectedType = designerOptions[selectedIndex];
            OnboardingData.TypeOfDesigner = selectedType;
            Debug.Log("Selected Designer Type: " + selectedType);

           UIManager.Instance.OpenScreen(UIScreenType.CeatorExperience); // Adjust screen as needed
        }
        else
        {
            Debug.LogWarning("Please select a designer type before continuing.");
        }
    }

    private void OnBack()
    {
        UIManager.Instance.OpenScreen(UIScreenType.CreatorLocation); // Replace with actual previous screen enum
    }

    private void OnSkip()
    {
        OnboardingData.TypeOfDesigner = null;
        UIManager.Instance.OpenScreen(UIScreenType.CeatorExperience); // Adjust screen as needed
    }
}