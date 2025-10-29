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
    
    [Header("API Configuration")]
    [SerializeField] private bool useDummyData = false; // Toggle for testing
    
    private string baseURL;
    private string authToken;
    
    [Header("Filter Popup Configuration")]
    [SerializeField] private VisualTreeAsset filterPopupVisualTree;
    
    [Header("Pull to Refresh Settings")]
    public float pullThreshold = 100f;
    public float refreshIndicatorSize = 50f;
    
    [Header("Search Configuration")]
    [SerializeField] private VisualTreeAsset searchScreenVisualTree;
    private VisualElement searchScreen;
    private bool isSearchScreenActive = false;
    [SerializeField] private VisualTreeAsset designerCardVisualTree; 
    
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
    
    void OnEnable()
    {
        baseURL = baseScript.baseURL;
        authToken = AuthTokenManager.GetToken();
        // If search screen was active, hide it when returning to this screen
        if (isSearchScreenActive)
        {
            HideSearchScreen();
        }
        StartCoroutine(ShowNavigationAfterDelay());   
        Invoke("InitializeExploreScreen", 0.1f);
    }
    
    private IEnumerator ShowNavigationAfterDelay()
    {
        yield return new WaitForSeconds(0.1f); // Small delay to ensure UI is ready
        
        Debug.Log("Showing navigation bar for Explore screen");
        
        // Show navigation bar and set Explore as selected
        NavigationManager.ToggleNavigationBar(true);
        NavigationManager.UpdateSelectedIcon(NavScreen.Explore);
        
        // Debug confirmation
        yield return new WaitForSeconds(0.1f);
        Debug.Log($"Navigation bar visible: {NavigationManager.IsNavigationBarVisible()}");
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
        LoadTrendingPosts(); // Changed from GenerateAndLoadPosts
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
    
    // Updated method to load trending posts from API
    void LoadTrendingPosts()
    {
        if (useDummyData)
        {
            Debug.Log("Using dummy data for testing...");
            int totalPosts = totalRows * postsPerRow;
            // allPosts = PostDataGetter.GenerateDummyPosts(totalPosts);
        }
        else
        {
            Debug.Log("Trending posts will be loaded when trending tag is selected...");
        }
    }
    
    private IEnumerator FetchTrendingPostsCoroutine()
    {
        bool apiCallCompleted = false;
        List<PostData> fetchedPosts = null;
        string errorMessage = null;

        yield return PostDataGetter.FetchTrendingFeed(
            baseURL,
            authToken,
            onSuccess: (posts) => 
            {
                fetchedPosts = posts;
                apiCallCompleted = true;
            },
            onError: (error) => 
            {
                errorMessage = error;
                apiCallCompleted = true;
            }
        );

        // Wait for API call to complete
        yield return new WaitUntil(() => apiCallCompleted);

        if (fetchedPosts != null && fetchedPosts.Count > 0)
        {
            allPosts = fetchedPosts;
            totalRows = Mathf.CeilToInt((float)allPosts.Count / postsPerRow);
            Debug.Log($"Successfully loaded {allPosts.Count} trending posts from API");
            
            // Recreate content if UI is already initialized
            if (mainScrollView != null)
            {
                CreateScrollableContent();
            }
        }
        else
        {
            Debug.LogError($"Failed to fetch trending posts: {errorMessage}");
            
            // Fallback to dummy data
            Debug.Log("Falling back to dummy data...");
            int totalPosts = totalRows * postsPerRow;
            // allPosts = PostDataGetter.GenerateDummyPosts(totalPosts);
            
            if (mainScrollView != null)
            {
                CreateScrollableContent();
            }
        }
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

        // Create and add header section with tag selection callback AND search callback
        VisualElement headerSection = ExploreHeaderUIBuilder.CreateHeaderSection(
            OnFilterClicked, 
            OnTagSelected, 
            OnSearchClicked  // Add this new callback
        );
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

        Debug.Log($"Created scrollable content with header and {allPosts.Count} posts");
    }
    
    //search bar functions
    private void OnSearchClicked()
    {
        Debug.Log("Search field clicked - showing search screen");
        ShowSearchScreen();
    }

    private void ShowSearchScreen()
    {
        if (searchScreen == null)
        {
            // Set the designer card template before creating search screen
            SearchScreenUIBuilder.SetDesignerCardTemplate(designerCardVisualTree);

            // Create the search screen using your UXML template
            searchScreen = SearchScreenUIBuilder.CreateSearchScreen(
                OnSearchBackClicked,
                searchScreenVisualTree
            );
    
            // Add to the root element (same level as exploreScreen)
            var rootElement = uiDocument.rootVisualElement;
            rootElement.Add(searchScreen);
    
            Debug.Log("Search screen created from UXML template and added to root");
        }

        // Show the search screen
        searchScreen.style.display = DisplayStyle.Flex;
        isSearchScreenActive = true;

        // Hide navigation bar when search is active
        NavigationManager.ToggleNavigationBar(false);

        // Focus the search field
        SearchScreenUIBuilder.FocusSearchField();

        Debug.Log("Search screen displayed");
    }

    private void OnSearchBackClicked()
    {
        Debug.Log("Search back button clicked");
        HideSearchScreen();
    }

    private void HideSearchScreen()
    {
        if (searchScreen != null)
        {
            searchScreen.style.display = DisplayStyle.None;
            isSearchScreenActive = false;
        
            // Show navigation bar again
            NavigationManager.ToggleNavigationBar(true);
            NavigationManager.UpdateSelectedIcon(NavScreen.Explore);
        
            Debug.Log("Search screen hidden");
        }
    }
    
    private void OnTagSelected(string tagType, int tagId)
    {
        Debug.Log($"Tag selected: {tagType} (ID: {tagId})");
        // Check if this is initial trending load
        bool isInitialTrending = (tagId == 0 && ExploreHeaderUIBuilder.IsInitialLoad());
        // Start coroutine to fetch posts for the selected tag
        StartCoroutine(FetchPostsByTagCoroutine(tagType, tagId, isInitialTrending));
    }
    
    private IEnumerator FetchPostsByTagCoroutine(string tagType, int tagId, bool isInitialLoad = false)
    {
        Debug.Log($"Fetching posts for tag type: {tagType} (Initial: {isInitialLoad})");
        
        bool apiCallCompleted = false;
        List<PostData> fetchedPosts = null;
        string errorMessage = null;

        // Use the modified FetchFeedByType method
        yield return PostDataGetter.FetchFeedByType(
            baseURL,
            authToken,
            tagType,
            onSuccess: (posts) => 
            {
                fetchedPosts = posts;
                apiCallCompleted = true;
            },
            onError: (error) => 
            {
                errorMessage = error;
                apiCallCompleted = true;
            }
        );

        // Wait for API call to complete
        yield return new WaitUntil(() => apiCallCompleted);

        if (fetchedPosts != null && fetchedPosts.Count >= 0)
        {
            allPosts = fetchedPosts;
            totalRows = Mathf.CeilToInt((float)allPosts.Count / postsPerRow);
            Debug.Log($"Successfully loaded {allPosts.Count} posts for tag: {tagType}");
            
            // Update the posts display
            UpdatePostsDisplay(allPosts);
            
            // Only notify header about loading completion for non-initial loads
            if (!isInitialLoad)
            {
                ExploreHeaderUIBuilder.OnDataLoadSuccess(tagId);
            }
        }
        else
        {
            Debug.LogError($"Failed to fetch posts for tag {tagType}: {errorMessage}");
            
            // Show error state only for non-initial loads
            if (!isInitialLoad)
            {
                // ShowPostsErrorState(errorMessage);
                ExploreHeaderUIBuilder.OnDataLoadError(tagId);
            }
            else
            {
                // For initial load failure, just log and continue
                Debug.LogWarning("Initial trending load failed, continuing with empty state");
            }
        }
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
        Debug.Log("Starting refresh - fetching data from API...");
        
        // Start the API call
        bool apiCallCompleted = false;
        bool apiCallSuccessful = false;
        
        if (useDummyData)
        {
            // For dummy data, simulate a quick load
            yield return new WaitForSeconds(0.5f);
            int totalPosts = totalRows * postsPerRow;
            // allPosts = PostDataGetter.GenerateDummyPosts(totalPosts);
            apiCallCompleted = true;
            apiCallSuccessful = true;
        }
        else
        {
            // Fetch real data from API
            StartCoroutine(FetchDataForRefresh(() => 
            {
                apiCallCompleted = true;
                apiCallSuccessful = allPosts != null && allPosts.Count > 0;
            }));
            
            // Wait until API call completes
            yield return new WaitUntil(() => apiCallCompleted);
        }
        
        // Recreate the content with new data
        if (apiCallSuccessful)
        {
            CreateScrollableContent();
            Debug.Log("Refresh completed - data loaded successfully!");
        }
        else
        {
            Debug.LogWarning("Refresh completed - failed to load data, keeping existing content");
        }
        
        // Small delay before hiding the indicator
        yield return new WaitForSeconds(0.2f);
        
        ResetPullIndicator();
        isRefreshing = false;
    }
    
    private IEnumerator FetchDataForRefresh(System.Action onComplete)
    {
        List<PostData> fetchedPosts = null;
        string errorMessage = null;
        bool callCompleted = false;

        yield return PostDataGetter.FetchTrendingFeed(
            baseURL,
            authToken,
            onSuccess: (posts) => 
            {
                fetchedPosts = posts;
                callCompleted = true;
            },
            onError: (error) => 
            {
                errorMessage = error;
                callCompleted = true;
            }
        );

        yield return new WaitUntil(() => callCompleted);

        if (fetchedPosts != null && fetchedPosts.Count > 0)
        {
            allPosts = fetchedPosts;
            totalRows = Mathf.CeilToInt((float)allPosts.Count / postsPerRow);
            Debug.Log($"Refresh: Successfully loaded {allPosts.Count} trending posts from API");
        }
        else
        {
            Debug.LogError($"Refresh: Failed to fetch trending posts: {errorMessage}");
        }
        
        onComplete?.Invoke();
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
        Debug.Log($"=== Post {index + 1} Clicked ===");
        Debug.Log($"Post ID: {postData.postId}");
        Debug.Log($"Designer: {postData.designerId}");
        Debug.Log($"Opening post screen...");

        // Convert PostData to Post and show the post screen
        Post post = ConvertPostDataToPost(postData);
        PostScreenDataHandler.ShowPostStatic(post, gameObject);
    }

    // Helper method to convert PostData to Post
    private Post ConvertPostDataToPost(PostData postData)
    {
        Post post = new Post();

        // Convert basic fields
        post.postId = postData.postId.ToString();
        post.description = postData.description;
        post.caption = postData.postTitle;
        post.designStyle = postData.designStyle;
        post.roomType = postData.roomType;
        post.likesCount = postData.likesCount;
        post.commentsCount = postData.commentsCount;
        post.bookmarksCount = postData.bookmarksCount;
        post.liked = postData.liked;
        post.bookmarked = postData.bookmarked;

        // Convert media URLs to PostMedia list
        post.postMedia = new List<PostMedia>();
        if (postData.mediaUrls != null && postData.mediaUrls.Count > 0)
        {
            foreach (string url in postData.mediaUrls)
            {
                post.postMedia.Add(new PostMedia { filePath = url });
            }
        }

        // Create User object from designer info
        post.author = new User
        {
            userName = postData.designerId,
            avatar = postData.userAvatar
        };

        // Initialize other fields with default values
        post.status = "published";
        post.createdAt = System.DateTime.Now.ToString();
        post.category = postData.roomType;
        post.tags = new List<string>();
        post.media = new List<Media>();

        return post;
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
    
        // Start coroutine to fetch filtered posts from API
        StartCoroutine(FetchFilteredPostsCoroutine(filterData));
    }
    
    private IEnumerator FetchFilteredPostsCoroutine(FilterData filterData)
    {
        Debug.Log($"Fetching filtered posts - Room Type: {filterData.RoomType}, Design Style: {filterData.DesignStyle}, Sort By: {filterData.SortBy}");
    
        bool apiCallCompleted = false;
        List<PostData> fetchedPosts = null;
        string errorMessage = null;

        // Use the new FetchFilteredPosts method
        yield return PostDataGetter.FetchFilteredPosts(
            baseURL,
            authToken,
            roomType: filterData.RoomType,
            designStyle: filterData.DesignStyle,
            sortBy: filterData.SortBy,
            onSuccess: (posts) => 
            {
                fetchedPosts = posts;
                apiCallCompleted = true;
            },
            onError: (error) => 
            {
                errorMessage = error;
                apiCallCompleted = true;
            }
        );

        // Wait for API call to complete
        yield return new WaitUntil(() => apiCallCompleted);

        if (fetchedPosts != null)
        {
            allPosts = fetchedPosts;
            totalRows = Mathf.CeilToInt((float)allPosts.Count / postsPerRow);
            Debug.Log($"Successfully loaded {allPosts.Count} filtered posts from API");
        
            // Update the posts display with filtered results
            UpdatePostsDisplay(allPosts);
        }
        else
        {
            Debug.LogError($"Failed to fetch filtered posts: {errorMessage}");
        
            // Show error message to user or keep existing posts
            if (allPosts.Count == 0)
            {
                // If no posts currently shown, could show empty state
                UpdatePostsDisplay(new List<PostData>());
            }
            // Otherwise keep existing posts displayed
        }
    }
    
    List<PostData> ApplyFiltersToPostData(List<PostData> posts, FilterData filterData)
    {
        Debug.Log("Using API-filtered posts, no additional client-side filtering needed");
        return posts;
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
        LoadTrendingPosts(); // Changed to use API method
    }
    
    public void AddMorePosts(List<PostData> newPosts)
    {
        allPosts.AddRange(newPosts);
        totalRows = Mathf.CeilToInt((float)allPosts.Count / postsPerRow);
        CreateScrollableContent();
    }
    
    // Toggle between dummy and real data for testing
    [ContextMenu("Toggle Dummy Data")]
    public void ToggleDummyData()
    {
        useDummyData = !useDummyData;
        Debug.Log($"Switched to {(useDummyData ? "dummy" : "API")} data mode");
        LoadTrendingPosts();
    }
    
    // Manual refresh method for testing
    [ContextMenu("Manual Refresh")]
    public void ManualRefresh()
    {
        LoadTrendingPosts();
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
    
        // Clean up search screen
        if (searchScreen != null && searchScreen.parent != null)
        {
            searchScreen.parent.Remove(searchScreen);
            searchScreen = null;
        }
    }
}