using System.Collections.Generic;

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
    public string postId;
    public string caption;
    public string description;
    public string createdAt;
    public string category;
    public int likesCount;
    public List<Media> media;
    public List<PostMedia> postMedia;
    public User user;
    
    // Helper method to get the first image URL only
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
        
        return string.Empty;
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
    public string userId;
    public string userName;
    public string firstName;
    public string lastName;
    public string email;
    public string avatar;
    public string bio;
}