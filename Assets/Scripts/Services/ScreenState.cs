using System;
using UnityEngine;

[System.Serializable]
public class ScreenState
{
    public string screenName;
    public bool isInitialized;
    public DateTime lastFetchTime;
    public Vector2 scrollPosition;
    public string selectedTag;
    public int selectedTagId;
    public string selectedFilter;
    public FilterData filterData;

    public ScreenState(string name)
    {
        screenName = name;
        isInitialized = false;
        lastFetchTime = DateTime.MinValue;
        scrollPosition = Vector2.zero;
        selectedTag = string.Empty;
        selectedTagId = -1;
        selectedFilter = string.Empty;
        filterData = null;
    }

    public bool ShouldRefresh(float cacheExpirationMinutes = 5f)
    {
        if (!isInitialized)
            return true;

        TimeSpan timeSinceLastFetch = DateTime.Now - lastFetchTime;
        return timeSinceLastFetch.TotalMinutes >= cacheExpirationMinutes;
    }

    public void MarkAsInitialized()
    {
        isInitialized = true;
        lastFetchTime = DateTime.Now;
    }

    public void UpdateFetchTime()
    {
        lastFetchTime = DateTime.Now;
    }

    public void Invalidate()
    {
        isInitialized = false;
        lastFetchTime = DateTime.MinValue;
    }
}
