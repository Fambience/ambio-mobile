using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FamilyComposition : MonoBehaviour
{
    private UIDocument uiDocument;

    [Header("Config")]
    private string onboardingEndpoint = "/api/v1/user/onboarding-details";
    private string baseURL;

    private Button backButton;
    private Button completeButton;
    private Button skipButton;

    private Toggle aloneToggle;
    private Toggle partnerToggle;
    private Toggle familyToggle;
    private Toggle roommatesToggle;
    private Toggle petsToggle;

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

        toggleLabels[aloneToggle] = "ALONE";
        toggleLabels[partnerToggle] = "PARTNER";
        toggleLabels[familyToggle] = "FAMILY";
        toggleLabels[roommatesToggle] = "ROOMMATES";
        toggleLabels[petsToggle] = "PET";

        aloneToggle.RegisterValueChangedCallback(OnAloneToggleChanged);
    }

    void SetupEventListeners()
    {
        if (backButton != null)
            backButton.clicked += OnBackButtonClicked;
        
        completeButton.clicked += OnCompleteButtonClicked;
        if (skipButton != null) skipButton.clicked += () => StartCoroutine(SendOnboardingData());

        foreach (var toggle in allToggles.Where(t => t != aloneToggle))
            toggle.RegisterValueChangedCallback(OnOtherToggleChanged);
    }

    void OnBackButtonClicked()
    {
        UIManager.Instance.OpenScreen(UIScreenType.ColorTone);
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
        var selected = allToggles.Where(t => t.value && toggleLabels.ContainsKey(t))
                                 .Select(t => toggleLabels[t])
                                 .ToList();

        if (selected.Count == 0)
        {
            Debug.LogWarning("No selections made.");
            return;
        }

        OnboardingData.HomeSharingWith = selected;
        Debug.Log("Selected options: " + string.Join(", ", selected));

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

        Debug.Log("himanshu" + OnboardingData.ColorScheme);
        OnboardingPayload data = new OnboardingPayload
        {
            firstName = OnboardingData.FirstName,
            lastName = OnboardingData.LastName,
            homeLocation = OnboardingData.HomeLocation,
            colorScheme = OnboardingData.ColorScheme,
            homeSharingWith = OnboardingData.HomeSharingWith,
            budget = new OnboardingPayload.Budget
            {
                min = OnboardingData.BudgetMin,
                max = OnboardingData.BudgetMax
            },
            designInspoScreen1 = OnboardingData.DesignInspoScreen1,
            designInspoScreen2 = OnboardingData.DesignInspoScreen2,
            completedQuestionEnums = BuildCompletedQuestionsList()
        };

        string json = JsonUtility.ToJson(data);
        Debug.Log("Submitting onboarding payload: " + json);

        using (UnityWebRequest request = new UnityWebRequest(baseURL + onboardingEndpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
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
                // TODO: Navigate to dashboard/success screen
            }
        }
    }

    List<string> BuildCompletedQuestionsList()
    {
        var q = new List<string>();
        if (!string.IsNullOrEmpty(OnboardingData.FirstName)) q.Add("Q_FIRST_NAME");
        if (!string.IsNullOrEmpty(OnboardingData.LastName)) q.Add("Q_LAST_NAME");
        if (!string.IsNullOrEmpty(OnboardingData.HomeLocation)) q.Add("Q_HOME_LOCATED");
        if (!string.IsNullOrEmpty(OnboardingData.ColorScheme)) q.Add("Q_COLOR_SCHEME");
        if (OnboardingData.BudgetMin > 0 && OnboardingData.BudgetMax > 0) q.Add("Q_YOUR_BUDGET");
        if (OnboardingData.DesignInspoScreen1?.Count > 0) q.Add("Q_DESIGN_INSPO_SCREEN_1");
        if (OnboardingData.DesignInspoScreen2?.Count > 0) q.Add("Q_DESIGN_INSPO_SCREEN_2");
        if (OnboardingData.HomeSharingWith?.Count > 0) q.Add("Q_HOME_SHARING_WITH");
        return q;
    }

    [System.Serializable]
    public class OnboardingPayload
    {
        public string firstName;
        public string lastName;
        public string homeLocation;
        public string colorScheme;
        public List<string> homeSharingWith;
        public Budget budget;
        public List<string> designInspoScreen1;
        public List<string> designInspoScreen2;
        public List<string> completedQuestionEnums;

        [System.Serializable]
        public class Budget
        {
            public int min;
            public int max;
        }
    }
}
