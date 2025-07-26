/*
using Services;
using UnityEngine;
using UnityEngine.UIElements;

public class ProfileScreenController : MonoBehaviour
{
    [Header("Card Templates")] public VisualTreeAsset postCardTemplate;
    [Header("UI Toolkit")] public UIDocument uiDocument;

    private VisualElement root;
    private ScrollView scrollView;
    private VisualElement tabContentContainer;
    private VisualElement aboutContent;
    private VisualElement designerField;
    private VisualElement userField;
    private Label fullNameLabel;
    private Label locationLabel;
    private Label statsLabel;

    // CREATOR-specific
    private Label creatorTagline;
    private Label creatorType;
    private Label creatorWebsite;
    private Label creatorRegion;
    private Label creatorSocials;

    // USER-specific
    private Label userBudget;
    private Label userColorScheme;
    private Label userSharing;
    private Label userDesignInspiration;

    private Button designsTab;
    private Button savedTab;
    private Button aboutTab;

    private VisualElement designsTabButton;
    private VisualElement savedTabButton;
    private VisualElement aboutTabButton;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        scrollView = root.Q<ScrollView>("scroll-container");
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        designsTab = root.Q<Button>("designsTab");
        savedTab = root.Q<Button>("savedTab");
        aboutTab = root.Q<Button>("aboutTab");

        designerField = root.Q<VisualElement>("designerFields");
        userField = root.Q<VisualElement>("userFields");

        fullNameLabel = root.Q<Label>("fullName");
        locationLabel = root.Q<Label>("location");
        statsLabel = root.Q<Label>("stats");

        // Creator fields
        creatorTagline = root.Q<Label>("creatorTagline");
        creatorType = root.Q<Label>("creatorType");
        creatorWebsite = root.Q<Label>("creatorWebsite");
        creatorRegion = root.Q<Label>("creatorRegion");
        creatorSocials = root.Q<Label>("creatorSocials");

        // User fields
        userBudget = root.Q<Label>("userBudget");
        userColorScheme = root.Q<Label>("userColorScheme");
        userSharing = root.Q<Label>("userSharing");
        userDesignInspiration = root.Q<Label>("userDesignInspiration");

        designsTabButton = designsTab?.parent;
        savedTabButton = savedTab?.parent;
        aboutTabButton = aboutTab?.parent;

        aboutContent = root.Q<VisualElement>("aboutContent");

        tabContentContainer = new VisualElement();
        tabContentContainer.name = "tabContentContainer";
        tabContentContainer.AddToClassList("tab-content");
        scrollView.Add(tabContentContainer);

        designsTab.clicked += () => ShowDesignsTab();
        savedTab.clicked += () => ShowSavedTab();
        aboutTab.clicked += () => ShowAboutTab();

        TryPopulateFromProfile();
    }

    private void TryPopulateFromProfile()
    {
        var profile = ProfileDataHandlers.Instance.ProfileData;
        if (profile == null)
        {
            Debug.LogWarning("Profile data is not yet available.");
            ShowEmptyState("Loading profile...");
            return;
        }

        if (fullNameLabel != null)
            fullNameLabel.text = $"{profile.firstName} {profile.lastName}";

        if (locationLabel != null)
            locationLabel.text = profile.homeLocation ?? "Unknown";

        if (statsLabel != null)
            statsLabel.text = $"{profile.postCount} posts · {profile.followerCount} followers · {profile.followingCount} following";

        // Toggle visibility based on role
        if (designerField != null && userField != null)
        {
            if (profile.role == "CREATOR")
            {
                designerField.style.display = DisplayStyle.Flex;
                userField.style.display = DisplayStyle.None;

                if (creatorTagline != null) creatorTagline.text = profile.tagline ?? "-";
                if (creatorType != null) creatorType.text = profile.creatorType ?? "-";
                if (creatorWebsite != null) creatorWebsite.text = profile.website ?? "-";
                if (creatorRegion != null) creatorRegion.text = string.Join(", ", profile.region ?? new());
                if (creatorSocials != null) creatorSocials.text = string.Join(", ", profile.socials ?? new());
            }
            else // USER
            {
                designerField.style.display = DisplayStyle.None;
                userField.style.display = DisplayStyle.Flex;

                if (userBudget != null) userBudget.text = $"₹{profile.minBudget:N0} - ₹{profile.maxBudget:N0}";
                if (userColorScheme != null) userColorScheme.text = string.Join(", ", profile.colorScheme ?? new());
                if (userSharing != null) userSharing.text = string.Join(", ", profile.homeSharingWith ?? new());

                if (userDesignInspiration != null)
                {
                    string formatted = "";
                    foreach (var pair in profile.designInspirations)
                    {
                        formatted += $"{pair.Key}: {string.Join(", ", pair.Value)}\n";
                    }
                    userDesignInspiration.text = formatted.Trim();
                }
            }
        }

        ShowDesignsTab(); // default
    }

    private void ShowDesignsTab()
    {
        RemoveSelectedFromAllTabs();
        designsTabButton.AddToClassList("selected");

        if (aboutContent != null)
            aboutContent.style.display = DisplayStyle.None;

        tabContentContainer.style.display = DisplayStyle.Flex;
        tabContentContainer.Clear();

        LoadDesignCards();
    }

    private void ShowSavedTab()
    {
        RemoveSelectedFromAllTabs();
        savedTabButton.AddToClassList("selected");

        if (aboutContent != null)
            aboutContent.style.display = DisplayStyle.None;

        tabContentContainer.style.display = DisplayStyle.Flex;
        tabContentContainer.Clear();

        LoadSavedCards();
    }

    private void ShowAboutTab()
    {
        RemoveSelectedFromAllTabs();
        aboutTabButton.AddToClassList("selected");

        tabContentContainer.style.display = DisplayStyle.None;
        tabContentContainer.Clear();

        if (aboutContent != null)
            aboutContent.style.display = DisplayStyle.Flex;
    }

    private void RemoveSelectedFromAllTabs()
    {
        designsTabButton?.RemoveFromClassList("selected");
        savedTabButton?.RemoveFromClassList("selected");
        aboutTabButton?.RemoveFromClassList("selected");
    }

    private void LoadDesignCards()
    {
        if (postCardTemplate == null)
        {
            Debug.LogError("Post card template is not assigned!");
            ShowEmptyState("No card template assigned");
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            VisualElement postCard = postCardTemplate.CloneTree();

            var userName = postCard.Q<TextElement>("userName");
            if (userName != null)
                userName.text = "Krishna Yadav";

            var description = postCard.Q<TextElement>("description");
            if (description != null)
                description.text = $"Design {i + 1}: Warm, minimal, clean palette setup.";

            var userImage = postCard.Q<Image>("userImage");
            if (userImage != null)
                userImage.image = LoadImage("person");

            var cardImage = postCard.Q<Image>("card-image");
            if (cardImage != null)
                cardImage.image = LoadImage("Contemporary");

            tabContentContainer.Add(postCard);
        }
    }

    private void LoadSavedCards()
    {
        if (postCardTemplate == null)
        {
            Debug.LogError("Post card template is not assigned!");
            ShowEmptyState("No card template assigned");
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            VisualElement postCard = postCardTemplate.CloneTree();

            var userName = postCard.Q<TextElement>("userName");
            if (userName != null)
                userName.text = "Krishna Yadav";

            var description = postCard.Q<TextElement>("description");
            if (description != null)
                description.text = $"Saved Design {i + 1} from your collection.";

            var userImage = postCard.Q<Image>("userImage");
            if (userImage != null)
                userImage.image = LoadImage("person");

            var cardImage = postCard.Q<Image>("card-image");
            if (cardImage != null)
                cardImage.image = LoadImage("Contemporary");

            tabContentContainer.Add(postCard);
        }
    }

    private void ShowEmptyState(string message)
    {
        var emptyState = new VisualElement();
        emptyState.AddToClassList("empty-state");

        var emptyText = new TextElement { text = message };
        emptyText.AddToClassList("empty-state-text");

        emptyState.Add(emptyText);
        tabContentContainer.Add(emptyState);
    }

    private Texture2D LoadImage(string imageName)
    {
        Texture2D texture = Resources.Load<Texture2D>(imageName);
        if (texture == null)
            texture = Resources.Load<Texture2D>($"Images/{imageName}");

        if (texture == null)
            Debug.LogWarning($"Could not load image: {imageName}");

        return texture;
    }

    private void OnDisable()
    {
        if (designsTab != null) designsTab.clicked -= ShowDesignsTab;
        if (savedTab != null) savedTab.clicked -= ShowSavedTab;
        if (aboutTab != null) aboutTab.clicked -= ShowAboutTab;
    }
}
*/

