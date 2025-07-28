using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

public class HomeScreenAPIManager : MonoBehaviour
{
    [Header("API Settings")]
    public string baseURL;
    public string exploreFeedUrl = "/api/v1/post/explore-feed";
    public string homeFeedUrl = "/api/v1/post/home";
    public string trendingDesignersUrl = "/api/v1/post/trending-designers";
    private string authToken;

    public int postsPerPage = 10;
    public int designersPerPage = 10;

    public static HomeScreenAPIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        baseURL = baseScript.baseURL;
        authToken = AuthTokenManager.GetToken();
    }

    public IEnumerator LoadHomeFeed(int page, System.Action<ApiResponse<HomeApiResponse>> callback)
    {
        string url = $"{baseURL}{homeFeedUrl}?page={page}&limit={postsPerPage}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", authToken);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                try
                {
                    ApiResponse<HomeApiResponse> response = JsonUtility.FromJson<ApiResponse<HomeApiResponse>>(jsonResponse);
                    callback?.Invoke(response);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Parse Error: {e.Message}");
                    callback?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"Network Error: {request.error}");
                callback?.Invoke(null);
            }
        }
    }

    public IEnumerator LoadExploreFeed(int page, System.Action<ApiResponse<List<Post>>> callback)
    {
        string url = $"{baseURL}{exploreFeedUrl}?page={page}&limit={postsPerPage}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", authToken);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                
                try
                {
                    ApiResponse<List<Post>> response = JsonUtility.FromJson<ApiResponse<List<Post>>>(jsonResponse);
                    callback?.Invoke(response);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Parse Error: {e.Message}");
                    callback?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"Network Error: {request.error}");
                callback?.Invoke(null);
            }
        }
    }

    public IEnumerator LoadTrendingDesigners(int page, System.Action<ApiResponse<List<User>>> callback)
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
                    callback?.Invoke(response);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing trending designers response: {e.Message}");
                    callback?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"Trending Designers Network Error: {request.error}");
                callback?.Invoke(null);
            }
        }
    }

    public IEnumerator LikePost(string postId, System.Action<LikeResponse> callback)
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
                        callback?.Invoke(response.data);
                        Debug.Log($"Like action successful: {response.data.message}");
                    }
                    else
                    {
                        Debug.LogError($"Like API Error: {response.message}");
                        callback?.Invoke(null);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing like response: {e.Message}");
                    callback?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"Like Network Error: {request.error}");
                callback?.Invoke(null);
            }
        }
    }

    public IEnumerator BookmarkPost(string postId, System.Action<BookmarkResponse> callback)
    {
        string url = $"{baseURL}/api/v1/post/bookmark/like/{postId}";
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
                        callback?.Invoke(response.data);
                        Debug.Log($"Bookmark action successful: {response.data.message}");
                    }
                    else
                    {
                        Debug.LogError($"Bookmark API Error: {response.message}");
                        callback?.Invoke(null);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing bookmark response: {e.Message}");
                    callback?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"Bookmark Network Error: {request.error}");
                callback?.Invoke(null);
            }
        }
    }

    public IEnumerator FollowUser(string userId, System.Action<FollowResponse> callback)
    {
        string url = $"{baseURL}/api/v1/profile/toggle-follow/{userId}";
        
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
                    callback?.Invoke(response);
                    Debug.Log($"Follow action successful: {response.message}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing follow response: {e.Message}");
                    callback?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"Follow Network Error: {request.error}");
                callback?.Invoke(null);
            }
        }
    }

    public IEnumerator LoadImageFromURL(string url, System.Action<Texture2D> callback)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                callback?.Invoke(texture);
            }
            else
            {
                Debug.LogError($"Failed to load image from {url}: {request.error}");
                callback?.Invoke(null);
            }
        }
    }
}