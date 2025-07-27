using Services;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Reflection;

public class ProfileScreenController : MonoBehaviour
{
    [Header("Card Templates")] public VisualTreeAsset postCardTemplate;
    [Header("UI Toolkit")] public UIDocument uiDocument;

    private VisualElement root;
    private ScrollView scrollView;
    private VisualElement tabContentContainer;
    private VisualElement aboutContent;
    
    // Elements that exist in UXML
    private TextElement userName;
    private TextElement profileTag;
    private TextElement creatorTagLine;
    private TextElement followingCount;
    private TextElement followerCount;
    private TextElement postCount;
    private Image profilePic;
    private Image settingsIcon;

    // Tab buttons
    private Button designsTab;
    private Button savedTab;
    private Button aboutTab;

    private VisualElement designsTabButton;
    private VisualElement savedTabButton;
    private VisualElement aboutTabButton;

    // About section elements
    private TextElement aboutDescription;
    private TextElement totalExperience;
    private TextElement contactLinks;

    private bool hasTriedToPopulate = false;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        // Get elements that actually exist in UXML
        scrollView = root.Q<ScrollView>("scroll-container");
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        // Get UI elements from UXML
        userName = root.Q<TextElement>("userName");
        profileTag = root.Q<TextElement>("profileTag");
        creatorTagLine = root.Q<TextElement>("profileTag"); // This is the tagline element
        followingCount = root.Q<TextElement>("followingCount");
        followerCount = root.Q<TextElement>("followerCount");
        postCount = root.Q<TextElement>("postCount");
        profilePic = root.Q<Image>("profilePic");
        settingsIcon = root.Q<Image>("settings");

        // Get tab buttons
        designsTab = root.Q<Button>("designsTab");
        savedTab = root.Q<Button>("savedTab");
        aboutTab = root.Q<Button>("aboutTab");

        // Get tab button containers
        designsTabButton = designsTab?.parent;
        savedTabButton = savedTab?.parent;
        aboutTabButton = aboutTab?.parent;

        // Get about content
        aboutContent = root.Q<VisualElement>("aboutContent");
        
        // Get about section elements
        aboutDescription = aboutContent?.Q<TextElement>("about-description");
        totalExperience = aboutContent?.Q<TextElement>("total-experience");
        contactLinks = aboutContent?.Q<TextElement>("contact-links");

        // Create tab content container
        tabContentContainer = new VisualElement();
        tabContentContainer.name = "tabContentContainer";
        tabContentContainer.AddToClassList("tab-content");
        scrollView.Add(tabContentContainer);

        // Set up tab button events
        if (designsTab != null) designsTab.clicked += ShowDesignsTab;
        if (savedTab != null) savedTab.clicked += ShowSavedTab;
        if (aboutTab != null) aboutTab.clicked += ShowAboutTab;

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
        Debug.Log($"[ProfileScreenController] User role: {profile.role}");

        // Populate basic info using elements that exist in UXML
        if (userName != null)
        {
            string fullName = $"{profile.firstName} {profile.lastName}".Trim();
            userName.text = string.IsNullOrEmpty(fullName) ? profile.userName : fullName;
            Debug.Log($"[ProfileScreenController] Set user name: {userName.text}");
        }

        // Handle role-based UI updates
        HandleRoleBasedUI(profile);

        // Update stats
        if (followingCount != null)
        {
            followingCount.text = profile.followingCount.ToString();
            Debug.Log($"[ProfileScreenController] Set following count: {followingCount.text}");
        }

        if (followerCount != null)
        {
            followerCount.text = profile.followerCount.ToString();
            Debug.Log($"[ProfileScreenController] Set follower count: {followerCount.text}");
        }

        if (postCount != null)
        {
            postCount.text = profile.postCount.ToString();
            Debug.Log($"[ProfileScreenController] Set post count: {postCount.text}");
        }

        // Update about section based on role
        UpdateAboutSection(profile);

        // Load profile picture if available
        LoadProfilePicture(profile);

