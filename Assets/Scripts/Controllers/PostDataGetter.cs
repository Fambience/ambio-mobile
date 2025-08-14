using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

public static class PostDataGetter
{
    // public static List<PostData> GenerateDummyPosts(int totalPosts)
    // {
    //     List<PostData> posts = new List<PostData>();
    //
    //     // Load the minimalist image from Resources
    //     Texture2D minimalistImage = Resources.Load<Texture2D>("minimalist");
    //
    //     if (minimalistImage == null)
    //     {
    //         Debug.LogWarning("Minimalist image not found in Resources folder. Creating posts without images for testing.");
    //     }
    //     else
    //     {
    //         Debug.Log("Minimalist image loaded successfully");
    //     }
    //
    //     // Create dummy posts using the minimalist image
    //     for (int i = 0; i < totalPosts; i++)
    //     {
    //         PostData post = new PostData(
    //             minimalistImage,
    //             $"designer_{i + 1}",
    //             $"Minimalist Design {i + 1}"
    //         );
    //
    //         posts.Add(post);
    //     }
    //
    //     Debug.Log($"Generated {posts.Count} dummy posts");
    //     return posts;
    // }
    
    [Serializable]
    private class TrendingFeedResponse
    {
        public bool success;
        public string message;
        public TrendingFeedData data;
    }

    [Serializable]
    private class TrendingFeedData
    {
        public TrendingPost[] posts;
        public string nextCursor;
    }

    [Serializable]
    private class TrendingPost
    {
        public int id;
        public string description;
        public string createdAt;
        public int optLock;
        public string postId;
        public string updatedAt;
        public string userId;
        public int bookmarksCount;
        public int commentsCount;
        public int likesCount;
        public string designStyle;
        public string roomType;
        public string status;
        public PostMedia[] postMedia;
        public PostUser user;
        public bool liked;
        public bool bookmarked;
    }

    [Serializable]
    private class PostMedia
    {
        public string filePath;
        public string postMediaId;
    }

    [Serializable]
    private class PostUser
    {
        public string userId;
        public string userName;
        public string role;
        public CreatorProfile creatorProfile;
    }

    [Serializable]
    private class CreatorProfile
    {
        public string avatar;
    }

    public static IEnumerator FetchTrendingFeed(
        string baseURL,
        string authToken,
        Action<List<PostData>> onSuccess,
        Action<string> onError = null)
    {
        string trendingEndpoint = $"{baseURL}/api/v1/post/feed?type=trending";
        
        using (UnityWebRequest request = UnityWebRequest.Get(trendingEndpoint))
        {
            request.SetRequestHeader("Authorization", $"{authToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                var msg = $"[PostDataGetter] Error fetching trending feed: {request.error}";
                Debug.LogError(msg);
                onError?.Invoke(msg);
                yield break;
            }

            string jsonResponse = request.downloadHandler.text;
            Debug.Log("[PostDataGetter] Trending Feed JSON Response:\n" + jsonResponse);

            // Parse JSON response (outside of try-catch to avoid yield issues)
            TrendingFeedResponse response = null;
            try
            {
                response = JsonUtility.FromJson<TrendingFeedResponse>(jsonResponse);
            }
            catch (Exception e)
            {
                var msg = "[PostDataGetter] Error parsing trending feed JSON: " + e.Message;
                Debug.LogError(msg);
                onError?.Invoke(msg);
                yield break;
            }

            if (response != null && response.success && response.data != null && response.data.posts != null)
            {
                List<PostData> postDataList = new List<PostData>();
                
                foreach (var apiPost in response.data.posts)
                {
                    // Extract media URLs
                    List<string> mediaUrls = new List<string>();
                    if (apiPost.postMedia != null)
                    {
                        foreach (var media in apiPost.postMedia)
                        {
                            mediaUrls.Add(media.filePath);
                        }
                    }

                    // Get user avatar
                    string userAvatar = null;
                    if (apiPost.user?.creatorProfile?.avatar != null)
                    {
                        userAvatar = apiPost.user.creatorProfile.avatar;
                    }

                    // Create PostData object using the API constructor
                    PostData postData = new PostData(
                        apiPost.user?.userName ?? "Unknown Designer",
                        apiPost.description ?? "",
                        apiPost.designStyle ?? "Unknown Style",
                        apiPost.roomType ?? "Unknown Room",
                        apiPost.likesCount,
                        apiPost.commentsCount,
                        apiPost.bookmarksCount,
                        apiPost.liked,
                        apiPost.bookmarked,
                        mediaUrls,
                        userAvatar,
                        apiPost.user?.role ?? "USER",
                        apiPost.id
                    );

                    postDataList.Add(postData);
                }

                Debug.Log($"[PostDataGetter] Successfully parsed {postDataList.Count} trending posts");
                
                // Start loading images for posts that have media URLs
                yield return LoadImagesForPosts(postDataList, onSuccess, onError);
            }
            else
            {
                var msg = "[PostDataGetter] No trending posts found in response";
                Debug.LogWarning(msg);
                onSuccess?.Invoke(new List<PostData>());
            }
        }
    }
    
