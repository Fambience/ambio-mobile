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
        StartCoroutine(LoadInitialData());
    }

    private void OnDisable()
    {
        uiController.OnRefreshRequested -= HandleRefresh;
    }

    private void HandleRefresh()
    {
        StartCoroutine(RefreshContent());
    }

    private IEnumerator RefreshContent()
    {
        dataHandler.ResetPaginationData();
        yield return StartCoroutine(LoadInitialData());
        uiController.CompleteRefresh();
    }
    
    private IEnumerator LoadInitialData()
    {
        isLoadingData = true;

        dataHandler.ResetPaginationData();

        yield return StartCoroutine(dataHandler.LoadHomeFeed(1));
        yield return StartCoroutine(dataHandler.LoadExploreFeed(1));
        yield return StartCoroutine(dataHandler.LoadTrendingDesigners(1));

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
        
        StartCoroutine(BuildUIGradually());
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
            StartCoroutine(LoadInitialData());
        }
    }
}