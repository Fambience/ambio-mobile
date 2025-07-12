using UnityEngine;
using UnityEngine.UIElements;

public class CreatorDetailsController : MonoBehaviour
{
    private UIDocument uiDocument;
    private TextField firstNameField;
    private Label warningFirstName;
    private Button completeButton;
    private Button backButton;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        firstNameField = root.Q<TextField>("firstName");
        warningFirstName = root.Q<Label>("WarningFirstName");
        completeButton = root.Q<Button>("completeButton");
        backButton = root.Q<Button>("backButton");

        firstNameField.RegisterValueChangedCallback(evt => ValidateName(evt.newValue));

        completeButton.clicked += OnComplete;
        if (backButton != null)
            backButton.clicked += OnBack;
    }

    private void ValidateName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            warningFirstName.text = "Name cannot be empty.";
        }
        else
        {
            warningFirstName.text = "";
        }
    }

    private void OnComplete()
    {
        string firstName = firstNameField.value?.Trim();

        if (string.IsNullOrEmpty(firstName))
        {
            warningFirstName.text = "Please enter your name.";
            return;
        }

        OnboardingData.DesignerName = firstName; 
        Debug.Log("Creator name set to: " + firstName);

        UIManager.Instance.OpenScreen(UIScreenType.CreatorLocation); 
    }

    private void OnBack()
    {
        //UIManager.Instance.OpenScreen(UIScreenType.PreviousScreen); // Optional: navigate back
    }
}