using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class HomeScreenController : MonoBehaviour
{
    [Header("Feed Settings")]
    public int homeCheckInterval = 10;

    private HomeDataHandler dataHandler;
    private HomeUIController uiController;
    
    private int cardsDisplayed = 0;
    private int currentHomePostIndex = 0;
    private int currentExplorePostIndex = 0;
    private int currentDesignerIndex = 0;
    private bool isShowingHomeFeed = false;
    private bool isLoadingData = false;
    
    private enum FeedType
    {
        Home,
        Explore
    }
    
    private void Awake()
    {
        dataHandler = GetComponent<HomeDataHandler>();
        if (dataHandler == null)
        {
            dataHandler = gameObject.AddComponent<HomeDataHandler>();
        }

        uiController = GetComponent<HomeUIController>();
        if (uiController == null)
        {
            uiController = gameObject.AddComponent<HomeUIController>();
        }
    }

    private void OnEnable()
    {
        uiController.OnRefreshRequested += HandleRefresh;

        // Check if we need to load data or use cached data
        if (ScreenStateManager.Instance.ShouldLoadData("Home"))
        {
            Debug.Log("[HomeScreen] Loading initial data (not cached or expired)");
            StartCoroutine(LoadInitialData());
        }
        else
        {
            Debug.Log("[HomeScreen] Using cached data, rebuilding UI");
            // Data is already in dataHandler from cache, just rebuild UI
            BuildInitialUI();
        }
    }

    private void OnDisable()
    {
        uiController.OnRefreshRequested -= HandleRefresh;
    }

    private void HandleRefresh()
    {
        Debug.Log("[HomeScreen] Manual refresh requested - invalidating cache");
        ScreenStateManager.Instance.InvalidateScreen("Home");
        StartCoroutine(RefreshContent());
    }

    private IEnumerator RefreshContent()
    {
        // Clear cache and reset pagination for fresh data
        DataCache.Instance.InvalidateScreen("Home");
        dataHandler.ResetPaginationData();
        yield return StartCoroutine(LoadInitialData());
        uiController.CompleteRefresh();
    }
    
    private IEnumerator LoadInitialData()
    {
        isLoadingData = true;

        // Only reset pagination if this is a fresh load (not using cache)
        if (!ScreenStateManager.Instance.IsScreenInitialized("Home"))
        {
            dataHandler.ResetPaginationData();
        }

        yield return StartCoroutine(dataHandler.LoadHomeFeed(1));
        yield return StartCoroutine(dataHandler.LoadExploreFeed(1));
        yield return StartCoroutine(dataHandler.LoadTrendingDesigners(1));

        // Mark screen as initialized after successful data load
        ScreenStateManager.Instance.MarkScreenInitialized("Home");

        BuildInitialUI();

        isLoadingData = false;
    }
    
    private void BuildInitialUI()
    {
        uiController.ClearContent();

        cardsDisplayed = 0;
        currentHomePostIndex = 0;
        currentExplorePostIndex = 0;
        currentDesignerIndex = 0;

        isShowingHomeFeed = dataHandler.HomePosts.Count > 0;

        // Check if we're using cached data (not first load)
        bool usingCache = ScreenStateManager.Instance.IsScreenInitialized("Home") &&
                         !ScreenStateManager.Instance.ShouldLoadData("Home", forceRefresh: false);

        if (usingCache)
        {
            Debug.Log("[HomeScreen] Using cached data - building all cards immediately");
            BuildAllCardsImmediately();
        }
        else
        {
            Debug.Log("[HomeScreen] Fresh load - building cards gradually");
            StartCoroutine(BuildUIGradually());
        }
    }
    
    private IEnumerator BuildUIGradually()
    {
        while (true)
        {
            if (cardsDisplayed > 0 && cardsDisplayed % homeCheckInterval == 0)
            {
                yield return StartCoroutine(CheckAndSwitchFeed());
            }
            
            Post postToShow = GetNextPost();
            
            if (postToShow != null)
            {
                VisualElement verticalCard = uiController.CreateVerticalCard(postToShow);
                uiController.AddToContainer(verticalCard);
                cardsDisplayed++;
                
                Debug.Log($"Added card {cardsDisplayed} from {(isShowingHomeFeed ? "Home" : "Explore")} feed");
                
                if (cardsDisplayed % 5 == 0)
                {
                    yield return StartCoroutine(AddTrendingDesignersSection());
                }
            }
            else
            {
                if (isShowingHomeFeed)
                {
                    if (!dataHandler.HasMoreHomePosts)
                    {
                        Debug.Log("HOME FEED EXHAUSTED - Forcing switch to Explore");
                        isShowingHomeFeed = false;
                        
                        if (dataHandler.ExplorePosts.Count == 0)
                        {
                            yield return StartCoroutine(dataHandler.LoadExploreFeed(1));
                        }
                        
                        continue;
                    }
                    else
                    {
                        yield return StartCoroutine(dataHandler.LoadHomeFeed(dataHandler.GetNextHomePage()));
                        continue;
                    }
                }
                else
                {
                    if (dataHandler.HasMoreExplorePosts)
                    {
                        yield return StartCoroutine(dataHandler.LoadExploreFeed(dataHandler.GetNextExplorePage()));
                        continue;
                    }
                    else
                    {
                        Debug.Log("Both Home and Explore feeds are exhausted");
                        break;
                    }
                }
            }
            
            yield return null;
        }
    }
    
    private IEnumerator CheckAndSwitchFeed()
    {
        if (isShowingHomeFeed)
        {
            if (currentHomePostIndex >= dataHandler.HomePosts.Count - 2 && dataHandler.HasMoreHomePosts)
            {
                yield return StartCoroutine(dataHandler.LoadHomeFeed(dataHandler.GetNextHomePage()));
            }
            
            if (currentHomePostIndex >= dataHandler.HomePosts.Count)
            {
                if (!dataHandler.HasMoreHomePosts)
                {
                    Debug.Log("HOME FEED EXHAUSTED - Switching to Explore Feed");
                    isShowingHomeFeed = false;
                    
                    if (dataHandler.ExplorePosts.Count == 0 || currentExplorePostIndex >= dataHandler.ExplorePosts.Count)
                    {
                        int nextPage = (dataHandler.ExplorePosts.Count == 0) ? 1 : dataHandler.GetNextExplorePage();
                        yield return StartCoroutine(dataHandler.LoadExploreFeed(nextPage));
                    }
                }
                else
                {
                    yield return StartCoroutine(dataHandler.LoadHomeFeed(dataHandler.GetNextHomePage()));
                }
            }
        }
        else
        {
            Debug.Log("CHECKING for new Home posts while showing Explore feed...");
            
            int previousHomeCount = dataHandler.HomePosts.Count;
            
            yield return StartCoroutine(dataHandler.LoadHomeFeed(1));
            
            if (dataHandler.HomePosts.Count > previousHomeCount)
            {
                Debug.Log($"NEW HOME POSTS FOUND! Previous: {previousHomeCount}, Now: {dataHandler.HomePosts.Count}");
                isShowingHomeFeed = true;
                currentHomePostIndex = 0;
            }
            else
            {
                if (currentExplorePostIndex >= dataHandler.ExplorePosts.Count - 2 && dataHandler.HasMoreExplorePosts)
                {
                    yield return StartCoroutine(dataHandler.LoadExploreFeed(dataHandler.GetNextExplorePage()));
                }
            }
        }
    }
    
    private Post GetNextPost()
    {
        if (isShowingHomeFeed)
        {
            if (currentHomePostIndex < dataHandler.HomePosts.Count)
            {
                Debug.Log($"Showing HOME post {currentHomePostIndex + 1}/{dataHandler.HomePosts.Count}");
                return dataHandler.HomePosts[currentHomePostIndex++];
            }
            else
            {
                Debug.Log("No more HOME posts available, returning null");
                return null;
            }
        }
        else
        {
            if (currentExplorePostIndex < dataHandler.ExplorePosts.Count)
            {
                Debug.Log($"Showing EXPLORE post {currentExplorePostIndex + 1}/{dataHandler.ExplorePosts.Count}");
                return dataHandler.ExplorePosts[currentExplorePostIndex++];
            }
            else
            {
                Debug.Log("No more EXPLORE posts available, returning null");
                return null;
            }
        }
    }
    
    private IEnumerator AddTrendingDesignersSection()
    {
        if (currentDesignerIndex >= dataHandler.TrendingDesigners.Count)
        {
            if (dataHandler.HasMoreDesigners)
            {
                yield return StartCoroutine(dataHandler.LoadTrendingDesigners(dataHandler.GetNextDesignerPage()));
            }

            if (currentDesignerIndex >= dataHandler.TrendingDesigners.Count)
            {
                yield break;
            }
        }

        ScrollView horizontalScroll = uiController.CreateTrendingDesignersSection(
            dataHandler.TrendingDesigners,
            currentDesignerIndex,
            10
        );

        int designersToShow = Mathf.Min(10, dataHandler.TrendingDesigners.Count - currentDesignerIndex);
        currentDesignerIndex += designersToShow;

        uiController.AddToContainer(horizontalScroll);
    }

    private void BuildAllCardsImmediately()
    {
        Debug.Log("[HomeScreen] Building all cards immediately from cache");

        int totalPostsToShow = dataHandler.HomePosts.Count + dataHandler.ExplorePosts.Count;
        int designerSectionsNeeded = Mathf.CeilToInt(totalPostsToShow / 5f);

        // Build all cards at once
        while (true)
        {
            // Add designer section every 5 cards
            if (cardsDisplayed > 0 && cardsDisplayed % 5 == 0 && currentDesignerIndex < dataHandler.TrendingDesigners.Count)
            {
                int designersToShow = Mathf.Min(10, dataHandler.TrendingDesigners.Count - currentDesignerIndex);
                if (designersToShow > 0)
                {
                    ScrollView horizontalScroll = uiController.CreateTrendingDesignersSection(
                        dataHandler.TrendingDesigners,
                        currentDesignerIndex,
                        designersToShow
                    );
                    currentDesignerIndex += designersToShow;
                    uiController.AddToContainer(horizontalScroll);
                }
            }

            Post postToShow = GetNextPost();

            if (postToShow != null)
            {
                VisualElement verticalCard = uiController.CreateVerticalCard(postToShow);
                uiController.AddToContainer(verticalCard);
                cardsDisplayed++;
            }
            else
            {
                // No more posts available
                break;
            }
        }

        Debug.Log($"[HomeScreen] Built {cardsDisplayed} cards immediately");

        // Restore scroll position after all cards are built
        uiController.RestoreScrollPosition();
    }
    
    public void StartUploadWithService(UploadService.UploadData uploadData)
    {
        uiController.ShowUploadProgress();
        UploadService.Instance.StartUpload(
            uploadData,
            OnUploadProgress,
            OnUploadComplete   
        );
    }

    private void OnUploadProgress(float progress)
    {
        uiController.UpdateUploadProgress(progress);
    }

    private void OnUploadComplete(bool success, string message)
    {
        uiController.SetUploadComplete(success);

        if (success)
        {
            // Invalidate cache to show new upload
            Debug.Log("[HomeScreen] Upload complete - refreshing data");
            ScreenStateManager.Instance.InvalidateScreen("Home");
            DataCache.Instance.InvalidateScreen("Home");
            StartCoroutine(LoadInitialData());
        }
    }
}