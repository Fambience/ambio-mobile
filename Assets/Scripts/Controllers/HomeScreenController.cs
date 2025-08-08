using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public class ApiResponse<T>
{
    public bool success;
    public string message;
    public T data;
    public Pagination pagination;
}

[System.Serializable]
public class FollowResponse
{
    public string message;
    public bool followed;
}

[System.Serializable]
public class BookmarkResponse
{
    public string message;
}

[System.Serializable]
public class LikeResponse
{
    public string message;
    public string postId;
    public string userId;
    public int likeId;
}

[System.Serializable]
public class HomeApiResponse
{
    public string type;
    public List<Post> posts;
    public Pagination pagination;
}

[System.Serializable]
public class Pagination
{
    public int page;
    public int limit;
    public int total;
}

[System.Serializable]
public class Post
{
    [SerializeField] public string postId;
    [SerializeField] public string caption;
    [SerializeField] public string description;
    [SerializeField] public string designStyle;
    [SerializeField] public string roomType;
    [SerializeField] public string status;
    [SerializeField] public string createdAt;
    [SerializeField] public string category;
    [SerializeField] public int likesCount;
    [SerializeField] public int commentsCount;
    [SerializeField] public int bookmarksCount;
    [SerializeField] public List<string> tags;
    [SerializeField] public List<Media> media;
    [SerializeField] public List<PostMedia> postMedia;
    [SerializeField] public User creator; // Changed from 'user' to 'creator'
    [SerializeField] public bool liked;
    [SerializeField] public bool bookmarked;
    
    // Helper method to get first image URL only
    public string GetFirstImageUrl()
    {
        if (postMedia != null && postMedia.Count > 0 && !string.IsNullOrEmpty(postMedia[0].filePath))
        {
            return postMedia[0].filePath;
        }
        else if (media != null && media.Count > 0 && !string.IsNullOrEmpty(media[0].url))
        {
            return media[0].url;
        }
        
        return null;
    }
}

[System.Serializable]
public class Media
{
    public string mediaId;
    public string url;
}

[System.Serializable]
public class PostMedia
{
    public string filePath;
}

[System.Serializable]
public class User
{
    [SerializeField] public string userId;
    [SerializeField] public string userName;
    [SerializeField] public string firstName;
    [SerializeField] public string lastName;
    [SerializeField] public string email;
    [SerializeField] public string avatar;
    [SerializeField] public string bio;
    [SerializeField] public int followersCount;
}

public class HomeScreenController : MonoBehaviour
{
    public VisualTreeAsset verticalCardTemplate;
    public VisualTreeAsset horizontalCardTemplate;

    [Header("API Settings")]
    public string baseURL;
    public string exploreFeedUrl = "/api/v1/post/explore-feed";
    public string homeFeedUrl = "/api/v1/post/user-feed";
    public string trendingDesignersUrl = "/api/v1/post/trending-designers";
    public string authToken;

    public int postsPerPage = 10;
    public int designersPerPage = 10;
    public int homeCheckInterval = 10; // Check home API every 10 cards

    [Header("Pull to Refresh Settings")]
    public float pullThreshold = 100f;
    public float refreshIndicatorSize = 50f;
    public float refreshDuration = 2f;
    
    private ScrollView container;
    private VisualElement refreshIndicator;
    private VisualElement refreshContainer;
    private Image refreshIcon;
    private bool isRefreshing = false;
    private bool isPulling = false;
    private float pullDistance = 0f;
    
    // Data storage for different feed types
    private List<Post> homePosts = new List<Post>();
    private List<Post> explorePosts = new List<Post>();
    private List<User> trendingDesigners = new List<User>();
    
    // Pagination tracking
    private int currentHomePage = 1;
    private int currentExplorePage = 1;
    private int currentDesignerPage = 1;
    
    // Feed management
    private bool isLoadingData = false;
    private int cardsDisplayed = 0;
    private int currentHomePostIndex = 0;
    private int currentExplorePostIndex = 0;
    private int currentDesignerIndex = 0; // Track which designers have been shown
    private bool isShowingHomeFeed = false;
    private bool hasMoreHomePosts = true;
    private bool hasMoreExplorePosts = true;
    private bool hasMoreDesigners = true;
    
