using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

public static class PostDataGetter
{
    public static List<PostData> GenerateDummyPosts(int totalPosts)
    {
        List<PostData> posts = new List<PostData>();

        // Load the minimalist image from Resources
        Texture2D minimalistImage = Resources.Load<Texture2D>("minimalist");

        if (minimalistImage == null)
        {
            Debug.LogWarning("Minimalist image not found in Resources folder. Creating posts without images for testing.");
        }
        else
        {
            Debug.Log("Minimalist image loaded successfully");
        }

        // Create dummy posts using the minimalist image
        for (int i = 0; i < totalPosts; i++)
        {
            PostData post = new PostData(
                minimalistImage,
                $"designer_{i + 1}",
                $"Minimalist Design {i + 1}"
            );

            posts.Add(post);
        }

        Debug.Log($"Generated {posts.Count} dummy posts");
        return posts;
    }

    // ===== API Models for Trending Feed =====
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
        string trendingEndpoint = $"https://ambiobackend-stage.onrender.com/api/v1/post/feed?type=trending";
        
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
        const string WeeklyTrendingEndpoint = "https://ambiobackend-stage.onrender.com/api/v1/post/weekly-trending-tags";
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
}