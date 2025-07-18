using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Networking;


public class SocialLinksController : MonoBehaviour
{
    public UIDocument uiDocument;
    public VisualTreeAsset socialInputTemplate;

    private VisualElement container;
    private Button addNewButton;
    private Button completeButton;
    private Button backButton;

    private TextField taglineField;
    private TextField websiteField;

    private Dictionary<string, string> socialsData = new();

    private string token = "";
    private void OnEnable()
    {
        token = AuthTokenManager.GetToken();
        var root = uiDocument.rootVisualElement;
        container = root.Q<VisualElement>("socialInputsContainer");
        addNewButton = root.Q<Button>("addNewButton");
        completeButton = root.Q<Button>("completeButton");
        backButton = root.Q<Button>("backButton");

        taglineField = root.Q<TextField>("taglineField");
        websiteField = root.Q<TextField>("websiteField");

        addNewButton.clicked += AddSocialInput;
        completeButton.clicked += SubmitProfileDetails;

        AddSocialInput(); // Add one input by default
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

    void SubmitProfileDetails()
{
    // Store in OnboardingData
    OnboardingData.Tagline = taglineField.value?.Trim();
    OnboardingData.Website = websiteField.value?.Trim();
    OnboardingData.SocialLinks = new Dictionary<string, string>(socialsData);

    StartCoroutine(SubmitDesignerOnboarding());
}

    private IEnumerator SubmitDesignerOnboarding()
    {
        token = AuthTokenManager.GetToken();
        Debug.Log("Creator Profile: " + token);
        string endpoint = "/api/v1/user/onboarding-details";
        string url = baseScript.baseURL + endpoint;

        // Convert social links dictionary to a list of URLs
        List<string> socials = new List<string>(OnboardingData.SocialLinks.Values);

        var payload = new Dictionary<string, object>
        {
            { "name", OnboardingData.DesignerName },
            { "region", OnboardingData.SelectedCities?.Count > 0 ? OnboardingData.SelectedCities : null },
            { "creatorType", string.IsNullOrWhiteSpace(OnboardingData.Occupation) ? null : OnboardingData.Occupation },
            {  "yearsOfExperience", OnboardingData.YearsOfExperience > 0 ? OnboardingData.YearsOfExperience : null },
            { "tagline", string.IsNullOrWhiteSpace(OnboardingData.Tagline) ? null : OnboardingData.Tagline },
            { "socials", OnboardingData.SocialLinks?.Values?.ToList() ?? new List<string>() },
            { "occupation", string.IsNullOrWhiteSpace(OnboardingData.Occupation) ? null : OnboardingData.Occupation },
            { "website", string.IsNullOrWhiteSpace(OnboardingData.Website) ? null : OnboardingData.Website }
        };


        string json = MiniJSON.JSON.Serialize(payload);
        Debug.Log("Payload: " + json);

        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"{token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Onboarding submission successful: " + request.downloadHandler.text);
            UIManager.Instance.OpenScreen(UIScreenType.Location);
        }
        else
        {
            Debug.LogError("Onboarding submission failed: " + request.downloadHandler.text);
        }
    }


}
