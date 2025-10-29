using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class UserProfileScreenController : MonoBehaviour
{
    [Header("UI Toolkit")]
    public UIDocument uiDocument;

    private VisualElement root;
    private ScrollView scrollView;

    // Header & stats
    private TextElement userName;
    private TextElement followingCount;
    private TextElement followerCount;
    private TextElement postCount;
    private Image profilePic;

    // Tabs
    private Button savedTab;
    private Button aboutTab;
    private VisualElement savedTabButton;
    private VisualElement aboutTabButton;

    // Containers
    private VisualElement aboutContent;         // name="aboutContent"
    private VisualElement tabContentContainer;  // holder for Saved tab content (kept empty unless you later plug a cache)

    // About sub-cards
    private VisualElement aboutCard;            // name="aboutCard"
    private VisualElement experienceCard;       // name="experienceCard"
    private VisualElement contactCard;          // name="contactCard"

    // About text elements (queried by class, since no 'name' in UXML)
    private TextElement aboutDescription;       // class="about-description"
    private TextElement totalExperience;        // class="total-experience"
    private TextElement contactLinks;           // class="contact-links"

    private bool populated;

    void OnEnable()
    {
        Debug.Log("Himanshu Kumar Mahto");
        root = uiDocument ? uiDocument.rootVisualElement : GetComponent<UIDocument>().rootVisualElement;

        scrollView = root.Q<ScrollView>("scroll-container");
        if (scrollView != null)
        {
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        }

        // Header & stats
        userName       = root.Q<TextElement>("userName");
        followingCount = root.Q<TextElement>("followingCount");
        followerCount  = root.Q<TextElement>("followerCount");
        postCount      = root.Q<TextElement>("postCount");
        profilePic     = root.Q<Image>("profilePic");

        // Tabs
        savedTab       = root.Q<Button>("savedTab");
        aboutTab       = root.Q<Button>("aboutTab");
        savedTabButton = savedTab?.parent;
        aboutTabButton = aboutTab?.parent;

        // About
        aboutContent   = root.Q<VisualElement>("aboutContent");
        aboutCard      = aboutContent?.Q<VisualElement>("aboutCard");
        experienceCard = aboutContent?.Q<VisualElement>("experienceCard");
        contactCard    = aboutContent?.Q<VisualElement>("contactCard");

        aboutDescription = aboutContent?.Query<TextElement>(className: "about-description").First();
        totalExperience  = aboutContent?.Query<TextElement>(className: "total-experience").First();
        contactLinks     = aboutContent?.Query<TextElement>(className: "contact-links").First();

        // Tab content holder (kept empty until you decide to show cached saved posts)
        tabContentContainer = new VisualElement { name = "tabContentContainer" };
        tabContentContainer.AddToClassList("tab-content");
        scrollView?.Add(tabContentContainer);

        if (savedTab != null) savedTab.clicked += ShowSavedTab;
        if (aboutTab != null) aboutTab.clicked += ShowAboutTab;

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

    private IEnumerator WaitAndPopulate()
    {
        // Wait for handler
        while (ProfileDataHandlers.Instance == null)
            yield return null;

        // Wait a short moment for cache to be present (we won’t invent placeholders)
        float t = 0f, timeout = 3f;
        while (ProfileDataHandlers.Instance.ProfileData == null && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        TryPopulate();
        ShowSavedTab(); // default: switch tabs only (no runtime cards)
    }

    private void TryPopulate()
    {
        if (populated) return;

        ProfileCache p = ProfileDataHandlers.Instance.ProfileData ?? ProfileDataHandlers.Instance.LoadProfileCache();
        if (p == null) return; // nothing to do, leave UXML defaults

        // Only proceed if this screen is for USER
        if (!string.Equals(p.role, "USER", StringComparison.OrdinalIgnoreCase))
        {
            // Not the matching role; don’t touch the UI
            populated = true;
            return;
        }

        // Name
        if (userName != null)
        {
            string full = $"{p.firstName} {p.lastName}".Trim();
            if (!string.IsNullOrEmpty(full)) userName.text = full;
            else if (!string.IsNullOrEmpty(p.userName)) userName.text = p.userName;
        }

        // Stats (assign only if values are present; otherwise leave UXML)
        if (followingCount != null && p.followingCount > 0) followingCount.text = p.followingCount.ToString();
        if (followerCount  != null && p.followerCount  > 0) followerCount.text  = p.followerCount.ToString();
        if (postCount      != null && p.postCount      > 0) postCount.text      = p.postCount.ToString();

        // Avatar (optional: uses cached URL only)
        if (!string.IsNullOrEmpty(p.avatar) && profilePic != null)
            StartCoroutine(LoadTextureFromUrl(p.avatar, tex => { if (tex != null) profilePic.image = tex; }));

        // ABOUT
        if (aboutDescription != null)
        {
            if (!string.IsNullOrEmpty(p.bio)) aboutDescription.text = p.bio;
            else if (aboutCard != null) aboutCard.style.display = DisplayStyle.None;
        }

        if (totalExperience != null)
        {
            // Users usually don’t have years; hide if not present
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

    private void ShowSavedTab()
    {
        savedTabButton?.AddToClassList("selected");
        aboutTabButton?.RemoveFromClassList("selected");

        if (aboutContent != null) aboutContent.style.display = DisplayStyle.None;

        if (tabContentContainer != null)
        {
            tabContentContainer.style.display = DisplayStyle.Flex;
            tabContentContainer.Clear();
            // Intentionally empty: populate here later from a real cached "bookmarks" list if you add one.
        }
    }

    private void ShowAboutTab()
    {
        savedTabButton?.RemoveFromClassList("selected");
        aboutTabButton?.AddToClassList("selected");

        if (tabContentContainer != null)
        {
            tabContentContainer.style.display = DisplayStyle.None;
            tabContentContainer.Clear();
        }
        if (aboutContent != null) aboutContent.style.display = DisplayStyle.Flex;
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
        if (savedTab != null) savedTab.clicked -= ShowSavedTab;
        if (aboutTab != null) aboutTab.clicked -= ShowAboutTab;
    }
}
