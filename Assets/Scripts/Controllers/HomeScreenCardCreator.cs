using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class HomeScreenCardCreator : MonoBehaviour
{
    [Header("Card Templates")]
    public VisualTreeAsset verticalCardTemplate;
    public VisualTreeAsset horizontalCardTemplate;

    public static HomeScreenCardCreator Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public VisualElement CreateVerticalCard(Post post)
    {
        VisualElement verticalCard = verticalCardTemplate.CloneTree();
        
        // Set user name
        string displayName = !string.IsNullOrEmpty(post.user.firstName) 
            ? $"{post.user.firstName} {post.user.lastName}" 
            : post.user.userName;
        verticalCard.Q<TextElement>("userName").text = displayName;
        
        // Set description (use caption if description is null)
        string description = !string.IsNullOrEmpty(post.description) ? post.description : post.caption;
        verticalCard.Q<TextElement>("description").text = description;
        
        // Set user image
        Image userImage = verticalCard.Q<Image>("userImage");
        if (!string.IsNullOrEmpty(post.user.avatar))
        {
            StartCoroutine(LoadImageFromURL(post.user.avatar, userImage));
        }
        else
        {
            userImage.image = LoadImage("user_placeholder");
        }
        
        // Set single post image
        SetupSingleImage(verticalCard, post);
        
        // Setting the like icon for each post
        SetupLikeIcon(verticalCard, post);
        
        // Setting the bookmark icon for each post
        SetupBookmarkIcon(verticalCard, post);
        
        return verticalCard;
    }

    private void SetupSingleImage(VisualElement verticalCard, Post post)
    {
        Image cardImage = verticalCard.Q<Image>("card-image");
        string imageUrl = post.GetFirstImageUrl();
        
        if (!string.IsNullOrEmpty(imageUrl))
        {
            StartCoroutine(LoadImageFromURL(imageUrl, cardImage));
        }
        else
        {
            cardImage.image = LoadImage("Contemporary");
        }
    }

    private void SetupLikeIcon(VisualElement verticalCard, Post post)
    {
        Image likeIcon = verticalCard.Q<Image>("favourite");
        if (likeIcon != null)
        {
            likeIcon.image = LoadImage("heart_outline");
            likeIcon.pickingMode = PickingMode.Position;
            likeIcon.RegisterCallback<PointerUpEvent>(evt => 
            {
                StartCoroutine(HandleLike(post.postId, likeIcon, post));
            });
            likeIcon.RegisterCallback<PointerEnterEvent>(evt => 
            {
                likeIcon.style.opacity = 0.7f;
            });
            likeIcon.RegisterCallback<PointerLeaveEvent>(evt => 
            {
                likeIcon.style.opacity = 1f;
            });
        }
    }

    private void SetupBookmarkIcon(VisualElement verticalCard, Post post)
    {
        Image bookmarkIcon = verticalCard.Q<Image>("bookmark");
        if (bookmarkIcon != null)
        {
            bookmarkIcon.image = LoadImage("Bookmark"); 
            bookmarkIcon.pickingMode = PickingMode.Position;
            bookmarkIcon.RegisterCallback<PointerUpEvent>(evt => 
            {
                StartCoroutine(HandleBookmark(post.postId, bookmarkIcon));
            });
            bookmarkIcon.RegisterCallback<PointerEnterEvent>(evt => 
            {
                bookmarkIcon.style.opacity = 0.7f;
            });
            bookmarkIcon.RegisterCallback<PointerLeaveEvent>(evt => 
            {
                bookmarkIcon.style.opacity = 1f;
            });
        }
    }

    private IEnumerator HandleLike(string postId, Image likeIcon, Post post)
    {
        yield return StartCoroutine(HomeScreenAPIManager.Instance.LikePost(postId, (response) =>
        {
            if (response != null)
            {
                // Update UI based on response message
                if (response.message == "Like created")
                {
                    Texture2D filledHeart = LoadImage("heart-filled");
                    if (filledHeart != null)
                    {
                        likeIcon.image = filledHeart;
                        post.likesCount++;
                    }
                }
                else if (response.message == "Like removed")
                {
                    Texture2D outlineHeart = LoadImage("favourite");
                    if (outlineHeart != null)
                    {
                        likeIcon.image = outlineHeart;
                        post.likesCount--;
                    }
                }
                
                Debug.Log($"Like action successful: {response.message}. New count: {post.likesCount}");
            }
        }));
    }

    private IEnumerator HandleBookmark(string postId, Image bookmarkIcon)
    {
        yield return StartCoroutine(HomeScreenAPIManager.Instance.BookmarkPost(postId, (response) =>
        {
            if (response != null)
            {
                if (response.message == "bookmark")
                {
                    Texture2D filledBookmark = LoadImage("bookmark-filled");
                    if (filledBookmark != null)
                    {
                        bookmarkIcon.image = filledBookmark;
                    }
                    Debug.Log("Post bookmarked successfully");
                }
                else if (response.message == "unbookmark" || response.message == "removed" || response.message == "removed bookmark")
                {
                    Texture2D outlineBookmark = LoadImage("Bookmark");
                    if (outlineBookmark != null)
                    {
                        bookmarkIcon.image = outlineBookmark;
                    }
                    Debug.Log("Bookmark removed successfully");
                }
            }
        }));
    }

    public VisualElement CreateHorizontalCard(User designerPost)
    {
        VisualElement horizontalCard = horizontalCardTemplate.CloneTree();
        string displayName = designerPost.userName;
        
        Debug.Log($"[HORIZONTAL CARD] Creating card for: {displayName}");
        
        horizontalCard.Q<Label>("userName").text = displayName;
        
        // Set designer image
        Image userImage = horizontalCard.Q<Image>("userImage");
        if (!string.IsNullOrEmpty(designerPost.avatar))
        {
            StartCoroutine(LoadImageFromURL(designerPost.avatar, userImage));
        }
        else
        {
            userImage.image = LoadImage("designer_placeholder");
        }
        
        // Set up follow button
        Label followText = horizontalCard.Q<Label>("followText");
        followText.text = "Follow";
        Button followButton = horizontalCard.Q<Button>("followButton");
    
        // Passing the userId and horizontalCard reference to the ToggleFollow function
        followButton.clicked += () => ToggleFollow(followText, designerPost.userId, horizontalCard);
        
        return horizontalCard;
    }

    private void ToggleFollow(Label followText, string userId, VisualElement horizontalCard)
    {
        Button followButton = followText.parent as Button;
    
        if (followText.text == "Follow")
        {
            // Immediately update UI to show "Following" state
            followText.text = "Following";
            followButton.style.backgroundColor = new StyleColor(new Color32(139, 76, 57, 255));
            followText.style.color = new StyleColor(Color.white);
        
            // Making API call
            StartCoroutine(HandleFollow(userId, followText, followButton, horizontalCard));
        }
        else
        {
            // For unfollow action
            followText.text = "Follow";
            followButton.style.backgroundColor = StyleKeyword.Null;
            followText.style.color = StyleKeyword.Null;
        
            // Making API call
            StartCoroutine(HandleFollow(userId, followText, followButton, horizontalCard));
        }
    }

    private IEnumerator HandleFollow(string userId, Label followText, Button followButton, VisualElement horizontalCard)
    {
        string originalText = followText.text;
        StyleColor originalBackgroundColor = followButton.resolvedStyle.backgroundColor;
        StyleColor originalTextColor = followText.resolvedStyle.color;
        
        yield return StartCoroutine(HomeScreenAPIManager.Instance.FollowUser(userId, (response) =>
        {
            if (response != null)
            {
                if (response.followed)
                {
                    Debug.Log($"Successfully followed user: {response.message}");
                    StartCoroutine(HideCardAfterDelay(horizontalCard, 10f));
                }
                else
                {
                    Debug.Log($"Successfully unfollowed user: {response.message}");
                    followText.text = "Follow";
                    followButton.style.backgroundColor = StyleKeyword.Null;
                    followText.style.color = StyleKeyword.Null;
                }
            }
            else
            {
                // Revert UI changes on error
                followText.text = originalText;
                followButton.style.backgroundColor = originalBackgroundColor;
                followText.style.color = originalTextColor;
                Debug.Log("Reverted follow button state due to error");
            }
        }));
    }

    private IEnumerator HideCardAfterDelay(VisualElement card, float delay)
    {
        yield return new WaitForSeconds(delay);
        // Optionally fade out the card after successful follow
        // StartCoroutine(FadeOutCard(card));
    }

    private IEnumerator LoadImageFromURL(string url, Image targetImage)
    {
        yield return StartCoroutine(HomeScreenAPIManager.Instance.LoadImageFromURL(url, (texture) =>
        {
            if (texture != null)
            {
                targetImage.image = texture;
            }
            else
            {
                Debug.LogError($"Failed to load image from {url}");
                // Keep placeholder image if loading fails
            }
        }));
    }
    
    private Texture2D LoadImage(string imageName)
    {
        return Resources.Load<Texture2D>(imageName);
    }
}