using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class HomeScreenCardHandler : MonoBehaviour
{
    public VisualTreeAsset verticalCardTemplate;
    public VisualTreeAsset horizontalCardTemplate;

    public int totalVerticalCards = 50;
    public int horizontalCardsPerGroup = 5;

    // Pull-to-refresh settings
    [Header("Pull to Refresh Settings")]
    public float pullThreshold = 100f;  // Distance needed to trigger refresh
    public float refreshIndicatorSize = 50f;  // Size of the refresh indicator
    public float refreshDuration = 2f;  // How long the refresh animation lasts
    
    private ScrollView container;
    private VisualElement refreshIndicator;
    private VisualElement refreshContainer;
    private Image refreshIcon;
    private bool isRefreshing = false;
    private bool isPulling = false;
    private float pullDistance = 0f;
    
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        container = root.Q<ScrollView>("main-container");
        
        SetupPullToRefresh();
        LoadInitialContent();
    }
    
    private void SetupPullToRefresh()
    {
        container.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        
        // Create refresh indicator container
        refreshContainer = new VisualElement();
        refreshContainer.style.height = 0;
        refreshContainer.style.flexDirection = FlexDirection.Row;
        refreshContainer.style.justifyContent = Justify.Center;
        refreshContainer.style.alignItems = Align.Center;
        refreshContainer.style.overflow = Overflow.Hidden;
        refreshContainer.style.marginTop = 0;
        
        // Create refresh indicator
        refreshIndicator = new VisualElement();
        refreshIndicator.style.width = refreshIndicatorSize;
        refreshIndicator.style.height = refreshIndicatorSize;
        refreshIndicator.style.justifyContent = Justify.Center;
        refreshIndicator.style.alignItems = Align.Center;
        refreshIndicator.style.opacity = 0;
        
        // Create refresh icon
        refreshIcon = new Image();
        refreshIcon.image = Resources.Load<Texture2D>("loader");
        refreshIcon.style.width = refreshIndicatorSize;
        refreshIcon.style.height = refreshIndicatorSize;
        
        refreshIndicator.Add(refreshIcon);
        refreshContainer.Add(refreshIndicator);
        
        // Insert refresh container at the beginning
        container.Insert(0, refreshContainer);
        
        // Register scroll event
        container.RegisterCallback<WheelEvent>(OnScroll);
        
        // Register pointer events for touch/mouse
        container.RegisterCallback<PointerDownEvent>(OnPointerDown);
        container.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        container.RegisterCallback<PointerUpEvent>(OnPointerUp);
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
        
        StartCoroutine(RefreshContent());
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
    
    private IEnumerator RefreshContent()
    {
        yield return new WaitForSeconds(refreshDuration);
        ClearContent();
        LoadInitialContent();
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
    
    private void ClearContent()
    {
        for (int i = container.childCount - 1; i > 0; i--)
        {
            container.RemoveAt(i);
        }
    }
    
    private void LoadInitialContent()
    {
        int verticalCount = 0;
        int horizontalGroupCount = 0;

        while (verticalCount < totalVerticalCards)
        {
            int groupSize = (verticalCount == 0) ? 5 : 10;

            // Add vertical cards
            for (int i = 0; i < groupSize && verticalCount < totalVerticalCards; i++, verticalCount++)
            {
                VisualElement verticalCard = verticalCardTemplate.CloneTree();
                // verticalCard.Q<TextElement>("userName").text = $"User {verticalCount + 1}";
                verticalCard.Q<Image>("userImage").image = LoadImage("user_placeholder");
                verticalCard.Q<Image>("card-image").image = LoadImage("room_placeholder");

                container.Add(verticalCard);
            }

            // Add heading for horizontal scroll section
            Label sectionHeading = new Label("Trending Designers");
            sectionHeading.style.fontSize = 45;
            sectionHeading.style.color = new StyleColor(Color.black);
            sectionHeading.style.marginBottom = 20;
            sectionHeading.style.marginLeft = 40;
            sectionHeading.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            container.Add(sectionHeading);

            // Add horizontal scroll section
            VisualElement horizontalScroll = new ScrollView(ScrollViewMode.Horizontal);
            horizontalScroll.style.flexDirection = FlexDirection.Row;
            horizontalScroll.style.marginBottom = 100;
            horizontalScroll.style.paddingLeft = 30;

            for (int j = 0; j < horizontalCardsPerGroup; j++)
            {
                VisualElement horizontalCard = horizontalCardTemplate.CloneTree();
                horizontalCard.Q<Label>("userName").text = $"Designer {horizontalGroupCount * horizontalCardsPerGroup + j + 1}";
                horizontalCard.Q<Image>("userImage").image = LoadImage("designer_placeholder");
                Label followText = horizontalCard.Q<Label>("followText");
                followText.text = "Follow";

                Button followButton = horizontalCard.Q<Button>("followButton");
                followButton.clicked += () => ToggleFollow(followText);

                horizontalScroll.Add(horizontalCard);
            }

            container.Add(horizontalScroll);
            horizontalGroupCount++;
        }
    }

    private Texture2D LoadImage(string imageName)
    {
        return Resources.Load<Texture2D>($"Images/{imageName}");
    }

    // Toggle logic for "Follow"/"Following" of designer card
    private void ToggleFollow(Label followText)
    {
        Button followButton = followText.parent as Button;

        if (followText.text == "Follow")
        {
            followText.text = "Following";
            followButton.style.backgroundColor = new StyleColor(new Color32(139, 76, 57, 255));
            followText.style.color = new StyleColor(Color.white);
        }
        else
        {
            followText.text = "Follow";
            followButton.style.backgroundColor = StyleKeyword.Null;
            followText.style.color = StyleKeyword.Null;
        }
    }
    
    private void OnDisable()
    {
        // Unregister events to prevent memory leaks
        if (container != null)
        {
            container.UnregisterCallback<WheelEvent>(OnScroll);
            container.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            container.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            container.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }
    }
}