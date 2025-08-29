using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;

public static class ExploreHeaderUIBuilder
{
    public delegate void SearchClickedDelegate();
    public static event SearchClickedDelegate OnSearchClicked;
    
    
    private static string baseURL;
    private static string authToken;
    private static VisualElement tagsContainer;
    
    public delegate void TagSelectedDelegate(string tagName, int tagId);
    public static event TagSelectedDelegate OnTagSelected;
    
    private static VisualElement currentlyLoadingTag;
    private static bool isLoadingData = false;
    private static bool isInitialLoad = true;

    // Track the currently selected tag (visual element)
    private static VisualElement currentlySelectedTag;

    public static VisualElement CreateHeaderSection(Action onFilterClicked, TagSelectedDelegate onTagSelected = null, SearchClickedDelegate onSearchClicked = null)
    {
        baseURL = baseScript.baseURL;
        authToken = AuthTokenManager.GetToken();

        // Store the callback for use in tag clicks
        OnTagSelected = onTagSelected;
        OnSearchClicked = onSearchClicked;

        VisualElement headerSection = new VisualElement();
        headerSection.name = "headerSection";
        headerSection.AddToClassList("headerSection");
        headerSection.style.marginTop = 120;
        headerSection.style.height = 250;

        // Create search section
        VisualElement searchSection = CreateSearchSection(onFilterClicked);

        // Create tag section
        VisualElement tagSection = CreateTagSection();

        headerSection.Add(searchSection);
        headerSection.Add(tagSection);

        // Fetch and populate trending tags via PostDataGetter (API moved out)
        CoroutineRunner.Instance.StartRoutine(
            PostDataGetter.FetchWeeklyTrendingTags(
                baseURL,
                authToken,
                onSuccess: tags => UpdateTagsWithData(tags),
                onError: _ => UpdateTagsWithError()
            )
        );

        return headerSection;
    }

    private static VisualElement CreateSearchSection(Action onFilterClicked)
    {
        VisualElement searchSection = new VisualElement();
        searchSection.AddToClassList("searchSection");
        searchSection.style.flexDirection = FlexDirection.Row;
        searchSection.style.alignItems = Align.Center;

        // Search wrapper and field
        VisualElement searchWrapper = CreateSearchWrapper();

        // Filter wrapper and button
        VisualElement filterWrapper = CreateFilterWrapper(onFilterClicked);

        searchSection.Add(searchWrapper);
        searchSection.Add(filterWrapper);

        return searchSection;
    }

    private static VisualElement CreateSearchWrapper()
    {
        VisualElement searchWrapper = new VisualElement();
        searchWrapper.AddToClassList("search-wrapper");
        searchWrapper.style.paddingLeft = 16;
        searchWrapper.style.paddingRight = 16;
        searchWrapper.style.paddingTop = 16;
        searchWrapper.style.paddingBottom = 16;
        searchWrapper.style.width = Length.Percent(80);

        TextField searchField = CreateSearchField();
        searchWrapper.Add(searchField);

        return searchWrapper;
    }

    private static TextField CreateSearchField()
    {
        TextField searchField = new TextField();
        searchField.name = "searchField";
        searchField.AddToClassList("search-field");
        searchField.value = "";
        searchField.SetValueWithoutNotify("");
        searchField.isReadOnly = true; // Make it read-only since it's just a button now

        // Styling
        searchField.style.width = Length.Percent(100);
        searchField.style.height = 100;
        searchField.style.borderBottomWidth = 2;
        searchField.style.borderTopWidth = 2;
        searchField.style.borderLeftWidth = 2;
        searchField.style.borderRightWidth = 2;
        searchField.style.marginLeft = Length.Percent(7);
        searchField.style.paddingLeft = 40;
        searchField.style.borderBottomColor = Color.black;
        searchField.style.borderTopColor = Color.black;
        searchField.style.borderLeftColor = Color.black;
        searchField.style.borderRightColor = Color.black;
        searchField.style.borderTopLeftRadius = 50;
        searchField.style.borderTopRightRadius = 50;
        searchField.style.borderBottomLeftRadius = 50;
        searchField.style.borderBottomRightRadius = 50;
        searchField.style.fontSize = 35;

        // Placeholder
        SetupSearchFieldPlaceholder(searchField);
    
        // Add click event to open search screen
        searchField.RegisterCallback<ClickEvent>(evt => OnSearchClicked?.Invoke());

        return searchField;
    }

    private static void SetupSearchFieldPlaceholder(TextField searchField)
    {
        string placeholderText = "Search for designers...";
        bool isPlaceholderActive = true;

        searchField.SetValueWithoutNotify(placeholderText);
        searchField.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);

