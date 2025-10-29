using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class CreatorProfileScreenController : MonoBehaviour
{
    [Header("UI Toolkit")]
    public UIDocument uiDocument;

    private VisualElement root;
    private ScrollView scrollView;

    // Header & stats
    private TextElement creatorUserName;        // name="creator-userName"
    private TextElement creatorTypeText;        // class="creatorTitle"
    private TextElement creatorTagLineText;     // class="creator-tag-line"
    private TextElement followingCount;         // name="creator-followingCount"
    private TextElement followerCount;          // name="creator-followerCount"
    private TextElement postCount;              // name="creator-postCount"
    private Image profilePic;                   // name="creator-profilePic"
    private Image settingsIcon;

    // Tabs
    private Button designsTab;   // name="creator-designsTab"
    private Button savedTab;     // name="creator-savedTab"
    private Button aboutTab;     // name="creator-aboutTab"
    private VisualElement designsTabButton;
    private VisualElement savedTabButton;
    private VisualElement aboutTabButton;

    // Containers
    private VisualElement aboutContent;         // name="creator-aboutContent"
    private VisualElement tabContentContainer;  // kept empty until you plug cached lists

    // About sub-cards
    private VisualElement aboutCard;            // name="creator-aboutCard"
    private VisualElement experienceCard;       // name="creator-experienceCard"
    private VisualElement contactCard;          // name="creator-contactCard"

    // About text by class
    private TextElement aboutDescription;       // class="about-description"
    private TextElement totalExperience;        // class="total-experience"
    private TextElement contactLinks;           // class="contact-links"

    private bool populated;

    void OnEnable()
    {
        root = uiDocument ? uiDocument.rootVisualElement : GetComponent<UIDocument>().rootVisualElement;

        scrollView = root.Q<ScrollView>("creator-scroll-container");
        if (scrollView != null)
        {
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        }

        // Header & stats
        creatorUserName    = root.Q<TextElement>("creator-userName");
        creatorTypeText    = root.Query<TextElement>(className: "creatorTitle").First();
        creatorTagLineText = root.Query<TextElement>(className: "creator-tag-line").First();
        followingCount     = root.Q<TextElement>("creator-followingCount");
        followerCount      = root.Q<TextElement>("creator-followerCount");
        postCount          = root.Q<TextElement>("creator-postCount");
        profilePic         = root.Q<Image>("creator-profilePic");
        settingsIcon       = root.Q<Image>("creator-settings");

        // Tabs
        designsTab         = root.Q<Button>("creator-designsTab");
        savedTab           = root.Q<Button>("creator-savedTab");
        aboutTab           = root.Q<Button>("creator-aboutTab");
        designsTabButton   = designsTab?.parent;
        savedTabButton     = savedTab?.parent;
        aboutTabButton     = aboutTab?.parent;

        // About
        aboutContent       = root.Q<VisualElement>("creator-aboutContent");
        aboutCard          = aboutContent?.Q<VisualElement>("creator-aboutCard");
        experienceCard     = aboutContent?.Q<VisualElement>("creator-experienceCard");
        contactCard        = aboutContent?.Q<VisualElement>("creator-contactCard");
        aboutDescription   = aboutContent?.Query<TextElement>(className: "about-description").First();
        totalExperience    = aboutContent?.Query<TextElement>(className: "total-experience").First();
        contactLinks       = aboutContent?.Query<TextElement>(className: "contact-links").First();

        // Tab content holder
        tabContentContainer = new VisualElement { name = "tabContentContainer" };
        tabContentContainer.AddToClassList("tab-content");
        scrollView?.Add(tabContentContainer);
        
        // if (settingsIcon != null)
        // {
        //     Debug.Log("Settings icon found in CreatorProfileScreen.");
        //     settingsIcon.RegisterCallback<ClickEvent>(evt => OnSettingsIconClicked());
        // }
        // else
        // {
        //     Debug.Log("Warning: settingsIcon not found in CreatorProfileScreen.");
        // }
        
        settingsIcon?.RegisterCallback<ClickEvent>(evt => OnSettingsIconClicked());

        if (designsTab != null) designsTab.clicked += ShowDesignsTab;
        if (savedTab   != null) savedTab.clicked   += ShowSavedTab;
        if (aboutTab   != null) aboutTab.clicked   += ShowAboutTab;

        StartCoroutine(ShowNavigationAfterDelay());
        StartCoroutine(WaitAndPopulate());
    }

    private IEnumerator ShowNavigationAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        
        Debug.Log("Showing navigation bar for Profile screen");
        
        NavigationManager.ToggleNavigationBar(true);
        NavigationManager.UpdateSelectedIcon(NavScreen.Profile);
        
        yield return new WaitForSeconds(0.1f);
        Debug.Log($"Navigation bar visible: {NavigationManager.IsNavigationBarVisible()}");
    }
    
    private void OnSettingsIconClicked()
    {
        Debug.Log("Settings icon clicked - navigating to Profile Settings screen.");
        UIManager.Instance.TransitionScreens(UIScreenType.Profile, UIScreenType.ProfileSetting);
    }

    private IEnumerator WaitAndPopulate()
    {
        while (ProfileDataHandlers.Instance == null)
            yield return null;

        float t = 0f, timeout = 3f;
        while (ProfileDataHandlers.Instance.ProfileData == null && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        TryPopulate();
        ShowDesignsTab(); // default: switch only
    }

    private void TryPopulate()
    {
        if (populated) return;

        ProfileCache p = ProfileDataHandlers.Instance.ProfileData ?? ProfileDataHandlers.Instance.LoadProfileCache();
        if (p == null) return;

        // Only proceed if this screen is for CREATOR
        if (!string.Equals(p.role, "CREATOR", StringComparison.OrdinalIgnoreCase))
        {
            populated = true;
            return;
        }

        // Name
        if (creatorUserName != null)
        {
            string full = $"{p.firstName} {p.lastName}".Trim();
            if (!string.IsNullOrEmpty(full)) creatorUserName.text = full;
            else if (!string.IsNullOrEmpty(p.userName)) creatorUserName.text = p.userName;
        }

        // Creator type (e.g., Interior Designer / Design Studio / Contractor)
        if (creatorTypeText != null && !string.IsNullOrEmpty(p.creatorType))
            creatorTypeText.text = p.creatorType;

        // Tagline
        if (creatorTagLineText != null)
        {
            if (!string.IsNullOrEmpty(p.tagline)) creatorTagLineText.text = p.tagline;
            else creatorTagLineText.style.display = DisplayStyle.None;
        }

        // Stats
        if (followingCount != null && p.followingCount > 0) followingCount.text = p.followingCount.ToString();
        if (followerCount  != null && p.followerCount  > 0) followerCount.text  = p.followerCount.ToString();
        if (postCount      != null && p.postCount      > 0) postCount.text      = p.postCount.ToString();

        // Avatar
        if (!string.IsNullOrEmpty(p.avatar) && profilePic != null)
            StartCoroutine(LoadTextureFromUrl(p.avatar, tex => { if (tex != null) profilePic.image = tex; }));

        // ABOUT
        if (aboutDescription != null)
        {
            // Prefer explicit bio; if missing, hide this card (we already show tagline above)
            if (!string.IsNullOrEmpty(p.bio)) aboutDescription.text = p.bio;
            else if (aboutCard != null) aboutCard.style.display = DisplayStyle.None;
        }

        if (totalExperience != null)
        {
            if (p.yearsOfExperience > 0)
                totalExperience.text = p.yearsOfExperience == 1 ? "1 year" : $"{p.yearsOfExperience} years";
            else if (experienceCard != null) experienceCard.style.display = DisplayStyle.None;
        }

        if (contactLinks != null)
        {
            if (!string.IsNullOrEmpty(p.website)) contactLinks.text = p.website;
            else if (contactCard != null) contactCard.style.display = DisplayStyle.None;
        }

        populated = true;
    }

    private void ShowDesignsTab()
    {
        SetSelected(designsTabButton, true);
        SetSelected(savedTabButton, false);
        SetSelected(aboutTabButton, false);

        if (aboutContent != null) aboutContent.style.display = DisplayStyle.None;

        if (tabContentContainer != null)
        {
            tabContentContainer.style.display = DisplayStyle.Flex;
            tabContentContainer.Clear();
            // Intentionally empty: later, populate strictly from a cached portfolio list.
        }
    }

    private void ShowSavedTab()
    {
        SetSelected(designsTabButton, false);
        SetSelected(savedTabButton, true);
        SetSelected(aboutTabButton, false);

        if (aboutContent != null) aboutContent.style.display = DisplayStyle.None;

        if (tabContentContainer != null)
        {
            tabContentContainer.style.display = DisplayStyle.Flex;
            tabContentContainer.Clear();
            // Intentionally empty: later, populate strictly from a cached saved list.
        }
    }

    private void ShowAboutTab()
    {
        SetSelected(designsTabButton, false);
        SetSelected(savedTabButton, false);
        SetSelected(aboutTabButton, true);

        if (tabContentContainer != null)
        {
            tabContentContainer.style.display = DisplayStyle.None;
            tabContentContainer.Clear();
        }
        if (aboutContent != null) aboutContent.style.display = DisplayStyle.Flex;
    }

    private static void SetSelected(VisualElement ve, bool on)
    {
        if (ve == null) return;
        if (on) ve.AddToClassList("selected");
        else ve.RemoveFromClassList("selected");
    }

    private IEnumerator LoadTextureFromUrl(string url, Action<Texture2D> onDone)
    {
        using (var req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                onDone?.Invoke(DownloadHandlerTexture.GetContent(req));
            else
                onDone?.Invoke(null);
        }
    }

    void OnDisable()
    {
        if (designsTab != null) designsTab.clicked -= ShowDesignsTab;
        if (savedTab   != null) savedTab.clicked   -= ShowSavedTab;
        if (aboutTab   != null) aboutTab.clicked   -= ShowAboutTab;
        settingsIcon?.UnregisterCallback<ClickEvent>(evt => OnSettingsIconClicked());
    }
}
