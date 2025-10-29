using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

public class ChatAPIService : MonoBehaviour
{
    private string TokenEndpointURL = $"{baseScript.baseURL}/api/v1/chat/stream-token";

    private List<FollowingUserData> cachedFollowingList;
    private DateTime followingListCacheTime;
    private const int FOLLOWING_CACHE_DURATION_MINUTES = 5;

    [System.Serializable]
    private class TokenResponseWrapper
    {
        public TokenData data;
    }

    [System.Serializable]
    private class TokenData
    {
        public string streamToken;
        public string userId;
        public string userName;
    }

    [System.Serializable]
    public class FollowingApiResponse
    {
        public bool success;
        public string message;
        public FollowingUserData[] data;
        public PaginationInfo pagination;
    }

    [System.Serializable]
    public class FollowingUserData
    {
        public string userId;
        public string firstName;
        public string lastName;
        public string userName;
        public string avatar;
    }

    [System.Serializable]
    public class PaginationInfo
    {
        public int total;
        public int page;
        public int limit;
        public int totalPages;
    }

    public async Task<(string token, string userId)> GetStreamTokenFromServerAsync()
    {
        var fullUrl = TokenEndpointURL;
        Debug.Log($"Attempting to fetch token from: {fullUrl}");

        using (var request = UnityWebRequest.Get(TokenEndpointURL))
        {
            request.SetRequestHeader("Authorization", AuthTokenManager.GetToken());

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching Stream token: {request.error}");
                return (null, null);
            }

            var jsonResponse = request.downloadHandler.text;
            Debug.Log($"Server response: {jsonResponse}");

            var tokenData = JsonUtility.FromJson<TokenResponseWrapper>(jsonResponse).data;

            return (tokenData.streamToken, tokenData.userId);
        }
    }

    // Gets following list with caching - returns cached data if fresh, otherwise fetches from API
    public async Task<List<FollowingUserData>> GetFollowingListAsync(string username, int page = 1, int limit = 20)
    {
        if (IsCacheValid())
        {
            Debug.Log("Returning cached following list");
            return cachedFollowingList;
        }

        return await FetchFollowingListFromAPIAsync(username, page, limit);
    }

    // Forces refresh of following list, ignoring cache
    public async Task<List<FollowingUserData>> ForceRefreshFollowingListAsync(string username, int page = 1, int limit = 20)
    {
        InvalidateFollowingCache();
        return await FetchFollowingListFromAPIAsync(username, page, limit);
    }

    public void InvalidateFollowingCache()
    {
        cachedFollowingList = null;
        followingListCacheTime = DateTime.MinValue;
    }

    private bool IsCacheValid()
    {
        if (cachedFollowingList == null) return false;

        var cacheAge = DateTime.Now - followingListCacheTime;
        return cacheAge.TotalMinutes < FOLLOWING_CACHE_DURATION_MINUTES;
    }

    private async Task<List<FollowingUserData>> FetchFollowingListFromAPIAsync(string username, int page = 1, int limit = 20)
    {
        var url = $"{baseScript.baseURL}/api/v1/profile/{username}/following?page={page}&limit={limit}";
        Debug.Log($"Fetching following list from API: {url}");

        using (var request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", AuthTokenManager.GetToken());
            request.SetRequestHeader("accept", "application/json, text/plain, */*");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching following list: {request.error}");
                return cachedFollowingList ?? new List<FollowingUserData>();
            }

            var jsonResponse = request.downloadHandler.text;
            Debug.Log($"Following list response: {jsonResponse}");

            var response = JsonUtility.FromJson<FollowingApiResponse>(jsonResponse);

            if (response.success && response.data != null)
            {
                cachedFollowingList = new List<FollowingUserData>(response.data);
                followingListCacheTime = DateTime.Now;
                return cachedFollowingList;
            }

            return cachedFollowingList ?? new List<FollowingUserData>();
        }
    }
}