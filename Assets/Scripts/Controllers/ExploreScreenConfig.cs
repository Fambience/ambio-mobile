using UnityEngine;

[CreateAssetMenu(fileName = "ExploreScreenConfig", menuName = "UI/Explore Screen Config")]
public class ExploreScreenConfig : ScriptableObject
{
    [Header("Layout Settings")]
    public int defaultTotalRows = 10;
    public int postsPerRow = 3;
    public float postSize = 320f;
    public float rowSpacing = 8f;
    
    [Header("Colors")]
    public Color backgroundColor = new Color(245f/255f, 240f/255f, 237f/255f, 1f);
    public Color borderColor = new Color(129f/255f, 129f/255f, 129f/255f, 0.84f);
    public Color placeholderTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Color activeTextColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    
    [Header("UI Constants")]
    public float headerMarginTop = 120f;
    public float headerHeight = 250f;
    public float searchFieldHeight = 100f;
    public float searchFieldBorderRadius = 50f;
    public float searchFieldPaddingLeft = 40f;
    public float searchFieldFontSize = 35f;
    public float filterIconSize = 55f;
    public float tagHeight = 70f;
    public float tagBorderRadius = 35f;
    public float tagFontSize = 35f;
    
    [Header("Resources")]
    public string minimalistImagePath = "minimalist";
    public string filterIconPath = "filter-Icon";
    
    [Header("Text")]
    public string searchPlaceholder = "Search for designers...";
    public string defaultTagText = "Minimal";
}

public static class ExploreScreenConstants
{
    // UI Element Names
    public const string EXPLORE_SCREEN = "exploreScreen";
    public const string SEARCH_FIELD = "searchField";
    public const string FILTER_BUTTON = "filterButton";
    public const string FILTER_ICON = "filterIcon";
    public const string TAG_SECTION = "tagSection";
    public const string POSTS_CONTAINER = "postsContainer";
    
    // CSS Classes
    public const string SCROLL_VIEW_CLASS = "scrollView";
    public const string HEADER_SECTION_CLASS = "headerSection";
    public const string SEARCH_SECTION_CLASS = "searchSection";
    public const string SEARCH_WRAPPER_CLASS = "search-wrapper";
    public const string SEARCH_FIELD_CLASS = "search-field";
    public const string FILTER_WRAPPER_CLASS = "filter-wrapper";
    public const string FILTER_BUTTON_CLASS = "filter-button";
    public const string FILTER_ICON_CLASS = "filter-icon";
    public const string TAG_SECTION_CLASS = "tag-section";
    public const string DESIGN_TAGS_CLASS = "designTags";
    public const string DESIGN_TAG_CLASS = "designTag";
    public const string POST_ROW_CLASS = "post-row";
    public const string POST_CLASS = "post";
    
    // Filter Popup Elements
    public const string POPUP_OVERLAY = "popupOverlay";
    public const string CLOSE_BUTTON = "closeButton";
    public const string CANCEL_BUTTON = "cancelButton";
    public const string APPLY_BUTTON = "applyButton";
    public const string MINIMAL_TOGGLE = "minimalToggle";
    public const string MODERN_TOGGLE = "modernToggle";
    public const string VINTAGE_TOGGLE = "vintageToggle";
    public const string ABSTRACT_TOGGLE = "abstractToggle";
    public const string NEWEST_RADIO = "newestRadio";
    public const string POPULAR_RADIO = "popularRadio";
    public const string RATING_RADIO = "ratingRadio";
    
    // Default Values
    public const int DEFAULT_POSTS_PER_ROW = 3;
    public const int DEFAULT_TOTAL_ROWS = 10;
    public const float DEFAULT_POST_SIZE = 320f;
}