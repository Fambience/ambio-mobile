using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PostData
{
    [Header("Basic Post Info")]
    public Texture2D image;
    public string designerId;
    public string postTitle;
    
    [Header("API Post Data")]
    public int postId;
    public string description;
    public string designStyle;
    public string roomType;
    public int likesCount;
    public int commentsCount;
    public int bookmarksCount;
    public bool liked;
    public bool bookmarked;
    public List<string> mediaUrls;
    public string userAvatar;
    public string userRole;
    
    // Original constructor for dummy/legacy posts
    public PostData(Texture2D img, string designer, string title)
    {
        image = img;
        designerId = designer;
        postTitle = title;
        mediaUrls = new List<string>();
        
        // Initialize default values for API fields
        postId = 0;
        description = "";
        designStyle = "";
        roomType = "";
        likesCount = 0;
        commentsCount = 0;
        bookmarksCount = 0;
        liked = false;
        bookmarked = false;
        userAvatar = "";
        userRole = "USER";
    }

    // New constructor for API posts
    public PostData(string designerName, string description, string designStyle, string roomType, 
                   int likesCount, int commentsCount, int bookmarksCount, bool liked, bool bookmarked,
                   List<string> mediaUrls, string userAvatar, string userRole, int postId)
    {
        this.designerId = designerName;
        this.description = description;
        this.designStyle = designStyle;
        this.roomType = roomType;
        this.likesCount = likesCount;
        this.commentsCount = commentsCount;
        this.bookmarksCount = bookmarksCount;
        this.liked = liked;
        this.bookmarked = bookmarked;
        this.mediaUrls = mediaUrls ?? new List<string>();
        this.userAvatar = userAvatar;
        this.userRole = userRole;
        this.postId = postId;
        this.postTitle = $"{designStyle} {roomType}"; // Generate title from style and room type
        
        // image will be set separately when loaded
        this.image = null;
    }

    // Constructor with all parameters (for maximum flexibility)
    public PostData(Texture2D img, string designerName, string title, string description, 
                   string designStyle, string roomType, int likesCount, int commentsCount, 
                   int bookmarksCount, bool liked, bool bookmarked, List<string> mediaUrls, 
                   string userAvatar, string userRole, int postId)
    {
        this.image = img;
        this.designerId = designerName;
        this.postTitle = title;
        this.description = description;
        this.designStyle = designStyle;
        this.roomType = roomType;
        this.likesCount = likesCount;
        this.commentsCount = commentsCount;
        this.bookmarksCount = bookmarksCount;
        this.liked = liked;
        this.bookmarked = bookmarked;
        this.mediaUrls = mediaUrls ?? new List<string>();
        this.userAvatar = userAvatar;
        this.userRole = userRole;
        this.postId = postId;
    }

    // Helper methods for easy access to post information
    public bool HasImage => image != null;
    public bool HasMediaUrls => mediaUrls != null && mediaUrls.Count > 0;
    public string GetFirstMediaUrl => HasMediaUrls ? mediaUrls[0] : "";
    public bool IsCreator => userRole == "CREATOR";
    public string GetDisplayTitle => !string.IsNullOrEmpty(postTitle) ? postTitle : $"{designStyle} {roomType}";
    public string GetDisplayName => !string.IsNullOrEmpty(designerId) ? designerId : "Unknown Designer";

    // Method to update engagement stats (useful for real-time updates)
    public void UpdateEngagement(int newLikes, int newComments, int newBookmarks, bool isLiked, bool isBookmarked)
    {
        likesCount = newLikes;
        commentsCount = newComments;
        bookmarksCount = newBookmarks;
        liked = isLiked;
        bookmarked = isBookmarked;
    }

    // Method to set the loaded image
    public void SetImage(Texture2D loadedImage)
    {
        image = loadedImage;
    }
}