    // Track total pages loaded to prevent duplicates
    private int totalHomePagesLoaded = 0;
    private int totalExplorePagesLoaded = 0;
    private int totalDesignerPagesLoaded = 0;
    
    private enum FeedType
    {
        Home,
        Explore
    }
    
    private void OnEnable()
    {
        baseURL = baseScript.baseURL;
        authToken = AuthTokenManager.GetToken();
        var root = GetComponent<UIDocument>().rootVisualElement;
        container = root.Q<ScrollView>("main-container");
        StartCoroutine(ShowNavigationAfterDelay());   
        SetupPullToRefresh();
        StartCoroutine(LoadInitialData());
    }
    private IEnumerator ShowNavigationAfterDelay()
    {
        yield return new WaitForSeconds(0.1f); // Small delay to ensure UI is ready
        
        Debug.Log("Showing navigation bar for Home screen");
        
        // Show navigation bar and set Home as selected
        NavigationManager.ToggleNavigationBar(true);
        NavigationManager.UpdateSelectedIcon(NavScreen.Home);
        
        // Debug confirmation
        yield return new WaitForSeconds(0.1f);
        Debug.Log($"Navigation bar visible: {NavigationManager.IsNavigationBarVisible()}");
    }
    
    private void SetupPullToRefresh()
    {
        container.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        
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
        
        // Insert refresh container at the beginning
        container.Insert(0, refreshContainer);
        
        // Register scroll event
        container.RegisterCallback<WheelEvent>(OnScroll);
        
        // Register pointer events for touch/mouse
        container.RegisterCallback<PointerDownEvent>(OnPointerDown);
        container.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        container.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }
    
    private IEnumerator LoadInitialData()
    {
        isLoadingData = true;
    
        // Clear existing data
        homePosts.Clear();
        explorePosts.Clear();
        trendingDesigners.Clear();
    
        // Reset pagination counters
        currentHomePage = 1;
        currentExplorePage = 1;
        currentDesignerPage = 1;
        totalHomePagesLoaded = 0;
        totalExplorePagesLoaded = 0;
        totalDesignerPagesLoaded = 0;
    
        // First check home feed
        yield return StartCoroutine(LoadHomeFeed(currentHomePage));
    
        // Then load explore feed
        yield return StartCoroutine(LoadExploreFeed(currentExplorePage));
    
        // Load trending designers
        yield return StartCoroutine(LoadTrendingDesigners(currentDesignerPage));
    
        // Build initial UI
        BuildInitialUI();
    
        isLoadingData = false;
    }
    
