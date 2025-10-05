using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class ProfileSettingsController : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument uiDocument;
    
    private VisualElement root;
    private Image userPhoto;
    private TextElement userName;
    private TextElement userEmail;
    private VisualElement designerTag;
    private Button backButton;
    private Button signOutButton;
    
    private VisualElement editProfileTab;
    private VisualElement privacySecurityTab;
    private VisualElement editOnboardingTab;
    private VisualElement helpSupportTab;
    private VisualElement shareAppTab;
    
    private void OnEnable()
    {
        InitializeUI();
        LoadProfileData();
    }
    
    private void InitializeUI()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
            
        root = uiDocument.rootVisualElement;
        
        userPhoto = root.Q<Image>("userPhoto");
        userName = root.Q<TextElement>("userName");
        userEmail = root.Q<TextElement>("userEmail");
        designerTag = root.Q<VisualElement>("designerTag");
        backButton = root.Q<Button>("backButton");
        signOutButton = root.Q<Button>("signOutButton");
        
        var accountDetailsList = root.Query<VisualElement>("accountDetails").ToList();
        if (accountDetailsList.Count >= 5)
        {
            editProfileTab = accountDetailsList[0];
            privacySecurityTab = accountDetailsList[1];
            editOnboardingTab = accountDetailsList[2];
            helpSupportTab = accountDetailsList[3];
            shareAppTab = accountDetailsList[4];
        }
        
        SetupButtonCallbacks();
    }
    
    private void SetupButtonCallbacks()
    {
        backButton?.RegisterCallback<ClickEvent>(evt => OnBackButtonClicked());
        signOutButton?.RegisterCallback<ClickEvent>(evt => OnSignOutButtonClicked());
        editProfileTab?.RegisterCallback<ClickEvent>(evt => OnEditProfileClicked());
        privacySecurityTab?.RegisterCallback<ClickEvent>(evt => OnPrivacySecurityClicked());
        editOnboardingTab?.RegisterCallback<ClickEvent>(evt => OnEditOnboardingClicked());
        helpSupportTab?.RegisterCallback<ClickEvent>(evt => OnHelpSupportClicked());
        shareAppTab?.RegisterCallback<ClickEvent>(evt => OnShareAppClicked());
    }
    
    private void LoadProfileData()
    {
        if (ProfileDataHandlers.Instance != null && ProfileDataHandlers.Instance.ProfileData != null)
        {
            UpdateUIWithProfileData(ProfileDataHandlers.Instance.ProfileData);
        }
        else
        {
            Debug.LogWarning("[ProfileSettingsController] ProfileDataHandlers instance or data is null");
            if (ProfileDataHandlers.Instance != null)
            {
                var cachedData = ProfileDataHandlers.Instance.LoadProfileCache();
                if (cachedData != null)
                {
                    UpdateUIWithProfileData(cachedData);
                }
                else
                {
                    Debug.LogWarning("[ProfileSettingsController] No cached profile data available");
                }
            }
        }
    }
    
    private void UpdateUIWithProfileData(ProfileCache profileData)
    {
        if (profileData == null)
        {
            Debug.LogError("[ProfileSettingsController] Profile data is null");
            return;
        }
        string fullName = "";
        if (!string.IsNullOrEmpty(profileData.firstName) || !string.IsNullOrEmpty(profileData.lastName))
        {
            fullName = $"{profileData.firstName} {profileData.lastName}".Trim();
        }
        else if (!string.IsNullOrEmpty(profileData.userName))
        {
            fullName = profileData.userName;
        }
        if (userName != null && !string.IsNullOrEmpty(fullName))
        {
            userName.text = fullName;
        }
        if (userEmail != null && !string.IsNullOrEmpty(profileData.email))
        {
            userEmail.text = profileData.email;
        }
        if (userPhoto != null && !string.IsNullOrEmpty(profileData.avatar))
        {
            StartCoroutine(LoadProfileImage(profileData.avatar));
        }
        if (designerTag != null)
        {
            bool isCreator = !string.IsNullOrEmpty(profileData.role) && 
                           profileData.role.ToLower() == "creator";
            if (isCreator)
            {
                designerTag.style.display = DisplayStyle.Flex;
                var profileTag = designerTag.Q<TextElement>("profileTag");
                if (profileTag != null && !string.IsNullOrEmpty(profileData.creatorType))
                {
                    profileTag.text = profileData.creatorType == "User" ? "User" : "  Interior Designer";
                }
            }
            else
            {
                designerTag.style.display = DisplayStyle.None;
            }
        }
        Debug.Log($"[ProfileSettingsController] UI updated for user: {fullName}, Role: {profileData.role}");
    }
    
    private IEnumerator LoadProfileImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            yield break;
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                if (userPhoto != null && texture != null)
                {
                    userPhoto.image = texture;
                    Debug.Log("[ProfileSettingsController] Profile image loaded successfully");
                }
            }
            else
            {
                Debug.LogWarning($"[ProfileSettingsController] Failed to load profile image: {request.error}");
            }
        }
    }
    
    #region Button Callbacks
    
    private void OnBackButtonClicked()
    {
        UIManager.Instance.TransitionScreens(UIScreenType.ProfileSetting, UIScreenType.Profile);
    }
    
    private void OnSignOutButtonClicked()
    {
        Debug.Log("[ProfileSettingsController] Sign out button clicked");
    }
    
    private void OnEditProfileClicked()
    {
        UIManager.Instance.TransitionScreens(UIScreenType.ProfileSetting, UIScreenType.EditProfile);
    }
    
    private void OnPrivacySecurityClicked()
    {
        UIManager.Instance.TransitionScreens(UIScreenType.ProfileSetting, UIScreenType.PrivacySecurity);
    }
    
    private void OnEditOnboardingClicked()
    {
        if (EditOnboardingManager.Instance != null)
        {
            EditOnboardingManager.Instance.StartEditOnboarding();
        }
        else
        {
            Debug.LogError("[ProfileSettingsController] EditOnboardingManager instance not found!");
        }
    }
    
    private void OnHelpSupportClicked()
    {
        UIManager.Instance.TransitionScreens(UIScreenType.ProfileSetting, UIScreenType.HelpSupport);
    }
    
    private void OnShareAppClicked()
    {
        Debug.Log("[ProfileSettingsController] Share App clicked");
    }
    
    #endregion
    
    private void OnDestroy()
    {
        backButton?.UnregisterCallback<ClickEvent>(evt => OnBackButtonClicked());
        signOutButton?.UnregisterCallback<ClickEvent>(evt => OnSignOutButtonClicked());
    }
}