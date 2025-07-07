using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class SocialLinksController : MonoBehaviour
{
    public UIDocument uiDocument;
    public VisualTreeAsset socialInputTemplate; // Create a UXML for single input row with icon and TextField

    private VisualElement container;
    private Button addNewButton;
    private Button completeButton;
    private Button backButton;

    private List<string> platformsAdded = new();
    private Dictionary<string, string> socialsData = new();

    private void Awake()
    {
        var root = uiDocument.rootVisualElement;
        container = root.Q<VisualElement>("socialInputsContainer");
        addNewButton = root.Q<Button>("addNewButton");
        completeButton = root.Q<Button>("completeButton");
        backButton = root.Q<Button>("backButton");

        addNewButton.clicked += AddSocialInput;
        completeButton.clicked += SubmitSocialLinks;

        AddSocialInput(); // Add one by default
    }

    void AddSocialInput()
    {
        if (socialInputTemplate == null)
        {
            Debug.LogError("Social Input Template is not assigned!");
            return;
        }

        VisualElement row = socialInputTemplate.Instantiate();
        TextField input = row.Q<TextField>("socialInputField");
        VisualElement icon = row.Q<VisualElement>("socialIcon");

        input.RegisterValueChangedCallback(evt =>
        {
            string platform = ParsePlatformFromInput(evt.newValue);
            UpdateSocialIcon(icon, platform);
            socialsData[platform] = evt.newValue;
        });

        container.Add(row);
    }

    string ParsePlatformFromInput(string input)
    {
        input = input.ToLower();
        if (input.Contains("instagram")) return "instagram";
        if (input.Contains("linkedin")) return "linkedin";
        if (input.Contains("behance")) return "behance";
        if (input.Contains("dribbble")) return "dribbble";
        if (input.Contains("facebook")) return "facebook";
        if (input.Contains("x.com") || input.Contains("twitter")) return "twitter";
        return "default";
    }

    void UpdateSocialIcon(VisualElement icon, string platform)
    {
        icon.ClearClassList();
        icon.AddToClassList("social-icon");
        icon.AddToClassList($"icon-{platform}");
    }

    void SubmitSocialLinks()
    {
        foreach (var entry in socialsData)
        {
            Debug.Log($"Platform: {entry.Key}, ID: {entry.Value}");
            // Prepare payload for backend here
        }
    }
}