    private static IEnumerator LoadImagesForPosts(List<PostData> posts, 
        Action<List<PostData>> onSuccess, Action<string> onError)
    {
        for (int i = 0; i < posts.Count; i++)
        {
            PostData post = posts[i];
            
            // Load the first image if available
            if (post.mediaUrls.Count > 0)
            {
                string imageUrl = post.mediaUrls[0];
                
                using (UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(imageUrl))
                {
                    yield return imageRequest.SendWebRequest();
                    
                    if (imageRequest.result == UnityWebRequest.Result.Success)
                    {
                        post.SetImage(DownloadHandlerTexture.GetContent(imageRequest));
                        Debug.Log($"[PostDataGetter] Successfully loaded image for post {post.postId}");
                    }
                    else
                    {
                        Debug.LogWarning($"[PostDataGetter] Failed to load image for post {post.postId}: {imageRequest.error}");
                        // Continue with null image, placeholder will be shown
                    }
                }
            }
        }
        
        onSuccess?.Invoke(posts);
    }

    // ===== Existing Weekly Trending Tags functionality =====
    [Serializable]
    private class WeeklyTrendingTagsResponse
    {
        public bool success;
        public string message;
        public TrendingTag[] data;
    }

    [Serializable]
    private class TrendingTag
    {
        public int id;
        public string name;
    }

    public struct TagItem
    {
        public int id;
        public string name;
        public TagItem(int id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
    
    public static IEnumerator FetchWeeklyTrendingTags(
        string baseURL,
        string authToken,
        Action<List<TagItem>> onSuccess,
        Action<string> onError = null)
    {
        string WeeklyTrendingEndpoint = $"{baseURL}/api/v1/post/weekly-trending-tags";
        using (UnityWebRequest request = UnityWebRequest.Get(WeeklyTrendingEndpoint))
        {
            // Keep header format same as before (you can prepend "Bearer " here if your backend expects it)
            request.SetRequestHeader("Authorization", $"{authToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                var msg = $"[PostDataGetter] Error fetching tags: {request.error}";
                Debug.LogError(msg);
                onError?.Invoke(msg);
                yield break;
            }

            string jsonResponse = request.downloadHandler.text;
            Debug.Log("[PostDataGetter] Weekly Trending Tags JSON Response:\n" + jsonResponse);

            try
            {
                WeeklyTrendingTagsResponse response = JsonUtility.FromJson<WeeklyTrendingTagsResponse>(jsonResponse);

                if (response != null && response.success && response.data != null && response.data.Length > 0)
                {
                    var list = new List<TagItem>(response.data.Length);
                    foreach (var t in response.data)
                    {
                        list.Add(new TagItem(t.id, t.name));
                    }
                    onSuccess?.Invoke(list);
                }
                else
                {
                    var msg = "[PostDataGetter] No trending tags found in response";
                    Debug.LogWarning(msg);
                    onSuccess?.Invoke(new List<TagItem>()); // return empty list cleanly
                }
            }
            catch (Exception e)
            {
                var msg = "[PostDataGetter] Error parsing tags JSON: " + e.Message;
                Debug.LogError(msg);
                onError?.Invoke(msg);
            }
        }
    }
    
    public static IEnumerator FetchFeedByType(
        string baseURL,
        string authToken,
        string feedType,
        Action<List<PostData>> onSuccess,
        Action<string> onError = null)
    {
        // Construct the endpoint with the dynamic type parameter
        string feedEndpoint = $"{baseURL}/api/v1/post/feed?type={feedType}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(feedEndpoint))
        {
            request.SetRequestHeader("Authorization", $"{authToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                var msg = $"[PostDataGetter] Error fetching {feedType} feed: {request.error}";
                Debug.LogError(msg);
                onError?.Invoke(msg);
                yield break;
            }

            string jsonResponse = request.downloadHandler.text;
            Debug.Log($"[PostDataGetter] {feedType.ToUpper()} Feed JSON Response:\n" + jsonResponse);

            // Parse JSON response (reuse the same parsing logic as FetchTrendingFeed)
            TrendingFeedResponse response = null;
            try
            {
                response = JsonUtility.FromJson<TrendingFeedResponse>(jsonResponse);
            }
            catch (Exception e)
            {
                var msg = $"[PostDataGetter] Error parsing {feedType} feed JSON: " + e.Message;
                Debug.LogError(msg);
                onError?.Invoke(msg);
                yield break;
            }

            if (response != null && response.success && response.data != null && response.data.posts != null)
            {
                List<PostData> postDataList = new List<PostData>();
                
                foreach (var apiPost in response.data.posts)
                {
                    // Extract media URLs
                    List<string> mediaUrls = new List<string>();
                    if (apiPost.postMedia != null)
                    {
                        foreach (var media in apiPost.postMedia)
                        {
                            mediaUrls.Add(media.filePath);
                        }
                    }

                    // Get user avatar
                    string userAvatar = null;
                    if (apiPost.user?.creatorProfile?.avatar != null)
                    {
                        userAvatar = apiPost.user.creatorProfile.avatar;
                    }

                    // Create PostData object using the API constructor
                    PostData postData = new PostData(
                        apiPost.user?.userName ?? "Unknown Designer",
                        apiPost.description ?? "",
                        apiPost.designStyle ?? "Unknown Style",
                        apiPost.roomType ?? "Unknown Room",
                        apiPost.likesCount,
                        apiPost.commentsCount,
                        apiPost.bookmarksCount,
                        apiPost.liked,
                        apiPost.bookmarked,
                        mediaUrls,
                        userAvatar,
                        apiPost.user?.role ?? "USER",
                        apiPost.id
                    );

                    postDataList.Add(postData);
                }

                Debug.Log($"[PostDataGetter] Successfully parsed {postDataList.Count} {feedType} posts");
                
                // Start loading images for posts that have media URLs
                yield return LoadImagesForPosts(postDataList, onSuccess, onError);
            }
            else
            {
                var msg = $"[PostDataGetter] No {feedType} posts found in response";
                Debug.LogWarning(msg);
                onSuccess?.Invoke(new List<PostData>());
            }
        }
    }
    
    public static IEnumerator FetchFilteredPosts(
    string baseURL,
    string authToken,
    string roomType = null,
    string designStyle = null,
    string sortBy = null,
    Action<List<PostData>> onSuccess = null,
    Action<string> onError = null)
    {
        // Build the query parameters
        List<string> queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(roomType) && roomType != "All Rooms")
        {
            queryParams.Add($"roomType={UnityEngine.Networking.UnityWebRequest.EscapeURL(roomType)}");
        }
        
        if (!string.IsNullOrEmpty(designStyle) && designStyle != "All Styles")
        {
            queryParams.Add($"designStyle={UnityEngine.Networking.UnityWebRequest.EscapeURL(designStyle)}");
        }
        
        if (!string.IsNullOrEmpty(sortBy) && sortBy != "Most Popular")
        {
            // Map the UI sort options to API values
            string apiSortBy = MapSortByToAPI(sortBy);
            if (!string.IsNullOrEmpty(apiSortBy))
            {
                queryParams.Add($"sortBy={UnityEngine.Networking.UnityWebRequest.EscapeURL(apiSortBy)}");
            }
        }
        
        // Build the final URL
        string baseUrl = $"{baseURL}/api/v1/post";
        string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams.ToArray()) : "";
        string finalUrl = baseUrl + queryString;
        