using Services;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class ProfileScreenController : MonoBehaviour
{
    [Header("Card Templates")] public VisualTreeAsset postCardTemplate;
    [Header("UI Toolkit")] public UIDocument uiDocument;

    private VisualElement root;
    private ScrollView scrollView;
    private VisualElement tabContentContainer;
    private VisualElement aboutContent;
    private VisualElement designerField;
    private VisualElement userField;
    private Label fullNameLabel;
    private Label locationLabel;
    private Label statsLabel;

    // CREATOR-specific
    private Label creatorTagline;
    private Label creatorType;
    private Label creatorWebsite;
    private Label creatorRegion;
    private Label creatorSocials;

    // USER-specific
    private Label userBudget;
    private Label userColorScheme;
    private Label userSharing;
    private Label userDesignInspiration;

    private Button designsTab;
    private Button savedTab;
    private Button aboutTab;

    private VisualElement designsTabButton;
    private VisualElement savedTabButton;
    private VisualElement aboutTabButton;

    private bool hasTriedToPopulate = false;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        scrollView = root.Q<ScrollView>("scroll-container");
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        designsTab = root.Q<Button>("designsTab");
        savedTab = root.Q<Button>("savedTab");
        aboutTab = root.Q<Button>("aboutTab");

        designerField = root.Q<VisualElement>("designerFields");
        userField = root.Q<VisualElement>("userFields");

        fullNameLabel = root.Q<Label>("fullName");
        locationLabel = root.Q<Label>("location");
        statsLabel = root.Q<Label>("stats");

        // Creator fields
        creatorTagline = root.Q<Label>("creatorTagline");
        creatorType = root.Q<Label>("creatorType");
        creatorWebsite = root.Q<Label>("creatorWebsite");
        creatorRegion = root.Q<Label>("creatorRegion");
        creatorSocials = root.Q<Label>("creatorSocials");

        // User fields
        userBudget = root.Q<Label>("userBudget");
        userColorScheme = root.Q<Label>("userColorScheme");
        userSharing = root.Q<Label>("userSharing");
        userDesignInspiration = root.Q<Label>("userDesignInspiration");

        designsTabButton = designsTab?.parent;
        savedTabButton = savedTab?.parent;
        aboutTabButton = aboutTab?.parent;

        aboutContent = root.Q<VisualElement>("aboutContent");

        tabContentContainer = new VisualElement();
        tabContentContainer.name = "tabContentContainer";
        tabContentContainer.AddToClassList("tab-content");
        scrollView.Add(tabContentContainer);

        designsTab.clicked += () => ShowDesignsTab();
        savedTab.clicked += () => ShowSavedTab();
        aboutTab.clicked += () => ShowAboutTab();

        // Start coroutine to periodically check for profile data
        StartCoroutine(WaitForProfileData());
    }

    private IEnumerator WaitForProfileData()
    {
        // Wait for ProfileDataHandlers to be available
        while (ProfileDataHandlers.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Keep checking until profile data is available
        while (ProfileDataHandlers.Instance.ProfileData == null && !hasTriedToPopulate)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Try to populate once data is available
        if (ProfileDataHandlers.Instance.ProfileData != null)
        {
            TryPopulateFromProfile();
        }
        else
        {
            // Try loading from cache if network fetch failed
            var cachedProfile = ProfileDataHandlers.Instance.LoadProfileCache();
            if (cachedProfile != null)
            {
                Debug.Log("[ProfileScreenController] Using cached profile data");
                TryPopulateFromProfile();
            }
            else
            {
                ShowEmptyState("Unable to load profile data");
            }
        }
    }

    private void TryPopulateFromProfile()
    {
        if (hasTriedToPopulate) return;
        hasTriedToPopulate = true;

        var profile = ProfileDataHandlers.Instance.ProfileData;
        if (profile == null)
        {
            Debug.LogWarning("[ProfileScreenController] Profile data is not yet available.");
            ShowEmptyState("Loading profile...");
            return;
        }

        Debug.Log($"[ProfileScreenController] Populating UI with profile data for: {profile.userName}");

        // Populate basic info
        if (fullNameLabel != null)
        {
            string fullName = $"{profile.firstName} {profile.lastName}".Trim();
            fullNameLabel.text = string.IsNullOrEmpty(fullName) ? profile.userName : fullName;
            Debug.Log($"[ProfileScreenController] Set full name: {fullNameLabel.text}");
        }

        if (locationLabel != null)
        {
            locationLabel.text = !string.IsNullOrEmpty(profile.homeLocation) ? profile.homeLocation : "Location not set";
            Debug.Log($"[ProfileScreenController] Set location: {locationLabel.text}");
        }

        if (statsLabel != null)
        {
            statsLabel.text = $"{profile.postCount} posts · {profile.followerCount} followers · {profile.followingCount} following";
            Debug.Log($"[ProfileScreenController] Set stats: {statsLabel.text}");
        }

        // Toggle visibility based on role
        if (designerField != null && userField != null)
        {
            Debug.Log($"[ProfileScreenController] User role: {profile.role}");
            
            if (profile.role == "CREATOR")
            {
                designerField.style.display = DisplayStyle.Flex;
                userField.style.display = DisplayStyle.None;

                // Populate creator fields
                if (creatorTagline != null) 
                {
                    creatorTagline.text = !string.IsNullOrEmpty(profile.tagline) ? profile.tagline : "No tagline set";
                    Debug.Log($"[ProfileScreenController] Set creator tagline: {creatorTagline.text}");
                }
                
                if (creatorType != null) 
                {
                    creatorType.text = !string.IsNullOrEmpty(profile.creatorType) ? profile.creatorType : "Creator";
                    Debug.Log($"[ProfileScreenController] Set creator type: {creatorType.text}");
                }
                
                if (creatorWebsite != null) 
                {
                    creatorWebsite.text = !string.IsNullOrEmpty(profile.website) ? profile.website : "No website";
                    Debug.Log($"[ProfileScreenController] Set website: {creatorWebsite.text}");
                }
                
                if (creatorRegion != null) 
                {
                    creatorRegion.text = (profile.region != null && profile.region.Count > 0) ? 
                        string.Join(", ", profile.region) : "No region set";
                    Debug.Log($"[ProfileScreenController] Set region: {creatorRegion.text}");
                }
                
                if (creatorSocials != null) 
                {
                    creatorSocials.text = (profile.socials != null && profile.socials.Count > 0) ? 
                        string.Join(", ", profile.socials) : "No socials";
                    Debug.Log($"[ProfileScreenController] Set socials: {creatorSocials.text}");
                }
            }
            else // USER
            {
                designerField.style.display = DisplayStyle.None;
                userField.style.display = DisplayStyle.Flex;

                // Populate user fields
                if (userBudget != null) 
                {
                    if (profile.minBudget > 0 || profile.maxBudget > 0)
                    {
                        userBudget.text = $"₹{profile.minBudget:N0} - ₹{profile.maxBudget:N0}";
                    }
                    else
                    {
                        userBudget.text = "Budget not set";
                    }
                    Debug.Log($"[ProfileScreenController] Set budget: {userBudget.text}");
                }
                
                if (userColorScheme != null) 
                {
                    userColorScheme.text = (profile.colorScheme != null && profile.colorScheme.Count > 0) ? 
                        string.Join(", ", profile.colorScheme) : "No color scheme set";
                    Debug.Log($"[ProfileScreenController] Set color scheme: {userColorScheme.text}");
                }
                
                if (userSharing != null) 
                {
                    userSharing.text = (profile.homeSharingWith != null && profile.homeSharingWith.Count > 0) ? 
                        string.Join(", ", profile.homeSharingWith) : "No sharing info";
                    Debug.Log($"[ProfileScreenController] Set sharing: {userSharing.text}");
                }

                if (userDesignInspiration != null)
                {
                    string formatted = "";
                    if (profile.designInspirations != null && profile.designInspirations.Count > 0)
                    {
                        foreach (var pair in profile.designInspirations)
                        {
                            formatted += $"{pair.Key}: {string.Join(", ", pair.Value)}\n";
                        }
                        userDesignInspiration.text = formatted.Trim();
                    }
                    else
                    {
                        userDesignInspiration.text = "No design inspirations set";
                    }
                    Debug.Log($"[ProfileScreenController] Set design inspiration: {userDesignInspiration.text}");
                }
            }
        }

        ShowDesignsTab(); // default tab
    }

    private void ShowDesignsTab()
    {
        RemoveSelectedFromAllTabs();
        designsTabButton.AddToClassList("selected");

        if (aboutContent != null)
            aboutContent.style.display = DisplayStyle.None;

        tabContentContainer.style.display = DisplayStyle.Flex;
        tabContentContainer.Clear();

        LoadDesignCards();
    }

    private void ShowSavedTab()
    {
        RemoveSelectedFromAllTabs();
        savedTabButton.AddToClassList("selected");

        if (aboutContent != null)
            aboutContent.style.display = DisplayStyle.None;

        tabContentContainer.style.display = DisplayStyle.Flex;
        tabContentContainer.Clear();

        LoadSavedCards();
    }

    private void ShowAboutTab()
    {
        RemoveSelectedFromAllTabs();
        aboutTabButton.AddToClassList("selected");

        tabContentContainer.style.display = DisplayStyle.None;
        tabContentContainer.Clear();

        if (aboutContent != null)
            aboutContent.style.display = DisplayStyle.Flex;
    }

    private void RemoveSelectedFromAllTabs()
    {
        designsTabButton?.RemoveFromClassList("selected");
        savedTabButton?.RemoveFromClassList("selected");
        aboutTabButton?.RemoveFromClassList("selected");
    }

    private void LoadDesignCards()
    {
        if (postCardTemplate == null)
        {
            Debug.LogError("Post card template is not assigned!");
            ShowEmptyState("No card template assigned");
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            VisualElement postCard = postCardTemplate.CloneTree();

            var userName = postCard.Q<TextElement>("userName");
            if (userName != null)
                userName.text = "Krishna Yadav";

            var description = postCard.Q<TextElement>("description");
            if (description != null)
                description.text = $"Design {i + 1}: Warm, minimal, clean palette setup.";

            var userImage = postCard.Q<Image>("userImage");
            if (userImage != null)
                userImage.image = LoadImage("person");

            var cardImage = postCard.Q<Image>("card-image");
            if (cardImage != null)
                cardImage.image = LoadImage("Contemporary");

            tabContentContainer.Add(postCard);
        }
    }

    private void LoadSavedCards()
    {
        if (postCardTemplate == null)
        {
            Debug.LogError("Post card template is not assigned!");
            ShowEmptyState("No card template assigned");
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            VisualElement postCard = postCardTemplate.CloneTree();

            var userName = postCard.Q<TextElement>("userName");
            if (userName != null)
                userName.text = "Krishna Yadav";

            var description = postCard.Q<TextElement>("description");
            if (description != null)
                description.text = $"Saved Design {i + 1} from your collection.";

            var userImage = postCard.Q<Image>("userImage");
            if (userImage != null)
                userImage.image = LoadImage("person");

            var cardImage = postCard.Q<Image>("card-image");
            if (cardImage != null)
                cardImage.image = LoadImage("Contemporary");

            tabContentContainer.Add(postCard);
        }
    }

    private void ShowEmptyState(string message)
    {
        tabContentContainer.Clear();
        
        var emptyState = new VisualElement();
        emptyState.AddToClassList("empty-state");

        var emptyText = new TextElement { text = message };
        emptyText.AddToClassList("empty-state-text");

        emptyState.Add(emptyText);
        tabContentContainer.Add(emptyState);
    }

    private Texture2D LoadImage(string imageName)
    {
        Texture2D texture = Resources.Load<Texture2D>(imageName);
        if (texture == null)
            texture = Resources.Load<Texture2D>($"Images/{imageName}");

        if (texture == null)
            Debug.LogWarning($"Could not load image: {imageName}");

        return texture;
    }

    private void OnDisable()
    {
        if (designsTab != null) designsTab.clicked -= ShowDesignsTab;
        if (savedTab != null) savedTab.clicked -= ShowSavedTab;
        if (aboutTab != null) aboutTab.clicked -= ShowAboutTab;
    }

    // Public method to manually refresh profile data
    public void RefreshProfile()
    {
        hasTriedToPopulate = false;
        StartCoroutine(WaitForProfileData());
    }
}