    private IEnumerator LikePost(string postId, Image likeIcon, Post post)
    {
        string url = $"{baseURL}/api/v1/post/like/{postId}";
        Debug.Log($"Liking post {postId}");
        
        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(url, ""))
        {
            request.SetRequestHeader("Authorization", authToken);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                try
                {
                    var response = JsonUtility.FromJson<ApiResponse<LikeResponse>>(jsonResponse);
                    
                    if (response.success)
                    {
                        // Toggle based on current state instead of message
                        if (post.liked)
                        {
                            // Currently liked, so unlike it
                            Texture2D outlineHeart = LoadImage("favourite");
                            if (outlineHeart != null)
                            {
                                likeIcon.image = outlineHeart;
                                post.likesCount--;
                                post.liked = false;
                            }
                            Debug.Log($"Post unliked. New count: {post.likesCount}");
                        }
                        else
                        {
                            // Currently not liked, so like it
                            Texture2D filledHeart = LoadImage("heart-filled");
                            if (filledHeart != null)
                            {
                                likeIcon.image = filledHeart;
                                post.likesCount++;
                                post.liked = true;
                            }
                            Debug.Log($"Post liked. New count: {post.likesCount}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Like API Error: {response.message}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing like response: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Like Network Error: {request.error}");
            }
        }
    }

    private IEnumerator BookmarkPost(string postId, Image bookmarkIcon, Post post)
    {
        string url = $"{baseURL}/api/v1/post/bookmark/{postId}";
        Debug.Log($"Bookmarking post {postId}");
        Debug.Log($"Bookmark URL: {url}");
        
        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(url, ""))
        {
            request.SetRequestHeader("Authorization", authToken);
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                try
                {
                    var response = JsonUtility.FromJson<ApiResponse<BookmarkResponse>>(jsonResponse);
                    if (response.success)
                    {
                        // Toggle based on current state instead of message
                        if (post.bookmarked)
                        {
                            // Currently bookmarked, so remove bookmark
                            Texture2D outlineBookmark = LoadImage("Bookmark");
                            if (outlineBookmark != null)
                            {
                                bookmarkIcon.image = outlineBookmark;
                                post.bookmarked = false;
                            }
                            Debug.Log("Bookmark removed successfully");
                        }
                        else
                        {
                            // Currently not bookmarked, so add bookmark
                            Texture2D filledBookmark = LoadImage("bookmark-filled");
                            if (filledBookmark != null)
                            {
                                bookmarkIcon.image = filledBookmark;
                                post.bookmarked = true;
                            }
                            Debug.Log("Post bookmarked successfully");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Bookmark API Error: {response.message}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing bookmark response: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Bookmark Network Error: {request.error}");
            }
        }
    }
    
    private IEnumerator LoadHomeFeed(int page)
    {
        string url = $"{baseURL}{homeFeedUrl}?page={page}&limit={postsPerPage}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", authToken);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log($"Home Feed Raw Response: {jsonResponse}");
                
                try
                {
                    ApiResponse<HomeApiResponse> response = JsonUtility.FromJson<ApiResponse<HomeApiResponse>>(jsonResponse);
                    
                    if (response.success && response.data != null && response.data.posts != null)
                    {
                        if (page == 1)
                        {
                            Debug.Log("REFRESH: Clearing home posts for page 1");
                            homePosts.Clear();
                            totalHomePagesLoaded = 0;
                        }
                        
                        // Only add if we haven't loaded this page before
                        if (page > totalHomePagesLoaded)
                        {
                            int postsBeforeAdd = homePosts.Count;
                            
                            // Debug JSON parsing for each post
                            Debug.Log($"=== HOME FEED DEBUG - Page {page} ===");
                            Debug.Log($"Raw JSON subset: {jsonResponse.Substring(0, Math.Min(500, jsonResponse.Length))}...");
                            
                            foreach (var post in response.data.posts)
                            {
                                Debug.Log($"--- Home Post Debug ---");
                                Debug.Log($"Post ID: {post.postId}");
                                Debug.Log($"Description: {post.description}");
                                Debug.Log($"Creator object exists: {post.creator != null}");
                                
                                if (post.creator != null)
                                {
                                    Debug.Log($"  Creator ID: '{post.creator.userId}'");
                                    Debug.Log($"  Creator Name: '{post.creator.userName}'");
                                    Debug.Log($"  First Name: '{post.creator.firstName}'");
                                    Debug.Log($"  Last Name: '{post.creator.lastName}'");
                                    Debug.Log($"  Email: '{post.creator.email}'");
                                    Debug.Log($"  Avatar: '{post.creator.avatar}'");
                                }
                                else
                                {
                                    Debug.LogError("Creator object is NULL!");
                                }
                                Debug.Log($"--- End Home Post Debug ---");
                            }
                            
                            homePosts.AddRange(response.data.posts);
                            totalHomePagesLoaded = page;
                            hasMoreHomePosts = response.data.posts.Count >= postsPerPage;
                            
                            Debug.Log($"SUCCESS: Added {response.data.posts.Count} posts. Total: {homePosts.Count} (was {postsBeforeAdd})");
                            Debug.Log($"PAGINATION: Page {page} loaded, hasMore: {hasMoreHomePosts}");
                        }
                        else
                        {
                            Debug.Log($"SKIP: Page {page} already loaded (totalLoaded: {totalHomePagesLoaded})");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"NO DATA: {response.message}");
                        hasMoreHomePosts = false;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"PARSE ERROR: {e.Message}");
                    hasMoreHomePosts = false;
                }
            }
            else
            {
                Debug.LogError($"NETWORK ERROR: {request.error}");
                hasMoreHomePosts = false;
            }
        }
    }

    private IEnumerator LoadExploreFeed(int page)
    {
        string url = $"{baseURL}{exploreFeedUrl}?page={page}&limit={postsPerPage}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", authToken);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log($"Explore Feed Raw Response: {jsonResponse}");
                
                try
                {
                    // Parse the explore feed response which has posts directly in data array
                    ApiResponse<List<Post>> response = JsonUtility.FromJson<ApiResponse<List<Post>>>(jsonResponse);
                    Debug.Log("Himanshu" + response);
                    
                    if (response.success && response.data != null)
                    {
                        if (page == 1)
                        {
                            explorePosts.Clear();
                            totalExplorePagesLoaded = 0;
                        }
                        
                        // Only add if we haven't loaded this page before
                        if (page > totalExplorePagesLoaded)
                        {
                            int postsBeforeAdd = explorePosts.Count;
                            
                            // Debug JSON parsing for each post
                            Debug.Log($"=== EXPLORE FEED DEBUG - Page {page} ===");
                            Debug.Log($"Raw JSON subset: {jsonResponse.Substring(0, Math.Min(500, jsonResponse.Length))}...");
                            
                            foreach (var post in response.data)
                            {
                                Debug.Log($"--- Post Debug ---");
                                Debug.Log($"Post ID: {post.postId}");
                                Debug.Log($"Description: {post.description}");
                                Debug.Log($"Creator object exists: {post.creator != null}");
                                
                                if (post.creator != null)
                                {
                                    Debug.Log($"  Creator ID: '{post.creator.userId}'");
                                    Debug.Log($"  Creator Name: '{post.creator.userName}'");
                                    Debug.Log($"  First Name: '{post.creator.firstName}'");
                                    Debug.Log($"  Last Name: '{post.creator.lastName}'");
                                    Debug.Log($"  Email: '{post.creator.email}'");
                                    Debug.Log($"  Avatar: '{post.creator.avatar}'");
                                }
                                else
                                {
                                    Debug.LogError("Creator object is NULL!");
                                }
                                Debug.Log($"--- End Post Debug ---");
                            }
                            
                            explorePosts.AddRange(response.data);
                            totalExplorePagesLoaded = page;
                            hasMoreExplorePosts = response.data.Count >= postsPerPage;
                            
                            Debug.Log($"SUCCESS: Added {response.data.Count} posts. Total: {explorePosts.Count} (was {postsBeforeAdd})");
                            Debug.Log($"PAGINATION: Page {page} loaded, hasMore: {hasMoreExplorePosts}");
                        }
                        else
                        {
                            Debug.Log($"SKIP: Page {page} already loaded (totalLoaded: {totalExplorePagesLoaded})");
                        }
                    }
                    else
                    {
                        Debug.LogError($"NO DATA: {response.message}");
                        hasMoreExplorePosts = false;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"PARSE ERROR: {e.Message}");
                    Debug.LogError($"JSON Response: {jsonResponse}");
                    hasMoreExplorePosts = false;
                }
            }
            else
            {
                Debug.LogError($"NETWORK ERROR: {request.error}");
                hasMoreExplorePosts = false;
            }
        }
    }
    
    private IEnumerator FollowUser(string userId, Label followText, Button followButton, VisualElement horizontalCard)
    {
        string url = $"{baseURL}/api/v1/profile/toggle-follow/{userId}";
        string originalText = followText.text;
        StyleColor originalBackgroundColor = followButton.resolvedStyle.backgroundColor;
        StyleColor originalTextColor = followText.resolvedStyle.color;
        
        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(url, ""))
        {
            request.SetRequestHeader("Authorization", authToken);
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                try
                {
                    var response = JsonUtility.FromJson<FollowResponse>(jsonResponse);
                    if (response.followed)
                    {
                        Debug.Log($"Successfully followed user: {response.message}");
                        StartCoroutine(HideCardAfterDelay(horizontalCard, 10f));
                    }
                    else
                    {
                        Debug.Log($"Successfully unfollowed user: {response.message}");
                        followText.text = "Follow";
                        followButton.style.backgroundColor = StyleKeyword.Null;
                        followText.style.color = StyleKeyword.Null;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing follow response: {e.Message}");
                    RevertFollowButton(followText, followButton, originalText, originalBackgroundColor, originalTextColor);
                }
            }
            else
            {
                Debug.LogError($"Follow Network Error: {request.error}");
                RevertFollowButton(followText, followButton, originalText, originalBackgroundColor, originalTextColor);
            }
        }
    }
    
    private void RevertFollowButton(Label followText, Button followButton, string originalText, 
        StyleColor originalBackgroundColor, StyleColor originalTextColor)
    {
        followText.text = originalText;
        followButton.style.backgroundColor = originalBackgroundColor;
        followText.style.color = originalTextColor;
        Debug.Log("Reverted follow button state due to error");
    }
    
    private IEnumerator HideCardAfterDelay(VisualElement card, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (card != null && card.parent != null)
        {
            StartCoroutine(FadeOutCard(card));
        }
    }
    
    private IEnumerator FadeOutCard(VisualElement card)
    {
        float fadeDuration = 0.2f;
        float elapsedTime = 0f;
        float startOpacity = card.resolvedStyle.opacity;
    
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeDuration;
            card.style.opacity = Mathf.Lerp(startOpacity, 0f, progress);
            yield return null;
        }
    
        // Remove the card from its parent
        card.parent.Remove(card);
        Debug.Log("Designer card hidden after successful follow");
    }
    
    private IEnumerator LoadTrendingDesigners(int page)
    {
        string url = $"{baseURL}{trendingDesignersUrl}?page={page}&limit={designersPerPage}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", authToken);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log($"Trending Designers API Response (Page {page}): {jsonResponse}");
                
                try
                {
                    ApiResponse<List<User>> response = JsonUtility.FromJson<ApiResponse<List<User>>>(jsonResponse);
                    
                    if (response.success && response.data != null)
                    {
                        if (page == 1)
                        {
                            trendingDesigners.Clear();
                            totalDesignerPagesLoaded = 0;
                        }
                        
                        // Only add if we haven't loaded this page before
                        if (page > totalDesignerPagesLoaded)
                        {
                            trendingDesigners.AddRange(response.data);
                            totalDesignerPagesLoaded = page;
                            hasMoreDesigners = response.data.Count >= designersPerPage;
                            
                            Debug.Log($"Loaded {response.data.Count} trending designers from page {page}. Total: {trendingDesigners.Count}");
                        }
                        else
                        {
                            Debug.Log($"Designer page {page} already loaded, skipping");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Trending Designers API Error: {response.message}");
                        hasMoreDesigners = false;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing trending designers response: {e.Message}");
                    hasMoreDesigners = false;
                }
            }
            else
            {
                Debug.LogError($"Trending Designers Network Error: {request.error}");
                hasMoreDesigners = false;
            }
        }
    }
    
    private void BuildInitialUI()
    {
        ClearContent();
        
        // Reset counters
        cardsDisplayed = 0;
        currentHomePostIndex = 0;
        currentExplorePostIndex = 0;
        currentDesignerIndex = 0;
        
        // Determine initial feed type
        isShowingHomeFeed = homePosts.Count > 0;
        
        Debug.Log($"Initial feed type: {(isShowingHomeFeed ? "Home" : "Explore")}");
        Debug.Log($"Home posts available: {homePosts.Count}, Explore posts available: {explorePosts.Count}");
        
        // Start building UI
        StartCoroutine(BuildUIGradually());
    }
    
    private IEnumerator BuildUIGradually()
    {
        while (HasMorePostsToShow())
        {
            // Add posts in groups
            int groupSize = (cardsDisplayed == 0) ? 5 : 10;
            int postsAddedInGroup = 0;
            
            for (int i = 0; i < groupSize && HasMorePostsToShow(); i++)
            {
                // Check if we need to switch feeds or load more data
                if (cardsDisplayed > 0 && cardsDisplayed % homeCheckInterval == 0)
                {
                    yield return StartCoroutine(CheckAndSwitchFeed());
                }
                
                Post postToShow = GetNextPost();
                if (postToShow != null)
                {
                    VisualElement verticalCard = CreateVerticalCard(postToShow);
                    container.Add(verticalCard);
                    cardsDisplayed++;
                    postsAddedInGroup++;
                    
                    Debug.Log($"Added card {cardsDisplayed} from {(isShowingHomeFeed ? "Home" : "Explore")} feed");
                }
                else
                {
                    break; // No more posts available
                }
            }
            
            // Add trending designers section after every 5 cards (multiples of 5)
            if (postsAddedInGroup > 0 && cardsDisplayed % 5 == 0)
            {
                yield return StartCoroutine(AddTrendingDesignersSection());
            }
            
            yield return null; // Allow UI to update
        }
    }
    
    private IEnumerator CheckAndSwitchFeed()
    {
        Debug.Log($"Checking feed switch at card {cardsDisplayed}");
        Debug.Log($"Current state - Home: page {currentHomePage}, index {currentHomePostIndex}/{homePosts.Count}");
        Debug.Log($"Current state - Explore: page {currentExplorePage}, index {currentExplorePostIndex}/{explorePosts.Count}");
        if (isShowingHomeFeed)
        {
            // If showing home feed and we're running low, try to load more
            if (currentHomePostIndex >= homePosts.Count - 2 && hasMoreHomePosts)
            {
                Debug.Log($"Loading more home posts - requesting page {currentHomePage + 1}");
                currentHomePage++;
                yield return StartCoroutine(LoadHomeFeed(currentHomePage));
            }
            
            // If no more home posts available, switch to explore
            if (currentHomePostIndex >= homePosts.Count && !hasMoreHomePosts)
            {
                Debug.Log("No more home posts available, switching to Explore feed");
                isShowingHomeFeed = false;
                
                // IMPORTANT: Don't reset explore index, continue where we left off
                Debug.Log($"Continuing explore from index {currentExplorePostIndex}");
            }
        }
        else
        {
            // Check for new home posts (only check pages we haven't loaded yet)
            int nextHomePageToCheck = totalHomePagesLoaded + 1;
            int previousHomeCount = homePosts.Count;
            
            Debug.Log($"Checking for new home posts on page {nextHomePageToCheck}");
            yield return StartCoroutine(LoadHomeFeed(nextHomePageToCheck));
            
            if (homePosts.Count > previousHomeCount)
            {
                Debug.Log($"New home posts found ({homePosts.Count - previousHomeCount} new posts), switching to Home feed");
                isShowingHomeFeed = true;
                
                // CRITICAL: Continue from where we left off, not from 0
                currentHomePostIndex = previousHomeCount;
                Debug.Log($"Continuing home feed from index {currentHomePostIndex}");
            }
            else
            {
                // Continue with explore feed, load more if needed
                if (currentExplorePostIndex >= explorePosts.Count - 2 && hasMoreExplorePosts)
                {
                    Debug.Log($"Loading more explore posts - requesting page {currentExplorePage + 1}");
                    currentExplorePage++;
                    yield return StartCoroutine(LoadExploreFeed(currentExplorePage));
                }
            }
        }
    }
    
    private Post GetNextPost()
    {
        if (isShowingHomeFeed)
        {
            if (currentHomePostIndex < homePosts.Count)
            {
                return homePosts[currentHomePostIndex++];
            }
        }
        else
        {
            if (currentExplorePostIndex < explorePosts.Count)
            {
                return explorePosts[currentExplorePostIndex++];
            }
        }
        
        return null;
    }
    
    private bool HasMorePostsToShow()
    {
        if (isShowingHomeFeed)
        {
            return currentHomePostIndex < homePosts.Count || hasMoreHomePosts;
        }
        else
        {
            return currentExplorePostIndex < explorePosts.Count || hasMoreExplorePosts;
        }
    }
    
    private VisualElement CreateVerticalCard(Post post)
    {
        VisualElement verticalCard = verticalCardTemplate.CloneTree();
        
        // Set creator name with multiple fallback options
        string displayName = "Unknown User"; // Default fallback
        
        if (post.creator != null)
        {
            if (!string.IsNullOrEmpty(post.creator.firstName) && !string.IsNullOrEmpty(post.creator.lastName))
            {
                displayName = $"{post.creator.firstName} {post.creator.lastName}";
                Debug.Log($"Using firstName + lastName: {displayName}");
            }
            else if (!string.IsNullOrEmpty(post.creator.firstName))
            {
                displayName = post.creator.firstName;
                Debug.Log($"Using firstName only: {displayName}");
            }
            else if (!string.IsNullOrEmpty(post.creator.userName))
            {
                displayName = post.creator.userName;
                Debug.Log($"Using userName: {displayName}");
            }
            else if (!string.IsNullOrEmpty(post.creator.email))
            {
                displayName = post.creator.email.Split('@')[0]; // Use email prefix as fallback
                Debug.Log($"Using email prefix: {displayName}");
            }
        }
        
        Debug.Log($"Final displayName: '{displayName}'");
        verticalCard.Q<TextElement>("userName").text = displayName;
        
        // Set description (use caption if description is null)
        string description = !string.IsNullOrEmpty(post.description) ? post.description : post.caption;
        if (!string.IsNullOrEmpty(description) && description.Length > 90)
        {
            description = description.Substring(0, 90) + "...";
        }
        verticalCard.Q<TextElement>("description").text = description;
        
        // Set creator image
        Image userImage = verticalCard.Q<Image>("userImage");
        if (post.creator != null && !string.IsNullOrEmpty(post.creator.avatar))
        {
            StartCoroutine(LoadImageFromURL(post.creator.avatar, userImage));
        }
        else
        {
            userImage.image = LoadImage("user_placeholder");
        }
        
        // Handle single image only
        SetupSingleImage(verticalCard, post);
        
        // Setting the like icon for each post based on liked status
        Image likeIcon = verticalCard.Q<Image>("favourite");
        if (likeIcon != null)
        {
            // Set initial icon based on liked status
            if (post.liked)
            {
                likeIcon.image = LoadImage("heart-filled");
            }
            else
            {
                likeIcon.image = LoadImage("favourite");
            }
            
            likeIcon.pickingMode = PickingMode.Position;
            likeIcon.RegisterCallback<PointerUpEvent>(evt => 
            {
                StartCoroutine(LikePost(post.postId, likeIcon, post));
            });
            likeIcon.RegisterCallback<PointerEnterEvent>(evt => 
            {
                likeIcon.style.opacity = 0.7f;
            });
            likeIcon.RegisterCallback<PointerLeaveEvent>(evt => 
            {
                likeIcon.style.opacity = 1f;
            });
        }
        
        // Setting the bookmark icon for each post based on bookmarked status
        Image bookmarkIcon = verticalCard.Q<Image>("bookmark");
        if (bookmarkIcon != null)
        {
            // Set initial icon based on bookmarked status
            if (post.bookmarked)
            {
                bookmarkIcon.image = LoadImage("bookmark-filled");
            }
            else
            {
                bookmarkIcon.image = LoadImage("Bookmark");
            }
            
            bookmarkIcon.pickingMode = PickingMode.Position;
            bookmarkIcon.RegisterCallback<PointerUpEvent>(evt => 
            {
                StartCoroutine(BookmarkPost(post.postId, bookmarkIcon, post));
            });
            bookmarkIcon.RegisterCallback<PointerEnterEvent>(evt => 
            {
                bookmarkIcon.style.opacity = 0.7f;
            });
            bookmarkIcon.RegisterCallback<PointerLeaveEvent>(evt => 
            {
                bookmarkIcon.style.opacity = 1f;
            });
        }
        
        return verticalCard;
    }

    private void SetupSingleImage(VisualElement verticalCard, Post post)
    {
        Image cardImage = verticalCard.Q<Image>("card-image");
        string imageUrl = post.GetFirstImageUrl();
        cardImage.scaleMode = ScaleMode.StretchToFill;
    
        if (string.IsNullOrEmpty(imageUrl))
        {
            cardImage.image = LoadImage("Contemporary");
        }
        else
        {
            StartCoroutine(LoadImageFromURL(imageUrl, cardImage));
        }
    }
    
    private IEnumerator AddTrendingDesignersSection()
    {
        // Check if we have designers to show
        if (currentDesignerIndex >= trendingDesigners.Count)
        {
            // Try to load more designers if available
            if (hasMoreDesigners)
            {
                currentDesignerPage++;
                yield return StartCoroutine(LoadTrendingDesigners(currentDesignerPage));
            }
            
            // If still no designers to show, return
            if (currentDesignerIndex >= trendingDesigners.Count)
            {
                Debug.Log("No more trending designers to show");
                yield break;
            }
        }
        
        // Add heading for horizontal scroll section
        Label sectionHeading = new Label("Trending Designers");
        sectionHeading.style.fontSize = 45;
        sectionHeading.style.color = new StyleColor(Color.black);
        sectionHeading.style.marginBottom = 20;
        sectionHeading.style.marginLeft = 40;
        sectionHeading.style.unityFontStyleAndWeight = FontStyle.Bold;
        
        container.Add(sectionHeading);
        
        // Add horizontal scroll section
        ScrollView horizontalScroll = new ScrollView(ScrollViewMode.Horizontal);
        horizontalScroll.style.flexDirection = FlexDirection.Row;
        horizontalScroll.style.marginBottom = 100;
        horizontalScroll.style.paddingLeft = 30;
        
        // Add designer cards (show 10 at a time)
        int designersToShow = Mathf.Min(10, trendingDesigners.Count - currentDesignerIndex);
        int endIndex = currentDesignerIndex + designersToShow;
        
        for (int i = currentDesignerIndex; i < endIndex; i++)
        {
            User designerPost = trendingDesigners[i];
            Debug.Log($"[TRENDING DESIGNERS SECTION] Creating card for designer: {designerPost.userName} (Index: {i})");
            
            VisualElement horizontalCard = CreateHorizontalCard(designerPost);
            horizontalScroll.Add(horizontalCard);
        }
        
        // Update the current designer index
        currentDesignerIndex = endIndex;
        
        container.Add(horizontalScroll);
        
        Debug.Log($"Added {designersToShow} trending designers. Next index: {currentDesignerIndex}");
    }
    
    private VisualElement CreateHorizontalCard(User designerPost)
    {
        VisualElement horizontalCard = horizontalCardTemplate.CloneTree();
        string displayName = designerPost.userName;
        
        Debug.Log($"[HORIZONTAL CARD] Creating card for: {displayName}");
        
        horizontalCard.Q<Label>("userName").text = displayName;
        
        // Set designer image
        Image userImage = horizontalCard.Q<Image>("userImage");

        if (!string.IsNullOrEmpty(designerPost.avatar))
        {
            StartCoroutine(LoadImageFromURL(designerPost.avatar, userImage));
        }
        else
        {
            userImage.image = LoadImage("designer_placeholder");
        }
        
        // Set up follow button
        Label followText = horizontalCard.Q<Label>("followText");
        followText.text = "Follow";
        Button followButton = horizontalCard.Q<Button>("followButton");
    
        // Passing the userId and horizontalCard reference to the ToggleFollow function
        followButton.clicked += () => ToggleFollow(followText, designerPost.userId, horizontalCard);
        
        return horizontalCard;
    }
    
    private IEnumerator LoadImageFromURL(string url, Image targetImage)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                targetImage.image = texture;
            }
            else
            {
                Debug.LogError($"Failed to load image from {url}: {request.error}");
                // Keep placeholder image if loading fails
            }
        }
    }
    
    private void OnScroll(WheelEvent evt)
    {
        if (container.scrollOffset.y <= 0 && evt.delta.y < 0 && !isRefreshing)
        {
            HandlePullToRefresh(-evt.delta.y * 10f);
        }
    }
    
    private void OnPointerDown(PointerDownEvent evt)
    {
        if (container.scrollOffset.y <= 0 && !isRefreshing)
        {
            isPulling = true;
            pullDistance = 0f;
        }
    }
    
    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (isPulling && container.scrollOffset.y <= 0 && !isRefreshing)
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
        // Reset pagination
        currentHomePage = 1;
        currentExplorePage = 1;
        currentDesignerPage = 1;
        yield return StartCoroutine(LoadInitialData());
        yield return new WaitForSeconds(0.5f);
        ResetPullIndicator();
        isRefreshing = false;
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
    
    private void ClearContent()
    {
        for (int i = container.childCount - 1; i > 0; i--)
        {
            container.RemoveAt(i);
        }
    }
    
    private Texture2D LoadImage(string imageName)
    {
        return Resources.Load<Texture2D>(imageName);
    }
    
    private void ToggleFollow(Label followText, string userId, VisualElement horizontalCard)
    {
        Button followButton = followText.parent as Button;
    
        if (followText.text == "Follow")
        {
            // Immediately update UI to show "Following" state
            followText.text = "Following";
            followButton.style.backgroundColor = new StyleColor(new Color32(139, 76, 57, 255));
            followText.style.color = new StyleColor(Color.white);
        
            // Making API call
            StartCoroutine(FollowUser(userId, followText, followButton, horizontalCard));
        }
        else
        {
            // For unfollow action
            followText.text = "Follow";
            followButton.style.backgroundColor = StyleKeyword.Null;
            followText.style.color = StyleKeyword.Null;
        
            // Making API call
            StartCoroutine(FollowUser(userId, followText, followButton, horizontalCard));
        }
    }

    
    private void OnDisable()
    {
        // Unregister events to prevent memory leaks
        if (container != null)
        {
            container.UnregisterCallback<WheelEvent>(OnScroll);
            container.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            container.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            container.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }
    }
}