        searchField.RegisterCallback<FocusInEvent>(evt =>
        {
            if (isPlaceholderActive)
            {
                searchField.SetValueWithoutNotify("");
                searchField.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                isPlaceholderActive = false;
            }
        });

        searchField.RegisterCallback<FocusOutEvent>(evt =>
        {
            if (string.IsNullOrEmpty(searchField.value.Trim()))
            {
                searchField.SetValueWithoutNotify(placeholderText);
                searchField.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                isPlaceholderActive = true;
            }
        });
    }

    private static VisualElement CreateFilterWrapper(Action onFilterClicked)
    {
        VisualElement filterWrapper = new VisualElement();
        filterWrapper.AddToClassList("filter-wrapper");
        filterWrapper.style.paddingLeft = 16;
        filterWrapper.style.paddingRight = 16;
        filterWrapper.style.paddingTop = 16;
        filterWrapper.style.paddingBottom = 16;
        filterWrapper.style.width = Length.Percent(30);

        Button filterButton = CreateFilterButton(onFilterClicked);
        filterWrapper.Add(filterButton);

        return filterWrapper;
    }

    private static Button CreateFilterButton(Action onFilterClicked)
    {
        Button filterButton = new Button();
        filterButton.name = "filterButton";
        filterButton.AddToClassList("filter-button");
        filterButton.style.width = Length.Percent(42.5f);
        filterButton.style.height = 100;
        filterButton.style.borderBottomWidth = 2;
        filterButton.style.borderTopWidth = 2;
        filterButton.style.borderLeftWidth = 2;
        filterButton.style.borderRightWidth = 2;
        filterButton.style.marginLeft = Length.Percent(35);
        filterButton.style.borderBottomColor = Color.black;
        filterButton.style.borderTopColor = Color.black;
        filterButton.style.borderLeftColor = Color.black;
        filterButton.style.borderRightColor = Color.black;
        filterButton.style.borderTopLeftRadius = 50;
        filterButton.style.borderTopRightRadius = 50;
        filterButton.style.borderBottomLeftRadius = 50;
        filterButton.style.borderBottomRightRadius = 50;
        filterButton.style.backgroundColor = Color.clear;
        filterButton.style.flexDirection = FlexDirection.Row;
        filterButton.style.alignItems = Align.Center;
        filterButton.style.justifyContent = Justify.Center;

        // Icon
        Texture2D filterIconTexture = Resources.Load<Texture2D>("filter-Icon");
        if (filterIconTexture != null)
        {
            Image filterIcon = new Image();
            filterIcon.name = "filterIcon";
            filterIcon.AddToClassList("filter-icon");
            filterIcon.style.width = 55;
            filterIcon.style.height = 55;
            filterIcon.image = filterIconTexture;
            filterButton.Add(filterIcon);
        }
        else
        {
            Label filterText = new Label("⚙");
            filterText.style.fontSize = 30;
            filterText.style.color = Color.black;
            filterButton.Add(filterText);
        }

        // Click
        filterButton.RegisterCallback<ClickEvent>(evt => onFilterClicked?.Invoke());
        return filterButton;
    }

    private static VisualElement CreateTagSection()
    {
        VisualElement tagSection = new VisualElement();
        tagSection.name = "tagSection";
        tagSection.AddToClassList("tag-section");
        tagSection.style.marginTop = 20;
        tagSection.style.marginLeft = Length.Percent(1);
        tagSection.style.marginRight = Length.Percent(1);

        // Horizontal scroll view
        ScrollView scrollView = new ScrollView();
        scrollView.name = "tagScrollView";
        scrollView.mode = ScrollViewMode.Horizontal;
        scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        scrollView.style.width = Length.Percent(100);
        scrollView.style.height = 70;

        // Container for tags - store reference for dynamic updates
        tagsContainer = new VisualElement();
        tagsContainer.name = "tagsContainer";
        tagsContainer.style.flexDirection = FlexDirection.Row;
        tagsContainer.style.alignItems = Align.Center;
        tagsContainer.style.height = 70;

        // Loading tag
        CreateLoadingTag();

        scrollView.Add(tagsContainer);
        tagSection.Add(scrollView);

        return tagSection;
    }

    private static void CreateLoadingTag()
    {
        VisualElement loadingTag = CreateDesignTag("Loading...");
        loadingTag.style.opacity = 0.5f;
        tagsContainer.Add(loadingTag);
    }

    private static VisualElement CreateDesignTag(string tagText, int? tagId = null)
    {
        VisualElement designTag = new VisualElement();
        designTag.name = "designTag";
        designTag.AddToClassList("designTag");

        if (tagId.HasValue)
        {
            designTag.userData = tagId.Value;

            // Click -> selection
            designTag.RegisterCallback<ClickEvent>(evt =>
            {
                OnTagClicked(tagId.Value, tagText, designTag);
            });

            // Hover (skip when selected)
            designTag.RegisterCallback<MouseEnterEvent>(evt =>
            {
                if (designTag != currentlySelectedTag)
                {
                    designTag.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f, 0.3f);
                }
            });
            designTag.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (designTag != currentlySelectedTag)
                {
                    designTag.style.backgroundColor = Color.clear;
                }
            });
        }

        // Base style
        designTag.style.width = StyleKeyword.Auto;
        designTag.style.minWidth = 120;
        designTag.style.height = 70;
        designTag.style.paddingLeft = 25;
        designTag.style.paddingRight = 25;
        designTag.style.borderBottomWidth = 2;
        designTag.style.borderTopWidth = 2;
        designTag.style.borderLeftWidth = 2;
        designTag.style.borderRightWidth = 2;
        var grey = new Color(129f / 255f, 129f / 255f, 129f / 255f, 0.84f);
        designTag.style.borderBottomColor = grey;
        designTag.style.borderTopColor = grey;
        designTag.style.borderLeftColor = grey;
        designTag.style.borderRightColor = grey;
        designTag.style.borderTopLeftRadius = 35;
        designTag.style.borderTopRightRadius = 35;
        designTag.style.borderBottomLeftRadius = 35;
        designTag.style.borderBottomRightRadius = 35;
        designTag.style.alignItems = Align.Center;
        designTag.style.justifyContent = Justify.Center;
        designTag.style.flexShrink = 0;

        Label tagLabel = new Label(tagText);
        tagLabel.name = "tagLabel";
        tagLabel.AddToClassList("tagLabel");
        tagLabel.style.fontSize = 35;
        tagLabel.style.color = grey; // default
        tagLabel.style.whiteSpace = WhiteSpace.NoWrap;
        tagLabel.style.width = StyleKeyword.Auto;
        tagLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        designTag.Add(tagLabel);
        return designTag;
    }

    private static void OnTagClicked(int tagId, string tagName, VisualElement clickedTag)
    {
        // Prevent clicking while another tag is loading
        if (isLoadingData)
        {
            Debug.Log("Another tag is still loading, ignoring click");
            return;
        }

        // Don't do anything if clicking the already selected tag
        if (clickedTag == currentlySelectedTag)
        {
            Debug.Log("Tag already selected, ignoring click");
            return;
        }

        Debug.Log($"Tag clicked: {tagName} (ID: {tagId})");

        // Set loading state (this only applies to manual clicks, not initial load)
        isLoadingData = true;
        currentlyLoadingTag = clickedTag;

        // Apply loading visual state to the clicked tag
        ApplyLoadingStyle(clickedTag);

        // Determine the API type parameter based on tag
        string apiType = GetAPITypeFromTag(tagName, tagId);
    
        // Call the API with the selected tag type
        OnTagSelected?.Invoke(apiType, tagId);
    }
    
    public static bool IsInitialLoad()
    {
        return isInitialLoad;
    }
    
    private static void ApplyLoadingStyle(VisualElement tag)
    {
        if (tag == null) return;
    
        var label = tag.Q<Label>("tagLabel");
        if (label != null)
        {
            // Make text slightly dimmer to show loading state
            label.style.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        }
    
        // Apply a subtle loading background
        tag.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 0.3f);
    
        // Optional: Add a subtle pulsing effect
        tag.style.opacity = 0.7f;
    }
    
    public static void OnDataLoadSuccess(int tagId)
    {
        if (!isLoadingData || currentlyLoadingTag == null) return;

        Debug.Log($"Data loaded successfully for tag ID: {tagId}");

        // Remove previous selection
        RemoveSelectedStyle(currentlySelectedTag);

        // Apply selected style to the loading tag
        ApplySelectedStyle(currentlyLoadingTag);

        // Update current selection
        currentlySelectedTag = currentlyLoadingTag;

        // Clear loading state
        ResetLoadingState();
    }
    
    public static void OnDataLoadError(int tagId)
    {
        if (!isLoadingData || currentlyLoadingTag == null) return;

        Debug.Log($"Data load failed for tag ID: {tagId}");

        // Remove loading style and revert to normal
        RemoveLoadingStyle(currentlyLoadingTag);

        // Clear loading state
        ResetLoadingState();

        // Optionally show error feedback (you could add a brief red border or something)
        ShowTagErrorFeedback(currentlyLoadingTag);
    }
    
    private static void RemoveLoadingStyle(VisualElement tag)
    {
        if (tag == null) return;
    
        var label = tag.Q<Label>("tagLabel");
        if (label != null)
        {
            var grey = new Color(129f / 255f, 129f / 255f, 129f / 255f, 0.84f);
            label.style.color = grey;
        }
    
        tag.style.backgroundColor = Color.clear;
        tag.style.opacity = 1f;
    }

    private static void ResetLoadingState()
    {
        isLoadingData = false;
        currentlyLoadingTag = null;
    }
    
    private static void ShowTagErrorFeedback(VisualElement tag)
    {
        if (tag == null) return;
    
        // Brief red border to indicate error
        tag.style.borderBottomColor = Color.red;
        tag.style.borderTopColor = Color.red;
        tag.style.borderLeftColor = Color.red;
        tag.style.borderRightColor = Color.red;
    
        // Revert back to normal after a short delay
        CoroutineRunner.Instance.StartRoutine(RevertErrorFeedback(tag));
    }
    
    private static IEnumerator RevertErrorFeedback(VisualElement tag)
    {
        yield return new WaitForSeconds(1.5f);
    
        if (tag != null)
        {
            var grey = new Color(129f / 255f, 129f / 255f, 129f / 255f, 0.84f);
            tag.style.borderBottomColor = grey;
            tag.style.borderTopColor = grey;
            tag.style.borderLeftColor = grey;
            tag.style.borderRightColor = grey;
        }
    }
    
    private static string GetAPITypeFromTag(string tagName, int tagId)
    {
        if (tagId == 0 || tagName.ToLower() == "trending")
        {
            return "trending";
        }
    
        return tagName.ToLower().Replace(" ", "-");
    }

    
    private static void UpdateTagsWithData(List<PostDataGetter.TagItem> tags)
    {
        tagsContainer.Clear();

        // Always add and preselect "Trending"
        VisualElement defaultTag = CreateDesignTag("Trending", 0);
        tagsContainer.Add(defaultTag);
    
        // For initial load, directly apply selected style without loading state
        if (isInitialLoad)
        {
            ApplySelectedStyle(defaultTag);
            currentlySelectedTag = defaultTag;
            isInitialLoad = false; // Mark that initial load is done
        }
        else
        {
            // For subsequent loads (like refresh), use loading state
            isLoadingData = true;
            currentlyLoadingTag = defaultTag;
            ApplyLoadingStyle(defaultTag);
        }

        // Add others from API
        if (tags != null)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                VisualElement designTag = CreateDesignTag(tags[i].name, tags[i].id);
                designTag.style.marginLeft = 15;
                tagsContainer.Add(designTag);
            }
        }

        // Automatically trigger trending API call
        OnTagSelected?.Invoke("trending", 0);
    }

    private static void UpdateTagsWithError()
    {
        tagsContainer.Clear();

        VisualElement errorTag = CreateDesignTag("Error loading tags");
        errorTag.style.opacity = 0.7f;
        tagsContainer.Add(errorTag);

        // Still show and preselect "Trending" so UI is usable
        VisualElement fallbackTrending = CreateDesignTag("Trending", 0);
        fallbackTrending.style.marginLeft = 15;
        tagsContainer.Add(fallbackTrending);
    
        // For initial load, directly apply selected style
        if (isInitialLoad)
        {
            ApplySelectedStyle(fallbackTrending);
            currentlySelectedTag = fallbackTrending;
            isInitialLoad = false;
        }
        else
        {
            // For subsequent loads, use loading state
            isLoadingData = true;
            currentlyLoadingTag = fallbackTrending;
            ApplyLoadingStyle(fallbackTrending);
        }

        // Automatically trigger trending API call
        OnTagSelected?.Invoke("trending", 0);
    }
    
    public static void ResetInitialLoadFlag()
    {
        isInitialLoad = true;
    }
    
    private static void ApplySelectedStyle(VisualElement tag)
    {
        if (tag == null) return;
        var label = tag.Q<Label>("tagLabel");
        if (label != null)
            label.style.color = HexToColor("#F5F0ED");
        tag.style.backgroundColor = HexToColor("#6B3629");
    }

    private static void RemoveSelectedStyle(VisualElement tag)
    {
        if (tag == null) return;
        var label = tag.Q<Label>("tagLabel");
        if (label != null)
        {
            var grey = new Color(129f / 255f, 129f / 255f, 129f / 255f, 0.84f);
            label.style.color = grey;
        }
        tag.style.backgroundColor = Color.clear;
    }

    private static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out var color))
            return color;
        return Color.white;
    }

    // Local coroutine runner (unchanged)
    private class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;
        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("CoroutineRunner");
                    _instance = obj.AddComponent<CoroutineRunner>();
                    UnityEngine.Object.DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        public void StartRoutine(IEnumerator routine)
        {
            StartCoroutine(routine);
        }
    }
}