        Debug.Log($"[PostDataGetter] Fetching filtered posts from: {finalUrl}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(finalUrl))
        {
            request.SetRequestHeader("Authorization", $"{authToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                var msg = $"[PostDataGetter] Error fetching filtered posts: {request.error}";
                Debug.LogError(msg);
                onError?.Invoke(msg);
                yield break;
            }

            string jsonResponse = request.downloadHandler.text;
            Debug.Log($"[PostDataGetter] Filtered Posts JSON Response:\n" + jsonResponse);

            // Parse JSON response using the same structure as your curl response
            FilteredPostsResponse response = null;
            try
            {
                response = JsonUtility.FromJson<FilteredPostsResponse>(jsonResponse);
            }
            catch (Exception e)
            {
                var msg = "[PostDataGetter] Error parsing filtered posts JSON: " + e.Message;
                Debug.LogError(msg);
                onError?.Invoke(msg);
                yield break;
            }

            if (response != null && response.success && response.data != null && response.data.Length > 0)
            {
                List<PostData> postDataList = new List<PostData>();
                
                foreach (var apiPost in response.data)
                {
                    // Extract media URLs
                    List<string> mediaUrls = new List<string>();
                    if (apiPost.postMedia != null)
                    {
                        foreach (var media in apiPost.postMedia)
                        {
                            mediaUrls.Add(media.filePath);
                        }
                    }

                    // Since the filtered API doesn't include user info, we'll use placeholder data
                    // You might need to make a separate call to get user details if needed
                    PostData postData = new PostData(
                        "Designer", // Placeholder - you might want to fetch user details separately
                        apiPost.description ?? "",
                        apiPost.designStyle ?? "Unknown Style",
                        apiPost.roomType ?? "Unknown Room",
                        apiPost.likesCount,
                        apiPost.commentsCount,
                        apiPost.bookmarksCount,
                        false, // liked - not provided in filtered API
                        false, // bookmarked - not provided in filtered API
                        mediaUrls,
                        null, // userAvatar - not provided in filtered API
                        "USER", // userRole - not provided in filtered API
                        apiPost.id
                    );

                    postDataList.Add(postData);
                }

                Debug.Log($"[PostDataGetter] Successfully parsed {postDataList.Count} filtered posts");
                
                // Load images for posts that have media URLs
                yield return LoadImagesForPosts(postDataList, onSuccess, onError);
            }
            else
            {
                var msg = "[PostDataGetter] No filtered posts found in response";
                Debug.LogWarning(msg);
                onSuccess?.Invoke(new List<PostData>());
            }
        }
    }
    
