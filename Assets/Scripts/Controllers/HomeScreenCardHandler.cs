using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class HomeScreenController : MonoBehaviour
{
    private ScrollView container;
    private HomeScreenPullToRefresh pullToRefreshHandler;
    
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        container = root.Q<ScrollView>("main-container");
        
        // Initialize pull to refresh
        pullToRefreshHandler = gameObject.AddComponent<HomeScreenPullToRefresh>();
        pullToRefreshHandler.Initialize(container);
        pullToRefreshHandler.OnRefreshRequested += HandleRefresh;
        
        StartCoroutine(LoadInitialData());
    }
    
    private IEnumerator LoadInitialData()
    {
        yield return StartCoroutine(HomeScreenFeedManager.Instance.LoadInitialData());
        BuildInitialUI();
    }
    
    private void HandleRefresh()
    {
        StartCoroutine(RefreshContent());
    }
    
    private IEnumerator RefreshContent()
    {
        yield return StartCoroutine(HomeScreenFeedManager.Instance.LoadInitialData());
        BuildInitialUI();
    }
    
    private void BuildInitialUI()
    {
        ClearContent();
        HomeScreenFeedManager.Instance.ResetFeedCounters();
        
        Debug.Log("Building initial UI");
        
        // Start building UI
        StartCoroutine(BuildUIGradually());
    }
    
    private IEnumerator BuildUIGradually()
    {
        while (HomeScreenFeedManager.Instance.HasMorePostsToShow())
        {
            // Add posts in groups
            int groupSize = 5;
            int postsAddedInGroup = 0;
            
            for (int i = 0; i < groupSize && HomeScreenFeedManager.Instance.HasMorePostsToShow(); i++)
            {
                // Check if we need to switch feeds or load more data
                if (HomeScreenFeedManager.Instance.ShouldCheckFeed())
                {
                    yield return StartCoroutine(HomeScreenFeedManager.Instance.CheckAndSwitchFeed());
                }
                
                Post postToShow = HomeScreenFeedManager.Instance.GetNextPost();
                if (postToShow != null)
                {
                    VisualElement verticalCard = HomeScreenCardCreator.Instance.CreateVerticalCard(postToShow);
                    container.Add(verticalCard);
                    HomeScreenFeedManager.Instance.IncrementCardsDisplayed();
                    postsAddedInGroup++;
                    
                    Debug.Log($"Added card from feed");
                }
                else
                {
                    break; // No more posts available
                }
            }
            
            // Add trending designers section after every 5 cards
            if (postsAddedInGroup > 0)
            {
                yield return StartCoroutine(AddTrendingDesignersSection());
            }
            
            yield return null; // Allow UI to update
        }
    }
    
    private IEnumerator AddTrendingDesignersSection()
    {
        // Check if we have designers to show
        if (!HomeScreenFeedManager.Instance.HasMoreDesigners())
        {
            yield return StartCoroutine(HomeScreenFeedManager.Instance.LoadMoreDesigners());
        }
        
        List<User> designers = HomeScreenFeedManager.Instance.GetTrendingDesigners(10);
        
        if (designers.Count == 0)
        {
            Debug.Log("No more trending designers to show");
            yield break;
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
        
        // Add designer cards
        foreach (User designer in designers)
        {
            Debug.Log($"[TRENDING DESIGNERS SECTION] Creating card for designer: {designer.userName}");
            
            VisualElement horizontalCard = HomeScreenCardCreator.Instance.CreateHorizontalCard(designer);
            horizontalScroll.Add(horizontalCard);
        }
        
        container.Add(horizontalScroll);
        
        Debug.Log($"Added {designers.Count} trending designers");
    }
    
    private void ClearContent()
    {
        // Keep the refresh indicator (index 0) and remove everything else
        for (int i = container.childCount - 1; i > 0; i--)
        {
            container.RemoveAt(i);
        }
    }
    
    private void OnDisable()
    {
        if (pullToRefreshHandler != null)
        {
            pullToRefreshHandler.OnRefreshRequested -= HandleRefresh;
        }
    }
}