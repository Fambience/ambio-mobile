using UnityEngine;
using UnityEngine.UIElements;
using System;

public static class ExploreHeaderUIBuilder
{
    public static VisualElement CreateHeaderSection(Action onFilterClicked)
    {
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
        
        // Add placeholder functionality
        SetupSearchFieldPlaceholder(searchField);
        
        return searchField;
    }
    
    private static void SetupSearchFieldPlaceholder(TextField searchField)
    {
        string placeholderText = "Search for designers...";
        bool isPlaceholderActive = true;
        
        // Set initial placeholder
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
        
        // Add filter icon
        AddFilterIcon(filterButton);
        
        // Add click event
        filterButton.RegisterCallback<ClickEvent>(evt => onFilterClicked?.Invoke());
        
        return filterButton;
    }
    
    private static void AddFilterIcon(Button filterButton)
    {
        // Try to load the filter icon from Resources
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
            Debug.Log("Filter icon loaded successfully");
        }
        else
        {
            Debug.LogWarning("Filter icon not found in Resources. Please ensure 'filter-Icon' is in the Resources folder.");
            // Create a simple text placeholder if icon is not found
            Label filterText = new Label("⚙");
            filterText.style.fontSize = 30;
            filterText.style.color = Color.black;
            filterButton.Add(filterText);
        }
    }
    
    private static VisualElement CreateTagSection()
    {
        VisualElement tagSection = new VisualElement();
        tagSection.name = "tagSection";
        tagSection.AddToClassList("tag-section");
        tagSection.style.marginTop = 20;
        tagSection.style.marginLeft = Length.Percent(1);
        tagSection.style.marginRight = Length.Percent(1);
        
        // Create horizontal scroll view
        ScrollView scrollView = new ScrollView();
        scrollView.name = "tagScrollView";
        scrollView.mode = ScrollViewMode.Horizontal;
        scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        scrollView.style.width = Length.Percent(100);
        scrollView.style.height = 70;
        
        // Container for tags
        VisualElement tagsContainer = new VisualElement();
        tagsContainer.name = "tagsContainer";
        tagsContainer.style.flexDirection = FlexDirection.Row;
        tagsContainer.style.alignItems = Align.Center;
        tagsContainer.style.height = 70;
        
        // Dummy tag data (replace with API data later)
        string[] dummyTags = { "Minimal", "Modern Design", "Corporate", "Creative Studio", "Bold Typography" };
        
        for (int i = 0; i < dummyTags.Length; i++)
        {
            VisualElement designTag = CreateDesignTag(dummyTags[i]);
            
            // Add margin between tags (except for the last one)
            if (i > 0)
            {
                designTag.style.marginLeft = 15;
            }
            
            tagsContainer.Add(designTag);
        }
        
        scrollView.Add(tagsContainer);
        tagSection.Add(scrollView);
        
        return tagSection;
    }
    
    private static VisualElement CreateDesignTag(string tagText)
    {
        VisualElement designTag = new VisualElement();
        designTag.name = "designTag";
        designTag.AddToClassList("designTag");
        designTag.style.width = StyleKeyword.Auto; // Auto width based on content
        designTag.style.minWidth = 120; // Minimum width to ensure it looks good
        designTag.style.height = 70;
        designTag.style.paddingLeft = 25; // Add horizontal padding
        designTag.style.paddingRight = 25; // Add horizontal padding
        designTag.style.borderBottomWidth = 2;
        designTag.style.borderTopWidth = 2;
        designTag.style.borderLeftWidth = 2;
        designTag.style.borderRightWidth = 2;
        designTag.style.borderBottomColor = new Color(129f/255f, 129f/255f, 129f/255f, 0.84f);
        designTag.style.borderTopColor = new Color(129f/255f, 129f/255f, 129f/255f, 0.84f);
        designTag.style.borderLeftColor = new Color(129f/255f, 129f/255f, 129f/255f, 0.84f);
        designTag.style.borderRightColor = new Color(129f/255f, 129f/255f, 129f/255f, 0.84f);
        designTag.style.borderTopLeftRadius = 35;
        designTag.style.borderTopRightRadius = 35;
        designTag.style.borderBottomLeftRadius = 35;
        designTag.style.borderBottomRightRadius = 35;
        designTag.style.alignItems = Align.Center;
        designTag.style.justifyContent = Justify.Center;
        designTag.style.flexShrink = 0; // Prevent tags from shrinking
        
        Label tagLabel = new Label(tagText);
        tagLabel.name = "tagLabel";
        tagLabel.AddToClassList("tagLabel");
        tagLabel.style.fontSize = 35;
        tagLabel.style.color = new Color(129f/255f, 129f/255f, 129f/255f, 0.84f);
        tagLabel.style.whiteSpace = WhiteSpace.NoWrap; // Prevent text wrapping
        tagLabel.style.width = StyleKeyword.Auto; // Auto width for label
        tagLabel.style.unityTextAlign = TextAnchor.MiddleCenter; // Center align text
        
        designTag.Add(tagLabel);
        
        return designTag;
    }
}