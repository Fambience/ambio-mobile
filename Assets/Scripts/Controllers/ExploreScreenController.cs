using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ExploreScreenController : MonoBehaviour
{
    [Header("Post Configuration")]
    [SerializeField] private int totalRows = 10;
    [SerializeField] private int postsPerRow = 3;
    
    [Header("Filter Popup Configuration")]
    [SerializeField] private VisualTreeAsset filterPopupVisualTree;
    
    [Header("Pull to Refresh Settings")]
    public float pullThreshold = 100f;
    public float refreshIndicatorSize = 50f;
    public float refreshDuration = 3f; // 3 seconds as requested
    
    private UIDocument uiDocument;
    private ScrollView mainScrollView;
    private VisualElement exploreScreen;
    
    // Pull to refresh elements
    private VisualElement refreshIndicator;
    private VisualElement refreshContainer;
    private Image refreshIcon;
    private bool isRefreshing = false;
    private bool isPulling = false;
    private float pullDistance = 0f;
    
    private List<PostData> allPosts = new List<PostData>();
    private FilterPopupHandler filterPopupHandler;
    
    void Start()
    {
        Invoke("InitializeExploreScreen", 0.1f);
    }
    
    void InitializeExploreScreen()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found!");
            return;
        }
        
        Debug.Log("Initializing Explore Screen...");
        
        InitializeUI();
        SetupPullToRefresh();
        InitializeFilterPopup();
        GenerateAndLoadPosts();
        CreateScrollableContent();
    }
    
    void InitializeUI()
    {
        var root = uiDocument.rootVisualElement;
        exploreScreen = root.Q<VisualElement>("exploreScreen");
        
        if (exploreScreen == null)
        {
            Debug.LogError("Explore screen not found in UXML!");
            return;
        }
        
        Debug.Log("Explore screen found successfully");
        
        // Clear existing content
        exploreScreen.Clear();
        
        // Create main scroll view
        mainScrollView = CreateMainScrollView();
        exploreScreen.Add(mainScrollView);
        
        Debug.Log("Main ScrollView created and added");
    }
    
    ScrollView CreateMainScrollView()
    {
        ScrollView scrollView = new ScrollView(ScrollViewMode.Vertical);
        scrollView.AddToClassList("scrollView");
        scrollView.style.flexGrow = 1;
        scrollView.style.width = Length.Percent(100);
        scrollView.style.height = Length.Percent(100);
        scrollView.style.backgroundColor = new Color(245f/255f, 240f/255f, 237f/255f, 1f);
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        
        return scrollView;
    }
    
    private void SetupPullToRefresh()
    {
        // Create refresh indicator container
        refreshContainer = new VisualElement();
        refreshContainer.style.height = 0;
        refreshContainer.style.flexDirection = FlexDirection.Row;
        refreshContainer.style.justifyContent = Justify.Center;
        refreshContainer.style.alignItems = Align.Center;
        refreshContainer.style.overflow = Overflow.Hidden;
        refreshContainer.style.marginTop = 0;
        
        // Create refresh indicator
        refreshIndicator = new VisualElement();
        refreshIndicator.style.width = refreshIndicatorSize;
        refreshIndicator.style.height = refreshIndicatorSize;
        refreshIndicator.style.justifyContent = Justify.Center;
        refreshIndicator.style.alignItems = Align.Center;
        refreshIndicator.style.opacity = 0;
        
        // Create refresh icon
        refreshIcon = new Image();
        refreshIcon.image = Resources.Load<Texture2D>("loader");
        refreshIcon.style.width = refreshIndicatorSize;
        refreshIcon.style.height = refreshIndicatorSize;
        
        refreshIndicator.Add(refreshIcon);
        refreshContainer.Add(refreshIndicator);
        
        // Insert refresh container at the beginning of scroll view
        // We'll add it after the main container is created
    }
    
    private void RegisterPullToRefreshEvents()
    {
        if (mainScrollView == null) return;
        
        // Register scroll event
        mainScrollView.RegisterCallback<WheelEvent>(OnScroll);
        
        // Register pointer events for touch/mouse
        mainScrollView.RegisterCallback<PointerDownEvent>(OnPointerDown);
        mainScrollView.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        mainScrollView.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }
    
    void InitializeFilterPopup()
    {
        filterPopupHandler = new FilterPopupHandler();
        filterPopupHandler.Initialize(filterPopupVisualTree, uiDocument.rootVisualElement);
        filterPopupHandler.OnFiltersApplied += OnFiltersApplied;
    }
    
    void GenerateAndLoadPosts()
    {
        int totalPosts = totalRows * postsPerRow;
        allPosts = PostDataGetter.GenerateDummyPosts(totalPosts);
    }
    
    void CreateScrollableContent()
    {
        if (mainScrollView == null) 
        {
            Debug.LogError("MainScrollView is null!");
            return;
        }
        
        Debug.Log("Creating scrollable content...");
        
        // Clear existing content
        mainScrollView.Clear();
        
        // Create main container for all content
        VisualElement mainContainer = new VisualElement();
        mainContainer.style.flexDirection = FlexDirection.Column;
        mainContainer.style.alignItems = Align.Stretch;
        mainContainer.style.width = Length.Percent(100);
        
        // Create and add header section
        VisualElement headerSection = ExploreHeaderUIBuilder.CreateHeaderSection(OnFilterClicked);
        mainContainer.Add(headerSection);
        
        // Insert refresh container after header section (just above posts)
        mainContainer.Add(refreshContainer);
        
        // Create and add posts section
        VisualElement postsContainer = ExplorePostUIBuilder.CreatePostsContainer(
            allPosts, 
            postsPerRow, 
            OnPostClicked
        );
        mainContainer.Add(postsContainer);
        
        mainScrollView.Add(mainContainer);
        
        // Register pull to refresh events after content is created
        RegisterPullToRefreshEvents();
        
        Debug.Log($"Created scrollable content with header and {totalRows} post rows");
    }
    
    #region Pull to Refresh Event Handlers
    
    private void OnScroll(WheelEvent evt)
    {
        if (mainScrollView.scrollOffset.y <= 0 && evt.delta.y < 0 && !isRefreshing)
        {
            HandlePullToRefresh(-evt.delta.y * 10f);
        }
    }
    
    private void OnPointerDown(PointerDownEvent evt)
    {
        if (mainScrollView.scrollOffset.y <= 0 && !isRefreshing)
        {
            isPulling = true;
            pullDistance = 0f;
        }
    }
    
    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (isPulling && mainScrollView.scrollOffset.y <= 0 && !isRefreshing)
        {
            pullDistance += evt.deltaPosition.y;
            HandlePullToRefresh(pullDistance);
        }
    }
    
    private void OnPointerUp(PointerUpEvent evt)
    {
        if (isPulling)
        {
            isPulling = false;
            
            if (pullDistance >= pullThreshold && !isRefreshing)
            {
                StartRefresh();
            }
            else
            {
                ResetPullIndicator();
            }
            
            pullDistance = 0f;
        }
    }
    
    private void HandlePullToRefresh(float distance)
    {
        if (isRefreshing) return;
        
        float normalizedDistance = Mathf.Clamp(distance, 0f, pullThreshold * 1.5f);
        float progress = normalizedDistance / pullThreshold;
        refreshContainer.style.height = normalizedDistance;
        refreshIndicator.style.opacity = Mathf.Clamp01(progress);
        float rotation = progress * 180f;
        refreshIcon.transform.rotation = Quaternion.Euler(0, 0, rotation);
    }
    
    private void StartRefresh()
    {
        if (isRefreshing) return;
        isRefreshing = true;
        refreshContainer.style.height = refreshIndicatorSize + 20f;
        refreshIndicator.style.opacity = 1f;
        StartCoroutine(RotateRefreshIcon());
        StartCoroutine(RefreshContent());
    }
    
    private IEnumerator RotateRefreshIcon()
    {
        float rotationSpeed = 360f;
        
        while (isRefreshing)
        {
            float currentRotation = refreshIcon.transform.rotation.eulerAngles.z;
            float newRotation = currentRotation + rotationSpeed * Time.deltaTime;
            refreshIcon.transform.rotation = Quaternion.Euler(0, 0, newRotation);
            yield return null;
        }
    }
    
    private IEnumerator RefreshContent()
    {
        Debug.Log("Starting refresh...");
        
        // Wait for the specified refresh duration (3 seconds)
        yield return new WaitForSeconds(refreshDuration);
        
        // Regenerate posts (simulate fresh data)
        GenerateAndLoadPosts();
        
        // Recreate the content
        CreateScrollableContent();
        
        // Small delay before hiding the indicator
        yield return new WaitForSeconds(0.5f);
        
        ResetPullIndicator();
        isRefreshing = false;
        
        Debug.Log("Refresh completed!");
    }
    
    private void ResetPullIndicator()
    {
        StartCoroutine(AnimateRefreshIndicatorOut());
    }
    
    private IEnumerator AnimateRefreshIndicatorOut()
    {
        float animationDuration = 0.3f;
        float startHeight = refreshContainer.resolvedStyle.height;
        float startOpacity = refreshIndicator.resolvedStyle.opacity;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            
            refreshContainer.style.height = Mathf.Lerp(startHeight, 0f, easedProgress);
            refreshIndicator.style.opacity = Mathf.Lerp(startOpacity, 0f, easedProgress);
            
            yield return null;
        }
        
        refreshContainer.style.height = 0;
        refreshIndicator.style.opacity = 0;
        refreshIcon.transform.rotation = Quaternion.identity;
    }
    
    #endregion
    
    // Event handlers
    void OnPostClicked(PostData postData, int index)
    {
        Debug.Log($"Post clicked: {postData.postTitle} by {postData.designerId}");
        // Here you can add navigation to post details or other actions
    }
    
    void OnFilterClicked()
    {
        Debug.Log("Filter button clicked");
        filterPopupHandler.ShowPopup();
    }
    
    void OnFiltersApplied()
    {
        Debug.Log("Filters applied, refreshing content...");
        
        // Get current filter data
        FilterData filterData = filterPopupHandler.GetCurrentFilterData();
        
        // Apply filters to posts (implement your filtering logic here)
        List<PostData> filteredPosts = ApplyFiltersToPostData(allPosts, filterData);
        
        // Update the display with filtered posts
        UpdatePostsDisplay(filteredPosts);
    }
    
    List<PostData> ApplyFiltersToPostData(List<PostData> posts, FilterData filterData)
    {
        // TODO: Implement actual filtering logic based on your requirements
        // For now, returning all posts as placeholder
        List<PostData> filteredPosts = new List<PostData>(posts);
        
        // Example filtering logic (customize based on your PostData structure):
        /*
        if (filterData.MinimalEnabled)
        {
            filteredPosts = filteredPosts.FindAll(post => 
                post.postTitle.ToLower().Contains("minimal"));
        }
        
        // Add more filter conditions as needed
        */
        
        // Apply sorting
        // switch (filterData.SortBy)
        // {
        //     case SortOption.Newest:
        //         // Sort by newest (you'd need a date field in PostData)
        //         break;
        //     case SortOption.MostPopular:
        //         // Sort by popularity (you'd need a popularity field in PostData)
        //         break;
        //     case SortOption.HighestRated:
        //         // Sort by rating (you'd need a rating field in PostData)
        //         break;
        // }
        
        return filteredPosts;
    }
    
    void UpdatePostsDisplay(List<PostData> filteredPosts)
    {
        // Update the total rows based on filtered posts
        totalRows = Mathf.CeilToInt((float)filteredPosts.Count / postsPerRow);
        
        // Clear and recreate the posts container
        var mainContainer = mainScrollView.Children().FirstOrDefault();
        if (mainContainer != null)
        {
            // Remove old posts container (it should be the last child after header and refresh container)
            var oldPostsContainer = mainContainer.Q<VisualElement>("postsContainer");
            if (oldPostsContainer != null)
            {
                mainContainer.Remove(oldPostsContainer);
            }
            
            // Add new posts container with filtered data
            VisualElement newPostsContainer = ExplorePostUIBuilder.CreatePostsContainer(
                filteredPosts, 
                postsPerRow, 
                OnPostClicked
            );
            mainContainer.Add(newPostsContainer);
        }
    }
    
    // Public methods for API integration
    public void LoadPostsFromAPI(List<PostData> apiPosts)
    {
        allPosts = apiPosts;
        totalRows = Mathf.CeilToInt((float)allPosts.Count / postsPerRow);
        CreateScrollableContent();
    }
    
    public void RefreshPosts()
    {
        CreateScrollableContent();
    }
    
    public void AddMorePosts(List<PostData> newPosts)
    {
        allPosts.AddRange(newPosts);
        totalRows = Mathf.CeilToInt((float)allPosts.Count / postsPerRow);
        CreateScrollableContent();
    }
    
    void OnDestroy()
    {
        // Unregister events to prevent memory leaks
        if (mainScrollView != null)
        {
            mainScrollView.UnregisterCallback<WheelEvent>(OnScroll);
            mainScrollView.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            mainScrollView.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            mainScrollView.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }
        
        // Unsubscribe from events to prevent memory leaks
        if (filterPopupHandler != null)
        {
            filterPopupHandler.OnFiltersApplied -= OnFiltersApplied;
        }
    }
}