    // Helper function to map UI sort options to API values
    private static string MapSortByToAPI(string uiSortBy)
    {
        switch (uiSortBy)
        {
            case "Most Popular":
                return "popular"; // Adjust based on your API
            case "Newest":
                return "newest";
            case "Highest Rated":
                return "rating"; // Adjust based on your API
            case "Price: Low to High":
                return "price_asc"; // Adjust based on your API
            case "Price: High to Low":
                return "price_desc"; // Adjust based on your API
            case "Most Viewed":
                return "views"; // Adjust based on your API
            default:
                return null;
        }
    }
    
    [Serializable]
    private class FilteredPostsResponse
    {
        public bool success;
        public string message;
        public FilteredPost[] data;
        public PaginationInfo pagination;
    }

    [Serializable]
    private class FilteredPost
    {
        public int id;
        public string description;
        public string createdAt;
        public int optLock;
        public string postId;
        public string updatedAt;
        public string userId;
        public int bookmarksCount;
        public int commentsCount;
        public int likesCount;
        public string designStyle;
        public string roomType;
        public string status;
        public PostMedia[] postMedia;
    }

    [Serializable]
    private class PaginationInfo
    {
        public string nextCursor;
        public int limit;
    }
    
    [Serializable]
    private class SearchResponse
    {
        public bool success;
        public string message;
        public SearchData data;
    }

    [Serializable]
    private class SearchData
    {
        public DesignerSearchResult[] results;
        public string nextCursor;
    }

    [Serializable]
    public class DesignerSearchResult
    {
        public string userId;
        public string userName;
        public string firstName;
        public string lastName;
        public int followersCount;
        public string avatar;

        public DesignerSearchResult(string userId, string userName, string firstName, string lastName, int followersCount, string avatar)
        {
            this.userId = userId;
            this.userName = userName;
            this.firstName = firstName;
            this.lastName = lastName;
            this.followersCount = followersCount;
            this.avatar = avatar;
        }
    }
    
    public static IEnumerator SearchDesigners(
        string baseURL,
        string authToken,
        string query,
        Action<List<DesignerSearchResult>> onSuccess,
        Action<string> onError = null)
    {
        if (string.IsNullOrEmpty(query))
        {
            onSuccess?.Invoke(new List<DesignerSearchResult>());
            yield break;
        }

        string searchEndpoint = $"{baseURL}/api/v1/post/search?q={UnityWebRequest.EscapeURL(query)}";
        
        Debug.Log($"[SearchDataGetter] Searching for: {query}");
        Debug.Log($"[SearchDataGetter] URL: {searchEndpoint}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(searchEndpoint))
        {
            request.SetRequestHeader("Authorization", $"{authToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                var msg = $"[SearchDataGetter] Error searching designers: {request.error}";
                Debug.LogError(msg);
                onError?.Invoke(msg);
                yield break;
            }

            string jsonResponse = request.downloadHandler.text;
            Debug.Log($"[SearchDataGetter] Search JSON Response:\n{jsonResponse}");

            // Parse JSON response
            SearchResponse response = null;
            try
            {
                response = JsonUtility.FromJson<SearchResponse>(jsonResponse);
            }
            catch (Exception e)
            {
                var msg = "[SearchDataGetter] Error parsing search JSON: " + e.Message;
                Debug.LogError(msg);
                onError?.Invoke(msg);
                yield break;
            }

            if (response != null && response.success && response.data != null && response.data.results != null)
            {
                List<DesignerSearchResult> designerList = new List<DesignerSearchResult>();
                
                foreach (var result in response.data.results)
                {
                    designerList.Add(result);
                }

                Debug.Log($"[SearchDataGetter] Successfully found {designerList.Count} designers");
                onSuccess?.Invoke(designerList);
            }
            else
            {
                var msg = "[SearchDataGetter] No designers found in search response";
                Debug.LogWarning(msg);
                onSuccess?.Invoke(new List<DesignerSearchResult>());
            }
        }
    }
}