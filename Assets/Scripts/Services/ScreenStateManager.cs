using System.Collections.Generic;
using UnityEngine;

public class ScreenStateManager
{
    private static ScreenStateManager instance;
    public static ScreenStateManager Instance
    {
        get
        {
            if (instance == null)
                instance = new ScreenStateManager();
            return instance;
        }
    }

    private Dictionary<string, ScreenState> screenStates;
    public float defaultCacheExpirationMinutes = 5f;

    private ScreenStateManager()
    {
        screenStates = new Dictionary<string, ScreenState>();
    }

    #region Screen State Management

    public ScreenState GetScreenState(string screenName)
    {
        if (!screenStates.ContainsKey(screenName))
        {
            screenStates[screenName] = new ScreenState(screenName);
        }
        return screenStates[screenName];
    }

    public bool ShouldLoadData(string screenName, bool forceRefresh = false)
    {
        if (forceRefresh)
        {
            Debug.Log($"[ScreenStateManager] Force refresh requested for {screenName}");
            return true;
        }

        ScreenState state = GetScreenState(screenName);
        bool shouldLoad = state.ShouldRefresh(defaultCacheExpirationMinutes);

        Debug.Log($"[ScreenStateManager] {screenName} - ShouldLoad: {shouldLoad} (Initialized: {state.isInitialized}, Age: {(System.DateTime.Now - state.lastFetchTime).TotalMinutes:F2} min)");

        return shouldLoad;
    }

    public void MarkScreenInitialized(string screenName)
    {
        ScreenState state = GetScreenState(screenName);
        state.MarkAsInitialized();
        Debug.Log($"[ScreenStateManager] {screenName} marked as initialized");
    }

    public void UpdateScreenFetchTime(string screenName)
    {
        ScreenState state = GetScreenState(screenName);
        state.UpdateFetchTime();
        Debug.Log($"[ScreenStateManager] {screenName} fetch time updated");
    }

    public void InvalidateScreen(string screenName)
    {
        ScreenState state = GetScreenState(screenName);
        state.Invalidate();
        DataCache.Instance.InvalidateScreen(screenName);
        Debug.Log($"[ScreenStateManager] {screenName} invalidated");
    }

    public void InvalidateAllScreens()
    {
        foreach (var state in screenStates.Values)
        {
            state.Invalidate();
        }
        DataCache.Instance.InvalidateAllCaches();
        Debug.Log("[ScreenStateManager] All screens invalidated");
    }

    #endregion

    #region Scroll Position Management

    public void SaveScrollPosition(string screenName, Vector2 scrollPosition)
    {
        ScreenState state = GetScreenState(screenName);
        state.scrollPosition = scrollPosition;
        Debug.Log($"[ScreenStateManager] Saved scroll position for {screenName}: {scrollPosition}");
    }

    public Vector2 GetScrollPosition(string screenName)
    {
        ScreenState state = GetScreenState(screenName);
        Debug.Log($"[ScreenStateManager] Retrieved scroll position for {screenName}: {state.scrollPosition}");
        return state.scrollPosition;
    }

    public void ResetScrollPosition(string screenName)
    {
        ScreenState state = GetScreenState(screenName);
        state.scrollPosition = Vector2.zero;
        Debug.Log($"[ScreenStateManager] Reset scroll position for {screenName}");
    }

    #endregion

    #region Filter and Tag State Management

    public void SaveSelectedTag(string screenName, string tagType, int tagId)
    {
        ScreenState state = GetScreenState(screenName);
        state.selectedTag = tagType;
        state.selectedTagId = tagId;
        Debug.Log($"[ScreenStateManager] Saved tag for {screenName}: {tagType} (ID: {tagId})");
    }

    public (string tagType, int tagId) GetSelectedTag(string screenName)
    {
        ScreenState state = GetScreenState(screenName);
        return (state.selectedTag, state.selectedTagId);
    }

    public void SaveFilterData(string screenName, FilterData filterData)
    {
        ScreenState state = GetScreenState(screenName);
        state.filterData = filterData;
        Debug.Log($"[ScreenStateManager] Saved filter data for {screenName}");
    }

    public FilterData GetFilterData(string screenName)
    {
        ScreenState state = GetScreenState(screenName);
        return state.filterData;
    }

    #endregion

    #region Home Feed Specific Methods

    public void SaveHomeFeedState(int cardsDisplayed, int currentHomePostIndex, int currentExplorePostIndex, int currentDesignerIndex, bool isShowingHomeFeed)
    {
        ScreenState state = GetScreenState("Home");
        // Store in a custom way - we could extend ScreenState to hold these, or use a separate dictionary
        // For now, we'll just track the basic state
        state.MarkAsInitialized();
        Debug.Log($"[ScreenStateManager] Saved Home feed state");
    }

    #endregion

    #region Utility Methods

    public bool IsScreenInitialized(string screenName)
    {
        ScreenState state = GetScreenState(screenName);
        return state.isInitialized;
    }

    public void ResetScreen(string screenName)
    {
        if (screenStates.ContainsKey(screenName))
        {
            screenStates[screenName] = new ScreenState(screenName);
            DataCache.Instance.InvalidateScreen(screenName);
            Debug.Log($"[ScreenStateManager] {screenName} completely reset");
        }
    }

    #endregion
}
