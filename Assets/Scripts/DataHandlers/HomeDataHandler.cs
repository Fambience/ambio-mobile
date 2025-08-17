using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public class ApiResponse<T>
{
    public bool success;
    public string message;
    public T data;
    public Pagination pagination;
}

[System.Serializable]
public class FollowResponse
{
    public string message;
    public bool followed;
}

[System.Serializable]
public class BookmarkResponse
{
    public string message;
}

[System.Serializable]
public class LikeResponse
{
    public string message;
    public string postId;
    public string userId;
    public int likeId;
}

[System.Serializable]
public class HomeApiResponse
{
    public string type;
    public List<Post> posts;
    public Pagination pagination;
}

[System.Serializable]
public class Pagination
{
    public int page;
    public int limit;
    public int total;
}

[System.Serializable]
public class Post
{
    [SerializeField] public string postId;
    [SerializeField] public string caption;
    [SerializeField] public string description;
    [SerializeField] public string designStyle;
    [SerializeField] public string roomType;
    [SerializeField] public string status;
    [SerializeField] public string createdAt;
    [SerializeField] public string category;
    [SerializeField] public int likesCount;
    [SerializeField] public int commentsCount;
    [SerializeField] public int bookmarksCount;
    [SerializeField] public List<string> tags;
    [SerializeField] public List<Media> media;
    [SerializeField] public List<PostMedia> postMedia;
    [SerializeField] public User author;
    [SerializeField] public bool liked;
    [SerializeField] public bool bookmarked;
    
    public string GetFirstImageUrl()
    {
        if (postMedia != null && postMedia.Count > 0 && !string.IsNullOrEmpty(postMedia[0].filePath))
        {
            return postMedia[0].filePath;
        }
        else if (media != null && media.Count > 0 && !string.IsNullOrEmpty(media[0].url))
        {
            return media[0].url;
        }
        
        return null;
    }
}

[System.Serializable]
public class Media
{
    public string mediaId;
    public string url;
}

[System.Serializable]
public class PostMedia
{
    public string filePath;
}

[System.Serializable]
public class User
{
    [SerializeField] public string userId;
    [SerializeField] public string userName;
    [SerializeField] public string firstName;
    [SerializeField] public string lastName;
    [SerializeField] public string email;
    [SerializeField] public string avatar;
    [SerializeField] public string bio;
    [SerializeField] public int followersCount;
}

public class HomeDataHandler : MonoBehaviour
{
    [Header("API Settings")]
    public string baseURL;
    public string exploreFeedUrl = "/api/v1/post/explore-feed";
    public string homeFeedUrl = "/api/v1/post/user-feed";
    public string trendingDesignersUrl = "/api/v1/post/trending-designers";
    public string authToken;

    public int postsPerPage = 10;
    public int designersPerPage = 10;

    private List<Post> homePosts = new List<Post>();
    private List<Post> explorePosts = new List<Post>();
    private List<User> trendingDesigners = new List<User>();
    
    private int currentHomePage = 1;
    private int currentExplorePage = 1;
    private int currentDesignerPage = 1;
    
    private bool hasMoreHomePosts = true;
    private bool hasMoreExplorePosts = true;
    private bool hasMoreDesigners = true;
    
    private int totalHomePagesLoaded = 0;
    private int totalExplorePagesLoaded = 0;
    private int totalDesignerPagesLoaded = 0;

    public List<Post> HomePosts => homePosts;
    public List<Post> ExplorePosts => explorePosts;
    public List<User> TrendingDesigners => trendingDesigners;
    public bool HasMoreHomePosts => hasMoreHomePosts;
    public bool HasMoreExplorePosts => hasMoreExplorePosts;
    public bool HasMoreDesigners => hasMoreDesigners;

    private void Awake()
    {
        baseURL = baseScript.baseURL;
        authToken = AuthTokenManager.GetToken();
    }

    public IEnumerator LoadHomeFeed(int page)
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
                    
