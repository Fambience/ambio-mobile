using UnityEngine;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;

public class UserDetailsController : MonoBehaviour
{
    private TextField firstNameField;
    private TextField lastNameField;
    private Label warningFirstName;
    private Label warningLastName;
    private Button completeButton;

    // Regex pattern for names like "John", "Alice", "Martin"
    private readonly Regex nameRegex = new(@"^[A-Z][a-z]+$");

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        firstNameField = root.Q<TextField>("firstName");
        lastNameField = root.Q<TextField>("lastName");
        warningFirstName = root.Q<Label>("WarningFirstName");
        warningLastName = root.Q<Label>("WarningLastName");
        completeButton = root.Q<Button>("completeButton");

        completeButton.clicked += OnCompleteClicked;

        // Optional: Clear warning labels on focus
        firstNameField.RegisterCallback<FocusInEvent>(evt => warningFirstName.text = "");
        lastNameField.RegisterCallback<FocusInEvent>(evt => warningLastName.text = "");
    }

    private void OnCompleteClicked()
    {
        string firstName = firstNameField.text.Trim();
        string lastName = lastNameField.text.Trim();

        bool isValid = true;

        // Validate first name
        if (string.IsNullOrEmpty(firstName))
        {
            warningFirstName.text = "First name is required.";
            isValid = false;
        }
        else if (!nameRegex.IsMatch(firstName))
        {
            warningFirstName.text = "Are you sure you entered your name correctly?";
            isValid = false;
        }

        // Validate last name
        if (string.IsNullOrEmpty(lastName))
        {
            warningLastName.text = "Last name is required.";
            isValid = false;
        }
        else if (!nameRegex.IsMatch(lastName))
        {
            warningLastName.text = "Are you sure you entered your name correctly?";
            isValid = false;
        }

        if (!isValid) return;

        // Save to shared data store
        OnboardingData.FirstName = firstName;
        OnboardingData.LastName = lastName;

        // Navigate to the next screen (implement your logic here)
        Debug.Log("User Details saved. Navigate to next screen.");
        UIManager.Instance.OpenScreen(UIScreenType.Location);
    }
}
