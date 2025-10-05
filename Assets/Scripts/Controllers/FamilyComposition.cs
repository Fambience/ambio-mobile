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

    private Button backButton, completeButton;
    private Toggle aloneToggle, partnerToggle, familyToggle, roommatesToggle, petsToggle;

    private List<Toggle> allToggles = new();
    private Dictionary<Toggle, string> toggleLabels = new();

    [SerializeField] private GameObject dataHandler;

    void OnEnable()
    {
        baseURL = baseScript.baseURL;
        InitializeUI();
        SetupEventListeners();
        UpdateCompleteButtonState();
        Debug.Log("📥 Entered FamilyComposition screen");
        
        if (EditOnboardingManager.IsInEditMode)
        {
            PrefillFamilyComposition();
        }

        if (OnboardingData.ColorScheme == null)
        {
            Debug.LogError("❌ ColorScheme is null on FamilyComposition!");
        }
        else
        {
            Debug.Log($"✅ ColorScheme received in FamilyComposition: {string.Join(",", OnboardingData.ColorScheme)}");
        }
    }
    
    void PrefillFamilyComposition()
    {
        if (OnboardingData.HomeSharingWith != null && OnboardingData.HomeSharingWith.Count > 0)
        {
            Debug.Log($"[FamilyComposition] Prefilling selections: {string.Join(", ", OnboardingData.HomeSharingWith)}");
        
            foreach (var selection in OnboardingData.HomeSharingWith)
            {
                var toggle = toggleLabels.FirstOrDefault(kvp => kvp.Value == selection).Key;
                if (toggle != null)
                {
                    toggle.value = true;
                }
            }
        }
        else
        {
            Debug.Log("[FamilyComposition] No existing family composition data to prefill");
        }
    }

    void InitializeUI()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        backButton = root.Q<Button>("backButton");
        completeButton = root.Q<Button>("completeButton");

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

        // Check if we're in edit mode
        if (EditOnboardingManager.IsInEditMode)
        {
            // Use update API for edit mode
            yield return UpdateOnboardingData(token);
        }
        else
        {
            // Use create API for normal onboarding
            yield return CreateOnboardingData(token);
        }
    }

    IEnumerator CreateOnboardingData(string token)
    {
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

        var filteredColorSchemes = OnboardingData.ColorScheme?
            .Where(cs => OnboardingEnumValidator.ColorSchemes.Contains(cs))
            .ToList();
        AddIfNotEmpty("colorScheme", filteredColorSchemes);

        AddIfNotEmpty("homeSharingWith", OnboardingData.HomeSharingWith);
        if (OnboardingData.BudgetMin > 0) payload["minBudget"] = OnboardingData.BudgetMin;
        if (OnboardingData.BudgetMax > 0) payload["maxBudget"] = OnboardingData.BudgetMax;

        if (OnboardingData.DesignInspoScreen1?.Count > 0 || OnboardingData.DesignInspoScreen2?.Count > 0)
        {
            var designMap = new Dictionary<string, List<string>>();

            if (OnboardingData.DesignInspoScreen1?.Count > 0)
            {
                var filteredCreative = OnboardingData.DesignInspoScreen1
                    .Where(style => OnboardingEnumValidator.CreativeStyles.Contains(style))
                    .ToList();

                if (filteredCreative.Count > 0)
                    designMap["CREATIVE_AND_CHARACTERFUL"] = filteredCreative;
            }

            if (OnboardingData.DesignInspoScreen2?.Count > 0)
            {
                var filteredModern = OnboardingData.DesignInspoScreen2
                    .Where(style => OnboardingEnumValidator.ModernStyles.Contains(style))
                    .ToList();

                if (filteredModern.Count > 0)
                    designMap["MODERN_AND_MINIMAL"] = filteredModern;
            }

            if (designMap.Count > 0)
                payload["designInspirations"] = designMap;
        }

        string json = JSON.Serialize(payload);
        Debug.Log("[FamilyComposition] Submitting onboarding payload: " + json);

        using (UnityWebRequest request = new UnityWebRequest(baseURL + onboardingEndpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", token);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[FamilyComposition] Submission failed: " + request.error);
                Debug.LogError(request.downloadHandler.text);
            }
            else
            {
                Debug.Log("[FamilyComposition] Onboarding data submitted successfully.");
                UIManager.Instance.OpenScreen(UIScreenType.Home);
                dataHandler.SetActive(true);
            }
        }
    }
    
    IEnumerator UpdateOnboardingData(string token)
    {
        var payload = new Dictionary<string, object>();

        // Add budget
        if (OnboardingData.BudgetMin > 0) payload["minBudget"] = OnboardingData.BudgetMin;
        if (OnboardingData.BudgetMax > 0) payload["maxBudget"] = OnboardingData.BudgetMax;

        // Add design inspirations with API-expected format (camelCase)
        if (OnboardingData.DesignInspoScreen1?.Count > 0 || OnboardingData.DesignInspoScreen2?.Count > 0)
        {
            var designMap = new Dictionary<string, List<string>>();

            if (OnboardingData.DesignInspoScreen1?.Count > 0)
            {
                var filteredCreative = OnboardingData.DesignInspoScreen1
                    .Where(style => OnboardingEnumValidator.CreativeStyles.Contains(style))
                    .ToList();

                if (filteredCreative.Count > 0)
                    designMap["creativeAndCharacterful"] = filteredCreative;
            }

            if (OnboardingData.DesignInspoScreen2?.Count > 0)
            {
                var filteredModern = OnboardingData.DesignInspoScreen2
                    .Where(style => OnboardingEnumValidator.ModernStyles.Contains(style))
                    .ToList();

                if (filteredModern.Count > 0)
                    designMap["modernAndMinimal"] = filteredModern;
            }

            if (designMap.Count > 0)
                payload["designInspirations"] = designMap;
        }

        // Add color scheme
        var filteredColorSchemes = OnboardingData.ColorScheme?
            .Where(cs => OnboardingEnumValidator.ColorSchemes.Contains(cs))
            .ToList();
        
        if (filteredColorSchemes != null && filteredColorSchemes.Count > 0)
            payload["colorScheme"] = filteredColorSchemes;

        // Add home sharing with
        if (OnboardingData.HomeSharingWith != null && OnboardingData.HomeSharingWith.Count > 0)
            payload["homeSharingWith"] = OnboardingData.HomeSharingWith;

        string json = JSON.Serialize(payload);
        Debug.Log("[FamilyComposition] Updating onboarding payload: " + json);

        string updateEndpoint = "/api/v1/profile/update-user-onboarding";
        using (UnityWebRequest request = new UnityWebRequest(baseURL + updateEndpoint, "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", token);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[FamilyComposition] Update failed: " + request.error);
                Debug.LogError("[FamilyComposition] Response: " + request.downloadHandler.text);
            }
            else
            {
                Debug.Log("[FamilyComposition] Onboarding data updated successfully.");
                Debug.Log("[FamilyComposition] Response: " + request.downloadHandler.text);
                
                // Complete edit session
                if (EditOnboardingManager.Instance != null)
                {
                    EditOnboardingManager.Instance.CompleteEditOnboarding();
                }
                
                if (ProfileDataHandlers.Instance != null)
                {
                    StartCoroutine(ProfileDataHandlers.Instance.FetchProfileData(token, success => {
                        if (success)
                        {
                            Debug.Log("[FamilyComposition] Profile data refreshed after update");
                        }
                    }));
                }
            }
        }
    }
}