        ShowDesignsTab(); // default tab
    }

    private void HandleRoleBasedUI(object profile)
    {
        // Get profile properties using reflection or casting
        var profileType = profile.GetType();
        var roleProperty = profileType.GetProperty("role");
        var creatorTypeProperty = profileType.GetProperty("creatorType");
        var taglineProperty = profileType.GetProperty("tagline");
        
        if (roleProperty == null) return;
        
        string role = roleProperty.GetValue(profile) as string;
        
        if (role == "CREATOR")
        {
            // CREATOR role - show creator-specific information
            Debug.Log("[ProfileScreenController] Setting up UI for CREATOR role");
            
            // Update profile tag with creator type
            if (profileTag != null)
            {
                string creatorType = creatorTypeProperty?.GetValue(profile) as string;
                profileTag.text = !string.IsNullOrEmpty(creatorType) ? creatorType : "Interior Designer";
                Debug.Log($"[ProfileScreenController] Set creator type: {profileTag.text}");
            }

            // Update creator tagline - need to target the second profileTag element with different class
            var taglineElements = root.Query<TextElement>("profileTag").ToList();
            if (taglineElements.Count > 1)
            {
                var taglineElement = taglineElements[1]; // Second element should be the tagline
                string tagline = taglineProperty?.GetValue(profile) as string;
                taglineElement.text = !string.IsNullOrEmpty(tagline) ? 
                    tagline : "Creating beautiful spaces that inspire and delight";
                Debug.Log($"[ProfileScreenController] Set creator tagline: {taglineElement.text}");
            }
        }
        else // USER role
        {
            // USER role - show user-specific information
            Debug.Log("[ProfileScreenController] Setting up UI for USER role");
            
            // Update profile tag for user
            if (profileTag != null)
            {
                profileTag.text = "Design Enthusiast";
                Debug.Log($"[ProfileScreenController] Set user type: {profileTag.text}");
            }

            // Update tagline for user
            var taglineElements = root.Query<TextElement>("profileTag").ToList();
            if (taglineElements.Count > 1)
            {
                var taglineElement = taglineElements[1]; // Second element should be the tagline
                taglineElement.text = "Exploring design inspirations and beautiful spaces";
                Debug.Log($"[ProfileScreenController] Set user tagline: {taglineElement.text}");
            }
        }
    }

    private void UpdateAboutSection(object profile)
    {
        // Get profile properties using reflection
        var profileType = profile.GetType();
        var roleProperty = profileType.GetProperty("role");
        var taglineProperty = profileType.GetProperty("tagline");
        var websiteProperty = profileType.GetProperty("website");
        
        if (roleProperty == null) return;
        
        string role = roleProperty.GetValue(profile) as string;
        
        if (role == "CREATOR")
        {
            // CREATOR-specific about section
            Debug.Log("[ProfileScreenController] Updating about section for CREATOR");
            
            // Update about description with creator-specific info
            if (aboutDescription != null)
            {
                string creatorAbout = "Interior designer with over 5 years of experience creating beautiful, functional spaces.";
                string tagline = taglineProperty?.GetValue(profile) as string;
                if (!string.IsNullOrEmpty(tagline))
                {
                    creatorAbout = tagline;
                }
                aboutDescription.text = creatorAbout;
                Debug.Log($"[ProfileScreenController] Set creator about: {aboutDescription.text}");
            }

            // Update experience for creators
            if (totalExperience != null)
            {
                totalExperience.text = "5+ years"; // You can add experience field to profile data
                Debug.Log($"[ProfileScreenController] Set creator experience: {totalExperience.text}");
            }

            // Update contact/website for creators
            if (contactLinks != null)
            {
                string website = websiteProperty?.GetValue(profile) as string;
                if (!string.IsNullOrEmpty(website))
                {
                    contactLinks.text = website;
                }
                else
                {
                    contactLinks.text = "www.example.com"; // Default for creators
                }
                Debug.Log($"[ProfileScreenController] Set creator contact: {contactLinks.text}");
            }
        }
        else // USER role
        {
            // USER-specific about section
            Debug.Log("[ProfileScreenController] Updating about section for USER");
            
            // Update about description for users
            if (aboutDescription != null)
            {
                aboutDescription.text = "Design enthusiast exploring beautiful spaces and gathering inspiration for future projects.";
                Debug.Log($"[ProfileScreenController] Set user about: {aboutDescription.text}");
            }

            // Update experience for users (show something different)
            if (totalExperience != null)
            {
                totalExperience.text = "Design Explorer";
                Debug.Log($"[ProfileScreenController] Set user experience: {totalExperience.text}");
            }

            // Update contact for users (might be different or hidden)
            if (contactLinks != null)
            {
                string website = websiteProperty?.GetValue(profile) as string;
                if (!string.IsNullOrEmpty(website))
                {
                    contactLinks.text = website;
                }
                else
                {
                    contactLinks.text = "Contact via app"; // Different default for users
                }
                Debug.Log($"[ProfileScreenController] Set user contact: {contactLinks.text}");
            }
        }

        // You could also show/hide additional profile information based on role
        // For example, show portfolio links only for creators, budget info only for users
        ShowRoleSpecificInformation(profile);
    }

    private void ShowRoleSpecificInformation(object profile)
    {
        // Get profile properties using reflection
        var profileType = profile.GetType();
        var roleProperty = profileType.GetProperty("role");
        
        if (roleProperty == null) return;
        
        string role = roleProperty.GetValue(profile) as string;
        
        if (role == "CREATOR")
        {
            // Show creator-specific information
            Debug.Log("[ProfileScreenController] Showing creator-specific information");
            
            // You could log or display additional creator info like:
            // - Portfolio links
            // - Service areas
            // - Specializations
            // - Years of experience
            // - Awards/certifications
            
            var regionProperty = profileType.GetProperty("region");
            if (regionProperty != null)
            {
                var region = regionProperty.GetValue(profile);
                if (region != null)
                {
                    Debug.Log($"[ProfileScreenController] Creator has region data");
                }
            }
            
            var socialsProperty = profileType.GetProperty("socials");
            if (socialsProperty != null)
            {
                var socials = socialsProperty.GetValue(profile);
                if (socials != null)
                {
                    Debug.Log($"[ProfileScreenController] Creator has social media data");
                }
            }
        }
        else // USER role
        {
            // Show user-specific information
            Debug.Log("[ProfileScreenController] Showing user-specific information");
            
            // You could log or display additional user info like:
            // - Budget range
            // - Design preferences
            // - Home type
            // - Project timeline
            
            var minBudgetProperty = profileType.GetProperty("minBudget");
            var maxBudgetProperty = profileType.GetProperty("maxBudget");
            if (minBudgetProperty != null && maxBudgetProperty != null)
            {
                var minBudget = minBudgetProperty.GetValue(profile);
                var maxBudget = maxBudgetProperty.GetValue(profile);
                if (minBudget != null && maxBudget != null)
                {
                    Debug.Log($"[ProfileScreenController] User has budget data");
                }
            }
            
            var colorSchemeProperty = profileType.GetProperty("colorScheme");
            if (colorSchemeProperty != null)
            {
                var colorScheme = colorSchemeProperty.GetValue(profile);
                if (colorScheme != null)
                {
                    Debug.Log($"[ProfileScreenController] User has color preferences");
                }
            }
            
            var designInspirationsProperty = profileType.GetProperty("designInspirations");
            if (designInspirationsProperty != null)
            {
                var designInspirations = designInspirationsProperty.GetValue(profile);
                if (designInspirations != null)
                {
                    Debug.Log($"[ProfileScreenController] User has design inspirations");
                }
            }
        }
    }

    private void LoadProfilePicture(object profile)
    {
        if (profilePic != null)
        {
            // Try to load profile picture
            // You might have a profile picture URL in your profile data
            Texture2D defaultProfilePic = LoadImage("person");
            if (defaultProfilePic != null)
            {
                profilePic.image = defaultProfilePic;
                Debug.Log("[ProfileScreenController] Set profile picture");
            }
        }
    }

    private void ShowDesignsTab()
    {
        RemoveSelectedFromAllTabs();
        designsTabButton?.AddToClassList("selected");

        if (aboutContent != null)
            aboutContent.style.display = DisplayStyle.None;

        tabContentContainer.style.display = DisplayStyle.Flex;
        tabContentContainer.Clear();

        LoadDesignCards();
    }

    private void ShowSavedTab()
    {
        RemoveSelectedFromAllTabs();
        savedTabButton?.AddToClassList("selected");

        if (aboutContent != null)
            aboutContent.style.display = DisplayStyle.None;

        tabContentContainer.style.display = DisplayStyle.Flex;
        tabContentContainer.Clear();

        LoadSavedCards();
    }

    private void ShowAboutTab()
    {
        RemoveSelectedFromAllTabs();
        aboutTabButton?.AddToClassList("selected");

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

        var profile = ProfileDataHandlers.Instance.ProfileData;
        string displayName = profile?.userName ?? "Krishna Yadav";

        for (int i = 0; i < 5; i++)
        {
            VisualElement postCard = postCardTemplate.CloneTree();

            var cardUserName = postCard.Q<TextElement>("userName");
            if (cardUserName != null)
                cardUserName.text = displayName;

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

        var profile = ProfileDataHandlers.Instance.ProfileData;
        string displayName = profile?.userName ?? "Krishna Yadav";

        for (int i = 0; i < 3; i++)
        {
            VisualElement postCard = postCardTemplate.CloneTree();

            var cardUserName = postCard.Q<TextElement>("userName");
            if (cardUserName != null)
                cardUserName.text = displayName;

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