                    if (response.success && response.data != null && response.data.posts != null)
                    {
                        if (page == 1)
                        {
                            homePosts.Clear();
                            totalHomePagesLoaded = 0;
                            Debug.Log("HOME FEED: Cleared previous data for fresh start");
                        }
                        
                        if (page > totalHomePagesLoaded)
                        {
                            int postsBeforeAdd = homePosts.Count;
                            
                            homePosts.AddRange(response.data.posts);
                            totalHomePagesLoaded = page;
                            hasMoreHomePosts = response.data.posts.Count >= postsPerPage;
                            
                            Debug.Log($"HOME FEED: Added {response.data.posts.Count} posts. Total now: {homePosts.Count}");
                        }
                        else
                        {
                            Debug.Log($"HOME FEED: Page {page} already loaded (totalLoaded: {totalHomePagesLoaded})");
                        }
                    }
                    else
                    {
                        hasMoreHomePosts = false;
                        Debug.Log("HOME FEED: No data received, marking as no more posts");
                    }
                }
                catch (System.Exception e)
                {
                    hasMoreHomePosts = false;
                    Debug.LogError($"HOME FEED: Parse error - {e.Message}");
                }
            }
            else
            {
                hasMoreHomePosts = false;
                Debug.LogError($"HOME FEED: Network error - {request.error}");
            }
        }

        currentHomePage = page;
    }

    public IEnumerator LoadExploreFeed(int page)
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
                    
                    if (response.success && response.data != null)
                    {
                        if (page == 1)
                        {
                            explorePosts.Clear();
                            totalExplorePagesLoaded = 0;
                        }
                        
                        if (page > totalExplorePagesLoaded)
                        {
                            int postsBeforeAdd = explorePosts.Count;
                            
                            explorePosts.AddRange(response.data);
                            totalExplorePagesLoaded = page;
                            hasMoreExplorePosts = response.data.Count >= postsPerPage;
                        }
                        else
                        {
                            Debug.Log($"SKIP: Page {page} already loaded (totalLoaded: {totalExplorePagesLoaded})");
                        }
                    }
                    else
                    {
                        hasMoreExplorePosts = false;
                    }
                }
                catch (System.Exception e)
                {
                    hasMoreExplorePosts = false;
                }
            }
            else
            {
                hasMoreExplorePosts = false;
            }
        }

        currentExplorePage = page;
    }

    public IEnumerator LoadTrendingDesigners(int page)
    {
        string url = $"{baseURL}{trendingDesignersUrl}?page={page}&limit={designersPerPage}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", authToken);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                
                try
                {
                    ApiResponse<List<User>> response = JsonUtility.FromJson<ApiResponse<List<User>>>(jsonResponse);
                    
                    if (response.success && response.data != null)
                    {
                        if (page == 1)
                        {
                            trendingDesigners.Clear();
                            totalDesignerPagesLoaded = 0;
                        }
                        
                        if (page > totalDesignerPagesLoaded)
                        {
                            trendingDesigners.AddRange(response.data);
                            totalDesignerPagesLoaded = page;
                            hasMoreDesigners = response.data.Count >= designersPerPage;
                        }
                        else
                        {
                            Debug.Log($"Designer page {page} already loaded, skipping");
                        }
                    }
                    else
                    {
                        hasMoreDesigners = false;
                    }
                }
                catch (System.Exception e)
                {
                    hasMoreDesigners = false;
                }
            }
            else
            {
                hasMoreDesigners = false;
            }
        }

        currentDesignerPage = page;
    }

    public IEnumerator LikePost(string postId, UnityEngine.UIElements.Image likeIcon, Post post)
    {
        string url = $"{baseURL}/api/v1/post/like/{postId}";
        
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
                        if (post.liked)
                        {
                            Texture2D outlineHeart = LoadImage("favourite");
                            if (outlineHeart != null)
                            {
                                likeIcon.image = outlineHeart;
                                post.likesCount--;
                                post.liked = false;
                            }
                        }
                        else
                        {
                            Texture2D filledHeart = LoadImage("heart-filled");
                            if (filledHeart != null)
                            {
                                likeIcon.image = filledHeart;
                                post.likesCount++;
                                post.liked = true;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"Like API Error: {response.message}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing like response: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Like Network Error: {request.error}");
            }
        }
    }

    public IEnumerator BookmarkPost(string postId, UnityEngine.UIElements.Image bookmarkIcon, Post post)
    {
        string url = $"{baseURL}/api/v1/post/bookmark/{postId}";
        
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
                        if (post.bookmarked)
                        {
                            Texture2D outlineBookmark = LoadImage("Bookmark");
                            if (outlineBookmark != null)
                            {
                                bookmarkIcon.image = outlineBookmark;
                                post.bookmarked = false;
                            }
                        }
                        else
                        {
                            Texture2D filledBookmark = LoadImage("bookmark-filled");
                            if (filledBookmark != null)
                            {
                                bookmarkIcon.image = filledBookmark;
                                post.bookmarked = true;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"Bookmark API Error: {response.message}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing bookmark response: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Bookmark Network Error: {request.error}");
            }
        }
    }

    public IEnumerator FollowUser(string userId, UnityEngine.UIElements.Label followText, UnityEngine.UIElements.Button followButton, UnityEngine.UIElements.VisualElement horizontalCard)
    {
        string url = $"{baseURL}/api/v1/profile/toggle-follow/{userId}";
        string originalText = followText.text;
        UnityEngine.UIElements.StyleColor originalBackgroundColor = followButton.resolvedStyle.backgroundColor;
        UnityEngine.UIElements.StyleColor originalTextColor = followText.resolvedStyle.color;
        
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
                    if (response.followed)
                    {
                        StartCoroutine(HideCardAfterDelay(horizontalCard, 10f));
                    }
                    else
                    {
                        followText.text = "Follow";
                        followButton.style.backgroundColor = UnityEngine.UIElements.StyleKeyword.Null;
                        followText.style.color = UnityEngine.UIElements.StyleKeyword.Null;
                    }
                }
                catch (System.Exception e)
                {
                    RevertFollowButton(followText, followButton, originalText, originalBackgroundColor, originalTextColor);
                }
            }
            else
            {
                RevertFollowButton(followText, followButton, originalText, originalBackgroundColor, originalTextColor);
            }
        }
    }

    private void RevertFollowButton(UnityEngine.UIElements.Label followText, UnityEngine.UIElements.Button followButton, string originalText, 
        UnityEngine.UIElements.StyleColor originalBackgroundColor, UnityEngine.UIElements.StyleColor originalTextColor)
    {
        followText.text = originalText;
        followButton.style.backgroundColor = originalBackgroundColor;
        followText.style.color = originalTextColor;
    }

    private IEnumerator HideCardAfterDelay(UnityEngine.UIElements.VisualElement card, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (card != null && card.parent != null)
        {
            StartCoroutine(FadeOutCard(card));
        }
    }

    private IEnumerator FadeOutCard(UnityEngine.UIElements.VisualElement card)
    {
        float fadeDuration = 0.2f;
        float elapsedTime = 0f;
        float startOpacity = card.resolvedStyle.opacity;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeDuration;
            card.style.opacity = Mathf.Lerp(startOpacity, 0f, progress);
            yield return null;
        }

        card.parent.Remove(card);
    }

    public IEnumerator LoadImageFromURL(string url, UnityEngine.UIElements.Image targetImage)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                targetImage.image = texture;
            }
            else
            {
                Debug.LogError($"Failed to load image from {url}: {request.error}");
            }
        }
    }

    private Texture2D LoadImage(string imageName)
    {
        return Resources.Load<Texture2D>(imageName);
    }

    public void ResetPaginationData()
    {
        currentHomePage = 1;
        currentExplorePage = 1;
        currentDesignerPage = 1;
        totalHomePagesLoaded = 0;
        totalExplorePagesLoaded = 0;
        totalDesignerPagesLoaded = 0;
        hasMoreHomePosts = true;
        hasMoreExplorePosts = true;
        hasMoreDesigners = true;
    }

    public int GetNextHomePage() => currentHomePage + 1;
    public int GetNextExplorePage() => currentExplorePage + 1;
    public int GetNextDesignerPage() => currentDesignerPage + 1;
}