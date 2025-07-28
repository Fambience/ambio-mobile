using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HomeScreenFeedManager : MonoBehaviour
{
    [Header("Feed Settings")]
    public int homeCheckInterval = 10; // Check home API every 10 cards

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
    private int currentDesignerIndex = 0;
    private bool isShowingHomeFeed = false;
    private bool hasMoreHomePosts = true;
    private bool hasMoreExplorePosts = true;
    private bool hasMoreDesigners = true;
    
    // Track total pages loaded to prevent duplicates
    private int totalHomePagesLoaded = 0;
    private int totalExplorePagesLoaded = 0;
    private int totalDesignerPagesLoaded = 0;

    public static HomeScreenFeedManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public IEnumerator LoadInitialData()
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
    
        isLoadingData = false;
    }

    private IEnumerator LoadHomeFeed(int page)
    {
        yield return StartCoroutine(HomeScreenAPIManager.Instance.LoadHomeFeed(page, (response) =>
        {
            if (response != null && response.success && response.data != null && response.data.posts != null)
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
                    homePosts.AddRange(response.data.posts);
                    totalHomePagesLoaded = page;
                    hasMoreHomePosts = response.data.posts.Count >= HomeScreenAPIManager.Instance.postsPerPage;
                    
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
                Debug.LogWarning($"NO DATA: {(response?.message ?? "Response is null")}");
                hasMoreHomePosts = false;
            }
        }));
    }

    private IEnumerator LoadExploreFeed(int page)
    {
        yield return StartCoroutine(HomeScreenAPIManager.Instance.LoadExploreFeed(page, (response) =>
        {
            if (response != null && response.success && response.data != null)
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
                    explorePosts.AddRange(response.data);
                    totalExplorePagesLoaded = page;
                    hasMoreExplorePosts = response.data.Count >= HomeScreenAPIManager.Instance.postsPerPage;
                    
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
                Debug.LogError($"NO DATA: {(response?.message ?? "Response is null")}");
                hasMoreExplorePosts = false;
            }
        }));
    }

    private IEnumerator LoadTrendingDesigners(int page)
    {
        yield return StartCoroutine(HomeScreenAPIManager.Instance.LoadTrendingDesigners(page, (response) =>
        {
            if (response != null && response.success && response.data != null)
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
                    hasMoreDesigners = response.data.Count >= HomeScreenAPIManager.Instance.designersPerPage;
                    
                    Debug.Log($"Loaded {response.data.Count} trending designers from page {page}. Total: {trendingDesigners.Count}");
                }
                else
                {
                    Debug.Log($"Designer page {page} already loaded, skipping");
                }
            }
            else
            {
                Debug.LogError($"Trending Designers API Error: {(response?.message ?? "Response is null")}");
                hasMoreDesigners = false;
            }
        }));
    }

    public IEnumerator CheckAndSwitchFeed()
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
                
                // Don't reset explore index, continue where we left off
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
                
                // Continue from where we left off, not from 0
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

    public Post GetNextPost()
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

    public bool HasMorePostsToShow()
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

    public bool ShouldCheckFeed()
    {
        return cardsDisplayed > 0 && cardsDisplayed % homeCheckInterval == 0;
    }

    public void IncrementCardsDisplayed()
    {
        cardsDisplayed++;
    }

    public void ResetFeedCounters()
    {
        cardsDisplayed = 0;
        currentHomePostIndex = 0;
        currentExplorePostIndex = 0;
        currentDesignerIndex = 0;
        isShowingHomeFeed = homePosts.Count > 0;
    }

    public List<User> GetTrendingDesigners(int count = 10)
    {
        List<User> designersToReturn = new List<User>();
        int endIndex = Mathf.Min(currentDesignerIndex + count, trendingDesigners.Count);
        
        for (int i = currentDesignerIndex; i < endIndex; i++)
        {
            designersToReturn.Add(trendingDesigners[i]);
        }
        
        currentDesignerIndex = endIndex;
        return designersToReturn;
    }

    public bool HasMoreDesigners()
    {
        return currentDesignerIndex < trendingDesigners.Count || hasMoreDesigners;
    }

    public IEnumerator LoadMoreDesigners()
    {
        if (hasMoreDesigners)
        {
            currentDesignerPage++;
            yield return StartCoroutine(LoadTrendingDesigners(currentDesignerPage));
        }
    }

    public bool IsLoadingData()
    {
        return isLoadingData;
    }
}