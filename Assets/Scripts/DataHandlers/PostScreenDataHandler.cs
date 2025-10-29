using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Collections;
using System.Text;

public class PostScreenDataHandler : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument uiDocument;

    public static PostScreenDataHandler Instance { get; private set; }

    [Header("Screen Management")]
    public GameObject homeScreenGameObject;

    [Header("API Configuration")]
    [SerializeField] private string baseApiUrl;
    [SerializeField] private string authToken;

    public static PostScreenDataHandler GetInstance()
    {
        if (Instance == null)
        {
            Instance = FindObjectOfType<PostScreenDataHandler>(true);
            if (Instance == null)
            {
                return null;
            }
        }
        return Instance;
    }

    [Header("Post Data")]
    private string postId;
    private string caption;
    private string description;
    private string designStyle;
    private string roomType;
    private string status;
    private string createdAt;
    private string category;
    private int likesCount;
    private int commentsCount;
    private int bookmarksCount;
    private List<string> tags;
    private List<Media> media;
    private List<PostMedia> postMedia;
    private User author;
    private bool liked;
    private bool bookmarked;

    private VisualElement mainContainer;
    private Button backButton;
    private Image userImage;
    private TextElement userName;
    private TextElement descriptionText;
    private VisualElement imageSliderContainer;
    private VisualElement imageContainer;
    private TextElement imageCounter;

    private VisualElement dotsContainer;
    private Image favouriteIcon;
    private Image shareIcon;
    private Image bookmarkIcon;
    private TextField addCommentField;
    private Button uploadCommentButton;
    private VisualElement commentsSection;
    private ScrollView commentsScrollView;

    private VisualElement loadingIndicator;
    private VisualElement paginationContainer;

    private List<Image> imageElements = new List<Image>();
    private List<string> imageUrls = new List<string>();
    private int currentImageIndex = 0;
    private bool isSliding = false;

    private List<CommentData> comments = new List<CommentData>();
    private bool isSubmittingComment = false;
    private bool isFetchingComments = false;

    private int currentPage = 1;
    private int totalPages = 1;
    private int totalComments = 0;
    private const int commentsPerPage = 10;
    private bool hasMoreComments = false;

    private const float loadMoreThreshold = 100f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (homeScreenGameObject == null)
            {
                homeScreenGameObject = GameObject.Find("HomeScreen");
                if (homeScreenGameObject == null)
                {
                    HomeScreenController homeController = FindObjectOfType<HomeScreenController>();
                    if (homeController != null)
                    {
                        homeScreenGameObject = homeController.gameObject;
                    }
                }
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (gameObject.activeInHierarchy && homeScreenGameObject != null && homeScreenGameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        baseApiUrl = baseScript.baseURL;
        authToken = AuthTokenManager.GetToken();
        InitializeUI();
        SetupEventListeners();
        LoadPostData();
    }

    private void OnDisable()
    {
        RemoveEventListeners();
    }

    private void InitializeUI()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        var root = uiDocument.rootVisualElement;

        mainContainer = root.Q<VisualElement>("mainContainer");
        backButton = root.Q<Button>("backButton");
        userImage = root.Q<Image>("userImage");
        userName = root.Q<TextElement>("userName");
        descriptionText = root.Q<TextElement>("description");
        imageSliderContainer = root.Q<VisualElement>("imageSliderContainer");
        imageContainer = root.Q<VisualElement>("imageContainer");
        imageCounter = root.Q<TextElement>("imageCounter");
        dotsContainer = root.Q<VisualElement>("dotsContainer");
        favouriteIcon = root.Q<Image>("favourite");
        shareIcon = root.Q<Image>("share");
        bookmarkIcon = root.Q<Image>("bookmark");
        addCommentField = root.Q<TextField>("addCommentField");
        uploadCommentButton = root.Q<Button>("uploadComment");
        commentsSection = root.Q<VisualElement>("commentsSection");

        SetupCommentsScrollView();
        SetupPaginationUI();
    }

    private void SetupCommentsScrollView()
    {
        if (commentsSection != null)
        {
            commentsScrollView = new ScrollView();
            commentsScrollView.name = "commentsScrollView";
            commentsScrollView.AddToClassList("comments-scroll-view");
            commentsScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            commentsScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            var existingChildren = new List<VisualElement>();
            foreach (var child in commentsSection.Children())
            {
                existingChildren.Add(child);
            }

            commentsSection.Clear();

            foreach (var child in existingChildren)
            {
                commentsScrollView.Add(child);
            }

            commentsSection.Add(commentsScrollView);

            commentsScrollView.RegisterCallback<WheelEvent>(OnScrollViewWheel);
            commentsScrollView.RegisterCallback<PointerMoveEvent>(OnScrollViewPointerMove);
        }
    }

    private void SetupPaginationUI()
    {
        paginationContainer = new VisualElement();
        paginationContainer.name = "paginationContainer";
        paginationContainer.AddToClassList("pagination-container");
        paginationContainer.style.display = DisplayStyle.None;
        paginationContainer.style.width = Length.Percent(100);
        paginationContainer.style.minHeight = 60;

        loadingIndicator = new VisualElement();
        loadingIndicator.name = "loadingIndicator";
        loadingIndicator.AddToClassList("loading-indicator");
        loadingIndicator.style.display = DisplayStyle.None;
        loadingIndicator.style.flexDirection = FlexDirection.Row;
        loadingIndicator.style.alignItems = Align.Center;
        loadingIndicator.style.justifyContent = Justify.Center;
        loadingIndicator.style.paddingTop = 15;
        loadingIndicator.style.paddingBottom = 15;
        loadingIndicator.style.paddingLeft = 10;
        loadingIndicator.style.paddingRight = 10;
        loadingIndicator.style.width = Length.Percent(100);
        loadingIndicator.style.backgroundColor = new Color(0, 0, 0, 0.05f);

        var loadingText = new TextElement();
        loadingText.text = "Loading more comments...";
        loadingText.AddToClassList("loading-text");
        loadingText.style.fontSize = 14;
        loadingText.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        loadingText.style.unityTextAlign = TextAnchor.MiddleCenter;

        loadingIndicator.Add(loadingText);
        paginationContainer.Add(loadingIndicator);

        if (commentsScrollView != null)
        {
            commentsScrollView.Add(paginationContainer);
        }
        else if (commentsSection != null)
        {
            commentsSection.Add(paginationContainer);
        }
    }

    private void OnScrollViewWheel(WheelEvent evt)
    {
        CheckScrollPosition();
    }

    private void OnScrollViewPointerMove(PointerMoveEvent evt)
    {
        CheckScrollPosition();
    }

    private void CheckScrollPosition()
    {
        if (commentsScrollView == null || !hasMoreComments || isFetchingComments)
            return;

        float scrollHeight = commentsScrollView.contentContainer.layout.height;
        float viewportHeight = commentsScrollView.layout.height;
        float scrollOffset = commentsScrollView.scrollOffset.y;

        float distanceFromBottom = scrollHeight - viewportHeight - scrollOffset;

        if (distanceFromBottom <= loadMoreThreshold)
        {
            LoadMoreComments();
        }
    }

    private void LoadMoreComments()
    {
        if (hasMoreComments && !isFetchingComments)
        {
            StartCoroutine(FetchCommentsFromAPI(currentPage + 1, true));
        }
    }

    private void SetupEventListeners()
    {
        backButton?.RegisterCallback<ClickEvent>(OnBackButtonClicked);
        favouriteIcon?.RegisterCallback<ClickEvent>(OnLikeButtonClicked);
        shareIcon?.RegisterCallback<ClickEvent>(OnShareButtonClicked);
        bookmarkIcon?.RegisterCallback<ClickEvent>(OnBookmarkButtonClicked);
        uploadCommentButton?.RegisterCallback<ClickEvent>(OnUploadCommentClicked);
        addCommentField?.RegisterCallback<KeyDownEvent>(OnCommentFieldKeyDown);
        imageContainer?.RegisterCallback<PointerDownEvent>(OnImagePointerDown);
        imageContainer?.RegisterCallback<PointerMoveEvent>(OnImagePointerMove);
        imageContainer?.RegisterCallback<PointerUpEvent>(OnImagePointerUp);
    }

    private void RemoveEventListeners()
    {
        backButton?.UnregisterCallback<ClickEvent>(OnBackButtonClicked);
        favouriteIcon?.UnregisterCallback<ClickEvent>(OnLikeButtonClicked);
        shareIcon?.UnregisterCallback<ClickEvent>(OnShareButtonClicked);
        bookmarkIcon?.UnregisterCallback<ClickEvent>(OnBookmarkButtonClicked);
        uploadCommentButton?.UnregisterCallback<ClickEvent>(OnUploadCommentClicked);
        addCommentField?.UnregisterCallback<KeyDownEvent>(OnCommentFieldKeyDown);
        imageContainer?.UnregisterCallback<PointerDownEvent>(OnImagePointerDown);
        imageContainer?.UnregisterCallback<PointerMoveEvent>(OnImagePointerMove);
        imageContainer?.UnregisterCallback<PointerUpEvent>(OnImagePointerUp);
        commentsScrollView?.UnregisterCallback<WheelEvent>(OnScrollViewWheel);
        commentsScrollView?.UnregisterCallback<PointerMoveEvent>(OnScrollViewPointerMove);
    }

    private void LoadPostData()
    {
        if (string.IsNullOrEmpty(postId)) return;

        ResetPaginationState();

        if (author != null)
        {
            string displayName = !string.IsNullOrEmpty(author.firstName) && !string.IsNullOrEmpty(author.lastName)
                ? $"{author.firstName} {author.lastName}"
                : author.userName;
            userName.text = displayName;
        }

        string displayText = !string.IsNullOrEmpty(description) ? description : caption;
        descriptionText.text = displayText;

        if (author != null && !string.IsNullOrEmpty(author.avatar))
        {
            StartCoroutine(LoadImageFromUrl(author.avatar, userImage));
        }

        LoadPostImages();
        StartCoroutine(FetchCommentsFromAPI(1, false));
        UpdateLikeState();
        UpdateBookmarkState();
    }

    private void ResetPaginationState()
    {
        currentPage = 1;
        totalPages = 1;
        totalComments = 0;
        hasMoreComments = false;
        comments.Clear();

        if (loadingIndicator != null)
            loadingIndicator.style.display = DisplayStyle.None;
        if (paginationContainer != null)
            paginationContainer.style.display = DisplayStyle.None;
    }

    private IEnumerator FetchCommentsFromAPI(int page = 1, bool isLoadMore = false)
    {
        if (string.IsNullOrEmpty(postId) || isFetchingComments)
        {
            yield break;
        }

        isFetchingComments = true;

        if (isLoadMore && hasMoreComments)
        {
            ShowLoadingIndicator();
        }

        string url = $"{baseApiUrl}/api/v1/post/comments/{postId}?page={page}&limit={commentsPerPage}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", authToken);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (isLoadMore)
            {
                HideLoadingIndicator();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;

                    CommentsApiResponse response = JsonUtility.FromJson<CommentsApiResponse>(jsonResponse);

                    if (response.success)
                    {
                        if (response.pagination != null)
                        {
                            totalComments = response.pagination.total;
                            currentPage = response.pagination.page;
                            totalPages = response.pagination.totalPages;
                            hasMoreComments = currentPage < totalPages;
                        }

                        if (page == 1 && !isLoadMore)
                        {
                            comments.Clear();
                        }

                        if (response.comments != null && response.comments.Length > 0)
                        {

                            foreach (var apiComment in response.comments)
                            {
                                var commentData = new CommentData
                                {
                                    commentId = apiComment.commentId,
                                    authorName = apiComment.userName,
                                    text = apiComment.comment,
                                    timestamp = System.DateTime.Parse(apiComment.createdAt),
                                    isOwnComment = apiComment.isOwnComment
                                };
                                comments.Add(commentData);
                            }
                        }

                        if (isLoadMore)
                        {
                            LoadNewCommentsToUI(response.comments);
                        }
                        else
                        {
                            LoadCommentsToUI();
                        }
                    }
                    else
                    {
                        if (page == 1 && !isLoadMore)
                        {
                            comments.Clear();
                            LoadCommentsToUI();
                        }
                    }
                }
                catch (System.Exception)
                {
                    if (page == 1 && !isLoadMore)
                    {
                        comments.Clear();
                        LoadCommentsToUI();
                    }
                }
            }
            else
            {
                if (page == 1 && !isLoadMore)
                {
                    comments.Clear();
                    LoadCommentsToUI();
                }
            }
        }

        isFetchingComments = false;
    }

    private void ShowLoadingIndicator()
    {
        if (paginationContainer != null)
        {
            paginationContainer.style.display = DisplayStyle.Flex;
        }

        if (loadingIndicator != null)
        {
            loadingIndicator.style.display = DisplayStyle.Flex;
        }
    }

    private void HideLoadingIndicator()
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.style.display = DisplayStyle.None;
        }

        if (paginationContainer != null && !hasMoreComments)
        {
            paginationContainer.style.display = DisplayStyle.None;
        }
    }

    private void LoadNewCommentsToUI(ApiComment[] newComments)
    {
        if (newComments == null || newComments.Length == 0)
        {
            return;
        }

        foreach (var apiComment in newComments)
        {
            var commentData = new CommentData
            {
                commentId = apiComment.commentId,
                authorName = apiComment.userName,
                text = apiComment.comment,
                timestamp = System.DateTime.Parse(apiComment.createdAt),
                isOwnComment = apiComment.isOwnComment
            };

            AddCommentToUI(commentData);
        }
    }

    private void LoadPostImages()
    {
        imageUrls.Clear();
        imageElements.Clear();
        imageContainer.Clear();
        dotsContainer.Clear();

        CollectImageUrls();

        if (imageUrls.Count == 0)
        {
            if (imageCounter != null)
            {
                imageCounter.style.display = DisplayStyle.None;
            }
            return;
        }
        
        for (int i = 0; i < imageUrls.Count; i++)
        {
            var imageElement = new Image();
            imageElement.AddToClassList("slider-image");
        
            imageElement.style.width = Length.Percent(100);
            imageElement.style.height = Length.Percent(100);
            imageElement.style.position = Position.Absolute;
            imageElement.style.top = 0;
            imageElement.style.left = 0;
            imageElement.scaleMode = ScaleMode.StretchToFill;
        
            imageElement.style.display = i == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            imageElements.Add(imageElement);
            imageContainer.Add(imageElement);
        
            StartCoroutine(LoadImageFromUrl(imageUrls[i], imageElement));
        }
        
        if (imageCounter != null && imageCounter.parent == imageSliderContainer)
        {
            imageCounter.RemoveFromHierarchy();
            imageSliderContainer.Add(imageCounter);
        }
        
        if (dotsContainer != null && dotsContainer.parent == imageSliderContainer)
        {
            dotsContainer.RemoveFromHierarchy();
            imageSliderContainer.Add(dotsContainer);
        }

        CreateNavigationDots();
        UpdateSliderUI();
    }

    private void CollectImageUrls()
    {
        if (postMedia != null && postMedia.Count > 0)
        {
            foreach (var media in postMedia)
            {
                if (!string.IsNullOrEmpty(media.filePath))
                {
                    imageUrls.Add(media.filePath);
                }
            }
        }
        else if (media != null && media.Count > 0)
        {
            foreach (var mediaItem in media)
            {
                if (!string.IsNullOrEmpty(mediaItem.url))
                {
                    imageUrls.Add(mediaItem.url);
                }
            }
        }
    }

    private void CreateNavigationDots()
    {
        dotsContainer.Clear();

        for (int i = 0; i < imageUrls.Count; i++)
        {
            var dot = new VisualElement();
            dot.AddToClassList("nav-dot");
            if (i == currentImageIndex)
                dot.AddToClassList("active");

            int index = i;
            dot.RegisterCallback<ClickEvent>(evt => NavigateToImage(index));

            dotsContainer.Add(dot);
        }
    }

    private void UpdateSliderUI()
    {
        if (imageCounter != null)
        {
            imageCounter.text = $"{currentImageIndex + 1}/{imageUrls.Count}";
        }

        if (dotsContainer != null)
        {
            dotsContainer.style.display = imageUrls.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (imageCounter != null)
        {
            imageCounter.style.display = imageUrls.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void NavigateToImage(int index)
    {
        if (index < 0 || index >= imageUrls.Count || index == currentImageIndex || isSliding)
            return;

        StartCoroutine(SlideToImage(index));
    }

    private IEnumerator SlideToImage(int targetIndex)
    {
        isSliding = true;

        if (currentImageIndex < imageElements.Count)
        {
            imageElements[currentImageIndex].style.display = DisplayStyle.None;
        }

        if (targetIndex < imageElements.Count)
        {
            imageElements[targetIndex].style.display = DisplayStyle.Flex;
        }

        UpdateNavigationDots(targetIndex);

        currentImageIndex = targetIndex;
        UpdateSliderUI();

        yield return new WaitForSeconds(0.1f);

        isSliding = false;
    }

    private void UpdateNavigationDots(int activeIndex)
    {
        var dots = dotsContainer.Children();
        int index = 0;
        foreach (var dot in dots)
        {
            dot.RemoveFromClassList("active");
            if (index == activeIndex)
                dot.AddToClassList("active");
            index++;
        }
    }

    private Vector2 pointerDownPosition;
    private bool isDragging = false;
    private const float swipeThreshold = 50f;

    private void OnImagePointerDown(PointerDownEvent evt)
    {
        if (imageUrls.Count <= 1) return;

        pointerDownPosition = (Vector2)evt.position;
        isDragging = true;
        imageContainer.CapturePointer(evt.pointerId);
    }

    private void OnImagePointerMove(PointerMoveEvent evt)
    {
        if (!isDragging || imageUrls.Count <= 1) return;
    }

    private void OnImagePointerUp(PointerUpEvent evt)
    {
        if (!isDragging || imageUrls.Count <= 1) return;

        isDragging = false;
        imageContainer.ReleasePointer(evt.pointerId);

        Vector2 swipeVector = (Vector2)evt.position - pointerDownPosition;
        float swipeDistance = Mathf.Abs(swipeVector.x);

        if (swipeDistance > swipeThreshold)
        {
            if (swipeVector.x > 0)
            {
                if (currentImageIndex > 0)
                    NavigateToImage(currentImageIndex - 1);
            }
            else
            {
                if (currentImageIndex < imageUrls.Count - 1)
                    NavigateToImage(currentImageIndex + 1);
            }
        }
    }

    private System.Collections.IEnumerator LoadImageFromUrl(string url, Image targetImage)
    {
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                targetImage.image = texture;
                
                targetImage.scaleMode = ScaleMode.StretchToFill;
                targetImage.style.width = Length.Percent(100);
                targetImage.style.height = Length.Percent(100);
            }
        }
    }

    private void LoadCommentsToUI()
    {
        if (commentsScrollView != null)
        {
            commentsScrollView.Clear();
        }

        if (comments == null || comments.Count == 0)
        {
            ShowNoCommentsMessage();
            return;
        }

        foreach (var comment in comments)
        {
            AddCommentToUI(comment);
        }

        if (commentsScrollView != null)
        {
            commentsScrollView.schedule.Execute(() =>
            {
                commentsScrollView.scrollOffset = Vector2.zero;
            }).ExecuteLater(50);
        }
    }

    private void ShowNoCommentsMessage()
    {
        var noCommentsContainer = new VisualElement();
        noCommentsContainer.AddToClassList("no-comments-container");

        var noCommentsText = new TextElement();
        noCommentsText.AddToClassList("no-comments-text");
        noCommentsText.text = "No comments yet";

        var subtitleText = new TextElement();
        subtitleText.AddToClassList("no-comments-subtitle");
        subtitleText.text = "Be the first to comment!";

        noCommentsContainer.Add(noCommentsText);
        noCommentsContainer.Add(subtitleText);

        if (commentsScrollView != null)
        {
            commentsScrollView.Add(noCommentsContainer);
        }
        else
        {
            commentsSection.Add(noCommentsContainer);
        }
    }

    private void AddCommentToUI(CommentData comment)
    {
        var commentItem = new VisualElement();
        commentItem.AddToClassList("comment-item");

        var userDetails = new VisualElement();
        userDetails.AddToClassList("user-details");

        var commentUserImage = new Image();
        commentUserImage.AddToClassList("comment-user-image");
        if (comment.authorProfileImage != null)
            commentUserImage.image = comment.authorProfileImage;

        var commentUserName = new TextElement();
        commentUserName.AddToClassList("comment-user-name");
        commentUserName.text = comment.authorName;

        var commentText = new TextElement();
        commentText.AddToClassList("comment-text");
        commentText.text = comment.text;

        userDetails.Add(commentUserImage);
        userDetails.Add(commentUserName);
        commentItem.Add(userDetails);
        commentItem.Add(commentText);

        if (commentsScrollView != null)
        {
            commentsScrollView.Add(commentItem);
        }
        else
        {
            commentsSection.Add(commentItem);
        }
    }

    private IEnumerator LikePost()
    {
        if (string.IsNullOrEmpty(postId)) yield break;

        string url = $"{baseApiUrl}/api/v1/post/like/{postId}";

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
                        if (liked)
                        {
                            Texture2D outlineHeart = LoadImage("favourite");
                            if (outlineHeart != null)
                            {
                                favouriteIcon.image = outlineHeart;
                                likesCount--;
                                liked = false;
                            }
                        }
                        else
                        {
                            Texture2D filledHeart = LoadImage("heart-filled");
                            if (filledHeart != null)
                            {
                                favouriteIcon.image = filledHeart;
                                likesCount++;
                                liked = true;
                            }
                        }

                        Debug.Log($"Like toggled successfully. New state: {liked}");
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

    private IEnumerator BookmarkPost()
    {
        if (string.IsNullOrEmpty(postId)) yield break;

        string url = $"{baseApiUrl}/api/v1/post/bookmark/{postId}";

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
                        if (bookmarked)
                        {
                            Texture2D outlineBookmark = LoadImage("Bookmark");
                            if (outlineBookmark != null)
                            {
                                bookmarkIcon.image = outlineBookmark;
                                bookmarked = false;
                            }
                        }
                        else
                        {
                            Texture2D filledBookmark = LoadImage("bookmark-filled");
                            if (filledBookmark != null)
                            {
                                bookmarkIcon.image = filledBookmark;
                                bookmarked = true;
                            }
                        }

                        Debug.Log($"Bookmark toggled successfully. New state: {bookmarked}");
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

    private Texture2D LoadImage(string imageName)
    {
        return Resources.Load<Texture2D>(imageName);
    }

    private void OnBackButtonClicked(ClickEvent evt)
    {
        Debug.Log("[PostScreen] Back button clicked - returning to previous screen");
        HidePostScreen();
    }

    private void OnLikeButtonClicked(ClickEvent evt)
    {
        StartCoroutine(LikePost());
    }

    private void OnShareButtonClicked(ClickEvent evt)
    {
        SharePost();
    }

    private void OnBookmarkButtonClicked(ClickEvent evt)
    {
        StartCoroutine(BookmarkPost());
    }

    private void OnUploadCommentClicked(ClickEvent evt)
    {
        SubmitComment();
    }

    private void OnCommentFieldKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            SubmitComment();
        }
    }

    private void SubmitComment()
    {
        string commentText = addCommentField.value.Trim();
        if (string.IsNullOrEmpty(commentText)) return;
        if (isSubmittingComment) return;
        if (string.IsNullOrEmpty(postId))
        {
            return;
        }
        if (string.IsNullOrEmpty(authToken))
        {
            return;
        }
        StartCoroutine(SubmitCommentToServer(commentText));
    }

    private IEnumerator SubmitCommentToServer(string commentText)
    {
        isSubmittingComment = true;
        if (uploadCommentButton != null)
            uploadCommentButton.SetEnabled(false);

        string url = $"{baseApiUrl}/api/v1/post/comments/{postId}";
        CommentRequest commentRequest = new CommentRequest
        {
            comment = commentText
        };

        string jsonBody = JsonUtility.ToJson(commentRequest);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", authToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    CommentResponse response = JsonUtility.FromJson<CommentResponse>(request.downloadHandler.text);
                    addCommentField.value = "";
                    ResetPaginationState();
                    StartCoroutine(FetchCommentsFromAPI(1, false));
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing comment response: {e.Message}");
                    addCommentField.value = "";
                    ResetPaginationState();
                    StartCoroutine(FetchCommentsFromAPI(1, false));
                }
            }
            else
            {
                ShowCommentError("Failed to submit comment. Please try again.");
            }
        }

        if (uploadCommentButton != null)
            uploadCommentButton.SetEnabled(true);

        isSubmittingComment = false;
    }

    private void ShowCommentError(string errorMessage)
    {
        Debug.LogError(errorMessage);
    }

    private void UpdateLikeState()
    {
        if (favouriteIcon != null)
        {
            if (liked)
            {
                favouriteIcon.image = LoadImage("heart-filled");
            }
            else
            {
                favouriteIcon.image = LoadImage("favourite");
            }
        }
    }

    private void UpdateBookmarkState()
    {
        if (bookmarkIcon != null)
        {
            if (bookmarked)
            {
                bookmarkIcon.image = LoadImage("bookmark-filled");
            }
            else
            {
                bookmarkIcon.image = LoadImage("Bookmark");
            }
        }
    }

    private void SharePost()
    {
        if (string.IsNullOrEmpty(postId)) return;

        string authorName = author != null ?
            (!string.IsNullOrEmpty(author.firstName) ? $"{author.firstName} {author.lastName}" : author.userName)
            : "Unknown";

        string shareText = $"Check out this {roomType} design in {designStyle} style by {authorName}";
        if (!string.IsNullOrEmpty(caption))
            shareText += $": {caption}";

        GUIUtility.systemCopyBuffer = shareText;
    }

    public void SetAuthToken(string token)
    {
        authToken = token;
    }

    public void SetBaseApiUrl(string url)
    {
        baseApiUrl = url;
    }

    public void SetPostData(Post post)
    {
        if (post == null) return;

        this.postId = post.postId;
        this.caption = post.caption;
        this.description = post.description;
        this.designStyle = post.designStyle;
        this.roomType = post.roomType;
        this.status = post.status;
        this.createdAt = post.createdAt;
        this.category = post.category;
        this.likesCount = post.likesCount;
        this.commentsCount = post.commentsCount;
        this.bookmarksCount = post.bookmarksCount;
        this.tags = post.tags;
        this.media = post.media;
        this.postMedia = post.postMedia;
        this.author = post.author;
        this.liked = post.liked;
        this.bookmarked = post.bookmarked;

        currentImageIndex = 0;

        if (gameObject.activeInHierarchy)
            LoadPostData();
    }

    public void ShowPost(Post post)
    {
        SetPostData(post);
        ShowPostScreen();
    }

    public static void ShowPostStatic(Post post)
    {
        PostScreenDataHandler instance = GetInstance();
        if (instance != null)
        {
            instance.ShowPost(post);
        }
    }

    public void ShowPostScreen()
    {
        if (homeScreenGameObject != null)
        {
            homeScreenGameObject.SetActive(false);
        }
        gameObject.SetActive(true);
    }

    public void HidePostScreen()
    {
        Debug.Log("[PostScreen] Hiding post screen - returning to previous screen");
        gameObject.SetActive(false);

        if (homeScreenGameObject != null)
        {
            homeScreenGameObject.SetActive(true);
        }

        // Note: We don't invalidate cache here - the home/explore screens
        // will use cached data when they become active again
    }

    public void ShowPostWithScreenSwitch(Post post)
    {
        ShowPost(post);
    }

    public Post GetCurrentPostData()
    {
        return new Post
        {
            postId = this.postId,
            caption = this.caption,
            description = this.description,
            designStyle = this.designStyle,
            roomType = this.roomType,
            status = this.status,
            createdAt = this.createdAt,
            category = this.category,
            likesCount = this.likesCount,
            commentsCount = this.commentsCount,
            bookmarksCount = this.bookmarksCount,
            tags = this.tags,
            media = this.media,
            postMedia = this.postMedia,
            author = this.author,
            liked = this.liked,
            bookmarked = this.bookmarked
        };
    }

    public void HidePost()
    {
        gameObject.SetActive(false);
    }
}

[System.Serializable]
public class CommentData
{
    public string commentId;
    public string authorName;
    public string text;
    public Texture2D authorProfileImage;
    public System.DateTime timestamp;
    public bool isOwnComment;
}

[System.Serializable]
public class ApiComment
{
    public string commentId;
    public string comment;
    public string createdAt;
    public string userId;
    public string userName;
    public bool isOwnComment;
}

[System.Serializable]
public class CommentsPagination
{
    public int total;
    public int page;
    public int limit;
    public int totalPages;
}

[System.Serializable]
public class CommentsApiResponse
{
    public bool success;
    public string message;
    public ApiComment[] comments;
    public CommentsPagination pagination;
}

[System.Serializable]
public class CommentRequest
{
    public string comment;
}

[System.Serializable]
public class CommentResponse
{
    public bool success;
    public string message;
    public CommentData data;
}
