using System;
using System.Collections.Generic;
using UnityEngine;

public class DataCache
{
    private static DataCache instance;
    public static DataCache Instance
    {
        get
        {
            if (instance == null)
                instance = new DataCache();
            return instance;
        }
    }

    private Dictionary<string, CachedData<List<Post>>> postListCache;
    private Dictionary<string, CachedData<List<User>>> userListCache;
    private Dictionary<string, CachedData<List<PostData>>> postDataListCache;

    public float defaultCacheExpirationMinutes = 5f;

    private DataCache()
    {
        postListCache = new Dictionary<string, CachedData<List<Post>>>();
        userListCache = new Dictionary<string, CachedData<List<User>>>();
        postDataListCache = new Dictionary<string, CachedData<List<PostData>>>();
    }

    #region Post Cache Methods

    public void CachePostList(string key, List<Post> data, float? expirationMinutes = null)
    {
        float expiration = expirationMinutes ?? defaultCacheExpirationMinutes;
        postListCache[key] = new CachedData<List<Post>>(data, expiration);
        Debug.Log($"[DataCache] Cached {data.Count} posts with key: {key}");
    }

    public List<Post> GetCachedPostList(string key)
    {
        if (postListCache.TryGetValue(key, out CachedData<List<Post>> cachedData))
        {
            if (!cachedData.IsExpired())
            {
                Debug.Log($"[DataCache] Cache HIT for key: {key} ({cachedData.data.Count} posts)");
                return cachedData.data;
            }
            else
            {
                Debug.Log($"[DataCache] Cache EXPIRED for key: {key}");
                postListCache.Remove(key);
            }
        }
        else
        {
            Debug.Log($"[DataCache] Cache MISS for key: {key}");
        }
        return null;
    }

    public void InvalidatePostList(string key)
    {
        if (postListCache.ContainsKey(key))
        {
            postListCache.Remove(key);
            Debug.Log($"[DataCache] Invalidated post cache: {key}");
        }
    }

    #endregion

    #region User Cache Methods

    public void CacheUserList(string key, List<User> data, float? expirationMinutes = null)
    {
        float expiration = expirationMinutes ?? defaultCacheExpirationMinutes;
        userListCache[key] = new CachedData<List<User>>(data, expiration);
        Debug.Log($"[DataCache] Cached {data.Count} users with key: {key}");
    }

    public List<User> GetCachedUserList(string key)
    {
        if (userListCache.TryGetValue(key, out CachedData<List<User>> cachedData))
        {
            if (!cachedData.IsExpired())
            {
                Debug.Log($"[DataCache] Cache HIT for key: {key} ({cachedData.data.Count} users)");
                return cachedData.data;
            }
            else
            {
                Debug.Log($"[DataCache] Cache EXPIRED for key: {key}");
                userListCache.Remove(key);
            }
        }
        else
        {
            Debug.Log($"[DataCache] Cache MISS for key: {key}");
        }
        return null;
    }

    public void InvalidateUserList(string key)
    {
        if (userListCache.ContainsKey(key))
        {
            userListCache.Remove(key);
            Debug.Log($"[DataCache] Invalidated user cache: {key}");
        }
    }

    #endregion

    #region PostData Cache Methods

    public void CachePostDataList(string key, List<PostData> data, float? expirationMinutes = null)
    {
        float expiration = expirationMinutes ?? defaultCacheExpirationMinutes;
        postDataListCache[key] = new CachedData<List<PostData>>(data, expiration);
        Debug.Log($"[DataCache] Cached {data.Count} PostData items with key: {key}");
    }

    public List<PostData> GetCachedPostDataList(string key)
    {
        if (postDataListCache.TryGetValue(key, out CachedData<List<PostData>> cachedData))
        {
            if (!cachedData.IsExpired())
            {
                Debug.Log($"[DataCache] Cache HIT for key: {key} ({cachedData.data.Count} PostData items)");
                return cachedData.data;
            }
            else
            {
                Debug.Log($"[DataCache] Cache EXPIRED for key: {key}");
                postDataListCache.Remove(key);
            }
        }
        else
        {
            Debug.Log($"[DataCache] Cache MISS for key: {key}");
        }
        return null;
    }

    public void InvalidatePostDataList(string key)
    {
        if (postDataListCache.ContainsKey(key))
        {
            postDataListCache.Remove(key);
            Debug.Log($"[DataCache] Invalidated PostData cache: {key}");
        }
    }

    #endregion

    #region General Cache Methods

    public void InvalidateAllCaches()
    {
        postListCache.Clear();
        userListCache.Clear();
        postDataListCache.Clear();
        Debug.Log("[DataCache] All caches invalidated");
    }

    public void InvalidateScreen(string screenName)
    {
        // Invalidate all caches related to a specific screen
        var keysToRemove = new List<string>();

        foreach (var key in postListCache.Keys)
        {
            if (key.StartsWith(screenName))
                keysToRemove.Add(key);
        }
        foreach (var key in keysToRemove)
        {
            postListCache.Remove(key);
        }

        keysToRemove.Clear();
        foreach (var key in userListCache.Keys)
        {
            if (key.StartsWith(screenName))
                keysToRemove.Add(key);
        }
        foreach (var key in keysToRemove)
        {
            userListCache.Remove(key);
        }

        keysToRemove.Clear();
        foreach (var key in postDataListCache.Keys)
        {
            if (key.StartsWith(screenName))
                keysToRemove.Add(key);
        }
        foreach (var key in keysToRemove)
        {
            postDataListCache.Remove(key);
        }

        Debug.Log($"[DataCache] Invalidated all caches for screen: {screenName}");
    }

    #endregion
}

[System.Serializable]
public class CachedData<T>
{
    public T data;
    public DateTime cacheTime;
    public float expirationMinutes;

    public CachedData(T data, float expirationMinutes)
    {
        this.data = data;
        this.cacheTime = DateTime.Now;
        this.expirationMinutes = expirationMinutes;
    }

    public bool IsExpired()
    {
        TimeSpan timeSinceCache = DateTime.Now - cacheTime;
        return timeSinceCache.TotalMinutes >= expirationMinutes;
    }

    public float GetAge()
    {
        TimeSpan age = DateTime.Now - cacheTime;
        return (float)age.TotalMinutes;
    }
}
