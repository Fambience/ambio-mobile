using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class HomeUIController : MonoBehaviour
{
    public VisualTreeAsset verticalCardTemplate;
    public VisualTreeAsset horizontalCardTemplate;

    [Header("Pull to Refresh Settings")]
    public float pullThreshold = 100f;
    public float refreshIndicatorSize = 50f;
    public float refreshDuration = 2f;
    
    private ScrollView container;
    private VisualElement refreshIndicator;
    private VisualElement refreshContainer;
    private Button messageButton;
    private Image refreshIcon;
    private bool isRefreshing = false;
    private bool isPulling = false;
    private float pullDistance = 0f;

    private HomeDataHandler dataHandler;
    private VisualElement uploadProgressSection;
    private UploadProgressController uploadProgressController;

    public System.Action OnRefreshRequested;

    private void Awake()
    {
        dataHandler = GetComponent<HomeDataHandler>();
        if (dataHandler == null)
        {
            dataHandler = gameObject.AddComponent<HomeDataHandler>();
        }
    }

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        container = root.Q<ScrollView>("main-container");
        messageButton = root.Q<Button>("newChatButton");
        uploadProgressSection = root.Q<VisualElement>("uploadProgressSection");
        uploadProgressController = GetComponent<UploadProgressController>();
        if (uploadProgressController == null)
        {
            uploadProgressController = gameObject.AddComponent<UploadProgressController>();
        }

        SetupPullToRefresh();
        StartCoroutine(ShowNavigationAfterDelay());
        messageButton?.RegisterCallback<ClickEvent>(evt => OnMessageButtonClick());
    }

    private IEnumerator ShowNavigationAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        
        Debug.Log("Showing navigation bar for Home screen");
        
        NavigationManager.ToggleNavigationBar(true);
        NavigationManager.UpdateSelectedIcon(NavScreen.Home);
        
        yield return new WaitForSeconds(0.1f);
        Debug.Log($"Navigation bar visible: {NavigationManager.IsNavigationBarVisible()}");
    }

    private void SetupPullToRefresh()
    {
        container.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        
        refreshContainer = new VisualElement();
        refreshContainer.style.height = 0;
        refreshContainer.style.flexDirection = FlexDirection.Row;
        refreshContainer.style.justifyContent = Justify.Center;
        refreshContainer.style.alignItems = Align.Center;
        refreshContainer.style.overflow = Overflow.Hidden;
        refreshContainer.style.marginTop = 0;
        
        refreshIndicator = new VisualElement();
        refreshIndicator.style.width = refreshIndicatorSize;
        refreshIndicator.style.height = refreshIndicatorSize;
        refreshIndicator.style.justifyContent = Justify.Center;
        refreshIndicator.style.alignItems = Align.Center;
        refreshIndicator.style.opacity = 0;
        
        refreshIcon = new Image();
        refreshIcon.image = Resources.Load<Texture2D>("loader");
        refreshIcon.style.width = refreshIndicatorSize;
        refreshIcon.style.height = refreshIndicatorSize;
        
        refreshIndicator.Add(refreshIcon);
        refreshContainer.Add(refreshIndicator);
        
        container.Insert(0, refreshContainer);
        
        container.RegisterCallback<WheelEvent>(OnScroll);
        container.RegisterCallback<PointerDownEvent>(OnPointerDown);
        container.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        container.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    public VisualElement CreateVerticalCard(Post post)
    {
        VisualElement verticalCard = verticalCardTemplate.CloneTree();
        
        string displayName = "Unknown User";
        
        if (post.author != null)
        {
            if (!string.IsNullOrEmpty(post.author.firstName) && !string.IsNullOrEmpty(post.author.lastName))
            {
                displayName = $"{post.author.firstName} {post.author.lastName}";
            }
            else if (!string.IsNullOrEmpty(post.author.firstName))
            {
                displayName = post.author.firstName;
            }
            else if (!string.IsNullOrEmpty(post.author.userName))
            {
                displayName = post.author.userName;
            }
            else if (!string.IsNullOrEmpty(post.author.email))
            {
                displayName = post.author.email.Split('@')[0];
            }
        }
        
        verticalCard.Q<TextElement>("userName").text = displayName;
        
        string description = !string.IsNullOrEmpty(post.description) ? post.description : post.caption;
        if (!string.IsNullOrEmpty(description) && description.Length > 90)
        {
            description = description.Substring(0, 90) + "...";
        }
        verticalCard.Q<TextElement>("description").text = description;
        
        Image userImage = verticalCard.Q<Image>("userImage");
        if (post.author != null && !string.IsNullOrEmpty(post.author.avatar))
        {
            StartCoroutine(dataHandler.LoadImageFromURL(post.author.avatar, userImage));
        }
        else
        {
            userImage.image = LoadImage("user_placeholder");
        }
        
        SetupSingleImage(verticalCard, post);
        SetupCardInteractions(verticalCard, post, userImage);
        
        return verticalCard;
    }

    private void SetupSingleImage(VisualElement verticalCard, Post post)
    {
        Image cardImage = verticalCard.Q<Image>("card-image");
        string imageUrl = post.GetFirstImageUrl();
        cardImage.scaleMode = ScaleMode.StretchToFill;

        if (string.IsNullOrEmpty(imageUrl))
        {
            cardImage.image = LoadImage("Contemporary");
        }
        else
        {
            StartCoroutine(dataHandler.LoadImageFromURL(imageUrl, cardImage));
        }
    }

    private void SetupCardInteractions(VisualElement verticalCard, Post post, Image userImage)
    {
        TextElement userNameElement = verticalCard.Q<TextElement>("userName");
        if (userNameElement != null)
        {
            userNameElement.pickingMode = PickingMode.Position;
            userNameElement.RegisterCallback<PointerUpEvent>(evt => 
            {
                evt.StopPropagation();
            });
            
            userNameElement.RegisterCallback<PointerEnterEvent>(evt => 
            {
                userNameElement.style.opacity = 0.7f;
            });
            userNameElement.RegisterCallback<PointerLeaveEvent>(evt => 
            {
                userNameElement.style.opacity = 1f;
            });
        }
        
        if (userImage != null)
        {
            userImage.pickingMode = PickingMode.Position;
            userImage.RegisterCallback<PointerUpEvent>(evt => 
            {
                evt.StopPropagation();
            });
            
            userImage.RegisterCallback<PointerEnterEvent>(evt => 
            {
                userImage.style.opacity = 0.8f;
            });
            userImage.RegisterCallback<PointerLeaveEvent>(evt => 
            {
                userImage.style.opacity = 1f;
            });
        }
        
        Image cardImage = verticalCard.Q<Image>("card-image");
        if (cardImage != null)
        {
            cardImage.pickingMode = PickingMode.Position;
            cardImage.RegisterCallback<PointerUpEvent>(evt => 
            {
                PostScreenDataHandler.ShowPostStatic(post);
                evt.StopPropagation();
            });
        }

        VisualElement commentSection = verticalCard.Q<VisualElement>("commentSection");
        if (commentSection != null)
        {
            commentSection.pickingMode = PickingMode.Position;
            commentSection.RegisterCallback<PointerUpEvent>(evt => 
            {
                PostScreenDataHandler.ShowPostStatic(post);
                evt.StopPropagation();
            });
        }
        
        Image likeIcon = verticalCard.Q<Image>("favourite");
        if (likeIcon != null)
        {
            if (post.liked)
            {
                likeIcon.image = LoadImage("heart-filled");
            }
            else
            {
                likeIcon.image = LoadImage("favourite");
            }
            
            likeIcon.pickingMode = PickingMode.Position;
            likeIcon.RegisterCallback<PointerUpEvent>(evt => 
            {
                StartCoroutine(dataHandler.LikePost(post.postId, likeIcon, post));
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
        
        Image bookmarkIcon = verticalCard.Q<Image>("bookmark");
        if (bookmarkIcon != null)
        {
            if (post.bookmarked)
            {
                bookmarkIcon.image = LoadImage("bookmark-filled");
            }
            else
            {
                bookmarkIcon.image = LoadImage("Bookmark");
            }
            
            bookmarkIcon.pickingMode = PickingMode.Position;
            bookmarkIcon.RegisterCallback<PointerUpEvent>(evt => 
            {
                StartCoroutine(dataHandler.BookmarkPost(post.postId, bookmarkIcon, post));
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

    public VisualElement CreateHorizontalCard(User designerPost)
    {
        VisualElement horizontalCard = horizontalCardTemplate.CloneTree();
        string displayName = designerPost.userName;
        
        horizontalCard.Q<Label>("userName").text = displayName;
        
        Image userImage = horizontalCard.Q<Image>("userImage");

        if (!string.IsNullOrEmpty(designerPost.avatar))
        {
            StartCoroutine(dataHandler.LoadImageFromURL(designerPost.avatar, userImage));
        }
        else
        {
            userImage.image = LoadImage("designer_placeholder");
        }
        
        Label followText = horizontalCard.Q<Label>("followText");
        followText.text = "Follow";
        Button followButton = horizontalCard.Q<Button>("followButton");

        followButton.clicked += () => ToggleFollow(followText, designerPost.userId, horizontalCard);
        
        return horizontalCard;
    }

    public ScrollView CreateTrendingDesignersSection(System.Collections.Generic.List<User> designers, int startIndex, int count)
    {
        Label sectionHeading = new Label("Trending Designers");
        sectionHeading.style.fontSize = 45;
        sectionHeading.style.color = new StyleColor(Color.black);
        sectionHeading.style.marginBottom = 20;
        sectionHeading.style.marginLeft = 40;
        sectionHeading.style.unityFontStyleAndWeight = FontStyle.Bold;
        
        container.Add(sectionHeading);
        
        ScrollView horizontalScroll = new ScrollView(ScrollViewMode.Horizontal);
        horizontalScroll.style.flexDirection = FlexDirection.Row;
        horizontalScroll.style.marginBottom = 100;
        horizontalScroll.style.paddingLeft = 30;
        
        int endIndex = Mathf.Min(startIndex + count, designers.Count);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            User designerPost = designers[i];
            VisualElement horizontalCard = CreateHorizontalCard(designerPost);
            horizontalScroll.Add(horizontalCard);
        }
        
        return horizontalScroll;
    }

    public void AddToContainer(VisualElement element)
    {
        container.Add(element);
    }

    public void ClearContent()
    {
        for (int i = container.childCount - 1; i > 0; i--)
        {
            container.RemoveAt(i);
        }
    }

    private void OnScroll(WheelEvent evt)
    {
        if (container.scrollOffset.y <= 0 && evt.delta.y < 0 && !isRefreshing)
        {
            HandlePullToRefresh(-evt.delta.y * 10f);
        }
    }
    
    private void OnPointerDown(PointerDownEvent evt)
    {
        if (container.scrollOffset.y <= 0 && !isRefreshing)
        {
            isPulling = true;
            pullDistance = 0f;
        }
    }
    
    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (isPulling && container.scrollOffset.y <= 0 && !isRefreshing)
        {
            pullDistance += evt.deltaPosition.y;
            HandlePullToRefresh(pullDistance);
        }
    }
    
    private void OnPointerUp(PointerUpEvent evt)
    {
        if (isPulling)
        {
            isPulling = false;
            
            if (pullDistance >= pullThreshold && !isRefreshing)
            {
                StartRefresh();
            }
            else
            {
                ResetPullIndicator();
            }
            
            pullDistance = 0f;
        }
    }
    
    private void HandlePullToRefresh(float distance)
    {
        if (isRefreshing) return;
        
        float normalizedDistance = Mathf.Clamp(distance, 0f, pullThreshold * 1.5f);
        float progress = normalizedDistance / pullThreshold;
        refreshContainer.style.height = normalizedDistance;
        refreshIndicator.style.opacity = Mathf.Clamp01(progress);
        float rotation = progress * 180f;
        refreshIcon.transform.rotation = Quaternion.Euler(0, 0, rotation);
    }
    
    private void StartRefresh()
    {
        if (isRefreshing) return;
        isRefreshing = true;
        refreshContainer.style.height = refreshIndicatorSize + 20f;
        refreshIndicator.style.opacity = 1f;
        StartCoroutine(RotateRefreshIcon());
        OnRefreshRequested?.Invoke();
    }
    
    private IEnumerator RotateRefreshIcon()
    {
        float rotationSpeed = 360f;
        
        while (isRefreshing)
        {
            float currentRotation = refreshIcon.transform.rotation.eulerAngles.z;
            float newRotation = currentRotation + rotationSpeed * Time.deltaTime;
            refreshIcon.transform.rotation = Quaternion.Euler(0, 0, newRotation);
            yield return null;
        }
    }
    
    public void CompleteRefresh()
    {
        StartCoroutine(CompleteRefreshCoroutine());
    }

    private IEnumerator CompleteRefreshCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        ResetPullIndicator();
        isRefreshing = false;
    }
    
    private void ResetPullIndicator()
    {
        StartCoroutine(AnimateRefreshIndicatorOut());
    }
    
    private IEnumerator AnimateRefreshIndicatorOut()
    {
        float animationDuration = 0.3f;
        float startHeight = refreshContainer.resolvedStyle.height;
        float startOpacity = refreshIndicator.resolvedStyle.opacity;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            
            refreshContainer.style.height = Mathf.Lerp(startHeight, 0f, easedProgress);
            refreshIndicator.style.opacity = Mathf.Lerp(startOpacity, 0f, easedProgress);
            
            yield return null;
        }
        
        refreshContainer.style.height = 0;
        refreshIndicator.style.opacity = 0;
        refreshIcon.transform.rotation = Quaternion.identity;
    }

    private Texture2D LoadImage(string imageName)
    {
        return Resources.Load<Texture2D>(imageName);
    }

    private void ToggleFollow(Label followText, string userId, VisualElement horizontalCard)
    {
        Button followButton = followText.parent as Button;

        if (followText.text == "Follow")
        {
            followText.text = "Following";
            followButton.style.backgroundColor = new StyleColor(new Color32(139, 76, 57, 255));
            followText.style.color = new StyleColor(Color.white);
        
            StartCoroutine(dataHandler.FollowUser(userId, followText, followButton, horizontalCard));
        }
        else
        {
            followText.text = "Follow";
            followButton.style.backgroundColor = StyleKeyword.Null;
            followText.style.color = StyleKeyword.Null;
        
            StartCoroutine(dataHandler.FollowUser(userId, followText, followButton, horizontalCard));
        }
    }

    public void ShowUploadProgress()
    {
        if (uploadProgressSection != null)
        {
            uploadProgressSection.style.display = DisplayStyle.Flex;
            uploadProgressSection.AddToClassList("show");
            uploadProgressController?.SendMessage("InitializeUI", SendMessageOptions.DontRequireReceiver);
            uploadProgressController?.StartUpload();
        }
    }

    public void SetUploadComplete(bool success)
    {
        if (uploadProgressController != null)
        {
            if (success)
            {
                uploadProgressController.SetProgress(100f);
                uploadProgressController.CompleteUpload();
            }
            else
            {
                uploadProgressController.TriggerError();
            }
            StartCoroutine(HideUploadProgressAfterDelay(5f));
        }
    }

    public void UpdateUploadProgress(float progress)
    {
        if (uploadProgressController != null)
        {
            uploadProgressController.SetProgress(progress);
        }
    }

    private IEnumerator HideUploadProgressAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (uploadProgressSection != null)
        {
            uploadProgressSection.style.display = DisplayStyle.None;
            uploadProgressSection.RemoveFromClassList("show");
        }
    }

    private void OnMessageButtonClick()
    {
        UIManager.Instance.TransitionScreens(UIScreenType.Home, UIScreenType.Messages);
    }

    private void OnDisable()
    {
        if (container != null)
        {
            container.UnregisterCallback<WheelEvent>(OnScroll);
            container.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            container.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            container.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }
    }
}