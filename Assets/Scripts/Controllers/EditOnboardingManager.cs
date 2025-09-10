using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EditOnboardingManager : MonoBehaviour
{
    public static EditOnboardingManager Instance { get; private set; }
    public bool debugMode = false;
    public static bool IsInEditMode { get; private set; } = false;
    private static OnboardingDataBackup originalData;
    private static Dictionary<string, object> changedData = new Dictionary<string, object>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void StartEditOnboarding()
    {
        if (debugMode) Debug.Log("[EditOnboardingManager] Starting edit onboarding flow");
        bool isCreator = IsUserCreator();
        if (debugMode) Debug.Log($"[EditOnboardingManager] User type: {(isCreator ? "Creator" : "User")}");
        
        BackupOnboardingData();
        LoadProfileDataToOnboardingData();
        IsInEditMode = true;
        
        // Open appropriate first screen based on user type
        if (isCreator)
        {
            UIManager.Instance.OpenScreen(UIScreenType.CreatorType);
        }
        else
        {
            UIManager.Instance.OpenScreen(UIScreenType.Budget);
        }
    }

    public void CompleteEditOnboarding()
    {
        if (debugMode) Debug.Log("[EditOnboardingManager] Completing edit onboarding");
        LogChangedData();
        UIManager.Instance.OpenScreen(UIScreenType.ProfileSetting);
    }

    public void CompleteEditOnboardingWithoutAPI(System.Collections.IEnumerator onboardingDataCoroutine)
    {
        if (debugMode) Debug.Log("[EditOnboardingManager] Completing edit onboarding");
        LogChangedData();
        IsInEditMode = false;
        originalData = null;
        changedData.Clear();
        StartCoroutine(onboardingDataCoroutine);
    }

    public void CancelEditOnboarding()
    {
        if (debugMode) Debug.Log("[EditOnboardingManager] Cancelling edit onboarding");
        if (originalData != null) RestoreOnboardingData();
        IsInEditMode = false;
        originalData = null;
        UIManager.Instance.OpenScreen(UIScreenType.ProfileSetting);
    }

    public void EndEditSession()
    {
        IsInEditMode = false;
        originalData = null;
        changedData.Clear();
    }

    public static void TrackDataChange(string fieldName, object newValue, object oldValue = null)
    {
        if (!IsInEditMode) return;
        changedData[fieldName] = new { oldValue = oldValue, newValue = newValue, timestamp = System.DateTime.Now };
    }

    private void LogChangedData()
    {
        if (changedData.Count == 0)
        {
            Debug.Log("[EditOnboardingManager] No changes were made during edit session");
            return;
        }
        Debug.Log("=== EDIT ONBOARDING CHANGES SUMMARY ===");
        foreach (var change in changedData)
        {
            string fieldName = change.Key;
            var changeInfo = change.Value;
            var changeType = changeInfo.GetType();
            var oldValue = changeType.GetProperty("oldValue")?.GetValue(changeInfo);
            var newValue = changeType.GetProperty("newValue")?.GetValue(changeInfo);
            Debug.Log($"Field Changed: {fieldName}");
            Debug.Log($"  Old Value: {FormatValueForLog(oldValue)}");
            Debug.Log($"  New Value: {FormatValueForLog(newValue)}");
            Debug.Log("---");
        }
        Debug.Log($"Total fields changed: {changedData.Count}");
        Debug.Log("=== END CHANGES SUMMARY ===");
    }

    private string FormatValueForLog(object value)
    {
        if (value == null) return "null";
        if (value is System.Collections.IList list)
        {
            var stringList = new List<string>();
            foreach (var item in list) stringList.Add(item?.ToString() ?? "null");
            return $"[{string.Join(", ", stringList)}]";
        }
        return value.ToString();
    }

    private bool IsUserCreator()
    {
        if (ProfileDataHandlers.Instance?.ProfileData != null)
        {
            string role = ProfileDataHandlers.Instance.ProfileData.role;
            return !string.IsNullOrEmpty(role) && role.ToLower() == "creator";
        }
        var cachedData = ProfileDataHandlers.Instance?.LoadProfileCache();
        if (cachedData != null) return !string.IsNullOrEmpty(cachedData.role) && cachedData.role.ToLower() == "creator";
        if (debugMode) Debug.LogWarning("[EditOnboardingManager] Could not determine user type, defaulting to User");
        return false;
    }

    private void LoadProfileDataToOnboardingData()
    {
        ProfileCache profileData = ProfileDataHandlers.Instance?.ProfileData ?? ProfileDataHandlers.Instance?.LoadProfileCache();
        if (profileData == null)
        {
            Debug.LogWarning("[EditOnboardingManager] No profile data available to load");
            return;
        }

        if (!string.IsNullOrEmpty(profileData.firstName)) OnboardingData.FirstName = profileData.firstName;
        if (!string.IsNullOrEmpty(profileData.lastName)) OnboardingData.LastName = profileData.lastName;
        if (!string.IsNullOrEmpty(profileData.homeLocation)) OnboardingData.HomeLocation = profileData.homeLocation;
        if (profileData.minBudget > 0) OnboardingData.BudgetMin = profileData.minBudget;
        if (profileData.maxBudget > 0) OnboardingData.BudgetMax = profileData.maxBudget;
        if (profileData.colorScheme != null && profileData.colorScheme.Count > 0) OnboardingData.ColorScheme = new List<string>(profileData.colorScheme);
        if (profileData.homeSharingWith != null && profileData.homeSharingWith.Count > 0) OnboardingData.HomeSharingWith = new List<string>(profileData.homeSharingWith);
        
        if (profileData.designInspirations != null)
        {
            if (profileData.designInspirations.ContainsKey("CREATIVE_AND_CHARACTERFUL"))
                OnboardingData.DesignInspoScreen1 = new List<string>(profileData.designInspirations["CREATIVE_AND_CHARACTERFUL"]);
            if (profileData.designInspirations.ContainsKey("MODERN_AND_MINIMAL"))
                OnboardingData.DesignInspoScreen2 = new List<string>(profileData.designInspirations["MODERN_AND_MINIMAL"]);
        }

        if (!string.IsNullOrEmpty(profileData.creatorType)) OnboardingData.Occupation = profileData.creatorType;
        if (profileData.yearsOfExperience > 0) OnboardingData.YearsOfExperience = profileData.yearsOfExperience;
        
        OnboardingData.Tagline = !string.IsNullOrEmpty(profileData.tagline) ? profileData.tagline : null;
        OnboardingData.Website = !string.IsNullOrEmpty(profileData.website) ? profileData.website : null;
        
        if (profileData.region != null && profileData.region.Count > 0) OnboardingData.SelectedCities = new List<string>(profileData.region);
        
        if (profileData.socials != null && profileData.socials.Count > 0)
        {
            OnboardingData.SocialLinks = new Dictionary<string, string>();
            for (int i = 0; i < profileData.socials.Count; i++)
            {
                string socialUrl = profileData.socials[i];
                string platform = ParsePlatformFromUrl(socialUrl);
                OnboardingData.SocialLinks[platform] = socialUrl;
            }
        }
        else
        {
            OnboardingData.SocialLinks = null;
        }

        if (debugMode)
        {
            Debug.Log($"[EditOnboardingManager] Loaded data - Budget: {OnboardingData.BudgetMin}-{OnboardingData.BudgetMax}");
            Debug.Log($"[EditOnboardingManager] Creator Type: {OnboardingData.Occupation}");
            Debug.Log($"[EditOnboardingManager] Years of Experience: {OnboardingData.YearsOfExperience}");
            Debug.Log($"[EditOnboardingManager] Website: {OnboardingData.Website ?? "null"}");
            Debug.Log($"[EditOnboardingManager] Tagline: {OnboardingData.Tagline ?? "null"}");
        }
    }

    private string ParsePlatformFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "default";
        
        string lowerUrl = url.ToLower();
        if (lowerUrl.Contains("instagram")) return "instagram";
        if (lowerUrl.Contains("linkedin")) return "linkedin";
        if (lowerUrl.Contains("behance")) return "behance";
        if (lowerUrl.Contains("dribbble")) return "dribbble";
        if (lowerUrl.Contains("facebook")) return "facebook";
        if (lowerUrl.Contains("x.com") || lowerUrl.Contains("twitter")) return "twitter";
        return "default";
    }

    private void BackupOnboardingData()
    {
        originalData = new OnboardingDataBackup
        {
            FirstName = OnboardingData.FirstName,
            LastName = OnboardingData.LastName,
            HomeLocation = OnboardingData.HomeLocation,
            BudgetMin = OnboardingData.BudgetMin,
            BudgetMax = OnboardingData.BudgetMax,
            ColorScheme = OnboardingData.ColorScheme != null ? new List<string>(OnboardingData.ColorScheme) : null,
            HomeSharingWith = OnboardingData.HomeSharingWith != null ? new List<string>(OnboardingData.HomeSharingWith) : null,
            DesignInspoScreen1 = OnboardingData.DesignInspoScreen1 != null ? new List<string>(OnboardingData.DesignInspoScreen1) : null,
            DesignInspoScreen2 = OnboardingData.DesignInspoScreen2 != null ? new List<string>(OnboardingData.DesignInspoScreen2) : null,
            // Creator fields
            Occupation = OnboardingData.Occupation,
            YearsOfExperience = OnboardingData.YearsOfExperience,
            Tagline = OnboardingData.Tagline,
            Website = OnboardingData.Website,
            SelectedCities = OnboardingData.SelectedCities != null ? new List<string>(OnboardingData.SelectedCities) : null,
            SocialLinks = OnboardingData.SocialLinks != null ? new Dictionary<string, string>(OnboardingData.SocialLinks) : null
        };
        if (debugMode) Debug.Log("[EditOnboardingManager] OnboardingData backed up");
    }

    private void RestoreOnboardingData()
    {
        if (originalData == null) return;
        
        OnboardingData.FirstName = originalData.FirstName;
        OnboardingData.LastName = originalData.LastName;
        OnboardingData.HomeLocation = originalData.HomeLocation;
        OnboardingData.BudgetMin = originalData.BudgetMin;
        OnboardingData.BudgetMax = originalData.BudgetMax;
        OnboardingData.ColorScheme = originalData.ColorScheme != null ? new List<string>(originalData.ColorScheme) : null;
        OnboardingData.HomeSharingWith = originalData.HomeSharingWith != null ? new List<string>(originalData.HomeSharingWith) : null;
        OnboardingData.DesignInspoScreen1 = originalData.DesignInspoScreen1 != null ? new List<string>(originalData.DesignInspoScreen1) : null;
        OnboardingData.DesignInspoScreen2 = originalData.DesignInspoScreen2 != null ? new List<string>(originalData.DesignInspoScreen2) : null;
        
        // Creator fields
        OnboardingData.Occupation = originalData.Occupation;
        OnboardingData.YearsOfExperience = originalData.YearsOfExperience;
        OnboardingData.Tagline = originalData.Tagline;
        OnboardingData.Website = originalData.Website;
        OnboardingData.SelectedCities = originalData.SelectedCities != null ? new List<string>(originalData.SelectedCities) : null;
        OnboardingData.SocialLinks = originalData.SocialLinks != null ? new Dictionary<string, string>(originalData.SocialLinks) : null;
        
        if (debugMode) Debug.Log("[EditOnboardingManager] OnboardingData restored from backup");
    }

    public static int GetBudgetSelectionIndex()
    {
        if (OnboardingData.BudgetMin == 0 && OnboardingData.BudgetMax == 300000) return 0;
        else if (OnboardingData.BudgetMin == 300000 && OnboardingData.BudgetMax == 500000) return 1;
        else if (OnboardingData.BudgetMin == 500000 && OnboardingData.BudgetMax == 700000) return 2;
        return -1;
    }

    public static int GetDesignerTypeIndex(string[] designerOptions)
    {
        if (string.IsNullOrEmpty(OnboardingData.Occupation)) return -1;
        
        for (int i = 0; i < designerOptions.Length; i++)
        {
            if (designerOptions[i].Equals(OnboardingData.Occupation, System.StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }
}

[System.Serializable]
public class OnboardingDataBackup
{
    // User fields
    public string FirstName;
    public string LastName;
    public string HomeLocation;
    public int BudgetMin;
    public int BudgetMax;
    public List<string> ColorScheme;
    public List<string> HomeSharingWith;
    public List<string> DesignInspoScreen1;
    public List<string> DesignInspoScreen2;
    
    // Creator fields
    public string Occupation;
    public int? YearsOfExperience;
    public string Tagline;
    public string Website;
    public List<string> SelectedCities;
    public Dictionary<string, string> SocialLinks;
}