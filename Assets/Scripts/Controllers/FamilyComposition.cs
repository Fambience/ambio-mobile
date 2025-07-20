using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniJSON;

public class FamilyComposition : MonoBehaviour
{
    private UIDocument uiDocument;

    [Header("Config")]
    private string onboardingEndpoint = "/api/v1/user/onboarding-details";
    private string baseURL;

    private Button backButton, completeButton, skipButton;
    private Toggle aloneToggle, partnerToggle, familyToggle, roommatesToggle, petsToggle;

    private List<Toggle> allToggles = new();
    private Dictionary<Toggle, string> toggleLabels = new();

    void OnEnable()
    {
        baseURL = baseScript.baseURL;
        InitializeUI();
        SetupEventListeners();
        UpdateCompleteButtonState();
    }

    void InitializeUI()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        backButton = root.Q<Button>("backButton");
        completeButton = root.Q<Button>("completeButton");
        skipButton = root.Q<Button>("skipButton");

        aloneToggle = root.Q<Toggle>("aloneToggle");
        partnerToggle = root.Q<Toggle>("partnerToggle");
        familyToggle = root.Q<Toggle>("familyToggle");
        roommatesToggle = root.Q<Toggle>("roommatesToggle");
        petsToggle = root.Q<Toggle>("petsToggle");

        allToggles.AddRange(new[] { aloneToggle, partnerToggle, familyToggle, roommatesToggle, petsToggle });

        toggleLabels[aloneToggle] = "LIVING_ALONE";
        toggleLabels[partnerToggle] = "LIVING_WITH_PARTNER";
        toggleLabels[familyToggle] = "LIVING_WITH_FAMILY";
        toggleLabels[roommatesToggle] = "LIVING_WITH_ROOMMATES";
        toggleLabels[petsToggle] = "LIVING_WITH_PET";

        aloneToggle.RegisterValueChangedCallback(OnAloneToggleChanged);
    }

    void SetupEventListeners()
    {
        backButton.clicked += () => UIManager.Instance.OpenScreen(UIScreenType.ColorTone);
        completeButton.clicked += OnCompleteButtonClicked;
        skipButton.clicked += () => StartCoroutine(SendOnboardingData());

        foreach (var toggle in allToggles.Where(t => t != aloneToggle))
            toggle.RegisterValueChangedCallback(OnOtherToggleChanged);
    }

    void OnAloneToggleChanged(ChangeEvent<bool> evt)
    {
        if (evt.newValue)
        {
            foreach (var toggle in allToggles)
                if (toggle != aloneToggle) toggle.value = false;
        }
        UpdateCompleteButtonState();
    }

    void OnOtherToggleChanged(ChangeEvent<bool> evt)
    {
        if (evt.newValue && aloneToggle != null)
            aloneToggle.value = false;

        UpdateCompleteButtonState();
    }

    void UpdateCompleteButtonState()
    {
        bool anySelected = allToggles.Any(t => t != null && t.value);
        completeButton.SetEnabled(anySelected);
    }

    void OnCompleteButtonClicked()
    {
        OnboardingData.HomeSharingWith = allToggles
            .Where(t => t.value && toggleLabels.ContainsKey(t))
            .Select(t => toggleLabels[t])
            .ToList();

        StartCoroutine(SendOnboardingData());
    }

    IEnumerator SendOnboardingData()
    {
        string token = AuthTokenManager.GetToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Token missing.");
            yield break;
        }

        var payload = new Dictionary<string, object>();

        void AddIfNotEmpty(string key, object value)
        {
            if (value is string strVal && !string.IsNullOrEmpty(strVal))
                payload[key] = strVal;
            else if (value is int intVal && intVal > 0)
                payload[key] = intVal;
            else if (value is List<string> listVal && listVal.Count > 0)
                payload[key] = listVal;
            else if (value is Dictionary<string, List<string>> dictVal && dictVal.Count > 0)
                payload[key] = dictVal;
        }

        AddIfNotEmpty("firstName", OnboardingData.FirstName);
        AddIfNotEmpty("lastName", OnboardingData.LastName);
        AddIfNotEmpty("homeLocation", OnboardingData.HomeLocation);
        AddIfNotEmpty("colorScheme", OnboardingData.ColorScheme);
        AddIfNotEmpty("homeSharingWith", OnboardingData.HomeSharingWith);
        if (OnboardingData.BudgetMin > 0) payload["minBudget"] = OnboardingData.BudgetMin;
        if (OnboardingData.BudgetMax > 0) payload["maxBudget"] = OnboardingData.BudgetMax;

        if (OnboardingData.DesignInspoScreen1?.Count > 0 || OnboardingData.DesignInspoScreen2?.Count > 0)
        {
            var designMap = new Dictionary<string, List<string>>();
            if (OnboardingData.DesignInspoScreen1?.Count > 0)
                designMap["CREATIVE_AND_CHARACTERFUL"] = OnboardingData.DesignInspoScreen1;
            if (OnboardingData.DesignInspoScreen2?.Count > 0)
                designMap["MODERN_AND_MINIMAL"] = OnboardingData.DesignInspoScreen2;

            payload["designInspirations"] = designMap;
        }

        string json = JSON.Serialize(payload);

        Debug.Log("Submitting onboarding payload: " + json);

        using (UnityWebRequest request = new UnityWebRequest(baseURL + onboardingEndpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", token);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Submission failed: " + request.error);
                Debug.LogError(request.downloadHandler.text);
            }
            else
            {
                Debug.Log("Onboarding data submitted successfully.");
                UIManager.Instance.OpenScreen(UIScreenType.Home);
            }
        }
    }
}
